using MessageBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.Performance
{
    [TestClass]
    public class OneEventTopicTests
    {
        public long EventsToFire { get; } = 500_000;

        private IMessageBroker CreateBroker()
        {
            return MemoryMessageBrokerBuilder.InProcessBroker();
        }

        [TestMethod]
        public void MessageBusWithOneEventTopicAndOneListener()
        {
            using var bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            RunTest(bus, numberOfSubscribers: 1);
        }

        [TestMethod]
        public void MessageBusWithOneEventTopicAndFiveListeners()
        {
            using var bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            RunTest(bus, numberOfSubscribers: 5);
        }

        [TestMethod]
        public void MessageBusWithOneEventTopicAndHundredListeners()
        {
            using var bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            RunTest(bus, numberOfSubscribers: 100);
        }

        private void RunTest(IMessageBus bus, int numberOfSubscribers)
        {
            using Counter counter = new Counter(EventsToFire * numberOfSubscribers);

            if (numberOfSubscribers > 1)
            {
                for (int i = 0; i < numberOfSubscribers; i++)
                {
                    bus.RegisterEventHandler(new TestEventHandler(counter));
                }
            }
            else
                bus.RegisterEventHandler(new VerifyTestEventHandler(counter));

            for (int i = 0; i < EventsToFire; i++)
            {
                bus.FireEvent(new TestEvent(i));
            }


            while (!counter.Wait(System.TimeSpan.FromSeconds(1)))
            {
            }
            Assert.AreEqual(EventsToFire * (long)numberOfSubscribers, counter.Value);
        }

        [Topic("Events/Test")]
        private class TestEvent : IMessageEvent
        {
            public TestEvent(int index)
            {
                Index = index;
            }

            public int Index { get; }

            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class TestEventHandler : IMessageEventHandler<TestEvent>
        {
            private readonly Counter _counter;

            public TestEventHandler(Counter counter)
            {
                _counter = counter;
            }

            public void Handle(TestEvent @event)
            {
                _counter.Increment();
            }
        }

        private class VerifyTestEventHandler : IMessageEventHandler<TestEvent>
        {
            private readonly Counter _counter;
            private int _lastId = -1;

            public VerifyTestEventHandler(Counter counter)
            {
                _counter = counter;
            }

            public void Handle(TestEvent @event)
            {
                if (_lastId != @event.Index - 1)
                    throw new System.Exception();
                _lastId = @event.Index;
                _counter.Increment();
            }
        }
    }
}
