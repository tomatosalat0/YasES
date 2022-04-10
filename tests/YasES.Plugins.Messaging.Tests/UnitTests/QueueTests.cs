using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Plugins.Messaging.Tests.UnitTests
{
    [TestClass]
    public class QueueTests
    {
        [TestMethod]
        public void QueueConstructorThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Queue(null));
        }

        [TestMethod]
        public void NewlyCreatedQueueIsEmpty()
        {
            Queue queue = new Queue((_) => { });
            Assert.IsTrue(queue.IsEmpty);
        }

        [TestMethod]
        public void EmptyQueueDoesntSendCallbackAction()
        {
            int numberOfCalls = 0;
            Queue queue = new Queue((_) => numberOfCalls++);
            Assert.IsFalse(queue.Send());
            Assert.AreEqual(0, numberOfCalls);
        }

        [TestMethod]
        public void QueueDoesntDirectlyCallCallbackMethodWhenAnItemGetsEnqueued()
        {
            int numberOfCalls = 0;
            Queue queue = new Queue((_) => numberOfCalls++);
            queue.Enqueue(new object());
            Assert.AreEqual(0, numberOfCalls);
        }

        [TestMethod]
        public void QueueThrowsExceptionIfNullIsEnqueued()
        {
            Queue queue = new Queue((_) => { });
            Assert.ThrowsException<ArgumentNullException>(() => queue.Enqueue(null));
        }

        [TestMethod]
        public void QueueCallsSendCallbackIfNotEmptyAndSendIsCalled()
        {
            int numberOfCalls = 0;
            object message = new object();

            Queue queue = new Queue((m) =>
            {
                Assert.AreSame(message, m.Payload);
                numberOfCalls++;
            });
            queue.Enqueue(message);
            Assert.IsTrue(queue.Send());

            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void QueueReturnsMessagesInTheOrderTheyWereEnqueued()
        {
            int index = 0;
            object[] messages = new object[] { new object(), new object() };

            Queue queue = new Queue(m => Assert.AreSame(messages[index++], m.Payload));
            queue.Enqueue(messages[0]);
            queue.Enqueue(messages[1]);

            Assert.IsTrue(queue.Send());
            Assert.IsTrue(queue.Send());
        }

        [TestMethod]
        public void ExceptionsInSubscriptionsStillDrainsTheQueue()
        {
            Queue queue = new Queue(m => throw new Exception());
            queue.Enqueue(new object());

            Assert.IsTrue(queue.Send());
            Assert.IsTrue(queue.IsEmpty);
        }
    }
}
