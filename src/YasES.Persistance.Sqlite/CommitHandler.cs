using System;
using System.Collections.Generic;
using System.Data;
using YasES.Core;
using YasES.Core.Persistance;

namespace YasES.Persistance.Sqlite
{
    internal class CommitHandler
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        public CommitHandler(IDbConnection connection, IDbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public void Commit(CommitAttempt commit)
        {
            try
            {
                InnerCommit(commit);
                _transaction.Commit();
            }
            catch (Exception ex)
            {
                TryRollback();
                if (!IgnoreException(ex))
                    throw;
            }
        }

        private bool IgnoreException(Exception ex)
        {
            return IsUniqueConstraintException(ex);
        }

        private bool IsUniqueConstraintException(Exception ex)
        {
            if (!(ex is Microsoft.Data.Sqlite.SqliteException sqliteException))
                return false;

            return sqliteException.SqliteErrorCode == 19;
        }

        private void TryRollback()
        {
            try
            {
                _transaction.Rollback();
            }
            catch (Exception)
            {
            }
        }

        private void InnerCommit(CommitAttempt commit)
        {
            using (IDbCommand command = _connection.CreateCommand())
            {
                PrepareCommand(command);
                InsertEvents(command, commit);
            }
        }

        private void PrepareCommand(IDbCommand command)
        {
            command.Transaction = _transaction;
            command.CommandText = InsertStatement;
        }

        private void InsertEvents(IDbCommand command, CommitAttempt commit)
        {
            IDbDataParameter bucketId = DefineParameter(command, "@BucketId", DbType.String );
            IDbDataParameter streamId = DefineParameter(command, "@StreamId", DbType.String );
            IDbDataParameter commitId = DefineParameter(command, "@CommitId", DbType.StringFixedLength );
            IDbDataParameter CommitMessageIndex = DefineParameter(command, "@CommitMessageIndex", DbType.Int64);
            IDbDataParameter commitCreationDate = DefineParameter(command, "@CommitCreationDate", DbType.DateTime );
            IDbDataParameter eventCreationDate = DefineParameter(command, "@EventCreationDate", DbType.DateTime );
            IDbDataParameter eventName = DefineParameter(command, "@EventName", DbType.String );
            IDbDataParameter messageId = DefineParameter(command, "@MessageId", DbType.String);
            IDbDataParameter correlationId = DefineParameter(command, "@CorrelationId", DbType.String);
            IDbDataParameter causationId = DefineParameter(command, "@CausationId", DbType.String);
            IDbDataParameter headers = DefineParameter(command, "@Headers", DbType.Binary );
            IDbDataParameter payload = DefineParameter(command, "@Payload", DbType.Binary );

            bucketId.Value = commit.StreamIdentifier.BucketId;
            streamId.Value = commit.StreamIdentifier.StreamId;
            commitId.Value = commit.CommitId.ToString();
            commitCreationDate.Value = commit.CommitAttemptDateUtc;

            int index = 0;
            foreach (var message in commit.Messages)
            {
                messageId.Value = message.Headers.GetValueOrDefault(CommonMetaData.EventId) ?? DBNull.Value;
                correlationId.Value = message.Headers.GetValueOrDefault(CommonMetaData.CorrelationId) ?? DBNull.Value;
                causationId.Value = message.Headers.GetValueOrDefault(CommonMetaData.CausationId) ?? DBNull.Value;

                eventCreationDate.Value = message.CreationDateUtc;
                eventName.Value = message.EventName;
                headers.Value = HeaderSerialization.HeaderToJson(message);
                payload.Value = message.Payload.ToArray();
                CommitMessageIndex.Value = index++;

                command.ExecuteNonQuery();
            }
        }

        private IDbDataParameter DefineParameter(IDbCommand command, string name, DbType type)
        {
            IDbDataParameter parameter = command.CreateParameter();
            parameter.DbType = type;
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);
            return parameter;
        }

        private const string InsertStatement =
@"INSERT INTO Events (
    BucketId,
    StreamId,
    CommitId,
    CommitMessageIndex,
    CommitCreationDate,
    EventCreationDate,
    EventName,
    MessageId,
	CorrelationId,
	CausationId,
    Headers,
    Payload)
VALUES (
    @BucketId,
    @StreamId,
    @CommitId,
    @CommitMessageIndex,
    @CommitCreationDate,
    @EventCreationDate,
    @EventName,
    @MessageId,
	@CorrelationId,
	@CausationId,
    @Headers,
    @Payload
);";
    }
}
