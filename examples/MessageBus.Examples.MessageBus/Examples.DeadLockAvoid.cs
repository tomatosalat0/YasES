using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Examples.MessageBus.ExampleService;
using MessageBus.Examples.MessageBus.Logging;

namespace MessageBus.Examples.MessageBus
{
    internal static class DeadLockAvoid
    {
        public static async Task Execute(Application system)
        {
            Console.WriteLine("This example shows that the tasked based event system prevents dead looks from happening.");
            Console.WriteLine("If the event execution is replaced with the BlockingEventExecuter (in MessageBrokerOptions.EventExecuter),");
            Console.WriteLine("this example will timeout. Because the queued EnsureSampleServiceWorkingCommand will not stop waiting for a result,");
            Console.WriteLine("The hole system becomes unresponsive so the command at the end will never return because the system will still");
            Console.WriteLine("wait for EnsureSampleServiceWorkingCommand to complete");
            Console.WriteLine();

            /*
                replace the constructor with the following line to see the dead lock in action
                
                    new MessageBrokerEventBus(MemoryMessageBrokerBuilder.BlockingBroker(messageBrokerThreads: 1), NullExceptionNotification.Instance);
            */
            using MessageBrokerMessageBus eventSystem = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NullExceptionNotification.Instance);

            eventSystem.RegisterQueryHandler(new ExampleServiceQueryHandler(TimeSpan.FromSeconds(1)));
            eventSystem.RegisterCommandHandler(new EnsureSampleServiceWorkingCommand.Handler(eventSystem));
            eventSystem.RegisterCommandHandler(new LogMessageCommandHandler(eventSystem));
            eventSystem.RegisterEventHandler(new CreatedMessageToConsoleHandler());

            Console.WriteLine($"\t > The command will dead lock on thread starvation - time out is 10 seconds.");
            using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await eventSystem.FireCommandAndWait(new EnsureSampleServiceWorkingCommand(), tokenSource.Token);
                Console.WriteLine($"\t > Dead lock was prevented");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\t > {ConsoleFormat.Format("Command failed", ConsoleFormat.ForegroundColor.Red)}: {ex}");
            }

            Console.WriteLine($"\t > Sending log message command");
            await eventSystem.FireCommandAndWait(new LogMessageCommand("Dead lock prevented and broker still working"), CancellationToken.None);
        }
    }
}
