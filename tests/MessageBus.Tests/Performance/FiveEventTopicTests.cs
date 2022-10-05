using MessageBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.Performance
{
    [TestClass]
    public class FiveEventTopicTests
    {
        public long EventsToFire { get; } = 500_000;

        private IMessageBroker CreateBroker()
        {
            return MemoryMessageBrokerBuilder.InProcessBroker();
        }

        [TestMethod]
        public void MessageBusWithFiveEventTopicAndFiveListener()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(CreateBroker(), NoExceptionNotification.Instance);

            RunTest(bus);
        }

        private void RunTest(IMessageBus bus)
        {
            using Counter counter = new Counter(EventsToFire);

            bus.RegisterEventHandler((IMessageEventHandler<TestEventOne>)new TestEventHandler(counter));
            bus.RegisterEventHandler((IMessageEventHandler<TestEventTwo>)new TestEventHandler(counter));
            bus.RegisterEventHandler((IMessageEventHandler<TestEventThree>)new TestEventHandler(counter));
            bus.RegisterEventHandler((IMessageEventHandler<TestEventFour>)new TestEventHandler(counter));
            bus.RegisterEventHandler((IMessageEventHandler<TestEventFive>)new TestEventHandler(counter));


            for (int i = 0; i < EventsToFire; i++)
            {
                switch (i % 5)
                {
                    case 0:
                        bus.FireEvent(new TestEventOne());
                        break;
                    case 1:
                        bus.FireEvent(new TestEventTwo());
                        break;
                    case 2:
                        bus.FireEvent(new TestEventThree());
                        break;
                    case 3:
                        bus.FireEvent(new TestEventFour());
                        break;
                    case 4:
                        bus.FireEvent(new TestEventFive());
                        break;
                };
            }


            while (!counter.Wait(System.TimeSpan.FromSeconds(1)))
            {
            }
            Assert.AreEqual(EventsToFire, counter.Value);
        }

        [Topic("Events/Test1")]
        private class TestEventOne : IMessageEvent { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Events/Test2")]
        private class TestEventTwo : IMessageEvent { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Events/Test3")]
        private class TestEventThree : IMessageEvent { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Events/Test4")]
        private class TestEventFour : IMessageEvent { public MessageId MessageId { get; } = MessageId.NewId(); }

        [Topic("Events/Test5")]
        private class TestEventFive : IMessageEvent { public MessageId MessageId { get; } = MessageId.NewId(); }

        private class TestEventHandler : 
            IMessageEventHandler<TestEventOne>, 
            IMessageEventHandler<TestEventTwo>,
            IMessageEventHandler<TestEventThree>, 
            IMessageEventHandler<TestEventFour>,
            IMessageEventHandler<TestEventFive>
        {
            private readonly Counter _counter;

            public TestEventHandler(Counter counter)
            {
                _counter = counter;
            }

            public void Handle(TestEventOne @event) => _counter.Increment();

            public void Handle(TestEventTwo @event) => _counter.Increment();

            public void Handle(TestEventThree @event) => _counter.Increment();

            public void Handle(TestEventFour @event) => _counter.Increment();

            public void Handle(TestEventFive @event) => _counter.Increment();
        }
    }
}
