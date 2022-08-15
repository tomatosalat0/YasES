using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasES.Plugins.Messaging;

namespace YasES.Core
{
    public sealed class NotificationEventStoreBuilder : EventStoreBuilder
    {
        public NotificationEventStoreBuilder(EventStoreBuilder parent, Func<IBrokerCommands, IDisposable?> initialization)
            : base(parent)
        {
            if (initialization is null) throw new ArgumentNullException(nameof(initialization));
            ConfigureServices((container) =>
            {
                IMessageBroker broker = new MessageBroker(initialization);
                container.RegisterSingleton<IMessageBroker>(broker);
            });
        }

        /// <summary>
        /// After every successful <see cref="IEventWrite.Commit(CommitAttempt)"/>,
        /// a message wihtin the <see cref="IMessageBroker"/> gets published. The topic
        /// of the event will be the provided <paramref name="topicName"/>. The message
        /// will be an instance of <see cref="AfterCommitEvent"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Is thrown if <paramref name="topicName"/> is null or empty or whitespace only.</exception>
        public NotificationEventStoreBuilder NotifyAfterCommit(TopicName topicName)
        {
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
        public NotificationEventStoreBuilder NotifyAfterCommitForStream(StreamIdentifier streamIdentifier, TopicName topicName)
        {
            AddTopicEventProducer(topicName, BuildStreamIdentifierMatchPredicate(streamIdentifier));
            return this;
        }

        private void AddTopicEventProducer(TopicName topicName, Func<CommitAttempt, bool> predicate)
        {
            ConfigureServices((services) =>
            {
                IEventReadWrite existing = services.Resolve<IEventReadWrite>();
                IMessageBroker broker = services.Resolve<IMessageBroker>();
                IEventReadWrite overwritten = new AfterCommitStore(existing, topicName, broker, predicate);
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
            private readonly TopicName _topicName;
            private readonly IMessageBroker _broker;
            private readonly Func<CommitAttempt, bool> _predicate;

            public static bool MatchesAll(CommitAttempt attempt)
            {
                return true;
            }

            public AfterCommitStore(IEventReadWrite inner, TopicName topicName, IMessageBroker broker)
                : this(inner, topicName, broker, MatchesAll)
            {
            }

            public AfterCommitStore(IEventReadWrite inner, TopicName topicName, IMessageBroker broker, Func<CommitAttempt, bool> predicate)
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
