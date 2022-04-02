using System.Data;

namespace YasES.Persistance.Sqlite
{
    internal class DatabaseSchema
    {
        private readonly IDbConnection _connection;

        public DatabaseSchema(IDbConnection connection)
        {
            _connection = connection;
        }

		public void Initialize()
        {
			using (IDbCommand command = _connection.CreateCommand())
            {
				command.CommandText = InitializeStatement;
				command.ExecuteNonQuery();
            }
        }

		private const string InitializeStatement =
@"
CREATE TABLE IF NOT EXISTS 'Events' (
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

CREATE UNIQUE INDEX IF NOT EXISTS 'IX_BucketId_StreamId_Checkpoint' ON 'Events' (
	'BucketId',
	'StreamId',
	'Checkpoint'	ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS 'IX_BucketId_StreamId_CommitId_CommitMessageIndex' ON 'Events' (
	'BucketId',
	'StreamId',
	'CommitId',
	'CommitMessageIndex'
);

CREATE INDEX IF NOT EXISTS 'IX_CorrelationId' ON 'Events' (
	'CorrelationId'
);

CREATE INDEX IF NOT EXISTS 'IX_CausationId' ON 'Events' (
	'CausationId'
);

CREATE TRIGGER IF NOT EXISTS 'TR_Block_Update' 
BEFORE UPDATE ON 'Events'
BEGIN
    SELECT RAISE(FAIL, 'update not allowed');
END;

CREATE TRIGGER IF NOT EXISTS 'TR_Block_Delete' 
BEFORE DELETE ON 'Events'
BEGIN
    SELECT RAISE(FAIL, 'delete not allowed');
END;
";

	}
}
