using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class CheckpointTokenTests
    {
        [TestMethod]
        public void CheckpointTokenDoesNotAllowNegativeNumbers()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CheckpointToken(-1));
        }

        [TestMethod]
        public void CheckpointTokenEqualsOperatorsTests()
        {
            Assert.IsTrue(new CheckpointToken(1) < new CheckpointToken(2));
            Assert.IsFalse(new CheckpointToken(1) < new CheckpointToken(1));
            
            Assert.IsTrue(new CheckpointToken(1) <= new CheckpointToken(2));
            Assert.IsTrue(new CheckpointToken(1) <= new CheckpointToken(1));

            Assert.IsTrue(new CheckpointToken(2) > new CheckpointToken(1));
            Assert.IsFalse(new CheckpointToken(1) > new CheckpointToken(1));

            Assert.IsTrue(new CheckpointToken(2) >= new CheckpointToken(1));
            Assert.IsTrue(new CheckpointToken(1) >= new CheckpointToken(1));

            Assert.IsTrue(new CheckpointToken(1) == new CheckpointToken(1));
            Assert.IsFalse(new CheckpointToken(1) == new CheckpointToken(2));

            Assert.IsTrue(new CheckpointToken(1) != new CheckpointToken(2));
            Assert.IsFalse(new CheckpointToken(2) != new CheckpointToken(2));
        }

        [TestMethod]
        public void CheckpintParsingTests()
        {
            Assert.AreEqual(new CheckpointToken(2), CheckpointToken.Parse("2"));
            Assert.ThrowsException<FormatException>(() => CheckpointToken.Parse(" 2"));
            Assert.ThrowsException<FormatException>(() => CheckpointToken.Parse("-2"));

            Assert.AreEqual(new CheckpointToken(2), CheckpointToken.Parse("2".AsSpan()));
            Assert.ThrowsException<FormatException>(() => CheckpointToken.Parse(" 2".AsSpan()));
            Assert.ThrowsException<FormatException>(() => CheckpointToken.Parse("-2".AsSpan()));

            Assert.IsTrue(CheckpointToken.TryParse("2", out var _));
            Assert.IsFalse(CheckpointToken.TryParse(" 2", out var _));
            Assert.IsFalse(CheckpointToken.TryParse("-2", out var _));

            Assert.IsTrue(CheckpointToken.TryParse("2".AsSpan(), out var _));
            Assert.IsFalse(CheckpointToken.TryParse(" 2".AsSpan(), out var _));
            Assert.IsFalse(CheckpointToken.TryParse("-2".AsSpan(), out var _));
        }

        [TestMethod]
        public void CheckpointEqualsTests()
        {
            CheckpointToken id1 = new CheckpointToken(2);
            CheckpointToken id1Copy = new CheckpointToken(id1.Value);

            Assert.IsFalse(id1.Equals(id1.Value));
            Assert.IsTrue(id1.Equals(id1Copy));
            Assert.IsFalse(id1.Equals(null));
            Assert.IsTrue(id1.Equals((object)id1Copy));
        }

        [TestMethod]
        public void CheckpointOverridenFrameworkMethodTests()
        {
            CheckpointToken id1 = new CheckpointToken(2);
            Assert.AreEqual(2L.GetHashCode(), id1.GetHashCode());
            Assert.AreEqual(2L.ToString(), id1.ToString());
        }

        [TestMethod]
        public void CheckpointMathOperatorTests()
        {
            Assert.AreEqual(new CheckpointToken(3), new CheckpointToken(2) + new CheckpointToken(1));
            Assert.AreEqual(new CheckpointToken(1), new CheckpointToken(4) - new CheckpointToken(3));
            Assert.AreEqual(new CheckpointToken(3), new CheckpointToken(2) + 1);
            Assert.AreEqual(new CheckpointToken(1), new CheckpointToken(4) - 3);
            Assert.AreEqual(new CheckpointToken(3), 2 + new CheckpointToken(1));
            Assert.AreEqual(new CheckpointToken(1), 4 - new CheckpointToken(3));
        }
    }
}
