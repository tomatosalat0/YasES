using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using YasES.Core;
using YasES.Persistance.Sql;

namespace YasES.Persistance.Sqlite
{
    internal class ReadHandler
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly SqliteConfiguration _configuration;

        public ReadHandler(IConnectionFactory connectionFactory, SqliteConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
        }

        public IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate)
        {
            return new Reader(_connectionFactory, predicate, _configuration);
        }

        private class Reader : IEnumerable<IStoredEventMessage>
        {
            private readonly IConnectionFactory _factory;
            private readonly ReadPredicate _predicate;
            private readonly SqliteConfiguration _configuration;

            public Reader(IConnectionFactory factory, ReadPredicate predicate, SqliteConfiguration configuration)
            {
                _factory = factory;
                _predicate = predicate;
                _configuration = configuration;
            }

            public IEnumerator<IStoredEventMessage> GetEnumerator()
            {
                return new ReadEnumerator(_factory.Open(), _predicate, _configuration);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ReadEnumerator : IEnumerator<IStoredEventMessage>
        {
            private const string PAGINATION_OFFSET_PARAMETER_NAME = "@PaginationOffset";

            private readonly IDbConnection _connection;
            private readonly IDbDataParameter _paginationOffset;
            private readonly ReadPredicate _predicate;
            private readonly IDbCommand _command;
            private long _currentCheckpointOffset;
            private long _lastKnownCheckpointOffset;
            private IPageReader? _pageReader;
            private bool _disposedValue;

            public ReadEnumerator(IDbConnection connection, ReadPredicate predicate, SqliteConfiguration configuration)
            {
                _connection = connection;
                _predicate = predicate;

                _command = _connection.CreateCommand();
                _command.CommandText = BuildQueryStatement(predicate, configuration);
                _paginationOffset = DefineParameter(_command, PAGINATION_OFFSET_PARAMETER_NAME, DbType.Int64);
                Reset();
            }

            private static string BuildQueryStatement(ReadPredicate predicate, SqliteConfiguration configuration)
            {
                const string Columns = "Checkpoint, BucketId, StreamId, CommitId, CommitCreationDate, EventCreationDate, EventName, Headers, Payload";
                string tableName = configuration.TableName;
                string orderDirection = predicate.Reverse ? "DESC" : "ASC";

                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"SELECT {Columns} FROM '{tableName}'");
                builder.AppendLine($"WHERE");

                List<string> conditions = new List<string>();
                FillBoundsFilter(conditions, predicate);
                FillStreamFilter(conditions, predicate);
                FillEventNameFilter(conditions, predicate);
                FillCorrelationIdFilter(conditions, predicate);
                FillPaginationOffsetFilter(conditions, predicate);

                builder.AppendLine("    " + string.Join("\n    AND ", conditions));
                builder.AppendLine($"ORDER BY Checkpoint {orderDirection}");
                builder.AppendLine($"LIMIT 0,{configuration.PaginationSize};");
                return builder.ToString();
            }

            private static void FillBoundsFilter(List<string> target, ReadPredicate predicate)
            {
                if (predicate.LowerExclusiveBound > CheckpointToken.Beginning)
                    target.Add($"Checkpoint > {predicate.LowerExclusiveBound.Value}");
                if (predicate.UpperExclusiveBound < CheckpointToken.Ending)
                    target.Add($"Checkpoint < {predicate.UpperExclusiveBound.Value}");
            }

            private static string BuildSQLiteStringValue(string input)
            {
                if (input.IndexOf('\'', StringComparison.Ordinal) < 0)
                    return string.Concat("'", input, "'");

                return string.Concat("'", input.Replace("'", "''", StringComparison.Ordinal), "'");
            }

            private static void FillEventNameFilter(List<string> target, ReadPredicate predicate)
            {
                if (predicate.EventNamesFilter == null)
                    return;

                string inStatement = string.Join(',', predicate.EventNamesFilter.Select(BuildSQLiteStringValue));
                if (predicate.EventNamesIncluding)
                    target.Add($"EventName IN ({inStatement})");
                else
                    target.Add($"EventName NOT IN ({inStatement})");
            }

            private static void FillCorrelationIdFilter(List<string> target, ReadPredicate predicate)
            {
                if (predicate.CorrelationId == null)
                    return;

                target.Add($"CorrelationId = {BuildSQLiteStringValue(predicate.CorrelationId)}");
            }

            private static void FillPaginationOffsetFilter(List<string> target, ReadPredicate predicate)
            {
                if (predicate.Reverse)
                    target.Add($"Checkpoint < {PAGINATION_OFFSET_PARAMETER_NAME}");
                else
                    target.Add($"Checkpoint > {PAGINATION_OFFSET_PARAMETER_NAME}");
            }

            private static void FillStreamFilter(List<string> target, ReadPredicate predicate)
            {
                List<string> orConditions = new List<string>();
                foreach (var group in predicate.Streams.GroupBy(p => p.BucketId))
                {
                    string bucketCondition = $"BucketId = {BuildSQLiteStringValue(group.Key)}";
                    List<StreamIdentifier> all = group.ToList();
                    if (all.Any(p => p.MatchesAllStreams))
                    {
                        orConditions.Add(bucketCondition);
                    }
                    else
                    {
                        List<StreamIdentifier> exactMatch = all.Where(p => p.IsSingleStream).ToList();
                        List<StreamIdentifier> prefixMatch = all.Where(p => !p.IsSingleStream && !p.MatchesAllStreams).ToList();
                        string exactCondition = $"StreamId IN ({string.Join(',', exactMatch.Select(p => p.StreamId).Distinct().Select(BuildSQLiteStringValue))})";
                        string prefixCondition = BuildStreamPrefixConditions(prefixMatch);
                        if (prefixMatch.Count == 0)
                            orConditions.Add($"({bucketCondition} AND {exactCondition})");
                        else
                        if (exactMatch.Count == 0)
                            orConditions.Add($"({bucketCondition} AND {prefixCondition})");
                        else
                            orConditions.Add($"({bucketCondition} AND ({exactCondition} OR {prefixCondition}))");
                    }
                }
                target.Add($"({string.Join(" OR ", orConditions.Select(p => $"({p})"))})");
            }

            private static string BuildStreamPrefixConditions(IEnumerable<StreamIdentifier> prefixStreams)
            {
                List<string> conditions = new List<string>();
                foreach (var stream in prefixStreams)
                {
                    string condition = $"(StreamId BETWEEN {BuildSQLiteStringValue(stream.StreamIdPrefix)} AND {BuildSQLiteStringValue(stream.StreamIdPrefix + '\uFFFF')})";
                    conditions.Add(condition);
                }
                return $"({string.Join(" OR ", conditions)})";
            }

            private static IDbDataParameter DefineParameter(IDbCommand command, string name, DbType type)
            {
                IDbDataParameter parameter = command.CreateParameter();
                parameter.DbType = type;
                parameter.ParameterName = name;
                command.Parameters.Add(parameter);
                return parameter;
            }

            public IStoredEventMessage Current { get; private set; } = default!;

            object IEnumerator.Current => Current;

            private IPageReader CreatePageReader()
            {
                IPageReader result = new SqlitePageReader(_command);
                result.Start();
                return result;
            }

            public bool MoveNext()
            {
                IStoredEventMessage message;
                if (_pageReader == null)
                {
                    _pageReader = CreatePageReader();
                    if (!_pageReader.TryReadNext(out message))
                        return false;

                    Current = message;
                    _lastKnownCheckpointOffset = message.Checkpoint.Value;
                    return true;
                }
                else
                {
                    if (_pageReader.TryReadNext(out message))
                    {
                        Current = message;
                        _lastKnownCheckpointOffset = message.Checkpoint.Value;
                        return true;
                    }
                    else
                    {
                        _pageReader.Dispose();
                        MoveToNextPage();
                        _pageReader = CreatePageReader();
                        if (!_pageReader.TryReadNext(out message))
                            return false;

                        Current = message;
                        _lastKnownCheckpointOffset = message.Checkpoint.Value;
                        return true;
                    }
                }
            }

            private void MoveToNextPage()
            {
                _currentCheckpointOffset = _lastKnownCheckpointOffset;
                _paginationOffset.Value = _lastKnownCheckpointOffset;
            }

            public void Reset()
            {
                _pageReader?.Dispose();
                _pageReader = null;
                _currentCheckpointOffset = _predicate.Reverse ? CheckpointToken.Ending.Value : CheckpointToken.Beginning.Value;
                _paginationOffset.Value = _currentCheckpointOffset;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        Reset();
                        _command.Dispose();
                        _connection.Dispose();
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
