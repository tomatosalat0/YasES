using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using YasES.Core;
using YasES.Core.Persistance;

namespace YasES.Persistance.Sqlite
{
    internal class SqlitePageReader : IPageReader
    {
        private readonly IDbCommand _command;
        private IDataReader _reader = null!;
        private bool _disposedValue;

        public SqlitePageReader(IDbCommand command)
        {
            _command = command;
        }

        public void Start()
        {
            _reader = _command.ExecuteReader();
        }

        public bool TryReadNext(out IStoredEventMessage message)
        {
            if (_disposedValue) throw new ObjectDisposedException(nameof(SqlitePageReader));

            message = default!;
            if (!_reader.Read())
                return false;

            message = DeserializeMessage();
            return true;
        }

        private IStoredEventMessage DeserializeMessage()
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

        [DebuggerDisplay("{EventName} ({CreationDateUtc})")]
        private class SqliteReadEventMessage : IStoredEventMessage
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
