using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class EventCollectorTests
    {
        [TestMethod]
        public void EventCollectorIsEmptyAfterCreation()
        {
            EventCollector collector = new EventCollector();
            Assert.IsTrue(collector.IsEmpty);
        }

        [TestMethod]
        public void EventCollectorIsNotCommitedAfterCreation()
        {
            EventCollector collector = new EventCollector();
            Assert.IsFalse(collector.IsCommited);
        }

        [TestMethod]
        public void EmptyEventCollectorThrowsExceptionIfEmpty()
        {
            EventCollector collector = new EventCollector();
            Assert.ThrowsException<InvalidOperationException>(() => collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId()));
        }

        [TestMethod]
        public void EventCollectorBuildsCommitWithMessages()
        {
            EventCollector collector = new EventCollector();
            collector.Add(new EventMessage("Test", Memory<byte>.Empty));

            CommitId commitId = CommitId.NewId();
            StreamIdentifier streamId = StreamIdentifier.SingleStream("bucket", "stream");

            CommitAttempt commit = collector.BuildCommit(streamId, commitId);
            Assert.IsNotNull(commit);
            Assert.AreEqual(commitId, commit.CommitId);
            Assert.AreEqual(streamId, commit.StreamIdentifier);
        }

        [TestMethod]
        public void EventCollectorCommitWillSetCommitedToTrue()
        {
            EventCollector collector = new EventCollector();
            collector.Add(new EventMessage("Test", Memory<byte>.Empty));
            collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId());

            Assert.IsTrue(collector.IsCommited);
            Assert.IsFalse(collector.IsEmpty);
        }

        [TestMethod]
        public void EventCollectorCommitsWithEnumerableAddedMessages()
        {
            EventCollector collector = new EventCollector();
            collector.Add(new[] { new EventMessage("Test", Memory<byte>.Empty), new EventMessage("Test1", Memory<byte>.Empty) });

            CommitAttempt commit = collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId());
            Assert.AreEqual(2, commit.Messages.Count);
        }

        [TestMethod]
        public void EventCollectorCommitsWithParamsAddedMessages()
        {
            EventCollector collector = new EventCollector();
            collector.Add(new EventMessage("Test", Memory<byte>.Empty), new EventMessage("Test1", Memory<byte>.Empty));

            CommitAttempt commit = collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId());
            Assert.AreEqual(2, commit.Messages.Count);
        }

        [TestMethod]
        public void EventCollectorDoesNotAllowAddAfterCommited()
        {
            EventCollector collector = new EventCollector();
            collector.Add(new EventMessage("Test", Memory<byte>.Empty));
            collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId());

            Assert.ThrowsException<InvalidOperationException>(() => collector.Add(new EventMessage("Test", Memory<byte>.Empty)));
        }

        [TestMethod]
        public void EventCollectorBuildCanNotBeCalledMultipleTimes()
        {
            EventCollector collector = new EventCollector();
            collector.Add(new EventMessage("Test", Memory<byte>.Empty));
            collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId());

            Assert.ThrowsException<InvalidOperationException>(() => collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"), CommitId.NewId()));
        }
    }
}
