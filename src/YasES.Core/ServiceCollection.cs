using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace YasES.Core
{
    public class ServiceCollection : IDisposable
    {
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private readonly bool _ownsServices;
        private bool _disposedValue;

        public ServiceCollection()
            : this(true)
        {
        }

        public ServiceCollection(bool ownsServices)
        {
            _ownsServices = ownsServices;
        }

        /// <summary>
        /// Register a service of the provided <typeparamref name="TService"/> which depends on <typeparamref name="TDependsOn"/>.
        /// The factory result will be kept in memory for the lifetime of the service collection.
        /// </summary>
        public ServiceCollection RegisterSingleton<TService, TDependsOn>(Func<ServiceCollection, TDependsOn, TService> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            TDependsOn dependsOn = Resolve<TDependsOn>();
            _registrations[typeof(TService)] = new SingletonRegistration(this, (c) => factory(c, dependsOn));
            return this;
        }

        /// <summary>
        /// Register a service of the provided <typeparamref name="TService"/>.
        /// The factory result will be kept in memory for the lifetime of the service collection.
        /// </summary>
        public ServiceCollection RegisterSingleton<TService>(Func<ServiceCollection, TService> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            _registrations[typeof(TService)] = new SingletonRegistration(this, (c) => factory(c));
            return this;
        }

        /// <summary>
        /// Register a service of the provided <typeparamref name="TService"/>.
        /// Each time "Resolve" with the same type is called, the provided <paramref name="factory"/>
        /// method will be called. The result instance will not be saved within the service collection.
        /// </summary>
        public ServiceCollection RegisterTransient<TService>(Func<ServiceCollection, TService> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            _registrations[typeof(TService)] = new Registration(this, (c) => factory(c));
            return this;
        }

        public ServiceCollection RegisterTransient(Type serviceType, Func<ServiceCollection, object> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            _registrations[serviceType] = new Registration(this, factory);
            return this;
        }

        /// <summary>
        /// Returns an instance of the provided <typeparamref name="TService"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if <typeparamref name="TService"/> is not a registered type.</exception>
        public TService Resolve<TService>()
        {
            ThrowDisposed();
            if (!TryResolve(out TService? result))
                throw new InvalidOperationException($"The required service for '{typeof(TService)}' could be found");
            return result;
        }

        /// <summary>
        /// Returns an instance of the provided <paramref name="serviceType"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if <paramref name="serviceType"/> is not a registered type.</exception>
        public object Resolve(Type serviceType)
        {
            ThrowDisposed();
            if (!TryResolve(serviceType, out object? result))
                throw new InvalidOperationException($"The required service for '{serviceType.Name}' could be found");
            return result;
        }

        /// <summary>
        /// Returns an instance of the provided <typeparamref name="TService"/>. If no service is registed, <paramref name="default"/>
        /// will get returned.
        /// </summary>
        public TService? ResolveOrDefault<TService>(TService? @default = default)
        {
            ThrowDisposed();
            if (!TryResolve(out TService? result))
                return @default;
            return result;
        }

        /// <summary>
        /// Returns an instance of the provided <paramref name="serviceType"/>. If no service is registed, <paramref name="default"/>
        /// will get returned.
        /// </summary>
        public object? ResolveOrDefault(Type serviceType, object? @default = default)
        {
            ThrowDisposed();
            if (!TryResolve(serviceType, out object? result))
                return @default;
            return result;
        }

        private bool TryResolve<TService>([NotNullWhen(true)] out TService? result)
        {
            if (TryResolve(typeof(TService), out object? service) && service != null)
            {
                result = (TService)service;
                return true;
            }
            result = default;
            return false;
        }

        private bool TryResolve(Type serviceType, [NotNullWhen(true)] out object? result)
        {
            if (!_registrations.TryGetValue(serviceType, out Registration? registration))
                registration = GetCompatibleAttributes(serviceType).FirstOrDefault();

            result = default;
            if (registration != null)
            {
                result = registration.Resolve();
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
                throw new ObjectDisposedException(nameof(ServiceCollection));
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
            private readonly ServiceCollection _services;
            private Func<ServiceCollection, object?>? _factory;

            public Registration(ServiceCollection services, Func<ServiceCollection, object?> factory)
            {
                _services = services;
                _factory = factory;
            }

            /// <summary>
            /// memory optimization: when the factory isn't needed anymore,
            /// remove the reference to the action so it can get collected
            /// by the GC. A factory action might held references which are
            /// not needed anymore.
            /// </summary>
            protected void DestroyFactory()
            {
                _factory = null;
            }

            public virtual object? Resolve()
            {
                return _factory!(_services);
            }

            public TService? Resolve<TService>()
            {
                return (TService?)Resolve();
            }

            internal virtual void DisposeInstance()
            {
            }
        }

        private class SingletonRegistration : Registration
        {
            private object? _instance;

            public SingletonRegistration(ServiceCollection services, Func<ServiceCollection, object?> factory)
                : base(services, factory)
            {
            }

            public override object? Resolve()
            {
                if (_instance == null)
                {
                    _instance = base.Resolve();
                    if (_instance != null)
                    {
                        DestroyFactory();
                    }
                }
                return _instance;
            }

            internal override void DisposeInstance()
            {
                (_instance as IDisposable)?.Dispose();
                _instance = null;
                base.DisposeInstance();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_ownsServices)
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
        public static ServiceCollection RegisterSingleton<TService>(this ServiceCollection services, TService instance)
        {
            services.RegisterSingleton((_) => instance);
            // activate the instance
            services.ResolveOrDefault<TService>();
            return services;
        }
    }
}
