using System;
using MessageBus.Messaging.InProcess.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Messaging.Tests.UnitTests.Messages
{
    [TestClass]
    public class MessageTests
    {
        [TestMethod]
        public void NewMessageIsNotAcknowldegedOrUnAcknowledged()
        {
            Message message = new Message(payload: null);
            Assert.AreEqual(MessageState.Initial, message.State);
        }

        [TestMethod]
        public void AcknowledgeSetsAcknowledgeFlag()
        {
            Message message = new Message(payload: null);
            message.Ack();
            Assert.AreEqual(MessageState.Acknowledged, message.State);
        }

        [TestMethod]
        public void AcknowledgeCanGetCalledMultipleTimes()
        {
            Message message = new Message(payload: null);
            message.Ack();
            message.Ack();
            message.Ack();
            Assert.AreEqual(MessageState.Acknowledged, message.State);
        }

        [TestMethod]
        public void NotAcknowldgeSetsNotAcknowledgedFlag()
        {
            Message message = new Message(payload: null);
            message.Nack();
            Assert.AreEqual(MessageState.NotAcknowledged, message.State);
        }

        [TestMethod]
        public void NotAcknowledgeCanGetCalledMultipleTimes()
        {
            Message message = new Message(payload: null);
            message.Nack();
            message.Nack();
            message.Nack();
            Assert.AreEqual(MessageState.NotAcknowledged, message.State);
        }

        [TestMethod]
        public void AcknowlegeThrowsExceptionOfNotAcknowledgedMessage()
        {
            Message message = new Message(payload: null);
            message.Nack();
            Assert.ThrowsException<InvalidOperationException>(() => message.Ack());
        }

        [TestMethod]
        public void NotAcknowlegeThrowsExceptionIfAcknowldgedMessage()
        {
            Message message = new Message(payload: null);
            message.Ack();
            Assert.ThrowsException<InvalidOperationException>(() => message.Nack());
        }

        [TestMethod]
        public void MessageToGenericMessageCastingPreservesState()
        {
            Message<string> originalMessage = new Message<string>("My Message");
            originalMessage.Ack();

            Message castedMessage = originalMessage;
            Assert.AreEqual(MessageState.Acknowledged, castedMessage.State);
            Assert.AreSame(originalMessage.Payload, castedMessage.Payload);
        }

        [TestMethod]
        public void GenericMessageToMessageCastingPreservesState()
        {
            Message originalMessage = new Message("My Message");
            originalMessage.Ack();

            Message<string> castedMessage = originalMessage;
            Assert.AreEqual(MessageState.Acknowledged, castedMessage.State);
            Assert.AreSame(originalMessage.Payload, castedMessage.Payload);
        }
    }
}
