using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace YasES.Core
{
    public class Container : IDisposable
    {
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private bool _disposedValue;

        public Container Register<TService, TDependsOn>(Func<Container, TDependsOn, TService> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            TDependsOn dependsOn = Resolve<TDependsOn>();
            _registrations[typeof(TService)] = new Registration(this, (c) => factory(c, dependsOn));
            return this;
        }

        public Container Register<TService>(Func<Container, TService> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            _registrations[typeof(TService)] = new Registration(this, (c) => factory(c));
            return this;
        }

        public TService Resolve<TService>()
        {
            ThrowDisposed();
            if (!TryResolve(out TService? result))
                throw new InvalidOperationException($"The required service for '{nameof(TService)}' could be found");
            return result;
        }

        public TService? ResolveOrDefault<TService>(TService? @default = default)
        {
            ThrowDisposed();
            if (!TryResolve(out TService? result))
                return @default;
            return result;
        }

        private bool TryResolve<TService>([NotNullWhen(true)] out TService? result)
        {
            if (!_registrations.TryGetValue(typeof(TService), out Registration? registration))
                registration = GetCompatibleAttributes(typeof(TService)).FirstOrDefault();

            result = default;
            if (registration != null)
            {
                result = registration.Resolve<TService>();
            }            
            return result != null;
        }

        private IEnumerable<Registration> GetCompatibleAttributes(Type t)
        {
            return _registrations
                .Where(p => p.Key.IsSubclassOf(t) || t == p.Key || (t.IsInterface && p.Key.GetInterfaces().Any(i => i == t || t.IsSubclassOf(i))))
                .Select(p => p.Value);
        }

        private void ThrowDisposed()
        {
            if (_disposedValue)
                throw new ObjectDisposedException(nameof(Container));
        }

        private void DisposeInstances()
        {
            foreach (var registration in _registrations.Values)
            {
                registration.DisposeInstance();
            }
        }

        private class Registration
        {
            private readonly Container _container;
            private Func<Container, object?>? _factory;
            private object? _instance;

            public Registration(Container container, Func<Container, object?> factory)
            {
                _container = container;
                _factory = factory;
            }

            public TService? Resolve<TService>()
            {
                if (_instance == null)
                {
                    _instance = _factory!(_container);
                    if (_instance != null)
                    {
                        // memory optimization: when the factory isn't needed anymore,
                        // remove the reference to the action so it can get collected
                        // by the GC. A factory action might held references which are 
                        // not needed anymore.
                        _factory = null;
                    }
                }
                return (TService?)_instance;
            }

            internal void DisposeInstance()
            {
                (_instance as IDisposable)?.Dispose();
                _instance = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    DisposeInstances();
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
    }

    public static class ContainerExtensions
    {
        public static Container Register<TService>(this Container container, TService instance)
        {
            container.Register((_) => instance);
            // activate the instance
            container.ResolveOrDefault<TService>();
            return container;
        }
    }
}
