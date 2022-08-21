using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Examples.MessageBus.Logging;

namespace MessageBus.Examples.MessageBus
{
    internal static class CommandExample
    {
        public static async Task Execute(Application system)
        {
            /*
             Output will be:

              -> "{DateTime.Now} Please log this message"
              -> "[Temporary]: {DateTime.Now} Please log this message"
              -> 
              -> <Temporary Log Event Handler Disposed>
              ->
              -> "{DateTime.Now} Please log this message"

             TemporaryLogEventHandler will remove its subscription when it gets disposed so the 
             second message won't get forward to that handler.

             Notice: the order of first two message might be flipped because each event handler gets
             called simulatinously and independently
             */
            await RunWithTemporaryHandler(system);

            Console.WriteLine();
            Console.WriteLine(ConsoleFormat.Format("<Temporary Log Event Handler Disposed>", ConsoleFormat.ForegroundColor.BrightBlack));
            Console.WriteLine();

            await RunWithoutTemporaryHandler(system);
        }

        static Task RunWithoutTemporaryHandler(Application system)
        {
            return FireCommand(system);
        }

        static async Task RunWithTemporaryHandler(Application system)
        {
            string prefix = ConsoleFormat.Format("[Temporary]:", ConsoleFormat.ForegroundColor.BrightMagenta);
            using var temporaryHandler = new CreatedMessageToConsoleHandler(prefix);

            system.Handler.RegisterEventHandler(temporaryHandler);
            await FireCommand(system);

            await Task.Delay(500);
        }

        static async Task FireCommand(Application system)
        {
            await system.Publish.FireCommandAndWait(new LogMessageCommand("Please log this message"), CancellationToken.None);
        }
    }
}
