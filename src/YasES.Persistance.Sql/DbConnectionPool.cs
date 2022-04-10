using System;
using System.Collections.Concurrent;
using System.Data;

namespace YasES.Persistance.Sql
{
    public class DbConnectionPool : IConnectionFactory, IDisposable
    {
        private readonly IConnectionFactory _inner;
        private readonly ConcurrentBag<IDbConnection> _connections = new ConcurrentBag<IDbConnection>();
        private readonly Action<IDbConnection> _onPooledConnectionClosed;
        private bool _disposedValue;

        public DbConnectionPool(IConnectionFactory inner, Action<IDbConnection> onPooledConnectionClosed)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _onPooledConnectionClosed = onPooledConnectionClosed ?? throw new ArgumentNullException(nameof(onPooledConnectionClosed));
        }

        /// <summary>
        /// Returns the number of connections which are ready for usage
        /// within the pool. The result will not include connections which
        /// are currently in use.
        /// </summary>
        public int GetReadyConnections()
        {
            return _connections.Count;
        }

        private IDbConnection GetConnection()
        {
            if (_connections.TryTake(out IDbConnection? result))
                return result;

            return _inner.Open();
        }

        private void ConnectionClosed(IDbConnection connection)
        {
            _connections.Add(connection);
        }

        public IDbConnection Open()
        {
            if (_disposedValue) throw new ObjectDisposedException(nameof(DbConnectionPool));
            return new SharedDbConnection(GetConnection(), ConnectionClosed);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_connections.TryTake(out IDbConnection? connection))
                    {
                        _onPooledConnectionClosed(connection);
                        connection.Close();
                        connection.Dispose();
                    }
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

        private sealed class SharedDbConnection : IDbConnection
        {
            private readonly IDbConnection _inner;
            private readonly Action<IDbConnection> _onClose;
            private bool disposedValue;

            public SharedDbConnection(IDbConnection inner, Action<IDbConnection> onClose)
            {
                _inner = inner;
                _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
            }

            public string ConnectionString
            {
                get => _inner.ConnectionString;
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
                set { }
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            }

            public int ConnectionTimeout => _inner.ConnectionTimeout;

            public string Database => _inner.Database;

            public ConnectionState State => _inner.State;

            public IDbTransaction BeginTransaction()
            {
                return _inner.BeginTransaction();
            }

            public IDbTransaction BeginTransaction(IsolationLevel il)
            {
                return _inner.BeginTransaction(il);
            }

            public void ChangeDatabase(string databaseName)
            {
                _inner.ChangeDatabase(databaseName);
            }

            public void Close()
            {
                Dispose(disposing: true);
            }

            public IDbCommand CreateCommand()
            {
                return _inner.CreateCommand();
            }

            public void Open()
            {
                // NOOP.
            }

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _onClose(_inner);
                    }
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
