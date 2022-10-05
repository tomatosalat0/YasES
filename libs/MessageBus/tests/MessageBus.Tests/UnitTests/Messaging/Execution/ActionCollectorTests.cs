using System;
using System.Threading;
using MessageBus.Messaging.InProcess.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Messaging.Tests.UnitTests.Execution
{
    [TestClass]
    public class ActionCollectorTests
    {
        [TestMethod]
        public void WaitForWorkReturnsFalseIfNoWorkAvailable()
        {
            ActionCollector collector = new ActionCollector(new EmptyCollectable());
            Assert.IsFalse(collector.HasWork());
            Assert.IsFalse(collector.TryWaitForWork(TimeSpan.FromSeconds(1), CancellationToken.None, out _));
        }

        [TestMethod]
        public void WaitForWorkReturnsFalseIfCollectorCompleted()
        {
            ActionCollector collector = new ActionCollector(new CompletedCollectable());
            Assert.IsFalse(collector.HasWork());
            Assert.IsFalse(collector.TryWaitForWork(TimeSpan.FromSeconds(1), CancellationToken.None, out _));
        }

        [TestMethod]
        public void ActionCollectorWillExecuteWork()
        {
            NeverEmptyCollectable collectable = new NeverEmptyCollectable();
            ActionCollector collector = new ActionCollector(collectable);
            Assert.IsTrue(collector.HasWork());
            Assert.IsTrue(collector.TryWaitForWork(TimeSpan.FromSeconds(1), CancellationToken.None, out var workToExecute));
            Assert.IsNotNull(workToExecute);
            workToExecute.Execute();
            Assert.AreEqual(1, collectable.Executions);
        }

        private class CompletedCollectable : ICollectable
        {
            public bool IsCompleted => true;

            public bool HasCollectables => false;

            public void Collect()
            {
                throw new InvalidOperationException();
            }

            public bool WaitFor(TimeSpan timeout, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException();
            }
        }

        private class EmptyCollectable : ICollectable
        {
            public bool IsCompleted => false;

            public bool HasCollectables => false;

            public void Collect()
            {
                throw new InvalidCastException();
            }

            public bool WaitFor(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return false;
            }
        }

        private class NeverEmptyCollectable : ICollectable
        {
            public int Executions { get; private set; }

            public bool IsCompleted => false;

            public bool HasCollectables => true;

            public void Collect()
            {
                Executions++;
            }

            public bool WaitFor(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return true;
            }
        }
    }
}
