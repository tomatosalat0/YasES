using System;
using System.Linq;
using YasES.Core;

namespace YasES.Examples.PersonList.Users
{
    public class UsersContext
    {
        internal static class Streams
        {
            public const string Bucket = "UserManagement";

            public const string UserStreamPrefix = "User/";
        }

        private readonly IEventReadWrite _eventStore;

        public UsersContext(IEventReadWrite eventStore)
        {
            _eventStore = eventStore;
        }

        public Guid CreateUser(string userName)
        {
            EnsureValidUserName(userName);
            EnsureUserNameDoesNotExist(userName);

            Guid id = Guid.NewGuid();
            _eventStore.Commit(new EventCollector()
                .Add(UserCreatedEvent.Build(id, userName))
                .BuildCommit(StreamIdentifier.SingleStream(Streams.Bucket, Streams.UserStreamPrefix + id.ToString())));
            return id;
        }

        public void RenameUser(Guid userId, string newUserName)
        {
            EnsureValidUserName(newUserName);
            EnsureUserExists(userId);
            EnsureUserHasntBeenRenamedRecently(userId);
            EnsureUserNameDoesNotExist(newUserName);

            _eventStore.Commit(new EventCollector()
                .Add(UserRenamedEvent.Build(userId, newUserName))
                .BuildCommit(StreamIdentifier.SingleStream(Streams.Bucket, Streams.UserStreamPrefix + userId.ToString())));
        }

        public void DeleteUser(Guid userId)
        {
            EnsureUserExists(userId);

            _eventStore.Commit(new EventCollector()
                .Add(UserDeletedEvent.Build(userId))
                .BuildCommit(StreamIdentifier.SingleStream(Streams.Bucket, Streams.UserStreamPrefix + userId.ToString())));
        }

        private void EnsureValidUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException($"'{nameof(userName)}' cannot be null or whitespace.", nameof(userName));
            if (userName.Trim().Length != userName.Length) throw new ArgumentException($"The user name must not start or end with a space, got '{userName}'");
        }

        private void EnsureUserNameDoesNotExist(string userName)
        {
            if (UserNameAlreadyExists(userName))
                throw new InvalidOperationException($"The given user name is already in use, got '{userName}'");
        }

        private void EnsureUserHasntBeenRenamedRecently(Guid userId)
        {
            TimeSpan minRenameTimeDifference = TimeSpan.FromMinutes(5);
            DateTime? lastRename = LastTimeUserHasBeenRenamed(userId);
            if (lastRename == null)
                return;

            TimeSpan difference = SystemClock.UtcNow.Subtract(lastRename.Value);
            if (difference < minRenameTimeDifference)
                throw new InvalidOperationException($"The user renaming must cool down for {minRenameTimeDifference - difference}");
        }

        private void EnsureUserExists(Guid userId)
        {
            if (!UserExists(userId))
                throw new InvalidOperationException($"The provided user {userId} does not exist");
        }

        private bool UserNameAlreadyExists(string userName)
        {
            ReadPredicate predicate = ReadPredicateBuilder.Custom()
                .FromAllStreamsInBucket(Streams.Bucket)
                .ReadBackwards()
                .OnlyIncluding(UserCreatedEvent.Name, UserRenamedEvent.Name, UserDeletedEvent.Name)
                .WithoutCheckpointLimit()
                .Build();

            IReadEventMessage? @event = _eventStore.Read(predicate)
                .Where(p => ChangedToName(p, userName))
                .FirstOrDefault();

            return @event != null;
        }

        private static bool ChangedToName(IEventMessage message, string compareTo)
        {
            switch (message.EventName)
            {
                case UserCreatedEvent.Name:
                    return compareTo.Equals(UserCreatedEvent.Deserialize(message).UserName, StringComparison.InvariantCultureIgnoreCase);
                case UserRenamedEvent.Name:
                    return compareTo.Equals(UserRenamedEvent.Deserialize(message).NewUserName, StringComparison.InvariantCultureIgnoreCase);
                default:
                    return false;
            }
        }

        private DateTime? LastTimeUserHasBeenRenamed(Guid userId)
        {
            ReadPredicate predicate = ReadPredicateBuilder.Custom()
                .FromStream(StreamIdentifier.SingleStream(Streams.Bucket, Streams.UserStreamPrefix + userId.ToString()))
                .ReadBackwards()
                .OnlyIncluding(UserCreatedEvent.Name, UserRenamedEvent.Name)
                .WithoutCheckpointLimit()
                .Build();

            IReadEventMessage? @event = _eventStore.Read(predicate)
                .FirstOrDefault();

            return @event?.CreationDateUtc;
        }

        private bool UserExists(Guid userId)
        {
            ReadPredicate predicate = ReadPredicateBuilder.Custom()
                .FromStream(StreamIdentifier.SingleStream(Streams.Bucket, Streams.UserStreamPrefix + userId.ToString()))
                .ReadBackwards()
                .OnlyIncluding(UserCreatedEvent.Name, UserDeletedEvent.Name)
                .WithoutCheckpointLimit()
                .Build();

            IReadEventMessage? @event = _eventStore.Read(predicate)
                .Where(p => p.EventName != UserDeletedEvent.Name)
                .FirstOrDefault();

            return @event != null;
        }
    }
}
