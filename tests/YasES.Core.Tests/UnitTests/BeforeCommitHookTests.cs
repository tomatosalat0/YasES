using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class BeforeCommitHookTests
    {
        [TestMethod]
        public void BeforeCommitThrowsArgumentNullExceptionIfHookIsNull()
        {
            EventStoreBuilder builder = EventStoreBuilder.Init()
                .UseInMemoryPersistance();

            Assert.ThrowsException<ArgumentNullException>(() => builder.WithBeforeCommitHook(null));
        }

        [TestMethod]
        public void BeforeCommitHookGetsExecutedOnceIfRegisteredOnce()
        {
            CommitCounterHook counter = new CommitCounterHook();
            IEventStore store = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .WithBeforeCommitHook(counter)
                .Build();

            CommitAttempt commit = new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));
            store.Events.Commit(commit);

            Assert.AreEqual(1, counter.CallCounter);
        }

        [TestMethod]
        public void BeforeCommitHookGetsExecutedForEveryCommit()
        {
            CommitCounterHook counter = new CommitCounterHook();
            IEventStore store = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .WithBeforeCommitHook(counter)
                .Build();

            store.Events.Commit(new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream")));
            store.Events.Commit(new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream")));
            store.Events.Commit(new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream")));

            Assert.AreEqual(3, counter.CallCounter);
        }

        [TestMethod]
        public void MultipleBeforeCommitHooksGetExecuted()
        {
            CommitCounterHook counter1 = new CommitCounterHook();
            CommitCounterHook counter2 = new CommitCounterHook();
            IEventStore store = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .WithBeforeCommitHook(counter1)
                .WithBeforeCommitHook(counter2)
                .Build();

            store.Events.Commit(new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream")));

            Assert.AreEqual(1, counter1.CallCounter);
            Assert.AreEqual(1, counter2.CallCounter);
        }

        [TestMethod]
        public void MultipleBeforeCommitHooksGetExecutedInReverseRegistrationOrder()
        {
            bool hook2Executed = false;
            CallbackCommitHook callbackHook1 = new CallbackCommitHook(() => Assert.IsTrue(hook2Executed));
            CallbackCommitHook callbackHook2 = new CallbackCommitHook(() => hook2Executed = true);

            IEventStore store = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .WithBeforeCommitHook(callbackHook1)
                .WithBeforeCommitHook(callbackHook2)
                .Build();

            store.Events.Commit(new EventCollector().Add(new EventMessage("MyEvent", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "stream")));

            Assert.IsTrue(hook2Executed);
        }

        private class CallbackCommitHook : IBeforeCommitHook
        {
            private readonly Action _callback;

            public CallbackCommitHook(Action callback)
            {
                _callback = callback;
            }

            public CommitAttempt BeforeCommit(CommitAttempt attempt)
            {
                _callback();
                return attempt;
            }
        }

        private class CommitCounterHook : IBeforeCommitHook
        {
            public int CallCounter;

            public CommitAttempt BeforeCommit(CommitAttempt attempt)
            {
                CallCounter++;
                return attempt;
            }
        }
    }
}
