using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MessageBus.Messaging.InProcess.Scheduler;
using MessageBus.Messaging.InProcess;
using System.Threading.Tasks;

namespace MessageBus.Messaging.Tests.UnitTests
{
    [TestClass]
    public class MessageBrokerTests
    {
        private IMessageBroker CreateBroker()
        {
            return CreateBroker(MessageBrokerOptions.Default());
        }

        private IMessageBroker CreateBrokerWithManualScheduler(out ManualScheduler scheduler)
        {
            scheduler = new ManualScheduler();
            return CreateBroker(MessageBrokerOptions.BlockingManual(scheduler));
        }

        private IMessageBroker CreateBroker(MessageBrokerOptions options)
        {
            return new InProcessMessageBroker(options);
        }

        [TestMethod]
        public void MessageBrokerThrowsExceptionIfInitializeCallbackIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new InProcessMessageBroker(null));
        }

        [TestMethod]
        public async Task MessageBrokerThrowsExceptionWhenPublishGetsNull()
        {
            using IMessageBroker broker = CreateBrokerWithManualScheduler(out _);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => broker.Publish<object>(null, new TopicName("Topic")));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => broker.Publish<object>(new object(), null));
        }

        [TestMethod]
        public async Task MessageBrokerThrowsExceptionWhenPublishingToNoChannel()
        {
            using IMessageBroker broker = CreateBrokerWithManualScheduler(out _);
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => broker.Publish(new object(), Array.Empty<TopicName>()));
        }

        [TestMethod]
        public void MessageBrokerReturnsChannelOnRequest()
        {
            using IMessageBroker broker = CreateBroker();
            Assert.IsNotNull(broker.Commands("MyChannel"));
        }

        [TestMethod]
        public async Task DisposedMessageBrokerThrowsDisposedException()
        {
            IMessageBroker broker = CreateBroker();
            broker.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => broker.Commands(new TopicName("Topic")));
            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => broker.Publish(new object(), new TopicName("Topic")));
        }

        [TestMethod]
        public async Task MessageBrokerReturnsTrueIfMessageHasArrived()
        {
            using IMessageBroker broker = CreateBrokerWithManualScheduler(out var scheduler);
            broker.Commands("Topic").Subscribe<object>((_) => { });
            await broker.Publish<object>(new object(), "Topic");
            Assert.IsTrue(scheduler.HasWork());
        }

        [TestMethod]
        public async Task DrainingTheMessageBrokerCallsUntilNoMessageIsLeft()
        {
            using IMessageBroker broker = CreateBrokerWithManualScheduler(out var scheduler);

            int numberOfCalls = 0;
            broker.Commands(new TopicName("Topic")).Subscribe((_) => { _.Ack(); numberOfCalls++; });
            await broker.Publish(new object(), new TopicName("Topic"));
            await broker.Publish(new object(), new TopicName("Topic"));

            scheduler.Drain();
            Assert.AreEqual(2, numberOfCalls);
        }

        [TestMethod]
        public void UnsubscribingFromEventsWithDefaultOptionsDoesntRemoveChannelAfterSubscriptionsEnd()
        {
            using IMessageBroker broker = CreateBrokerWithManualScheduler(out var _);

            ISubscribable current = broker.Events("Topic");
            current.Subscribe((_) => { }).Dispose();

            Assert.AreSame(current, broker.Events("Topic"));
        }

        [TestMethod]
        public void UnsubscribingFromEventsWithTemporaryChannelRemovesChannelAfterSubscriptionsEnd()
        {
            using IMessageBroker broker = CreateBrokerWithManualScheduler(out var _);

            ISubscribable current = broker.Events("Topic", EventsOptions.Temporary);
            current.Subscribe((_) => { }).Dispose();

            Assert.AreNotSame(current, broker.Events("Topic"));
        }
    }
}
