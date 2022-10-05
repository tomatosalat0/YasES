using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public class MessageIdTests
    {
        [TestMethod]
        public void MessageIdCanNotBeCreatedWithNoId()
        {
            Assert.ThrowsException<ArgumentException>(() => new MessageId(null));
            Assert.ThrowsException<ArgumentException>(() => new MessageId(string.Empty));
        }

        [TestMethod]
        public void NewMessageIdHasNoCausation()
        {
            MessageId id = MessageId.NewId();
            Assert.IsNull(id.CausationId);
        }

        [TestMethod]
        public void MessageWithCorrelationSetsCausation()
        {
            MessageId causation = MessageId.NewId();
            MessageId id = MessageId.CausedBy(causation);

            Assert.AreEqual(id.CausationId, causation.Value);
        }

        [TestMethod]
        public void MessageIdEqualityCheck()
        {
            MessageId idA = new MessageId("Test");
            MessageId idB = new MessageId("Test");

            Assert.AreEqual(idA, idB);
            Assert.IsTrue(idA.Equals(idB));
            Assert.IsFalse(idA != idB);
            Assert.IsTrue(idA == idB);
        }

        [TestMethod]
        public void MessageIdEqualityOnlyAcceptsOtherMessageIds()
        {
            MessageId id = new MessageId();
            Assert.IsFalse(id.Equals(id.Value));
        }

        [TestMethod]
        public void MessageIdStringContainsCausation()
        {
            MessageId causation = MessageId.NewId();
            MessageId id = MessageId.CausedBy(causation);

            Assert.IsTrue(id.ToString().Contains(causation.Value));
            Assert.IsTrue(id.ToString().Contains(id.Value));
        }

        [TestMethod]
        public void MessageIdOnlyContainsIdWithNoCausation()
        {
            MessageId id = MessageId.NewId();
            Assert.AreEqual(id.Value, id.ToString());
        }

        [TestMethod]
        public void MessageIdEqualityWorksAsExpected()
        {
            MessageId idA = MessageId.NewId();
            MessageId idB = MessageId.NewId();

            HashSet<MessageId> messages = new HashSet<MessageId>();
            Assert.IsTrue(messages.Add(idA));
            Assert.IsTrue(messages.Add(idB));
            Assert.IsFalse(messages.Add(idA));
            Assert.IsFalse(messages.Add(idB));
        }
    }
}
