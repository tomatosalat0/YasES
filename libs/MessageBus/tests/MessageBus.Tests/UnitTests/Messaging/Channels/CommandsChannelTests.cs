using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessageBus.Messaging.InProcess.Channels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Messaging.Tests.UnitTests.Channels
{
    [TestClass]
    public class CommandsChannelTests
    {
        [TestMethod]
        public void ChannelThrowsArgumentNullExceptionWhenNoCallbackIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CommandsChannel(null));
        }

        [TestMethod]
        public void SubscribeThrowsArgumentNullException()
        {
            CommandsChannel channel = new CommandsChannel(() => { });
            Assert.ThrowsException<ArgumentNullException>(() => channel.Subscribe<object>(null));
        }

        [TestMethod]
        public void NewlyCreatedChannelHasNoWork()
        {
            CommandsChannel channel = new CommandsChannel(() => { });
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public void SubscribingToChannelHasNoWork()
        {
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => { });
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public async Task UnubscribedChannelWontCreateWorkAfterPublish()
        {
            CommandsChannel channel = new CommandsChannel(() => { });
            await channel.Publish(new object());
            Assert.AreEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public async Task SubscribeAfterPublishWillReceivePendingEvents()
        {
            CommandsChannel channel = new CommandsChannel(() => { });
            await channel.Publish(new object());
            channel.Subscribe<object>((_) => { });
            Assert.AreNotEqual(0, channel.CollectWork().Count);
        }

        [TestMethod]
        public async Task SingleSubscriberReceivesEvent()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });

            await channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            Assert.AreNotEqual(0, work.Count);
            foreach (var p in work)
                p();
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public async Task OnlyOneSubscriberReceiveOneEvent()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });

            await channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            Assert.AreNotEqual(0, work.Count);
            foreach (var p in work)
                p();
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public async Task SubscriberWontReceiveEventsAfterSubscriptionDispose()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            IDisposable subscription = channel.Subscribe<object>((_) => { callCount++; });
            subscription.Dispose();

            await channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            foreach (var p in work)
                p();
            Assert.AreEqual(0, callCount);
        }

        [TestMethod]
        public async Task UndisposedSubscriptionWillReceiveEvents()
        {
            int callCountA = 0;
            int callCountB = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            IDisposable subscriptionA = channel.Subscribe<object>((_) => { _.Ack(); callCountA++; });
            IDisposable subscriptionB = channel.Subscribe<object>((_) => { _.Ack(); callCountB++; });
            subscriptionA.Dispose();

            await channel.Publish(new object());

            IReadOnlyList<Action> work = channel.CollectWork();
            foreach (var p in work)
                p();
            Assert.AreEqual(0, callCountA);
            Assert.AreEqual(1, callCountB);
        }

        [TestMethod]
        public async Task CommandsChannelFiresUnacknowledgedEventAgain()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => {
                if (callCount > 0)
                    _.Ack();
                callCount++; 
            });
            await channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task CommandsChannelDoesntFireAcknowledgedEventAgain()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });
            await channel.Publish(new object());

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
        public async Task CommandsChannelDoesntFireNotAcknowledgedEventAgain()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => { _.Nack(); callCount++; });
            await channel.Publish(new object());

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
        public async Task CommandsChannelFiresUnacknowledgedEventAgainWithException()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => {
                callCount++;
                if (callCount < 2)
                    throw new Exception();
                else
                    _.Ack();
            });
            await channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task CommandsChannelFiresNotAcknowledgedEventAgainWithException()
        {
            int callCount = 0;
            CommandsChannel channel = new CommandsChannel(() => { });
            channel.Subscribe<object>((_) => {
                _.Nack();
                callCount++;
                if (callCount < 2)
                    throw new Exception();
            });
            await channel.Publish(new object());

            while (true)
            {
                IReadOnlyList<Action> work = channel.CollectWork();
                foreach (var p in work)
                    p();
                if (work.Count == 0)
                    break;
            }

            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task CleanupWillRemoveDisposedSubscriptions()
        {
            CommandsChannel channel = new CommandsChannel(() => { });

            int callCount = 0;
            channel.Subscribe<object>((_) => { _.Ack(); }).Dispose();
            channel.Subscribe<object>((_) => { _.Ack(); callCount++; });

            channel.Cleanup();

            await channel.Publish(new object());

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
    }
}
