using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class CommitIdTests
    {
        [TestMethod]
        public void CommitIdDoesNotAllowGuidEmpty()
        {
            Assert.ThrowsException<ArgumentException>(() => new CommitId(Guid.Empty));
        }

        [TestMethod]
        public void CommitIdGeneratesNewGuid()
        {
            Assert.AreNotEqual(Guid.Empty, CommitId.NewId().Value);
        }

        [TestMethod]
        public void CommitIdParseTests()
        {
            Assert.ThrowsException<ArgumentException>(() => CommitId.Parse(Guid.Empty.ToString()));
            Assert.ThrowsException<FormatException>(() => CommitId.Parse("asgdg"));

            Guid id = Guid.NewGuid();
            Assert.AreEqual(id, CommitId.Parse(id.ToString()).Value);
            Assert.AreEqual(id, CommitId.Parse(id.ToString().AsSpan()).Value);

            Assert.IsTrue(CommitId.TryParse(id.ToString(), out var _));
            Assert.IsFalse(CommitId.TryParse(Guid.Empty.ToString(), out var _));
            Assert.IsFalse(CommitId.TryParse("agsdg", out var _));

            Assert.IsTrue(CommitId.TryParse(id.ToString().AsSpan(), out var _));
            Assert.IsFalse(CommitId.TryParse(Guid.Empty.ToString().AsSpan(), out var _));
            Assert.IsFalse(CommitId.TryParse("agsdg".AsSpan(), out var _));
        }

        [TestMethod]
        public void CommitIdCompareOperatorTests()
        {
            CommitId id1 = CommitId.NewId();
            CommitId id2 = CommitId.NewId();

            CommitId id1Copy = new CommitId(id1.Value);
            Assert.IsFalse(id1 == id2);
            Assert.IsTrue(id1 != id2);
            Assert.IsTrue(id1 == id1Copy);
        }

        [TestMethod]
        public void CommitIdEqualsTests()
        {
            CommitId id1 = CommitId.NewId();
            CommitId id1Copy = new CommitId(id1.Value);


            Assert.IsFalse(id1.Equals(id1.Value));
            Assert.IsTrue(id1.Equals(id1Copy));
            Assert.IsFalse(id1.Equals(null));
            Assert.IsTrue(id1.Equals((object)id1Copy));
        }
    }
}
