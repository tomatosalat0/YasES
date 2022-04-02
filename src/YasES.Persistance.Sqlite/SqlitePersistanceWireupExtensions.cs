using YasES.Persistance.Sql;
using YasES.Persistance.Sqlite;

namespace YasES.Core
{
    public static class SqlitePersistanceWireupExtensions
    {
        public static Wireup UseSqlitePersistance(this Wireup wireup, string connectionString)
        {
            return wireup.ConfigureContainer((container) =>
            {
                container.Register<IConnectionFactory>((_) => new SqliteConnectionFactory(connectionString).UseConnectionPool());
                container.Register<IEventReadWrite, IConnectionFactory>((_, connection) => {
                    SqlitePersistanceEngine result = new SqlitePersistanceEngine(connection);
                    result.Initialize();
                    return result;
                });
            });
        }
    }
}
