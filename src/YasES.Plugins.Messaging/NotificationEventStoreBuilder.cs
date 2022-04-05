using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasES.Plugins.Messaging;

namespace YasES.Core
{
    public sealed class NotificationEventStoreBuilder : EventStoreBuilder
    {
        private readonly EventStoreBuilder _inner;

        public NotificationEventStoreBuilder(EventStoreBuilder inner, Func<IBrokerCommands, IDisposable?> initialization)
        {
            if (initialization is null) throw new ArgumentNullException(nameof(initialization));
            _inner = inner;
            _inner.ConfigureContainer((container) =>
            {
                IMessageBroker broker = new MessageBroker(initialization);
                container.Register<IMessageBroker>(broker);
            });
        }

        public override EventStoreBuilder ConfigureContainer(Action<Container> handler)
        {
            _inner.ConfigureContainer(handler);
            return this;
        }

        /// <summary>
        /// After every successful <see cref="IEventWrite.Commit(CommitAttempt)"/>, 
        /// a message wihtin the <see cref="IMessageBroker"/> gets published. The topic
        /// of the event will be the provided <paramref name="topicName"/>. The message
        /// will be an instance of <see cref="AfterCommitEvent"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Is thrown if <paramref name="topicName"/> is null or empty or whitespace only.</exception>
        public NotificationEventStoreBuilder NotifyAfterCommit(string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException($"'{nameof(topicName)}' cannot be null or whitespace.", nameof(topicName));
            AddTopicEventProducer(topicName, AfterCommitStore.MatchesAll);
            return this;
        }

        /// <summary>
        /// After an successful call to <see cref="IEventWrite.Commit(CommitAttempt)"/> and
        /// the commited stream matches the provided <paramref name="streamIdentifier"/> 
        /// (see <see cref="CommitAttempt.StreamIdentifier"/> and <see cref="StreamIdentifier.Matches(StreamIdentifier)"/>),
        /// a message within the <see cref="IMessageBroker"/> gets published. The topic
        /// of the event will be the provided <paramref name="topicName"/>. The message
        /// will be an instance of <see cref="AfterCommitEvent"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Is thrown if <paramref name="topicName"/> is null or empty or whitespace only.</exception>
        public NotificationEventStoreBuilder NotifyAfterCommitForStream(StreamIdentifier streamIdentifier, string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException($"'{nameof(topicName)}' cannot be null or whitespace.", nameof(topicName));
            AddTopicEventProducer(topicName, BuildStreamIdentifierMatchPredicate(streamIdentifier));
            return this;
        }

        private void AddTopicEventProducer(string topicName, Func<CommitAttempt, bool> predicate)
        {
            ConfigureContainer((container) =>
            {
                IEventReadWrite existing = container.Resolve<IEventReadWrite>();
                IMessageBroker broker = container.Resolve<IMessageBroker>();
                IEventReadWrite overwritten = new AfterCommitStore(existing, topicName, broker, predicate);
                container.Register<IEventReadWrite>(overwritten);
            });
        }

        private Func<CommitAttempt, bool> BuildStreamIdentifierMatchPredicate(StreamIdentifier streamIdentifier)
        {
            return (attempt) => streamIdentifier.Matches(attempt.StreamIdentifier);
        }

        public override IEventStore Build()
        {
            return _inner.Build();
        }

        private class AfterCommitStore : IEventReadWrite
        {
            private readonly IEventReadWrite _inner;
            private readonly string _topicName;
            private readonly IMessageBroker _broker;
            private readonly Func<CommitAttempt, bool> _predicate;

            public static bool MatchesAll(CommitAttempt attempt)
            {
                return true;
            }

            public AfterCommitStore(IEventReadWrite inner, string topicName, IMessageBroker broker)
                : this(inner, topicName, broker, MatchesAll)
            {
            }

            public AfterCommitStore(IEventReadWrite inner, string topicName, IMessageBroker broker, Func<CommitAttempt, bool> predicate)
            {
                _inner = inner;
                _topicName = topicName;
                _broker = broker;
                _predicate = predicate;
            }

            public void Commit(CommitAttempt attempt)
            {
                _inner.Commit(attempt);
                Task.Run(() =>
                {
                    if (_predicate(attempt))
                        _broker.Publish<AfterCommitEvent>(new AfterCommitEvent(attempt), _topicName);
                });
            }

            public IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate)
            {
                return _inner.Read(predicate);
            }
        }
    }

    public class AfterCommitEvent
    {
        public AfterCommitEvent(CommitAttempt attempt)
        {
            Attempt = attempt;
        }

        public CommitAttempt Attempt { get; }

        public DateTime EventRaisedUtc { get; } = SystemClock.UtcNow;
    }
}
