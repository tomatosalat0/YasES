using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Plugins.Messaging.Tests.UnitTests
{
    [TestClass]
    public class ChannelTests
    {
        [TestMethod]
        public void ChannelThrowsArgumentNullExceptionWhenNoCallbackIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Channel(null));
        }

        [TestMethod]
        public void NewlyCreatedChannelIsEmpty()
        {
            Channel channel = new Channel((_) => { });
            Assert.IsTrue(channel.IsEmpty);
        }

        [TestMethod]
        public void SubscribingToChannelIsNotEmpty()
        {
            Channel channel = new Channel((_) => { });
            channel.Subscribe<object>((_) => { });
            Assert.IsFalse(channel.IsEmpty);
        }

        [TestMethod]
        public void SubscribeThrowsArgumentNullException()
        {
            Channel channel = new Channel((_) => { });
            Assert.ThrowsException<ArgumentNullException>(() => channel.Subscribe<object>(null));
        }

        [TestMethod]
        public void DisposingASubscriptionMakesChannelEmpty()
        {
            Channel channel = new Channel((_) => { });
            IDisposable subscription = channel.Subscribe<object>((_) => { });
            subscription.Dispose();
            Assert.IsTrue(channel.IsEmpty);
        }

        [TestMethod]
        public void SnapshotOfEmptyChannelIsEmpty()
        {
            Channel channel = new Channel((_) => { });
            IReadOnlyList<Queue> snapshot = channel.CreateSnapshotOfAllQueues();
            Assert.AreEqual(0, snapshot.Count);
        }

        [TestMethod]
        public void SnapshotOfNonEmptyChannelIsNotEmpty()
        {
            Channel channel = new Channel((_) => { });
            channel.Subscribe<object>((_) => { });
            IReadOnlyList<Queue> snapshot = channel.CreateSnapshotOfAllQueues();
            Assert.AreEqual(1, snapshot.Count);
        }

        [TestMethod]
        public void PublishingToAnEmptyChannelKeepsItEmpty()
        {
            Channel channel = new Channel((_) => { });
            channel.Publish(new object());
            Assert.IsTrue(channel.IsEmpty);
        }

        [TestMethod]
        public void PublishingNullThrowsAnException()
        {
            Channel channel = new Channel((_) => { });
            Assert.ThrowsException<ArgumentNullException>(() => channel.Publish<object>(null));
        }

        [TestMethod]
        public void PublishingToAnEmptyChannelDoesNotCallNotificationCallback()
        {
            int numberOfCalls = 0;
            Channel channel = new Channel((_) => numberOfCalls++);
            channel.Publish(new object());
            Assert.AreEqual(0, numberOfCalls);
        }

        [TestMethod]
        public void PublishingToANonEmptyChannelExecutesCallback()
        {
            int numberOfCalls = 0;
            Channel channel = new Channel((_) => numberOfCalls++);
            channel.Subscribe<object>((_) => { });
            channel.Publish(new object());
            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void ExceptionsInChangeNotificationCallbackAreCatched()
        {
            Channel channel = new Channel((_) => throw new Exception());
            channel.Subscribe<object>((_) => { });
            channel.Publish(new object());
        }

        [TestMethod]
        public void SubscriptionIsNotCalledDirectlyWhenPublishGetsCalled()
        {
            Channel channel = new Channel((_) => { });

            int numberOfCalls = 0;
            channel.Subscribe<object>((_) => { numberOfCalls++; });
            channel.Publish(new object());

            Assert.AreEqual(0, numberOfCalls);
        }

        [TestMethod]
        public void SubscriptionAreCallNotCalledIfQueuesAreEmpty()
        {
            Channel channel = new Channel((_) => { });

            int numberOfCalls = 0;
            channel.Subscribe<object>((_) => { numberOfCalls++; });

            Assert.AreEqual(0, channel.SendQueueMessages());
            Assert.AreEqual(0, numberOfCalls);
        }

        [TestMethod]
        public void SubscriptionIsCalledOnlyForOneMessagePerRound()
        {
            Channel channel = new Channel((_) => { });

            int numberOfCalls = 0;
            channel.Subscribe((_) => { numberOfCalls++; });
            channel.Publish(new object());
            channel.Publish(new object());

            Assert.AreEqual(1, channel.SendQueueMessages());
            Assert.AreEqual(1, numberOfCalls);
        }

        [TestMethod]
        public void OneSendingRoundTouchesAllActiveQueues()
        {
            Channel channel = new Channel((_) => { });

            int numberOfCalls = 0;
            channel.Subscribe((_) => { numberOfCalls++; });
            channel.Subscribe((_) => { numberOfCalls++; });
            channel.Publish(new object());
            channel.Publish(new object());

            Assert.AreEqual(2, channel.SendQueueMessages());
            Assert.AreEqual(2, numberOfCalls);
        }

        [TestMethod]
        public void EmptySubscriberListWillNotIncreaseCallCount()
        {
            Channel channel = new Channel((_) => { });

            int numberOfCalls = 0;
            IDisposable subscription = channel.Subscribe<object>((_) => { numberOfCalls++; });
            subscription.Dispose();

            channel.Publish(new object());
            channel.Publish(new object());

            Assert.AreEqual(0, channel.SendQueueMessages());
            Assert.AreEqual(0, numberOfCalls);
        }

        [TestMethod]
        public void ChannelCastsToGenericMessageProperly()
        {
            Channel channel = new Channel((_) => { });
            int numberOfCalls = 0;
            channel.Subscribe<string>((message) =>
            {
                Assert.IsInstanceOfType(message, typeof(Message<string>));
                numberOfCalls++;
            });
            channel.Subscribe((message) =>
            {
                Assert.IsInstanceOfType(message, typeof(Message));
                numberOfCalls++;
            });

            channel.Publish("test");
            Assert.AreEqual(2, channel.SendQueueMessages());
            Assert.AreEqual(2, numberOfCalls);
        }
    }
}
