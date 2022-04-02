using System;
using YasES.Core;

namespace YasES.Examples.PersonList.Users
{
    public static class UserRenamedEvent
    {
        public const string Name = "UserRenamed";

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

                public Payload(Guid userId, string newUserName)
                {
                    UserId = userId;
                    NewUserName = newUserName;
                }

                public Guid UserId { get; set; }

                public string NewUserName { get; set; } = default!;
            }
        }

        public static IEventMessage Build(Guid userId, string newUserName)
        {
            return new EventMessage(Name, JsonSerialization.Serialize(new MessageBody(new V1.Payload(userId, newUserName))));
        }

        public static Parameter Deserialize(IEventMessage message)
        {
            MessageBody body = JsonSerialization.Deserialize<MessageBody>(message.Payload);
            return new Parameter()
            {
                UserId = body.V1.UserId,
                NewUserName = body.V1.NewUserName
            };
        }

        public class Parameter
        {
            public Guid UserId { get; init; }

            public string NewUserName { get; init; } = null!;
        }
    }
}
