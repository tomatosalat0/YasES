using System.Collections.Generic;
using System.Data;
using YasES.Core;
using YasES.Persistance.Sql;

namespace YasES.Persistance.Sqlite
{
    public class SqlitePersistanceEngine : IEventReadWrite, IEventRead, IEventWrite, IStorageInitialization
    {
        private readonly IConnectionFactory _connectionFactory;

        public SqlitePersistanceEngine(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Commit(CommitAttempt attempt)
        {
            using (IDbConnection connection = _connectionFactory.Open())
            using (IDbTransaction transaction = connection.BeginTransaction())
            {
                CommitHandler handler = new CommitHandler(connection, transaction);
                handler.Commit(attempt);
            }
        }

        public IEnumerable<IReadEventMessage> Read(ReadPredicate predicate)
        {
            ReadHandler handler = new ReadHandler(_connectionFactory);
            return handler.Read(predicate);
        }

        public void Initialize()
        {
            using (IDbConnection connection = _connectionFactory.Open())
            {
                DatabaseSchema schema = new DatabaseSchema(connection);
                schema.Initialize();
            }
        }
    }
}
