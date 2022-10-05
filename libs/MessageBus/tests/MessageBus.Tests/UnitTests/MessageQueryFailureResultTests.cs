using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public class MessageQueryFailureResultTests
    {
        [TestMethod]
        public void FailureDoesntAcceptNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new IMessageQueryResult.FailureResult(MessageId.NewId(), null));
        }

        [TestMethod]
        public void FailureKeepsExceptionAndMessageId()
        {
            MessageId id = MessageId.NewId();
            Exception ex = new Exception();

            var failure = new IMessageQueryResult.FailureResult(id, ex);

            Assert.AreEqual(id, failure.MessageId);
            Assert.AreEqual(ex, failure.Exception);
        }
    }
}
