using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class CorrelationEventCollectorTests
    {
        [TestMethod]
        public void CorrelationEventCollectorDoesNotAllowNullCorrelationId()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CorrelationEventCollector(null, new EventCollector()));
            Assert.ThrowsException<ArgumentNullException>(() => new CorrelationEventCollector("asds", null));
        }

        [TestMethod]
        public void CorrelationEventCollectorAssignsCorrelationIdIfNotSet()
        {
            string correlationIdValue = "correlationValue";

            IEventCollector collector = new CorrelationEventCollector(correlationIdValue, new EventCollector());
            collector.Add(new EventMessage("MyEvent", Memory<byte>.Empty));
            CommitAttempt commit = collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));

            Assert.AreEqual(correlationIdValue, commit.Messages[0].Headers[CommonMetaData.CorrelationId]);
        }

        [TestMethod]
        public void CorrelationEventCollectorAllowsMessagesWithAlreadySetCorrelationIdOfSameValue()
        {
            string correlationIdValue = "correlationValue";

            IEventCollector collector = new CorrelationEventCollector(correlationIdValue, new EventCollector());
            collector.Add(new EventMessage("MyEvent", new Dictionary<string, object>() { [CommonMetaData.CorrelationId] = correlationIdValue }, Memory<byte>.Empty));
            CommitAttempt commit = collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));

            Assert.AreEqual(correlationIdValue, commit.Messages[0].Headers[CommonMetaData.CorrelationId]);
        }

        [TestMethod]
        public void CorrelationEventCollectorThrowsExceptionIfMessageContainsDifferentCorrelationId()
        {
            string correlationIdValue = "correlationValue";

            IEventCollector collector = new CorrelationEventCollector(correlationIdValue, new EventCollector());
            Assert.ThrowsException<InvalidOperationException>(() =>
                collector.Add(new EventMessage("MyEvent", new Dictionary<string, object>() { [CommonMetaData.CorrelationId] = "A different value" }, Memory<byte>.Empty))
            );
        }

        [TestMethod]
        public void CorrelationEventCollectorPreservesExistingHeaderValues()
        {
            string correlationIdValue = "correlationValue";

            IEventCollector collector = new CorrelationEventCollector(correlationIdValue, new EventCollector());
            collector.Add(new EventMessage("MyEvent", new Dictionary<string, object>() { ["userHeader"] = "value" }, Memory<byte>.Empty));
            CommitAttempt commit = collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));

            Assert.AreEqual(correlationIdValue, commit.Messages[0].Headers[CommonMetaData.CorrelationId]);
            Assert.AreEqual("value", commit.Messages[0].Headers["userHeader"]);
        }

        [TestMethod]
        public void CorrelationEventColloectorPreservesEventMetaData()
        {
            int utcCalls = 0;
            SystemClock.ResolveUtcNow = () => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(++utcCalls);

            IEventMessage originalEvent = new EventMessage("MyEvent", Memory<byte>.Empty);
            IEventCollector collector = new CorrelationEventCollector("correlationValue", new EventCollector());
            collector.Add(originalEvent);
            CommitAttempt commit = collector.BuildCommit(StreamIdentifier.SingleStream("bucket", "stream"));

            Assert.AreEqual(originalEvent.EventName, commit.Messages[0].EventName);
            Assert.AreEqual(originalEvent.CreationDateUtc, commit.Messages[0].CreationDateUtc);
            Assert.AreEqual(originalEvent.Payload, commit.Messages[0].Payload);
        }
    }
}
