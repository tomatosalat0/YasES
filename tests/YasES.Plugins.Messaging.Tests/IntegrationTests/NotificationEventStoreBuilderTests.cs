using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YasES.Core;

namespace YasES.Plugins.Messaging.Tests.IntegrationTests
{
    [TestClass]
    public class NotificationEventStoreBuilderTests
    {
        [TestMethod]
        public void GlobalCommitCompleteEventGetsRaisedAfterConfigure()
        {
            const string TopicName = "EventStore/AfterCommit";

            ManualBrokerScheduler manual = null;
            using IEventStore eventStore = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .UseMessageBroker((c) => (manual = new ManualBrokerScheduler(c)) as IDisposable)
                .NotifyAfterCommit(TopicName)
                .Build();
            IMessageBroker broker = eventStore.Services.Resolve<IMessageBroker>();

            CommitAttempt commit = new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));
            int numberOfCalls = 0;
            DateTime mockTime = new DateTime(2000, 10, 10, 10, 10, 10, DateTimeKind.Utc);

            broker.Channel(TopicName).Subscribe<AfterCommitEvent>((message) =>
            {
                Assert.AreSame(commit, message.Payload.Attempt);
                Assert.AreEqual(mockTime, message.Payload.EventRaisedUtc);
                numberOfCalls++;
            });

            SystemClock.ResolveUtcNow = () => mockTime;
            eventStore.Events.Commit(commit);

            // wait for commit event to actually execute.
            Thread.Sleep(200);

            // wait for broker
            manual.CallSubscribers();
            SystemClock.ResolveUtcNow = () => DateTime.UtcNow;

            Assert.AreEqual(1, numberOfCalls);
        }
    }
}
