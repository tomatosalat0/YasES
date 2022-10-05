using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Examples.MessageBus.ExampleService;

namespace MessageBus.Examples.MessageBus
{
    internal static class MissingHandler
    {
        public static async Task Execute(Application system)
        {
            Console.WriteLine($"\t > The query will fail after two seconds - please be patient");
            Console.WriteLine();
            using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var query = new ExampleServiceQuery();
            try
            {
                await system.Publish.FireQuery<ExampleServiceQuery, ExampleServiceQuery.Result>(query, tokenSource.Token);
            }
            catch (Exception ex)
            {
                string exceptionString = ConsoleFormat.Format(ex.ToString(), ConsoleFormat.ForegroundColor.BrightBlack);
                string queryFailed = ConsoleFormat.Format("Query failed", ConsoleFormat.ForegroundColor.Red);
                Console.WriteLine($"\t > {queryFailed}(correlation {query.MessageId}): {exceptionString}");
            }
        }
    }
}
