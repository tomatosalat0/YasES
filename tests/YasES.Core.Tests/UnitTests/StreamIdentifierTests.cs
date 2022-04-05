using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class StreamIdentifierTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(" a")]
        [DataRow("a ")]
        [DataRow("$")]
        [DataRow("*")]
        [DataRow("asg*")]
        [DataRow("asg*sdh")]
        [DataRow(" a ")]
        public void StreamIdentifierDoesNotNullOrEmptyStrings(string valueToTest)
        {
            Assert.ThrowsException<ArgumentException>(() => StreamIdentifier.AllStreams(valueToTest));
            Assert.ThrowsException<ArgumentException>(() => StreamIdentifier.SingleStream(valueToTest, "stream"));
            Assert.ThrowsException<ArgumentException>(() => StreamIdentifier.SingleStream("bucket", valueToTest));
        }

        [TestMethod]
        [DataRow("a b")]
        [DataRow("atest$")]
        [DataRow("atest$sdhgsdh")]
        [DataRow("sdh'sdhds")]
        [DataRow("sdh?sdhds")]
        [DataRow("sdh\\sdhds")]
        [DataRow("sdh/sdhds")]
        [DataRow("sdh%sdhds")]
        [DataRow("sdh(sdhds")]
        [DataRow("sdh)sdhds")]
        [DataRow("sdh]sdhds")]
        [DataRow("sdh[sdhds")]
        public void StreamIdentifierAllowsSpecialCharactersWhenNotAtStart(string valueToTest)
        {
            Assert.AreEqual(valueToTest, StreamIdentifier.AllStreams(valueToTest).BucketId);
            Assert.AreEqual(valueToTest, StreamIdentifier.SingleStream(valueToTest, "stream").BucketId);
            Assert.AreEqual(valueToTest, StreamIdentifier.SingleStream("bucket", valueToTest).StreamId);
        }

        [TestMethod]
        public void WildcardStreamIdentifierSetsMatchesAllToTrue()
        {
            Assert.IsTrue(StreamIdentifier.AllStreams("bucket").MatchesAllStreams);
        }

        [TestMethod]
        public void SingleStreamIdentifierSetsMatchesAllToFalse()
        {
            Assert.IsFalse(StreamIdentifier.SingleStream("bucket", "stream").MatchesAllStreams);
        }

        [TestMethod]
        public void StreamIdentifierCompareOperatorTests()
        {
            StreamIdentifier id1 = StreamIdentifier.SingleStream("bucket1", "stream1");
            StreamIdentifier id2 = StreamIdentifier.SingleStream("bucket1", "stream2");
            StreamIdentifier id3 = StreamIdentifier.SingleStream("bucket2", "stream1");

            StreamIdentifier id4 = StreamIdentifier.AllStreams("bucket1");
            StreamIdentifier id5 = StreamIdentifier.AllStreams("bucket2");

            Assert.IsFalse(id1 == id2);
            Assert.IsFalse(id1 == id3);
            Assert.IsFalse(id1 == id4);
            Assert.IsFalse(id1 == id5);
            Assert.IsTrue(id1 != id2);
            Assert.IsTrue(id1 != id3);
            Assert.IsTrue(id1 != id4);
            Assert.IsTrue(id1 != id5);

            Assert.IsFalse(id4 == id5);
            Assert.IsTrue(id1 == StreamIdentifier.SingleStream("bucket1", "stream1"));
            Assert.IsTrue(id4 == StreamIdentifier.AllStreams("bucket1"));
        }

        [TestMethod]
        public void StreamIdentifierEqualsTests()
        {
            StreamIdentifier id1 = StreamIdentifier.SingleStream("bucket1", "stream1");
            StreamIdentifier id1Copy = StreamIdentifier.SingleStream("bucket1", "stream1");

            Assert.IsFalse(id1.Equals(id1.BucketId));
            Assert.IsFalse(id1.Equals(id1.StreamId));
            Assert.IsTrue(id1.Equals(id1Copy));
            Assert.IsFalse(id1.Equals(null));
            Assert.IsTrue(id1.Equals((object)id1Copy));
        }

        [TestMethod]
        public void StreamIdentifierToStringTests()
        {
            Assert.AreEqual("bucket1/stream1", StreamIdentifier.SingleStream("bucket1", "stream1").ToString());
            Assert.AreEqual("bucket1/*", StreamIdentifier.AllStreams("bucket1").ToString());
        }

        [TestMethod]
        public void StreamIdentifierMatchesSingleStream()
        {
            StreamIdentifier lhs = StreamIdentifier.SingleStream("bucket1", "stream1");
            Assert.IsTrue(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "stream1")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "stream12")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.SingleStream("bucket2", "stream1")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "Stream1")));
        }

        [TestMethod]
        public void StreamIdentifierMatchesAllStreamsInSameBucket()
        {
            StreamIdentifier lhs = StreamIdentifier.AllStreams("bucket1");
            Assert.IsTrue(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "stream1")));
            Assert.IsTrue(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "stream12")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.SingleStream("bucket2", "stream1")));
            Assert.IsTrue(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "Stream1")));
        }

        [TestMethod]
        public void StreamIdentifierMatchesPrefix()
        {
            StreamIdentifier lhs = StreamIdentifier.StreamsPrefixedWith("bucket1", "stream");
            Assert.IsTrue(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "stream1")));
            Assert.IsTrue(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "stream12")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.SingleStream("bucket2", "stream1")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.SingleStream("bucket1", "Stream1")));
        }

        [TestMethod]
        public void StreamIdentifierReturnsFalseIfOtherIsNotASingleStream()
        {
            StreamIdentifier lhs = StreamIdentifier.StreamsPrefixedWith("bucket1", "stream");
            Assert.IsFalse(lhs.Matches(StreamIdentifier.StreamsPrefixedWith("bucket1", "stream")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.StreamsPrefixedWith("bucket1", "stream1")));
            Assert.IsFalse(lhs.Matches(StreamIdentifier.AllStreams("bucket1")));
        }
    }
}
