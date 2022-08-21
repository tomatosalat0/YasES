using System;
using System.Threading;
using MessageBus.Messaging.InProcess;
using MessageBus.Messaging.InProcess.Scheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public partial class MessageBusTests
    {
        [TestMethod]
        public void EventSubscriptionThrowsExceptionForInvalidEvents()
        {
            ManualScheduler scheduler = new ManualScheduler();
            using IMessageBus bus = new MessageBrokerMessageBus(new InProcessMessageBroker(MessageBrokerOptions.BlockingManual(scheduler)), NullExceptionNotification.Instance);

            Assert.ThrowsException<IncompleteConfigurationException>(() => bus.RegisterEventDelegate<MissingTopicEvent>((_) => { }));
            Assert.ThrowsException<IncompleteConfigurationException>(() => bus.FireEvent(new MissingTopicEvent()));

            Assert.ThrowsException<IncompleteConfigurationException>(() => bus.RegisterCommandDelegate<MissingTopicCommand>((_) => { }));
            Assert.ThrowsException<IncompleteConfigurationException>(() => bus.FireCommand(new MissingTopicCommand()));

            Assert.ThrowsException<IncompleteConfigurationException>(() => bus.RegisterQueryDelegate<MissingTopicQuery, MessageQueryResult>((_) => new MessageQueryResult(_.MessageId)));
            Assert.ThrowsExceptionAsync<IncompleteConfigurationException>(async () => await bus.FireQuery<MissingTopicQuery, MessageQueryResult>(new MissingTopicQuery(), CancellationToken.None));
        }

        [TestMethod]
        public void DisposedBusThrowsObjectDisposedException()
        {
            ManualScheduler scheduler = new ManualScheduler();
            IMessageBus bus = new MessageBrokerMessageBus(new InProcessMessageBroker(MessageBrokerOptions.BlockingManual(scheduler)), NullExceptionNotification.Instance);
            bus.Dispose();


            Assert.ThrowsException<ObjectDisposedException>(() => bus.RegisterEventDelegate<MissingTopicEvent>((_) => { }));
            Assert.ThrowsException<ObjectDisposedException>(() => bus.FireEvent(new MissingTopicEvent()));

            Assert.ThrowsException<ObjectDisposedException>(() => bus.RegisterCommandDelegate<MissingTopicCommand>((_) => { }));
            Assert.ThrowsException<ObjectDisposedException>(() => bus.FireCommand(new MissingTopicCommand()));

            Assert.ThrowsException<ObjectDisposedException>(() => bus.RegisterQueryDelegate<MissingTopicQuery, MessageQueryResult>((_) => new MessageQueryResult(_.MessageId)));
            Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () => await bus.FireQuery<MissingTopicQuery, MessageQueryResult>(new MissingTopicQuery(), CancellationToken.None));
        }

        private class MissingTopicEvent : IMessageEvent
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class MissingTopicCommand : IMessageCommand
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class MissingTopicQuery : IMessageQuery<MessageQueryResult>
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class MessageQueryResult : IMessageQueryResult
        {
            public MessageQueryResult(MessageId messageId)
            {
                MessageId = messageId;
            }

            public MessageId MessageId { get; }
        }
    }
}
