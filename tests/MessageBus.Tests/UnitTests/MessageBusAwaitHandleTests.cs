using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Tests.UnitTests
{
    [TestClass]
    public class MessageBusAwaitHandleTests
    {
        [TestMethod]
        public async Task AwaitHandleGetsCompletedWhenPredicateMatches()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NullExceptionNotification.Instance);

            AtLeast5EvenNumbersPredicate predicate = new AtLeast5EvenNumbersPredicate();
            AwaitHandle<NumberEvent> handle = bus.AwaitEvent(predicate.Predicate, CancellationToken.None);

            Task fireProcess = Task.Run(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    await bus.FireEvent(new NumberEvent(i));
                    await Task.Delay(10);
                }
            });

            NumberEvent @event = await handle.Await();
            Assert.AreEqual(8, @event.Value);
            await fireProcess;
        }

        [TestMethod]
        public void AwaitHandleCanBeDisposedWithoutAwaiting()
        {
            using IMessageBus bus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NullExceptionNotification.Instance);

            AtLeast5EvenNumbersPredicate predicate = new AtLeast5EvenNumbersPredicate();
            AwaitHandle<NumberEvent> handle = bus.AwaitEvent(predicate.Predicate, CancellationToken.None);
            handle.Dispose();
        }

        private class AtLeast5EvenNumbersPredicate
        {
            private int _counter;

            public AtLeast5EvenNumbersPredicate()
            {
                Predicate = (@event) =>
                {
                    if (@event.Value % 2 == 0)
                    {
                        _counter++;
                    }
                    return _counter >= 5;
                };
            }

            public Func<NumberEvent, bool> Predicate { get; }
        }

        [Topic("Events/NumberEvent")]
        private class NumberEvent : IMessageEvent
        {
            public NumberEvent(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public MessageId MessageId { get; }
        }
    }
}
