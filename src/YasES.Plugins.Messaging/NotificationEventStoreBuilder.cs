using System;
using System.Collections.Generic;
using MessageBus;
using MessageBus.Messaging;
using MessageBus.Messaging.InProcess;

namespace YasES.Core
{
    public sealed class NotificationEventStoreBuilder : EventStoreBuilder
    {
        public NotificationEventStoreBuilder(EventStoreBuilder parent, MessageBrokerOptions options)
            : base(parent)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            ConfigureServices((container) =>
            {
                IMessageBroker broker = new InProcessMessageBroker(options);
                IMessageBus bus = new MessageBrokerMessageBus(broker, NoExceptionNotification.Instance);
                container.RegisterSingleton<IMessageBroker>(broker);
                container.RegisterSingleton<IMessageBus>(bus);
            });
        }

        /// <summary>
        /// After every successful <see cref="IEventWrite.Commit(CommitAttempt)"/>,
        /// a message wihtin the <see cref="IMessageBroker"/> gets published. The message
        /// will be an instance of <see cref="IAfterCommitEvent"/>.
        /// </summary>
        public NotificationEventStoreBuilder NotifyAfterCommit()
        {
            AddTopicEventProducer(AfterCommitStore.MatchesAll);
            return this;
        }

        /// <summary>
        /// After an successful call to <see cref="IEventWrite.Commit(CommitAttempt)"/> and
        /// the commited stream matches the provided <paramref name="streamIdentifier"/>
        /// (see <see cref="CommitAttempt.StreamIdentifier"/> and <see cref="StreamIdentifier.Matches(StreamIdentifier)"/>),
        /// a message within the <see cref="IMessageBroker"/> gets published. The message
        /// will be an instance of <see cref="IAfterCommitEvent"/>.
        /// </summary>
        public NotificationEventStoreBuilder NotifyAfterCommitForStream(StreamIdentifier streamIdentifier)
        {
            AddTopicEventProducer(BuildStreamIdentifierMatchPredicate(streamIdentifier));
            return this;
        }

        private void AddTopicEventProducer(Func<CommitAttempt, bool> predicate)
        {
            ConfigureServices((services) =>
            {
                IEventReadWrite existing = services.Resolve<IEventReadWrite>();
                IMessageBus bus = services.Resolve<IMessageBus>();
                IEventReadWrite overwritten = new AfterCommitStore(existing, bus, predicate);
                services.RegisterSingleton<IEventReadWrite>(overwritten);
            });
        }

        private Func<CommitAttempt, bool> BuildStreamIdentifierMatchPredicate(StreamIdentifier streamIdentifier)
        {
            return (attempt) => streamIdentifier.Matches(attempt.StreamIdentifier);
        }

        private class AfterCommitStore : IEventReadWrite
        {
            private readonly IEventReadWrite _inner;
            private readonly IMessageBus _bus;
            private readonly Func<CommitAttempt, bool> _predicate;

            public static bool MatchesAll(CommitAttempt attempt)
            {
                return true;
            }

            public AfterCommitStore(IEventReadWrite inner, IMessageBus bus)
                : this(inner, bus, MatchesAll)
            {
            }

            public AfterCommitStore(IEventReadWrite inner, IMessageBus bus, Func<CommitAttempt, bool> predicate)
            {
                _inner = inner;
                _bus = bus;
                _predicate = predicate;
            }

            public void Commit(CommitAttempt attempt)
            {
                _inner.Commit(attempt);
                if (_predicate(attempt))
                    _bus.FireEvent<IAfterCommitEvent>(new AfterCommitEvent(attempt));
            }

            public IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate)
            {
                return _inner.Read(predicate);
            }

            private class AfterCommitEvent : IAfterCommitEvent
            {
                public AfterCommitEvent(CommitAttempt attempt)
                {
                    Attempt = attempt;
                    MessageId = MessageId.NewId();
                }

                public CommitAttempt Attempt { get; }

                public DateTime EventRaisedUtc { get; } = SystemClock.UtcNow;

                public MessageId MessageId { get; }
            }
        }
    }
}
