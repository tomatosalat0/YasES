namespace MessageBus.Examples.MessageBus.Logging
{
    [Topic("Commands/LogMessage")]
    public sealed class LogMessageCommand : IMessageCommand
    {
        public LogMessageCommand(string logMessage)
        {
            if (string.IsNullOrWhiteSpace(logMessage)) throw new System.ArgumentException($"'{nameof(logMessage)}' cannot be null or whitespace.", nameof(logMessage));
            LogMessage = logMessage;
        }

        public string LogMessage { get; }

        public MessageId MessageId { get; } = MessageId.NewId();
    }
}
