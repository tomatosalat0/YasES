﻿using System;
using YasES.Persistance.Sql;
using YasES.Persistance.Sqlite;

namespace YasES.Core
{
    public static class EventStoreBuilderSqlitePersistanceExtensions
    {
        public static EventStoreBuilder UseSqlitePersistance(this EventStoreBuilder wireup, string connectionString)
        {
            return UseSqlitePersistance(wireup, connectionString, (c) => { });
        }

        public static EventStoreBuilder UseSqlitePersistance(this EventStoreBuilder wireup, string connectionString, Action<SqliteConfiguration> configure)
        {
            return wireup.ConfigureServices((container) =>
            {
                container.RegisterSingleton<IConnectionFactory>((_) => new SqliteConnectionFactory(connectionString).UseConnectionPool());
                container.RegisterSingleton<IEventReadWrite, IConnectionFactory>((_, connection) =>
                {
                    SqlitePersistanceEngine result = new SqlitePersistanceEngine(connection, configure);
                    result.Initialize();
                    return result;
                });
            });
        }
    }
}
