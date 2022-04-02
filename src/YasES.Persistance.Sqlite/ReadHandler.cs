using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using YasES.Core;
using YasES.Core.Persistance;
using YasES.Persistance.Sql;

namespace YasES.Persistance.Sqlite
{
    internal class ReadHandler
    {
        private readonly IConnectionFactory _connectionFactory;

        public ReadHandler(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<IReadEventMessage> Read(ReadPredicate predicate)
        {
            return new Reader(_connectionFactory, predicate);
        }

        private class Reader : IEnumerable<IReadEventMessage>
        {
            private readonly IConnectionFactory _factory;
            private readonly ReadPredicate _predicate;

            public Reader(IConnectionFactory factory, ReadPredicate predicate)
            {
                _factory = factory;
                _predicate = predicate;
            }

            public IEnumerator<IReadEventMessage> GetEnumerator()
            {
                return new ReadEnumerator(_factory.Open(), _predicate);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ReadEnumerator : IEnumerator<IReadEventMessage>
        {
            private const string PAGINATION_OFFSET_PARAMETER_NAME = "@PaginationOffset";
            private const int PAGE_SIZE = 500;

            private readonly IDbConnection _connection;
            private readonly IDbDataParameter _paginationOffset;
            private readonly ReadPredicate _predicate;
            private readonly IDbCommand _command;
            private long _currentCheckpointOffset;
            private long _lastKnownCheckpointOffset;
            private PageReader? _pageReader;
            private bool _disposedValue;

            public ReadEnumerator(IDbConnection connection, ReadPredicate predicate)
            {
                _connection = connection;
                _predicate = predicate;

                _command = _connection.CreateCommand();
                _command.CommandText = BuildQueryStatement(predicate);
                _paginationOffset = DefineParameter(_command, PAGINATION_OFFSET_PARAMETER_NAME, DbType.Int64);
                Reset();
            }

            private static string BuildQueryStatement(ReadPredicate predicate)
            {
                const string TableName = "Events";
                const string Columns = "Checkpoint, BucketId, StreamId, CommitId, CommitCreationDate, EventCreationDate, EventName, Headers, Payload";
                string orderDirection = predicate.Reverse ? "DESC" : "ASC";

                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"SELECT {Columns} FROM {TableName}");
                builder.AppendLine($"WHERE");

                List<string> conditions = new List<string>();
                FillBoundsFilter(conditions, predicate);
                FillStreamFilter(conditions, predicate);
                FillEventNameFilter(conditions, predicate);
                FillCorrelationIdFilter(conditions, predicate);
                FillPaginationOffsetFilter(conditions, predicate);

                builder.AppendLine("    " + string.Join("\n    AND ", conditions));
                builder.AppendLine($"ORDER BY Checkpoint {orderDirection}");
                builder.AppendLine($"LIMIT 0,{PAGE_SIZE};");
                return builder.ToString();
            }

            private static void FillBoundsFilter(List<string> target, ReadPredicate predicate)
            {
                if (predicate.LowerBound > CheckpointToken.Beginning)
                    target.Add($"Checkpoint > {predicate.LowerBound.Value}");
                if (predicate.UpperBound < CheckpointToken.Ending)
                    target.Add($"Checkpoint < {predicate.UpperBound.Value}");
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
                        string streamCondition = $"StreamId IN ({string.Join(',', all.Select(p => p.StreamId).Distinct().Select(BuildSQLiteStringValue))})";
                        orConditions.Add($"({bucketCondition} AND {streamCondition})");
                    }
                }
                target.Add($"({ string.Join(" OR ", orConditions.Select(p => $"({p})")) })");
            }

            private static IDbDataParameter DefineParameter(IDbCommand command, string name, DbType type)
            {
                IDbDataParameter parameter = command.CreateParameter();
                parameter.DbType = type;
                parameter.ParameterName = name;
                command.Parameters.Add(parameter);
                return parameter;
            }

            public IReadEventMessage Current { get; private set; } = default!;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                IReadEventMessage message;
                if (_pageReader == null)
                {
                    _pageReader = new PageReader(_command);
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
                        _pageReader = new PageReader(_command);
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

        private class PageReader : IDisposable
        {
            private readonly IDataReader _reader;
            private bool _disposedValue;

            public PageReader(IDbCommand command)
            {
                _reader = command.ExecuteReader();
            }

            public bool TryReadNext(out IReadEventMessage message)
            {
                if (_disposedValue) throw new ObjectDisposedException(nameof(PageReader));

                message = default!;
                if (!_reader.Read())
                    return false;

                message = DeserializeMessage();
                return true;
            }

            private IReadEventMessage DeserializeMessage()
            {
                return new SqliteReadEventMessage(_reader);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _reader.Dispose();
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

        private class SqliteReadEventMessage : IReadEventMessage
        {
            public SqliteReadEventMessage(IDataReader reader)
            {
                const int OffsetCheckpoint = 0;
                const int OffsetBucketId = 1;
                const int OffsetStreamId = 2;
                const int OffsetCommitId = 3;
                const int OffsetCommitCreationDate = 4;
                const int OffsetEventCreationDate = 5;
                const int OffsetEventName = 6;
                const int OffsetHeaders = 7;
                const int OffsetPayload = 8;

                Checkpoint = new CheckpointToken(reader.GetInt64(OffsetCheckpoint));
                StreamIdentifier = StreamIdentifier.SingleStream(reader.GetString(OffsetBucketId), reader.GetString(OffsetStreamId));
                CommitId = new CommitId(reader.GetGuid(OffsetCommitId));
                EventName = reader.GetString(OffsetEventName); 
                CommitTimeUtc = DateTime.SpecifyKind(Convert.ToDateTime(reader.GetValue(OffsetCommitCreationDate)), DateTimeKind.Utc);
                CreationDateUtc = DateTime.SpecifyKind(Convert.ToDateTime(reader.GetValue(OffsetEventCreationDate)), DateTimeKind.Utc);
                Headers = HeaderSerialization.DeserializeHeaderJsonOrDefault(reader.GetValue(OffsetHeaders) as byte[] ?? null);
                Payload = reader.GetValue(OffsetPayload) as byte[] ?? Memory<byte>.Empty;
            }

            public CheckpointToken Checkpoint { get; }

            public StreamIdentifier StreamIdentifier { get; }

            public CommitId CommitId { get; }

            public DateTime CommitTimeUtc { get; }

            public string EventName { get; }

            public IReadOnlyDictionary<string, object> Headers { get; }

            public ReadOnlyMemory<byte> Payload { get; }

            public DateTime CreationDateUtc { get; }
        }
    }
}
