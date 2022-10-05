using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MessageBus.Examples.MessageBus.ExampleService
{
    public sealed class ExampleServiceQueryHandler : IAsyncMessageQueryHandler<ExampleServiceQuery, ExampleServiceQuery.Result>
    {
        private readonly TimeSpan _duration;

        public ExampleServiceQueryHandler(TimeSpan duration)
        {
            _duration = duration;
        }

        async Task<ExampleServiceQuery.Result> IAsyncMessageQueryHandler<ExampleServiceQuery, ExampleServiceQuery.Result>.HandleAsync(ExampleServiceQuery query)
        {
            Stopwatch watch = Stopwatch.StartNew();
            await Task.Delay(_duration);
            watch.Stop();
            return new ExampleServiceQuery.Result(query.MessageId, watch.Elapsed);
        }
    }
}
