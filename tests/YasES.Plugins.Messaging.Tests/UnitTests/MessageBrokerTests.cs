using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Plugins.Messaging.Tests.UnitTests
{
    [TestClass]
    public class MessageBrokerTests
    {
        [TestMethod]
        public void MessageBrokerThrowsExceptionIfInitializeCallbackIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new MessageBroker(null));
        }

        [TestMethod]
        public void MessageBrokerReturnsChannelOnRequest()
        {
            using MessageBroker broker = new MessageBroker((b) => new ManualBrokerScheduler(b) as IDisposable);
            Assert.IsNotNull(broker.Channel("MyChannel"));
        }

        [TestMethod]
        public void MessageBrokerDoesntAllowNullOrEmptyChannelNames()
        {
            using MessageBroker broker = new MessageBroker((b) => new ManualBrokerScheduler(b) as IDisposable);
            Assert.ThrowsException<ArgumentException>(() => broker.Channel(null));
            Assert.ThrowsException<ArgumentException>(() => broker.Channel(string.Empty));
        }

        [TestMethod]
        public void DisposedMessageBrokerThrowsDisposedException()
        {
            MessageBroker broker = new MessageBroker((b) => new ManualBrokerScheduler(b) as IDisposable);
            broker.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => broker.Channel("Topic"));
            Assert.ThrowsException<ObjectDisposedException>(() => broker.Publish(new object(), "Topic"));
        }

        [TestMethod]
        public void MessageBrokerThrowsExceptionWhenPublishGetsNull()
        {
            using MessageBroker broker = new MessageBroker((b) => new ManualBrokerScheduler(b) as IDisposable);
            Assert.ThrowsException<ArgumentNullException>(() => broker.Publish<object>(null, "Topic"));
            Assert.ThrowsException<ArgumentNullException>(() => broker.Publish<object>(new object(), null));
        }

        [TestMethod]
        public void MessageBrokerReturnsSameChannelIfAlreadyExists()
        {
            using MessageBroker broker = new MessageBroker((b) => new ManualBrokerScheduler(b) as IDisposable);
            IChannel channel1 = broker.Channel("MyChannel");
            IChannel channel2 = broker.Channel("MyChannel");
            Assert.AreSame(channel1, channel2);
        }

        [TestMethod]
        public void MessageBrokerCallsDisposeOfSchedulerIfItIsDisposable()
        {
            DisposableTrack tracker = new DisposableTrack();
            using (var broker = new MessageBroker(_ => tracker))
            {
            }
            Assert.AreEqual(1, tracker.DisposeCallCounts);
        }

        [TestMethod]
        public void MessageBrokerReturnsFalseIfNoMessageHasArrived()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            Assert.IsFalse(scheduler.WaitForMessages(200));
        }

        [TestMethod]
        public void MessageBrokerReturnsTrueIfMessageHasArrived()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            broker.Channel("Topic").Subscribe<object>((_) => { });
            broker.Publish<object>(new object(), "Topic");
            Assert.IsTrue(scheduler.WaitForMessages(200));
        }

        [TestMethod]
        public void MessageBrokerCleansUpEmptyChannels()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            IChannel channel1 = broker.Channel("Topic");
            scheduler.RemoveEmptyChannels();
            IChannel channel2 = broker.Channel("Topic");
            Assert.AreNotSame(channel1, channel2);
        }

        [TestMethod]
        public void MessageBrokerDoesntCleanupChannelWithSubscribers()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            IChannel channel1 = broker.Channel("Topic");
            channel1.Subscribe<object>((_) => { });

            scheduler.RemoveEmptyChannels();
            IChannel channel2 = broker.Channel("Topic");
            Assert.AreSame(channel1, channel2);
        }

        [TestMethod]
        public void MessageBrokerCleansupChannelWithMessagesButNoSubscribers()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            IChannel channel1 = broker.Channel("Topic");
            broker.Publish<object>(new object(), "Topic");
            scheduler.RemoveEmptyChannels();
            IChannel channel2 = broker.Channel("Topic");
            Assert.AreNotSame(channel1, channel2);
        }

        [TestMethod]
        public void MessageBrokerDoesntCreateTopicOnPublish()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            IChannel channel1 = broker.Channel("Topic");
            scheduler.RemoveEmptyChannels();

            broker.Publish<object>(new object(), "Topic");
            Assert.AreEqual(0, broker.ActiveChannels);
        }

        [TestMethod]
        public void MessageBrokerReturnsNumberOfActiveTopics()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            broker.Channel("Topic").Subscribe<object>((_) => { });
            Assert.AreEqual(1, broker.ActiveChannels);
        }

        [TestMethod]
        public void MessageBrokerReturnsZeroIfNoMessageHasBeenSend()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);
            Assert.AreEqual(0, scheduler.CallSubscribers());
        }

        [TestMethod]
        public void MessageBrokerCallsSubscribersSubscriberOncePerRound()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);

            int numberOfCalls = 0;
            broker.Channel("Topic").Subscribe<object>((_) => numberOfCalls++);
            broker.Publish(new object(), "Topic");
            broker.Publish(new object(), "Topic");

            Assert.AreEqual(1, scheduler.CallSubscribers());
            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void DrainingTheMessageBrokerCallsUntilNoMessageIsLeft()
        {
            ManualBrokerScheduler scheduler = null;
            using MessageBroker broker = new MessageBroker(b => (scheduler = new ManualBrokerScheduler(b)) as IDisposable);

            int numberOfCalls = 0;
            broker.Channel("Topic").Subscribe((_) => numberOfCalls++);
            broker.Publish(new object(), "Topic");
            broker.Publish(new object(), "Topic");

            Assert.AreEqual(2, scheduler.Drain());
            Assert.AreEqual(2, numberOfCalls);
        }

        private class DisposableTrack : IDisposable
        {
            public int DisposeCallCounts;

            public void Dispose()
            {
                DisposeCallCounts++;
            }
        }
    }
}
