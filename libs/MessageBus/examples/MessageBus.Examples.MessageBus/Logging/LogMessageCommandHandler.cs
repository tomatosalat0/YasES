using System;

namespace MessageBus.Examples.MessageBus.Logging
{
    public sealed class LogMessageCommandHandler : IMessageCommandHandler<LogMessageCommand>
    {
        private readonly MessageBrokerMessageBus _system;

        public LogMessageCommandHandler(MessageBrokerMessageBus system)
        {
            _system = system;
        }

        public void Handle(LogMessageCommand command)
        {
            string currentTime = ConsoleFormat.Format($"[{DateTime.Now:o}]", ConsoleFormat.ForegroundColor.Yellow);
            string transformed = $"{currentTime}{command.LogMessage}";
            _system.FireEvent(new LogMessageCreated(command.MessageId, transformed));
        }
    }
}
