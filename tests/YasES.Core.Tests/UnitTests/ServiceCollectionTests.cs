using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class ServiceCollectionTests
    {
        [TestMethod]
        public void EmptyContainerThrowsExceptionWhenTryingToResolveInstance()
        {
            ServiceCollection container = new ServiceCollection();
            Assert.ThrowsException<InvalidOperationException>(() => container.Resolve<object>());
        }

        [TestMethod]
        public void EmptyContainerResolvesEverythingToDefault()
        {
            ServiceCollection container = new ServiceCollection();
            Assert.IsNull(container.ResolveOrDefault<object>());
        }

        [TestMethod]
        public void DisposedContainerThrowsExceptionWhenDisposed()
        {
            ServiceCollection container = new ServiceCollection();
            container.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => container.ResolveOrDefault<object>());
            Assert.ThrowsException<ObjectDisposedException>(() => container.Resolve<object>());
        }

        [TestMethod]
        public void ServiceCollectionResolvesRegisteredItem()
        {
            ServiceCollection container = new ServiceCollection();
            container.RegisterSingleton<int>(42);
            Assert.AreEqual(42, container.Resolve<int>());
        }

        [TestMethod]
        public void ServiceCollectionThrowsExceptionWhenPassingNullFactory()
        {
            ServiceCollection container = new ServiceCollection();
            Assert.ThrowsException<ArgumentNullException>(() => container.RegisterSingleton<int>(null));
            Assert.ThrowsException<ArgumentNullException>(() => container.RegisterSingleton<int, int>(null));
        }

        [TestMethod]
        public void ServiceCollectionCallsFactoryWhenNeeded()
        {
            int numberOfCalls = 0;
            Func<int> producer = () =>
            {
                numberOfCalls++;
                return 42;
            };

            ServiceCollection container = new ServiceCollection();
            container.RegisterSingleton<int>((c) => producer());
            Assert.AreEqual(0, numberOfCalls);
            Assert.AreEqual(42, container.Resolve<int>());
            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void ServiceCollectionCallsFactoryOnlyOnce()
        {
            int numberOfCalls = 0;
            Func<int> producer = () =>
            {
                numberOfCalls++;
                return 42;
            };

            ServiceCollection container = new ServiceCollection();
            container.RegisterSingleton<int>((c) => producer());
            container.Resolve<int>();
            container.Resolve<int>();

            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void ServiceCollectionReplacesExistingItem()
        {
            CBase first = new CBase();
            CBase second = new CBase();

            ServiceCollection container = new ServiceCollection();
            container.RegisterSingleton(first);
            container.RegisterSingleton(second);

            Assert.AreSame(second, container.Resolve<CBase>());
        }

        [TestMethod]
        public void ServiceCollectionResolvesAsSoonAsTypeIsRegistered()
        {
            ServiceCollection container = new ServiceCollection();
            Assert.IsNull(container.ResolveOrDefault<int?>());
            container.RegisterSingleton<int?>(42);
            Assert.AreEqual(42, container.ResolveOrDefault<int?>());
        }

        [TestMethod]
        public void ServiceCollectionPassesDependencyInCallback()
        {
            SomeDerived dependency = new SomeDerived();
            CDerived different = new CDerived();

            ServiceCollection container = new ServiceCollection();
            container.RegisterSingleton<SomeDerived>(dependency);
            container.RegisterSingleton<CBase, IBase>((c, dep) =>
            {
                Assert.AreSame(dependency, dep);
                return different;
            });
            Assert.AreSame(different, container.Resolve<CBase>());
        }

        [TestMethod]
        public void ServiceCollectionThrowsExceptionIfDependencyNotAvailable()
        {
            ServiceCollection container = new ServiceCollection();
            Assert.ThrowsException<InvalidOperationException>(() => container.RegisterSingleton<CBase, IBase>((c, dep) => new CBase()));
        }

        [TestMethod]
        public void ServiceCollectionDisposesActiveInstancesIfIDisposableIsImplemented()
        {
            ServiceCollection container = new ServiceCollection();
            DisposableTrack tracker = new DisposableTrack();
            container.RegisterSingleton<DisposableTrack>((_) => tracker);
            container.Resolve<DisposableTrack>();
            container.Dispose();
            Assert.AreEqual(1, tracker.DisposeCalls);
        }

        [TestMethod]
        public void ServiceCollectionDisposesInstancesWhichGotDirectlyPassed()
        {
            ServiceCollection container = new ServiceCollection();
            DisposableTrack tracker = new DisposableTrack();
            container.RegisterSingleton<DisposableTrack>(tracker);
            container.Dispose();
            Assert.AreEqual(1, tracker.DisposeCalls);
        }

        [TestMethod]
        public void ServiceCollectionIgnoresNonDisposableItems()
        {
            ServiceCollection container = new ServiceCollection();
            container.RegisterSingleton<int>(42);
            container.Dispose();
        }

        private class DisposableTrack : IDisposable
        {
            public int DisposeCalls;

            public void Dispose()
            {
                DisposeCalls++;
            }
        }

        [TestMethod]
        public void ServiceCollectionResolvesToDerivedInterfacesIfTypeNotDirectlyFound()
        {
            ServiceCollection container = new ServiceCollection();
            SomeDerived checkInstance = new SomeDerived();
            container.RegisterSingleton<IDerived>(checkInstance);
            IBase resolved = container.Resolve<IBase>();
            Assert.AreSame(checkInstance, resolved);
        }

        [TestMethod]
        public void ServiceCollectionResolvesToDerivedClassesIfTypeNotDirectlyFound()
        {
            ServiceCollection container = new ServiceCollection();
            CDerived checkInstance = new CDerived();
            container.RegisterSingleton<CDerived>(checkInstance);
            CBase resolved = container.Resolve<CBase>();
            Assert.AreSame(checkInstance, resolved);
        }

        private interface IBase { }

        private interface IDerived : IBase { }

        private class SomeDerived : IDerived { }

        private class CBase { }

        private class CDerived : CBase { }
    }
}
