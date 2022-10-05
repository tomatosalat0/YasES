using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public class MessageBusRpcTests
    {
        [TestMethod]
        public async Task RpcHandlerIsCalledForRpcs()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            bus.RegisterRpcDelegate<ToUpperCase, ToUpperCaseResult>(m => new ToUpperCaseResult(m.MessageId, m.Value.ToUpperInvariant()));
            ToUpperCase query = new ToUpperCase("test");

            ToUpperCaseResult result = await bus.FireRpc<ToUpperCase, ToUpperCaseResult>(query, CancellationToken.None);

            Assert.AreEqual(query.MessageId, result.MessageId);
            Assert.AreEqual(query.Value.ToUpperInvariant(), result.Result);
        }

        [TestMethod]
        public async Task ExceptionsInQueryWillGetThrownOnReceive()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            bus.RegisterRpcDelegate<ToUpperCase, ToUpperCaseResult>(m => throw new NotSupportedException());
            ToUpperCase query = new ToUpperCase("test");

            await Assert.ThrowsExceptionAsync<NotSupportedException>(async () =>
                await bus.FireRpc<ToUpperCase, ToUpperCaseResult>(query, CancellationToken.None)
            );
        }

        [TestMethod]
        public async Task QueryHandlerSupportsAsync()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);
            using SemaphoreSlim semaphore = new SemaphoreSlim(1);

            await semaphore.WaitAsync();
            int numberOfCalls = 0;
            bus.RegisterRpcDelegateAsync<ToUpperCase, ToUpperCaseResult>(async m =>
            {
                await Task.Delay(1);
                numberOfCalls++;
                semaphore.Release();
                return new ToUpperCaseResult(m.MessageId, m.Value.ToUpperInvariant());
            });
            ToUpperCase query = new ToUpperCase("Test");

            ToUpperCaseResult result = await bus.FireRpc<ToUpperCase, ToUpperCaseResult>(query, CancellationToken.None);
            await semaphore.WaitAsync();
            Assert.AreEqual(1, numberOfCalls);
            Assert.AreEqual(query.MessageId, result.MessageId);
            Assert.AreEqual(query.Value.ToUpperInvariant(), result.Result);
        }

        [Topic("Rpc/RpcA")]
        protected class ToUpperCase : IMessageRpc<ToUpperCaseResult>
        {
            public ToUpperCase(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public MessageId MessageId { get; } = MessageId.NewId();
        }

        protected class ToUpperCaseResult : IMessageRpcResult
        {
            public ToUpperCaseResult(MessageId messageId, string result)
            {
                MessageId = messageId;
                Result = result;
            }

            public MessageId MessageId { get; }

            public string Result { get; }
        }
    }
}
