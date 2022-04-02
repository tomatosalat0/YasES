using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class ContainerTests
    {
        [TestMethod]
        public void EmptyContainerThrowsExceptionWhenTryingToResolveInstance()
        {
            Container container = new Container();
            Assert.ThrowsException<InvalidOperationException>(() => container.Resolve<object>());
        }

        [TestMethod]
        public void EmptyContainerResolvesEverythingToDefault()
        {
            Container container = new Container();
            Assert.IsNull(container.ResolveOrDefault<object>());
        }

        [TestMethod]
        public void DisposedContainerThrowsExceptionWhenDisposed()
        {
            Container container = new Container();
            container.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => container.ResolveOrDefault<object>());
            Assert.ThrowsException<ObjectDisposedException>(() => container.Resolve<object>());
        }

        [TestMethod]
        public void ContainerResolvesRegisteredItem()
        {
            Container container = new Container();
            container.Register<int>(42);
            Assert.AreEqual(42, container.Resolve<int>());
        }

        [TestMethod]
        public void ContainerThrowsExceptionWhenPassingNullFactory()
        {
            Container container = new Container();
            Assert.ThrowsException<ArgumentNullException>(() => container.Register<int>(null));
            Assert.ThrowsException<ArgumentNullException>(() => container.Register<int, int>(null));
        }

        [TestMethod]
        public void ContainerCallsFactoryWhenNeeded()
        {
            int numberOfCalls = 0;
            Func<int> producer = () =>
            {
                numberOfCalls++;
                return 42;
            };

            Container container = new Container();
            container.Register<int>((c) => producer());
            Assert.AreEqual(0, numberOfCalls);
            Assert.AreEqual(42, container.Resolve<int>());
            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void ContainerCallsFactoryOnlyOnce()
        {
            int numberOfCalls = 0;
            Func<int> producer = () =>
            {
                numberOfCalls++;
                return 42;
            };

            Container container = new Container();
            container.Register<int>((c) => producer());
            container.Resolve<int>();
            container.Resolve<int>();

            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void ContainerReplacesExistingItem()
        {
            CBase first = new CBase();
            CBase second = new CBase();

            Container container = new Container();
            container.Register(first);
            container.Register(second);

            Assert.AreSame(second, container.Resolve<CBase>());
        }

        [TestMethod]
        public void ContainerResolvesAsSoonAsTypeIsRegistered()
        {
            Container container = new Container();
            Assert.IsNull(container.ResolveOrDefault<int?>());
            container.Register<int?>(42);
            Assert.AreEqual(42, container.ResolveOrDefault<int?>());
        }

        [TestMethod]
        public void ContainerPassesDependencyInCallback()
        {
            SomeDerived dependency = new SomeDerived();
            CDerived different = new CDerived();

            Container container = new Container();
            container.Register<SomeDerived>(dependency);
            container.Register<CBase, IBase>((c, dep) =>
            {
                Assert.AreSame(dependency, dep);
                return different;
            });
            Assert.AreSame(different, container.Resolve<CBase>());
        }

        [TestMethod]
        public void ContainerThrowsExceptionIfDependencyNotAvailable()
        {
            Container container = new Container();
            Assert.ThrowsException<InvalidOperationException>(() => container.Register<CBase, IBase>((c, dep) => new CBase()));
        }

        [TestMethod]
        public void ContainerDisposesActiveInstancesIfIDisposableIsImplemented()
        {
            Container container = new Container();
            DisposableTrack tracker = new DisposableTrack();
            container.Register<DisposableTrack>((_) => tracker);
            container.Resolve<DisposableTrack>();
            container.Dispose();
            Assert.AreEqual(1, tracker.DisposeCalls);
        }

        [TestMethod]
        public void ContainerDisposesInstancesWhichGotDirectlyPassed()
        {
            Container container = new Container();
            DisposableTrack tracker = new DisposableTrack();
            container.Register<DisposableTrack>(tracker);
            container.Dispose();
            Assert.AreEqual(1, tracker.DisposeCalls);
        }

        [TestMethod]
        public void ContainerIgnoresNonDisposableItems()
        {
            Container container = new Container();
            container.Register<int>(42);
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
        public void ContainerResolvesToDerivedInterfacesIfTypeNotDirectlyFound()
        {
            Container container = new Container();
            SomeDerived checkInstance = new SomeDerived();
            container.Register<IDerived>(checkInstance);
            IBase resolved = container.Resolve<IBase>();
            Assert.AreSame(checkInstance, resolved);
        }

        [TestMethod]
        public void ContainerResolvesToDerivedClassesIfTypeNotDirectlyFound()
        {
            Container container = new Container();
            CDerived checkInstance = new CDerived();
            container.Register<CDerived>(checkInstance);
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
