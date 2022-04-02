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
                [UserCreatedEvent.Name] = HandleUserCreated,
                [UserRenamedEvent.Name] = HandleUserNameChanged
            };
        }

        private void HandleUserNameChanged(IEventMessage message)
        {
            UserRenamedEvent.Parameter parameter = UserRenamedEvent.Deserialize(message);
            Handle(message, parameter);
        }

        private void HandleUserCreated(IEventMessage message)
        {
            UserCreatedEvent.Parameter parameter = UserCreatedEvent.Deserialize(message);
            Handle(message, parameter);
        }

        public void Handle(IEventMessage message)
        {
            _handler[message.EventName](message);
        }

        public void Handle(IEventMessage message, UserCreatedEvent.Parameter parameter)
        {
            CreationDate = message.CreationDateUtc;
            LastModificationDate = message.CreationDateUtc;
            Id = parameter.UserId;
            Name = parameter.UserName;
        }

        public void Handle(IEventMessage message, UserRenamedEvent.Parameter parameter)
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
