﻿using System;
using System.Collections.Generic;
using System.Linq;
using YasES.Core;

namespace YasES.Examples.PersonList.Users.Projections.ActiveUsers
{
    public class UserList
    {
        private readonly Dictionary<string, Action<IEventMessage>> _handler;
        private readonly List<User> _users = new List<User>();
        private readonly IEventRead _read;
        private CheckpointToken _lastKnownEvent;

        public UserList(IEventRead read)
        {
            _handler = new Dictionary<string, Action<IEventMessage>>()
            {
                [UserCreatedEvent.Name] = WhenUserCreated,
                [UserDeletedEvent.Name] = WhenUserDeleted,
                [UserRenamedEvent.Name] = WhenUserRenamed,
            };
            _read = read;
            _lastKnownEvent = CheckpointToken.Beginning;
        }

        private void WhenUserDeleted(IEventMessage message)
        {
            UserDeletedEvent.Parameter parameter = UserDeletedEvent.Deserialize(message);
            _users.RemoveAll(p => p.Id == parameter.UserId);
            LastUserListChangedDate = message.CreationDateUtc;
        }

        private void WhenUserCreated(IEventMessage message)
        {
            User user = new User();
            user.When(message);
            _users.Add(user);
            LastUserListChangedDate = message.CreationDateUtc;
        }

        private void WhenUserRenamed(IEventMessage message)
        {
            UserRenamedEvent.Parameter parameter = UserRenamedEvent.Deserialize(message);
            _users.FirstOrDefault(p => p.Id == parameter.UserId)?.When(message, parameter);
        }

        private void When(IEventMessage message)
        {
            if (_handler.TryGetValue(message.EventName, out var handler))
            {
                handler(message);
            }
        }

        public void Update()
        {
            ReadPredicate predicate = ReadPredicateBuilder.Custom()
                .FromStream(StreamIdentifier.StreamsPrefixedWith(UsersContext.Streams.Bucket, UsersContext.Streams.UserStreamPrefix))
                .ReadForwards()
                .OnlyIncluding(UserCreatedEvent.Name, UserCreatedEvent.Name, UserDeletedEvent.Name)
                .RaisedAfterCheckpoint(_lastKnownEvent)
                .Build();

            foreach (var @event in _read.Read(predicate))
            {
                When(@event);
                _lastKnownEvent = @event.Checkpoint;
                LastDetectedUpdate = @event.CreationDateUtc;
            }
        }

        /// <summary>
        /// The current list of users.
        /// </summary>
        public IReadOnlyList<User> CurrentUsers => _users;

        /// <summary>
        /// The date/time value, the number of active users has changed.
        /// </summary>
        public DateTime LastUserListChangedDate { get; private set; }

        /// <summary>
        /// The date/time value of the last event this list has seen.
        /// </summary>
        public DateTime LastDetectedUpdate { get; private set; }
    }
}
