using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Messaging;
using MessageBus.Messaging.InProcess;
using MessageBus.Messaging.InProcess.Scheduler;

#nullable enable

namespace MessageBus.Examples.Broker.Commands
{
    class Program
    {
        private static readonly TopicName MyChannel = "MyChannel";

        static async Task Main(string[] args)
        {
            await RunWithAutomaticSubscriptionCalls();
            await RunWithAutomaticSubscriptionCallsMultipleSubscribers();
            await RunWithAutomaticSubscriptionOneSlowSubscriber();
            await RunWithAutomaticSubscriptionOneWillFail();
            await RunWithManualSubscriptionCalls();
        }

        static async Task RunWithAutomaticSubscriptionCalls()
        {
            Console.Clear();
            Console.WriteLine($"A single subscriber for a topic will get created. Multiple messages will get published to that topic.");
            Console.WriteLine($"The subscribers will get called from different threads, the subscriber will only get one message at a time.");
            Console.WriteLine();
            Console.WriteLine($"Press ENTER to start ...");
            Console.ReadLine();
            Console.WriteLine();

            using IMessageBroker broker = new InProcessMessageBroker(MessageBrokerOptions.Default());
            Subscribe(broker, "A");
            Task t = FireAsync(broker);
            await t;
            RunComplete();
        }

        static async Task RunWithAutomaticSubscriptionCallsMultipleSubscribers()
        {
            Console.Clear();
            Console.WriteLine($"Multiple subscribers for a topic will get created. Multiple messages will get published to that topic.");
            Console.WriteLine($"Every message will only get forwarded to one subscriber, each subscriber will only get one message at a time.");
            Console.WriteLine();
            Console.WriteLine($"Press ENTER to start ...");
            Console.ReadLine();
            Console.WriteLine();

            using IMessageBroker broker = new InProcessMessageBroker(MessageBrokerOptions.Default());
            Subscribe(broker, "A");
            Subscribe(broker, "B");
            Subscribe(broker, "C");
            Task t = FireAsync(broker);
            await t;
            RunComplete();
        }

        static async Task RunWithAutomaticSubscriptionOneSlowSubscriber()
        {
            Console.Clear();
            Console.WriteLine($"Two subscribers will get added to a topic. One will be fast and one will be slow.");
            Console.WriteLine($"Every message will only get forwarded to one subscriber, each subscriber will only get one message at a time.");
            Console.WriteLine();
            Console.WriteLine($"Press ENTER to start ...");
            Console.ReadLine();
            Console.WriteLine();

            using IMessageBroker broker = new InProcessMessageBroker(MessageBrokerOptions.Default());
            Subscribe(broker, "Fast", () => Thread.Sleep(100));
            Subscribe(broker, "Slow", () => Thread.Sleep(200));
            Task t = FireAsync(broker);

            await t;
            RunComplete();
        }

        static async Task RunWithAutomaticSubscriptionOneWillFail()
        {
            Console.Clear();
            Console.WriteLine($"Two subscribers will get added to a topic. One will be normal and the other one will always fail.");
            Console.WriteLine($"Every subscriber will get all messages called from different threads, each subscriber will only get one message at a time.");
            Console.WriteLine();
            Console.WriteLine($"Press ENTER to start ...");
            Console.ReadLine();
            Console.WriteLine();

            using IMessageBroker broker = new InProcessMessageBroker(MessageBrokerOptions.Default());
            Subscribe(broker, "Working", () => Thread.Sleep(100));
            broker.Commands(MyChannel).Subscribe((_) => { Thread.Sleep(100); throw new ExampleException(); });
            Task t = FireAsync(broker);

            await t;
            RunComplete();
        }

        static async Task RunWithManualSubscriptionCalls()
        {
            Console.Clear();
            Console.WriteLine($"A single subscriber for a topic will get created. Multiple messages will get published to that topic.");
            Console.WriteLine($"The subscribers will get called after you have pressed ENTER.");
            Console.WriteLine();
            Console.WriteLine($"Press ENTER to start ...");
            Console.ReadLine();
            Console.WriteLine();

            ManualScheduler manual = new ManualScheduler();
            using IMessageBroker broker = new InProcessMessageBroker(MessageBrokerOptions.BlockingManual(manual));
            Subscribe(broker, "A");
            Task t = FireAsync(broker);

            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}]: Press ENTER to start sending the messages");
            Console.ReadLine();
            await t;

            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}]: Executing manual drain");
            manual.Drain();
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}]: Draining complete");

            RunComplete();
        }

        static void RunComplete()
        {
            Console.WriteLine();
            Console.WriteLine($"Run complete [{Thread.CurrentThread.ManagedThreadId:D2}]: Press ENTER to continue");
            Console.ReadLine();
        }

        static void Subscribe(IMessageBroker broker, string subscriberName, Action? onComplete = null)
        {
            broker.Commands(MyChannel).Subscribe<MyChannelMessage>(m => HandleMyChannelMessage(m, subscriberName, onComplete));
        }

        static void HandleMyChannelMessage(IMessage<MyChannelMessage> message, string subscriberName, Action? onComplete)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId:D2}]: [{subscriberName}] Got message: '{message.Payload.Text}'");
            onComplete?.Invoke();
            message.Ack();
        }

        static async Task FireAsync(IMessageBroker broker)
        {
            await broker.Publish(new MyChannelMessage("Hello World!"), MyChannel);
            foreach (var id in Enumerable.Range(0, 10))
                await broker.Publish(new MyChannelMessage($"Message {id}"), MyChannel);
            await Task.Delay(2000);
            await broker.Publish(new MyChannelMessage("Hello World after 2 sec."), MyChannel);
        }
    }

    public readonly struct MyChannelMessage
    {
        public MyChannelMessage(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    [Serializable]
    public class ExampleException : Exception
    {
        public ExampleException()
        {
        }

        public ExampleException(string? message) : base(message)
        {
        }

        public ExampleException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ExampleException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
