using System;
using System.Collections.Generic;
using System.Linq;
using YasES.Persistance.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests.Persistance.InMemory
{
    [TestClass]
    public class InMemoryPersistanceEngineTests
    {
        [TestMethod]
        public void EmptyEngineReturnsEmptyOnRead()
        {
            IEventReadWrite engine = new InMemoryPersistanceEngine();

            ReadPredicate singleStream = ReadPredicateBuilder.Forwards(StreamIdentifier.SingleStream("test", "test"));
            Assert.AreEqual(0, engine.Read(singleStream).Count());

            ReadPredicate multipleStreams = ReadPredicateBuilder.Forwards(
                StreamIdentifier.SingleStream("test", "test")
            );
            Assert.AreEqual(0, engine.Read(multipleStreams).Count());
        }

        [TestMethod]
        public void EngineReturnsSingleAppendedMessages()
        {
            int utcCalls = 0;
            SystemClock.ResolveUtcNow = () => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(++utcCalls);
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            StreamIdentifier stream = StreamIdentifier.SingleStream("test", "test");
            IEventMessage @event = new EventMessage("MyEvent", Memory<byte>.Empty);
            CommitId commitId = CommitId.NewId();

            engine.Commit(new CommitAttempt(
                stream,
                commitId,
                new[] { @event }
            ));

            IReadEventMessage received = engine.Read(ReadPredicateBuilder.Forwards(stream)).Single();
            Assert.AreEqual(@event.EventName, received.EventName);
            Assert.AreEqual(commitId, received.CommitId);
            Assert.AreEqual(new CheckpointToken(1), received.Checkpoint);
            Assert.AreEqual(new DateTime(2000, 1, 4, 0, 0, 0, DateTimeKind.Utc), received.CommitTimeUtc);
            Assert.AreEqual(new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc), received.CreationDateUtc);
        }

        [TestMethod]
        public void EngineReturnsMultipleMessagesFromSingleCommit()
        {
            int utcCalls = 0;
            SystemClock.ResolveUtcNow = () => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(++utcCalls);
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            StreamIdentifier stream = StreamIdentifier.SingleStream("test", "test");
            IEventMessage event1 = new EventMessage("MyEvent1", Memory<byte>.Empty);
            IEventMessage event2 = new EventMessage("MyEvent2", Memory<byte>.Empty);
            CommitId commitId = CommitId.NewId();

            engine.Commit(new CommitAttempt(
                stream,
                commitId,
                new[] { event1, event2 }
            ));

            List<IReadEventMessage> received = engine.Read(ReadPredicateBuilder.Forwards(stream)).ToList();
            Assert.AreEqual(2, received.Count);

            Assert.AreEqual(event1.EventName, received[0].EventName);
            Assert.AreEqual(commitId, received[0].CommitId);
            Assert.AreEqual(new CheckpointToken(1), received[0].Checkpoint);
            Assert.AreEqual(new DateTime(2000, 1, 5, 0, 0, 0, DateTimeKind.Utc), received[0].CommitTimeUtc);
            Assert.AreEqual(new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc), received[0].CreationDateUtc);

            Assert.AreEqual(event2.EventName, received[1].EventName);
            Assert.AreEqual(commitId, received[1].CommitId);
            Assert.AreEqual(new CheckpointToken(2), received[1].Checkpoint);
            Assert.AreEqual(new DateTime(2000, 1, 5, 0, 0, 0, DateTimeKind.Utc), received[1].CommitTimeUtc);
            Assert.AreEqual(new DateTime(2000, 1, 3, 0, 0, 0, DateTimeKind.Utc), received[1].CreationDateUtc);
        }

        [TestMethod]
        public void EngineReturnsEventsFromMultipleStreamsInOrderTheyWereCommited()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            StreamIdentifier stream2 = StreamIdentifier.SingleStream("test", "test2");
            IEventMessage event1 = new EventMessage("MyEvent1", Memory<byte>.Empty);
            IEventMessage event2 = new EventMessage("MyEvent2", Memory<byte>.Empty);
            IEventMessage event3 = new EventMessage("MyEvent3", Memory<byte>.Empty);
            CommitAttempt commit1 = new CommitAttempt(stream1, CommitId.NewId(), new[] { event1 });
            CommitAttempt commit2 = new CommitAttempt(stream2, CommitId.NewId(), new[] { event2 });
            CommitAttempt commit3 = new CommitAttempt(stream1, CommitId.NewId(), new[] { event3 });
            IEventReadWrite engine = new InMemoryPersistanceEngine();

            engine.Commit(commit1);
            engine.Commit(commit2);
            engine.Commit(commit3);

            List<IReadEventMessage> received = engine.Read(ReadPredicateBuilder.Forwards(stream1, stream2)).ToList();
            Assert.AreEqual(3, received.Count);

            Assert.AreEqual(event1.EventName, received[0].EventName);
            Assert.AreEqual(event2.EventName, received[1].EventName);
            Assert.AreEqual(event3.EventName, received[2].EventName);
        }

        [TestMethod]
        public void EngineReturnsOnlyMatchingEventNames()
        {
            CommitAttempt commit = new EventCollector()
                .Add(new EventMessage("MyEvent1", Memory<byte>.Empty))
                .Add(new EventMessage("MyEvent2", Memory<byte>.Empty))
                .Add(new EventMessage("MyEvent3", Memory<byte>.Empty))
                .BuildCommit(StreamIdentifier.SingleStream("test", "test1"));
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            engine.Commit(commit);

            List<IReadEventMessage> messages = engine.Read(
                ReadPredicateBuilder.Custom().FromAllStreamsInBucket("test").ReadForwards().OnlyIncluding("MyEvent1", "MyEvent2").WithoutCheckpointLimit().Build()
            ).ToList();

            Assert.AreEqual(2, messages.Count);
        }

        [TestMethod]
        public void EngineReturnsAllEventsExcludingTheFilteredEvents()
        {
            CommitAttempt commit = new EventCollector()
                .Add(new EventMessage("MyEvent1", Memory<byte>.Empty))
                .Add(new EventMessage("MyEvent2", Memory<byte>.Empty))
                .Add(new EventMessage("MyEvent3", Memory<byte>.Empty))
                .BuildCommit(StreamIdentifier.SingleStream("test", "test1"));
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            engine.Commit(commit);

            List<IReadEventMessage> messages = engine.Read(
                ReadPredicateBuilder.Custom().FromAllStreamsInBucket("test").ReadForwards().AllExcluding("MyEvent1", "MyEvent2").WithoutCheckpointLimit().Build()
            ).ToList();

            Assert.AreEqual(1, messages.Count);
        }

        [TestMethod]
        public void EngineReturnsEventsOnceIfStreamIsRequestedMultipleTimes()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            IEventMessage event1 = new EventMessage("MyEvent1", Memory<byte>.Empty);
            CommitAttempt commit1 = new CommitAttempt(stream1, CommitId.NewId(), new[] { event1 });
            IEventReadWrite engine = new InMemoryPersistanceEngine();

            engine.Commit(commit1);

            List<IReadEventMessage> received = engine.Read(ReadPredicateBuilder.Forwards(stream1, stream1)).ToList();
            Assert.AreEqual(1, received.Count);
        }

        [TestMethod]
        public void EngineRunsCommitOnlyOnce()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            IEventMessage event1 = new EventMessage("MyEvent1", Memory<byte>.Empty);
            CommitAttempt commit1 = new CommitAttempt(stream1, CommitId.NewId(), new[] { event1 });
            IEventReadWrite engine = new InMemoryPersistanceEngine();

            engine.Commit(commit1);
            engine.Commit(commit1);
            engine.Commit(commit1);

            List<IReadEventMessage> received = engine.Read(ReadPredicateBuilder.Forwards(stream1)).ToList();
            Assert.AreEqual(1, received.Count);
        }

        [TestMethod]
        public void EngineReturnsEventsBackwardsFromMultipleStreamsInReverseOrderTheyWereCommited()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            StreamIdentifier stream2 = StreamIdentifier.SingleStream("test", "test2");
            IEventMessage event1 = new EventMessage("MyEvent1", Memory<byte>.Empty);
            IEventMessage event2 = new EventMessage("MyEvent2", Memory<byte>.Empty);
            IEventMessage event3 = new EventMessage("MyEvent3", Memory<byte>.Empty);
            CommitAttempt commit1 = new CommitAttempt(stream1, CommitId.NewId(), new[] { event1 });
            CommitAttempt commit2 = new CommitAttempt(stream2, CommitId.NewId(), new[] { event2 });
            CommitAttempt commit3 = new CommitAttempt(stream1, CommitId.NewId(), new[] { event3 });
            IEventReadWrite engine = new InMemoryPersistanceEngine();

            engine.Commit(commit1);
            engine.Commit(commit2);
            engine.Commit(commit3);

            List<IReadEventMessage> received = engine.Read(ReadPredicateBuilder.Backwards(stream2, stream1)).ToList();
            Assert.AreEqual(3, received.Count);

            Assert.AreEqual(event3.EventName, received[0].EventName);
            Assert.AreEqual(event2.EventName, received[1].EventName);
            Assert.AreEqual(event1.EventName, received[2].EventName);
        }

        [TestMethod]
        public void EngineOnlyReturnsRequestedEvents()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            for (int i = 0; i < 10; i++)
            {
                engine.Commit(stream1, new[] { new EventMessage($"MyEvent{i}", Memory<byte>.Empty) });
            }

            Assert.AreEqual(0, engine.ReadForwardFrom(stream1, new CheckpointToken(100)).Count());
            Assert.AreEqual(5, engine.ReadForwardFrom(stream1, new CheckpointToken(5)).Count());
            Assert.AreEqual(2, engine.ReadForwardFromTo(stream1, CheckpointToken.Beginning, new CheckpointToken(3)).Count());
            Assert.AreEqual(0, engine.ReadForwardFromTo(stream1, new CheckpointToken(1), new CheckpointToken(2)).Count());

            Assert.AreEqual(0, engine.ReadBackwardFrom(stream1, new CheckpointToken(1)).Count());
            Assert.AreEqual(10, engine.ReadBackwardFrom(stream1, CheckpointToken.Ending).Count());
            Assert.AreEqual(5, engine.ReadBackwardFromTo(stream1, new CheckpointToken(5), CheckpointToken.Ending).Count());
            Assert.AreEqual(1, engine.ReadBackwardFromTo(stream1, CheckpointToken.Beginning, new CheckpointToken(2)).Count());
        }

        [TestMethod]
        public void EngineOnlyReturnsPointInTimeViewWhenReading()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            IEventReadWrite engine = new InMemoryPersistanceEngine();

            engine.Commit(stream1, new EventMessage("MyEvent1", Memory<byte>.Empty));
            IEnumerable<IReadEventMessage> read = engine.Read(ReadPredicateBuilder.Forwards(stream1));
            engine.Commit(stream1, new EventMessage("MyEvent2", Memory<byte>.Empty));

            Assert.AreEqual(1, read.Count());
        }

        [TestMethod]
        public void EngineOnlyReturnsEventsWithSameCorrelationId()
        {
            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            string correlationId = Guid.NewGuid().ToString();

            engine.Commit(stream1, new EventMessage("MyEvent1", new Dictionary<string, object>() { [CommonMetaData.CorrelationId] = correlationId }, Memory<byte>.Empty));
            engine.Commit(stream1, new EventMessage("MyEvent1", new Dictionary<string, object>() { [CommonMetaData.CorrelationId] = Guid.NewGuid().ToString() }, Memory<byte>.Empty));

            var predicate = ReadPredicateBuilder.Custom()
                .FromStream(stream1)
                .ReadForwards()
                .IncludeAllEvents()
                .HavingTheCorrelationId(correlationId)
                .Build();

            List<IReadEventMessage> read = engine.Read(predicate).ToList();
            Assert.AreEqual(1, read.Count);
            Assert.AreEqual(correlationId, read[0].Headers[CommonMetaData.CorrelationId]);
        }

        [TestMethod]
        public void EngineReturnsStreamsWithTheSamePrefix()
        {
            IEventReadWrite engine = new InMemoryPersistanceEngine();
            engine.Commit(new EventCollector().Add(new EventMessage("event1", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "A/stream1")));
            engine.Commit(new EventCollector().Add(new EventMessage("event2", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "A/stream2")));
            engine.Commit(new EventCollector().Add(new EventMessage("event3", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "B/stream3")));
            engine.Commit(new EventCollector().Add(new EventMessage("event3", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("bucket", "a/stream4")));
            engine.Commit(new EventCollector().Add(new EventMessage("event5", Memory<byte>.Empty)).BuildCommit(StreamIdentifier.SingleStream("different", "A/stream5")));

            var predicate = ReadPredicateBuilder.Forwards(StreamIdentifier.StreamsPrefixedWith("bucket", "A/"));
            List<IReadEventMessage> read = engine.Read(predicate).ToList();
            Assert.AreEqual(2, read.Count);
            Assert.AreEqual("A/stream1", read[0].StreamIdentifier.StreamId);
            Assert.AreEqual("A/stream2", read[1].StreamIdentifier.StreamId);
        }
    }
}
