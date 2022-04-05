using System.Data;

namespace YasES.Persistance.Sqlite
{
    internal class DatabaseSchema
	{
		private const string InitializeStatement =
@"
CREATE TABLE IF NOT EXISTS '{{TableName}}' (
	'Checkpoint'	INTEGER,
	'BucketId'	varchar(255) NOT NULL,
	'StreamId'	varchar(255) NOT NULL,
	'CommitId' Guid NOT NULL,
	'CommitMessageIndex' INTEGER NOT NULL,
	'CommitCreationDate' datetime NOT NULL,
	'EventCreationDate'	datetime NOT NULL,
	'EventName'	varchar(255) NOT NULL,
	'MessageId' varchar(255),
	'CorrelationId' varchar(255),
	'CausationId' varchar(255),
	'Headers'	BLOB NOT NULL,
	'Payload'	BLOB,
	PRIMARY KEY('Checkpoint' AUTOINCREMENT)
);

CREATE UNIQUE INDEX IF NOT EXISTS 'IX_BucketId_StreamId_Checkpoint' ON '{{TableName}}' (
	'BucketId',
	'StreamId',
	'Checkpoint'	ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS 'IX_BucketId_StreamId_CommitId_CommitMessageIndex' ON '{{TableName}}' (
	'BucketId',
	'StreamId',
	'CommitId',
	'CommitMessageIndex'
);

CREATE INDEX IF NOT EXISTS 'IX_CorrelationId' ON '{{TableName}}' (
	'CorrelationId'
);

CREATE INDEX IF NOT EXISTS 'IX_CausationId' ON '{{TableName}}' (
	'CausationId'
);

CREATE TRIGGER IF NOT EXISTS 'TR_Block_Update' 
BEFORE UPDATE ON '{{TableName}}'
BEGIN
    SELECT RAISE(FAIL, 'update not allowed');
END;

CREATE TRIGGER IF NOT EXISTS 'TR_Block_Delete' 
BEFORE DELETE ON '{{TableName}}'
BEGIN
    SELECT RAISE(FAIL, 'delete not allowed');
END;
";

		private readonly IDbConnection _connection;
		private readonly SqliteConfiguration _configuration;

		public DatabaseSchema(IDbConnection connection, SqliteConfiguration configuration)
        {
            _connection = connection;
            _configuration = configuration;
        }

		public void Initialize()
        {
			using (IDbCommand command = _connection.CreateCommand())
            {
				command.CommandText = InitializeStatement.Replace("{{TableName}}", _configuration.TableName);
				command.ExecuteNonQuery();
            }
        }
	}
}
