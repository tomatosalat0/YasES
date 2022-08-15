using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Plugins.Messaging.Tests.UnitTests
{
    [TestClass]
    public class ThreadedBrokerSchedulingTests
    {
        [TestMethod]
        public void ThreadedBrokerSchedulingThrowsExceptionIfInvalidArgumentGetsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new ThreadedBrokerScheduling(1, null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ThreadedBrokerScheduling(0, new NullScheduling()));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ThreadedBrokerScheduling(-1, new NullScheduling()));
        }

        [TestMethod]
        public void ThreadedBrokerSchedulingAllocatesNumberOfThreads()
        {
            using ThreadedBrokerScheduling scheduler = new ThreadedBrokerScheduling(1, new NullScheduling());
            Assert.AreEqual(1, scheduler.NumberOfThreads);
        }

        [TestMethod]
        public void ThreadedBrokerWorksIfWaitForReturnsImmediately()
        {
            using ThreadedBrokerScheduling scheduler = new ThreadedBrokerScheduling(1, new NoWaitScheduling());
            Assert.AreEqual(1, scheduler.NumberOfThreads);
        }

        [TestMethod]
        [Timeout(2000)]
        public void ThreadedBrokerCallsSubscribersIfThereArePotentialMessages()
        {
            CallMethodCounter counter = new CallMethodCounter();
            counter.HasMessages = true;
            using ThreadedBrokerScheduling scheduler = new ThreadedBrokerScheduling(1, counter);
            Thread.Sleep(100);
            Assert.AreNotEqual(0, counter.CallSubscribersCalls);
        }

        [TestMethod]
        [Timeout(2000)]
        public void ThreadedBrokerOnlyCallsSubscribersOnPotentialMessage()
        {
            CallMethodCounter counter = new CallMethodCounter();
            counter.HasMessages = false;
            using ThreadedBrokerScheduling scheduler = new ThreadedBrokerScheduling(1, counter);
            Thread.Sleep(100);
            Assert.AreEqual(0, counter.CallSubscribersCalls);
        }

        [TestMethod]
        public void ThreadedBorkerOnlyCallsSubscribersUntilItReturnedZero()
        {
            CallMethodCounterWithTracking counter = new CallMethodCounterWithTracking();
            using ThreadedBrokerScheduling scheduler = new ThreadedBrokerScheduling(1, counter);
            Thread.Sleep(100);

            // two calls: one after "CallSubscribers" returned 1 and one after which "CallSubscribers" returned 0
            Assert.AreEqual(2, counter.CallSubscribersCalls);
            Assert.IsFalse(counter.HasMessage);
        }

        private class NullScheduling : IBrokerCommands
        {
            public int CallSubscribers()
            {
                return 0;
            }

            public void RemoveEmptyChannels()
            {
            }

            public virtual bool WaitForMessages(TimeSpan timeout, CancellationToken cancellationToken)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.WaitHandle.WaitOne(timeout);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return false;
            }
        }

        private class NoWaitScheduling : NullScheduling
        {
            public override bool WaitForMessages(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return false;
            }
        }

        private class CallMethodCounter : IBrokerCommands
        {
            public int CallSubscribersCalls;
            public bool HasMessages = true;

            public int CallSubscribers()
            {
                CallSubscribersCalls++;
                return 1;
            }

            public void RemoveEmptyChannels()
            {
            }

            public bool WaitForMessages(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return HasMessages;
            }
        }

        private class CallMethodCounterWithTracking : IBrokerCommands
        {
            public int CallSubscribersCalls;
            public bool HasMessage = true;

            public int CallSubscribers()
            {
                CallSubscribersCalls++;
                if (HasMessage)
                {
                    HasMessage = false;
                    return 1;
                }
                else
                    return 0;
            }

            public void RemoveEmptyChannels()
            {
            }

            public bool WaitForMessages(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return HasMessage;
            }
        }
    }
}
