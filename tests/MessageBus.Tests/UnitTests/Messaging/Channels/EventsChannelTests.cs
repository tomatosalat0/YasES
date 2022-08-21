using System;
using System.Collections.Generic;
using MessageBus.Messaging.InProcess.Channels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Messaging.Tests.UnitTests.Channels
{
    [TestClass]
    public class EventsChannelTests
    {
        [TestMethod]
        public void ChannelThrowsArgumentNullExceptionWhenNoCallbackIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new EventsChannel(null));
        }

        [TestMethod]
        public void SubscribeThrowsArgumentNullException()
        {
            EventsChannel channel = new EventsChannel(() => { });
            Assert.ThrowsException<ArgumentNullException>(() => channel.Subscribe<object>(null));
        }

        [TestMethod]
        public void NewlyCreatedChannelHasNoWork()
        {
            EventsChannel channel = new EventsChannel(() => { });
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public void SubscribingToChannelHasNoWork()
        {
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { });
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public void UnubscribedChannelWontCreateWorkAfterPublish()
        {
            EventsChannel channel = new EventsChannel(() => { });
            channel.Publish(new object());
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public void SubscribeAfterPublishDoesNotReceivePreviousEvents()
        {
            EventsChannel channel = new EventsChannel(() => { });
            channel.Publish(new object());
            channel.Subscribe<object>((_) => { });
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public void SingleSubscriberReceivesEvent()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { callCount++; });

            channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            Assert.AreNotEqual(0, work.Count);
            foreach (var p in work)
                p();
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void AllSubscriberReceiveOneEvent()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { callCount++; });
            channel.Subscribe<object>((_) => { callCount++; });
            channel.Subscribe<object>((_) => { callCount++; });

            channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            Assert.AreNotEqual(0, work.Count);
            foreach (var p in work)
                p();
            Assert.AreEqual(3, callCount);
        }

        [TestMethod]
        public void SubscriberWontReceiveEventsAfterSubscriptionDispose()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            IDisposable subscription = channel.Subscribe<object>((_) => { callCount++; });
            subscription.Dispose();

            channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            foreach (var p in work)
                p();
            Assert.AreEqual(0, callCount);
        }

        [TestMethod]
        public void EventsChannelDoesntFireUnacknowledgedEventAgain()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { callCount++; });
            channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void EventsChannelDoesntFireAcknowledgedEventAgain()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });
            channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void EventsChannelDoesntFireNotAcknowledgedEventAgain()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { _.Nack(); callCount++; });
            channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void EventsChannelDoesntFireEventAgainWithException()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.Subscribe<object>((_) => { callCount++; throw new Exception(); });
            channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void EventSubscriptionCanBeDisposedInCallback()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            IDisposable subscription = null!;
            subscription = channel.Subscribe<object>((_) => { callCount++; subscription.Dispose(); });

            channel.Publish(new object());
            channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void CleanupWillRemoveDisposedSubscriptions()
        {
            EventsChannel channel = new EventsChannel(() => { });

            int callCount = 0;
            channel.Subscribe<object>((_) => { callCount++; }).Dispose();
            channel.Subscribe<object>((_) => { callCount++; });

            channel.Cleanup();

            channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void EventsChannelFiresLastSubscriptionIfSubscriped()
        {
            int callCount = 0;
            EventsChannel channel = new EventsChannel(() => { });
            channel.OnLastSubscriptionRemoved = () => callCount++;

            IDisposable subscriptionA = channel.Subscribe<object>((_) => { });
            IDisposable subscriptionB = channel.Subscribe<object>((_) => { });

            Assert.AreEqual(0, callCount);

            subscriptionA.Dispose();
            Assert.AreEqual(0, callCount);

            subscriptionB.Dispose();
            Assert.AreEqual(1, callCount);

            subscriptionB.Dispose();
            Assert.AreEqual(1, callCount);
        }
    }
}
