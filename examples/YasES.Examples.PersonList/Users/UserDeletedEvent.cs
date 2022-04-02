using System;
using YasES.Core;

namespace YasES.Examples.PersonList.Users
{
    public static class UserDeletedEvent
    {
        public const string Name = "UserDeleted";

        internal class MessageBody
        {
            [Obsolete]
            internal MessageBody()
            {
            }

            public MessageBody(V1.Payload V1)
            {
                this.V1 = V1;
            }

            public V1.Payload V1 { get; set; } = null!;
        }

        internal static class V1
        {
            internal class Payload
            {
                [Obsolete()]
                internal Payload()
                {
                }

                public Payload(Guid userId)
                {
                    UserId = userId;
                }

                public Guid UserId { get; set; }
            }
        }

        public static IEventMessage Build(Guid userId)
        {
            return new EventMessage(Name, JsonSerialization.Serialize(new MessageBody(new V1.Payload(userId))));
        }

        public static Parameter Deserialize(IEventMessage message)
        {
            MessageBody body = JsonSerialization.Deserialize<MessageBody>(message.Payload);
            return new Parameter()
            {
                UserId = body.V1.UserId
            };
        }

        public class Parameter
        {
            public Guid UserId { get; init; }
        }
    }
}
