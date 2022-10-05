using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public class MessageBusQueriesTests
    {
        [TestMethod]
        public async Task QueryHandlerIsCalledForQueries()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            int numberOfCalls = 0;
            bus.RegisterQueryDelegate<QueryA, MessageQueryResult>(m =>
            {
                numberOfCalls++;
                return new MessageQueryResult(m.MessageId);
            });
            QueryA query = new QueryA();

            MessageQueryResult result = await bus.FireQuery<QueryA, MessageQueryResult>(query, CancellationToken.None);

            Assert.AreEqual(1, numberOfCalls);
            Assert.AreEqual(query.MessageId, result.MessageId);
        }

        [TestMethod]
        public async Task ExceptionsInQueryWillGetThrownOnReceive()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            bus.RegisterQueryDelegate<QueryA, MessageQueryResult>(m => throw new NotSupportedException());
            QueryA query = new QueryA();

            await Assert.ThrowsExceptionAsync<NotSupportedException>(async () =>
                await bus.FireQuery<QueryA, MessageQueryResult>(query, CancellationToken.None)
            );
        }

        [TestMethod]
        public async Task QueryHandlerSupportsAsync()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);
            using SemaphoreSlim semaphore = new SemaphoreSlim(1);

            await semaphore.WaitAsync();
            int numberOfCalls = 0;
            bus.RegisterQueryDelegateAsync<QueryA, MessageQueryResult>(async m =>
            {
                await Task.Delay(1);
                numberOfCalls++;
                semaphore.Release();
                return new MessageQueryResult(m.MessageId);
            });
            QueryA query = new QueryA();

            MessageQueryResult result = await bus.FireQuery<QueryA, MessageQueryResult>(query, CancellationToken.None);
            await semaphore.WaitAsync();
            Assert.AreEqual(1, numberOfCalls);
            Assert.AreEqual(query.MessageId, result.MessageId);
        }

        [Topic("Queries/QueryA")]
        public class QueryA : IMessageQuery<MessageQueryResult>
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
