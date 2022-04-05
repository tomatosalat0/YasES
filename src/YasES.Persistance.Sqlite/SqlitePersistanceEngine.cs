using System;
using System.Collections.Generic;
using System.Data;
using YasES.Core;
using YasES.Persistance.Sql;

namespace YasES.Persistance.Sqlite
{
    public class SqlitePersistanceEngine : IEventReadWrite, IEventRead, IEventWrite, IStorageInitialization
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly SqliteConfiguration _configuration;

        public SqlitePersistanceEngine(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _configuration = new SqliteConfiguration();
        }

        public SqlitePersistanceEngine(IConnectionFactory connectionFactory, Action<SqliteConfiguration> configure)
            : this(connectionFactory)
        {
            configure(_configuration);
        }

        public void Commit(CommitAttempt attempt)
        {
            using (IDbConnection connection = _connectionFactory.Open())
            using (IDbTransaction transaction = connection.BeginTransaction())
            {
                CommitHandler handler = new CommitHandler(connection, transaction, _configuration);
                handler.Commit(attempt);
            }
        }

        public IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate)
        {
            ReadHandler handler = new ReadHandler(_connectionFactory, _configuration);
            return handler.Read(predicate);
        }

        public void Initialize()
        {
            using (IDbConnection connection = _connectionFactory.Open())
            {
                DatabaseSchema schema = new DatabaseSchema(connection, _configuration);
                schema.Initialize();
            }
        }
    }
}
