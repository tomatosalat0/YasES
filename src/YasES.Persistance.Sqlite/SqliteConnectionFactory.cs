using System;
using System.Data;
using YasES.Persistance.Sql;

namespace YasES.Persistance.Sqlite
{
    public class SqliteConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace.", nameof(connectionString));
            _connectionString = connectionString;
        }

        private IDbConnection GetConnection()
        {
            IDbConnection result = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
            result.Open();
            SetupConnection(result);
            return result;
        }

        private void SetupConnection(IDbConnection connection)
        {
            RunCommand(connection, "PRAGMA journal_mode = 'WAL';");
            RunCommand(connection, "PRAGMA synchronous = 'normal';");
            RunCommand(connection, "PRAGMA temp_store = memory;");
            RunCommand(connection, "PRAGMA mmap_size = 30000000000;");
        }

        private void RunCommand(IDbConnection connection, string sql)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        public IDbConnection Open()
        {
            return GetConnection();
        }

        internal void ConnectionClose(IDbConnection connection)
        {
            RunCommand(connection, "pragma optimize;");
        }

        public DbConnectionPool UseConnectionPool()
        {
            return new DbConnectionPool(this, this.ConnectionClose);
        }
    }
}
