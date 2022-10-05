using System;
using System.Threading.Tasks;
using MessageBus.Examples.MessageBus.Logging;

namespace MessageBus.Examples.MessageBus
{
    internal static class InlineHandler
    {
        public static async Task Execute(Application system)
        {
            using MessageBrokerMessageBus eventSystem = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NullExceptionNotification.Instance);

            eventSystem.RegisterCommandDelegate<LogMessageCommand>(
                command => eventSystem.FireEvent(new LogMessageCreated(command.MessageId, $"[{DateTime.Now:o}] {command.LogMessage}"))
            );
            eventSystem.RegisterEventDelegate<LogMessageCreated>(
                message => Console.WriteLine($"\t > [{message.MessageId}] {message.LogMessage}")
            );

            await eventSystem.FireCommand(new LogMessageCommand("Please log this message"));

            // the LogMessageCreated handler will be called asynchroniously after the command completed.
            // So just wait until its done.
            await Task.Delay(100);
        }
    }
}
