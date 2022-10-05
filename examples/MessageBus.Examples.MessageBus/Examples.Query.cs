using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Examples.MessageBus.Weather;

namespace MessageBus.Examples.MessageBus
{
    internal static class QueryExample
    {
        public static async Task Execute(Application system)
        {
            Console.WriteLine($"\t > Executing query to web service - please be patient");
            Console.WriteLine();

            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                WeatherQuery.Result weather = await system.Publish.FireQuery<WeatherQuery, WeatherQuery.Result>(new WeatherQuery(), CancellationToken.None);
                watch.Stop();

                Console.WriteLine($"\t > Query completed in {watch.Elapsed} [Correlation: {weather.MessageId}]");
                Console.WriteLine($"\t > Weather Response:");
                Console.WriteLine(weather.Weather);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\t > {ConsoleFormat.Format("Service query failed", ConsoleFormat.ForegroundColor.Red)}: {ex}");
            }
        }
    }
}
