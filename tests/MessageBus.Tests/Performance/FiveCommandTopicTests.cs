using MessageBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.Performance
{
    [TestClass]
    public class FiveCommandTopicTests
    {
        public long CommandsToFire { get; } = 500_000;

        private IMessageBroker CreateBroker()
        {
            return MemoryMessageBrokerBuilder.InProcessBroker();
        }

        [TestMethod]
        public void MessageBusWithFiveCommandTopicAndFiveListener()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            RunTest(bus);
        }

        private void RunTest(IMessageBus bus)
        {
            using Counter counter = new Counter(CommandsToFire);

            bus.RegisterCommandHandler((IMessageCommandHandler<TestCommandOne>)new TestCommandHandler(counter));
            bus.RegisterCommandHandler((IMessageCommandHandler<TestCommandTwo>)new TestCommandHandler(counter));
            bus.RegisterCommandHandler((IMessageCommandHandler<TestCommandThree>)new TestCommandHandler(counter));
            bus.RegisterCommandHandler((IMessageCommandHandler<TestCommandFour>)new TestCommandHandler(counter));
            bus.RegisterCommandHandler((IMessageCommandHandler<TestCommandFive>)new TestCommandHandler(counter));

            for (int i = 0; i < CommandsToFire; i++)
            {
                switch (i % 5)
                {
                    case 0:
                        bus.FireCommand(new TestCommandOne());
                        break;
                    case 1:
                        bus.FireCommand(new TestCommandTwo());
                        break;
                    case 2:
                        bus.FireCommand(new TestCommandThree());
                        break;
                    case 3:
                        bus.FireCommand(new TestCommandFour());
                        break;
                    case 4:
                        bus.FireCommand(new TestCommandFive());
                        break;
                };
            }


            while (!counter.Wait(System.TimeSpan.FromSeconds(1)))
            {
            }
            Assert.AreEqual(CommandsToFire, counter.Value);
        }

        [Topic("Commands/Test1")]
        private class TestCommandOne : IMessageCommand { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Commands/Test2")]
        private class TestCommandTwo : IMessageCommand { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Commands/Test3")]
        private class TestCommandThree : IMessageCommand { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Commands/Test4")]
        private class TestCommandFour : IMessageCommand { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Commands/Test5")]
        private class TestCommandFive : IMessageCommand { public MessageId MessageId { get; } = MessageId.NewId(); }

        private class TestCommandHandler :
            IMessageCommandHandler<TestCommandOne>,
            IMessageCommandHandler<TestCommandTwo>,
            IMessageCommandHandler<TestCommandThree>,
            IMessageCommandHandler<TestCommandFour>,
            IMessageCommandHandler<TestCommandFive>
        {
            private readonly Counter _counter;

            public TestCommandHandler(Counter counter)
            {
                _counter = counter;
            }

            public void Handle(TestCommandOne command) => _counter.Increment();

            public void Handle(TestCommandTwo command) => _counter.Increment();

            public void Handle(TestCommandThree command) => _counter.Increment();

            public void Handle(TestCommandFour command) => _counter.Increment();

            public void Handle(TestCommandFive command) => _counter.Increment();
        }
    }
}
