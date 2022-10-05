using System;

namespace MessageBus.Examples.MessageBus.ExampleService
{
    [Topic("Queries/ExampleService/Delay")]
    public sealed class ExampleServiceQuery : IMessageQuery<ExampleServiceQuery.Result>
    {
        public ExampleServiceQuery()
        {
            MessageId = MessageId.NewId();
        }

        public ExampleServiceQuery(MessageId correlationId)
        {
            MessageId = MessageId.CausedBy(correlationId);
        }

        public MessageId MessageId { get; } = MessageId.NewId();

        public sealed class Result : IMessageQueryResult
        {
            public Result(MessageId messageId, TimeSpan duration)
            {
                MessageId = messageId;
                Duration = duration;
            }

            public MessageId MessageId { get; }

            public TimeSpan Duration { get; }
        }
    }
}
