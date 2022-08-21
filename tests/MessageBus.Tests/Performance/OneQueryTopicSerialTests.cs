using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.Performance
{
    [TestClass]
    public class OneQueryTopicSerialTests
    {
        public long QueriesToFire { get; } = 10_000;

        private IMessageBroker CreateBroker()
        {
            return MemoryMessageBrokerBuilder.InProcessBroker();
        }

        [TestMethod]
        public async Task MessageBusWithOneEventTopicAndOneListener()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            await RunTest(bus, numberOfSubscribers: 1);
        }

        [TestMethod]
        public async Task MessageBusWithOneEventTopicAndFiveListeners()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(CreateBroker(), NullExceptionNotification.Instance);

            await RunTest(bus, numberOfSubscribers: 5);
        }

        private async Task RunTest(IMessageBus bus, int numberOfSubscribers)
        {
            using Counter counter = new Counter(QueriesToFire);
            List<TestQueryHandler> handlers = Enumerable.Range(0, numberOfSubscribers)
                .Select(_ => new TestQueryHandler(counter))
                .ToList();

            foreach (var handler in handlers)
                bus.RegisterQueryHandler(handler);

            for (int i = 0; i < QueriesToFire; i++)
            {
                await bus.FireQuery<TestQuery, TestQueryResult>(new TestQuery(), CancellationToken.None);
            }
            Assert.AreEqual(QueriesToFire, counter.Value);
            Assert.AreEqual(QueriesToFire, handlers.Sum(p => (long)p.HandledQueries));
        }

        [Topic("Queries/Test")]
        private class TestQuery : IMessageQuery<TestQueryResult>
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class TestQueryResult : IMessageQueryResult
        {
            public TestQueryResult(MessageId messageId)
            {
                MessageId = messageId;
            }

            public MessageId MessageId { get; }
        }

        private class TestQueryHandler : IMessageQueryHandler<TestQuery, TestQueryResult>
        {
            private readonly Counter _counter;

            public int HandledQueries { get; private set; }

            public TestQueryHandler(Counter counter)
            {
                _counter = counter;
            }

            public TestQueryResult Handle(TestQuery query)
            {
                HandledQueries++;
                _counter.Increment();
                return new TestQueryResult(query.MessageId);
            }
        }
    }
}
