using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public class MessageBusCommandsTests
    {
        [TestMethod]
        public async Task ExecuteCommandWillGetCalledInTheBackground()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            int numberOfCalls = 0;
            bus.RegisterCommandDelegate<CommandA>(m => numberOfCalls++);
            CommandA command = new CommandA();

            await bus.FireCommand(command);

            await Task.Delay(100);

            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public async Task ExecuteCommandAndWaitWillWaitForCompletion()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            int numberOfCalls = 0;
            bus.RegisterCommandDelegate<CommandA>(m => numberOfCalls++);
            CommandA command = new CommandA();

            await bus.FireCommandAndWait(command, CancellationToken.None);

            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public async Task ExecuteCommandAndWaitWillReThrowException()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            bus.RegisterCommandDelegate<CommandA>(m => throw new NotSupportedException());
            CommandA command = new CommandA();

            await Assert.ThrowsExceptionAsync<NotSupportedException>(async () =>
                await bus.FireCommandAndWait(command, CancellationToken.None)
            );
        }

        [TestMethod]
        public async Task ExecuteCommandWithExceptionWillNotPropagateException()
        {
            Exception raisedException = null;
            MessageId failureMessage = default;
            ExceptionLogger logger = new ExceptionLogger((messageId, message, ex) =>
            {
                raisedException = ex;
                failureMessage = messageId;
            });
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), logger);

            bus.RegisterCommandDelegate<CommandA>(m => throw new NotSupportedException());
            CommandA command = new CommandA();

            await bus.FireCommand(command);

            await Task.Delay(1000);

            Assert.IsInstanceOfType(raisedException, typeof(NotSupportedException));
            Assert.AreEqual(failureMessage, command.MessageId);
        }

        [TestMethod]
        public async Task CommandHandlerSupportsAsync()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);
            using SemaphoreSlim semaphore = new SemaphoreSlim(1);

            await semaphore.WaitAsync();
            int numberOfCalls = 0;
            bus.RegisterCommandDelegateAsync<CommandA>(async m =>
            {
                await Task.Delay(1);
                numberOfCalls++;
                semaphore.Release();
            });
            CommandA command = new CommandA();

            await bus.FireCommand(command);
            await semaphore.WaitAsync();
            Assert.AreEqual(1, numberOfCalls);
        }


        [Topic("Commands/CommandA")]
        public class CommandA : IMessageCommand
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

    }
}
