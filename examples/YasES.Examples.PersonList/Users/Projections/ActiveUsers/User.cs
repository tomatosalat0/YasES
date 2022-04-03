using System;
using System.Collections.Generic;
using YasES.Core;

namespace YasES.Examples.PersonList.Users.Projections.ActiveUsers
{
    public class User
    {
        private readonly Dictionary<string, Action<IEventMessage>> _handler;

        public User()
        {
            _handler = new Dictionary<string, Action<IEventMessage>>()
            {
                [UserCreatedEvent.Name] = WhenUserCreated,
                [UserRenamedEvent.Name] = WhenUserNameChanged
            };
        }

        private void WhenUserNameChanged(IEventMessage message)
        {
            UserRenamedEvent.Parameter parameter = UserRenamedEvent.Deserialize(message);
            When(message, parameter);
        }

        private void WhenUserCreated(IEventMessage message)
        {
            UserCreatedEvent.Parameter parameter = UserCreatedEvent.Deserialize(message);
            When(message, parameter);
        }

        public void When(IEventMessage message)
        {
            _handler[message.EventName](message);
        }

        public void When(IEventMessage message, UserCreatedEvent.Parameter parameter)
        {
            CreationDate = message.CreationDateUtc;
            LastModificationDate = message.CreationDateUtc;
            Id = parameter.UserId;
            Name = parameter.UserName;
        }

        public void When(IEventMessage message, UserRenamedEvent.Parameter parameter)
        {
            Name = parameter.NewUserName;
            LastModificationDate = message.CreationDateUtc;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; } = null!;

        public DateTime CreationDate { get; private set; }

        public DateTime LastModificationDate { get; private set; }
    }
}
