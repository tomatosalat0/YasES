using System;
using System.Threading;
using MessageBus;
using MessageBus.Messaging.InProcess;
using MessageBus.Messaging.InProcess.Scheduler;
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
            ManualScheduler manual = new ManualScheduler();
            using IEventStore eventStore = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .UseMessageBroker(MessageBrokerOptions.BlockingManual(manual))
                .NotifyAfterCommit()
                .Build();
            IMessageBus broker = eventStore.Services.Resolve<IMessageBus>();

            CommitAttempt commit = new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));
            int numberOfCalls = 0;
            DateTime mockTime = new DateTime(2000, 10, 10, 10, 10, 10, DateTimeKind.Utc);

            broker.RegisterEventDelegate<IAfterCommitEvent>((message) =>
            {
                Assert.AreSame(commit, message.Attempt);
                Assert.AreEqual(mockTime, message.EventRaisedUtc);
                numberOfCalls++;
            });

            SystemClock.ResolveUtcNow = () => mockTime;
            eventStore.Events.Commit(commit);

            // wait for commit event to actually execute.
            Thread.Sleep(200);

            // wait for broker
            manual.Drain();
            SystemClock.ResolveUtcNow = () => DateTime.UtcNow;

            Assert.AreEqual(1, numberOfCalls);
        }
    }
}
