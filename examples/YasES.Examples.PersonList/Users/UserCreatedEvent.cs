using System;
using YasES.Core;

namespace YasES.Examples.PersonList.Users
{
    public static class UserCreatedEvent
    {
        public const string Name = "UserCreated";

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

                public Payload(Guid userId, string userName)
                {
                    UserId = userId;
                    UserName = userName;
                }

                public Guid UserId { get; set; }

                public string UserName { get; set; } = default!;
            }
        }

        public static IEventMessage Build(Guid userId, string userName)
        {
            return new EventMessage(Name, JsonSerialization.Serialize(new MessageBody(new V1.Payload(userId, userName))));
        }

        public static Parameter Deserialize(IEventMessage message)
        {
            MessageBody body = JsonSerialization.Deserialize<MessageBody>(message.Payload);
            return new Parameter()
            {
                UserId = body.V1.UserId,
                UserName = body.V1.UserName
            };
        }

        public class Parameter
        {
            public Guid UserId { get; init; }

            public string UserName { get; init; } = null!;
        }
    }
}
