namespace MessageBus.Examples.MessageBus.Logging
{
    [Topic("Events/LogMessageCreated")]
    public sealed class LogMessageCreated : IMessageEvent
    {
        public LogMessageCreated(MessageId correlationId, string logMessage)
        {
            MessageId = MessageId.CausedBy(correlationId);
            LogMessage = logMessage;
        }

        public string LogMessage { get; }

        public MessageId MessageId { get; }
    }
}
