using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Examples.MessageBus.ExampleService;

namespace MessageBus.Examples.MessageBus
{
    internal static class ParallelQuery
    {
        public static async Task Execute(Application system)
        {
            const int NUMBER_OF_QUERY_HANDLERS = 5;
            const int NUMBER_OF_QUERY_REQUESTS = 10;

            List<IDisposable> subscriptions = Enumerable.Range(0, NUMBER_OF_QUERY_HANDLERS)
                .Select(_ => system.Handler.RegisterQueryHandler(new ExampleServiceQueryHandler(TimeSpan.FromSeconds(1.234d))))
                .ToList();

            Stopwatch watch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, NUMBER_OF_QUERY_REQUESTS)
                .Select(i => system.Publish.FireQuery<ExampleServiceQuery, ExampleServiceQuery.Result>(new ExampleServiceQuery(), CancellationToken.None));

            var results = await Task.WhenAll(tasks);
            watch.Stop();

            TimeSpan averageDuration = TimeSpan.FromMilliseconds(results.Sum(p => p.Duration.TotalMilliseconds) / results.Length);
            TimeSpan minDuration = results.Min(p => p.Duration);
            TimeSpan maxDuration = results.Max(p => p.Duration);
            Console.WriteLine($"\t > {results.Length} parallel requests executed took {watch.Elapsed} in total");
            Console.WriteLine($"\t > Average duration: {averageDuration}, min: {minDuration}, max: {maxDuration}");

            foreach (var p in subscriptions)
                p.Dispose();
        }
    }
}
