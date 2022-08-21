using MessageBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.Performance
{
    [TestClass]
    public class OneCommandTopicTests
    {
        public long CommandsToFire { get; } = 500_000;

        private IMessageBroker CreateBroker()
        {
            return MemoryMessageBrokerBuilder.InProcessBroker();
        }

        [TestMethod]
        public void MessageBusWithOneCommandTopicAndOneListener()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            RunTest(bus, numberOfSubscribers: 1);
        }

        [TestMethod]
        public void MessageBusWithOneCommandTopicAndFiveListeners()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            RunTest(bus, numberOfSubscribers: 5);
        }

        private void RunTest(IMessageBus bus, int numberOfSubscribers)
        {
            using Counter counter = new Counter(CommandsToFire);
            for (int i = 0; i < numberOfSubscribers; i++)
            {
                bus.RegisterCommandHandler(new TestCommandHandler(counter));
            }

            for (int i = 0; i < CommandsToFire; i++)
            {
                bus.FireCommand(new TestCommand());
            }

            while (!counter.Wait(System.TimeSpan.FromSeconds(1)))
            {
            }
            Assert.AreEqual(CommandsToFire, counter.Value);
        }

        [Topic("Commands/Test")]
        private class TestCommand : IMessageCommand
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class TestCommandHandler : IMessageCommandHandler<TestCommand>
        {
            private readonly Counter _counter;

            public TestCommandHandler(Counter counter)
            {
                _counter = counter;
            }

            public void Handle(TestCommand command)
            {
                _counter.Increment();
            }
        }
    }
}
