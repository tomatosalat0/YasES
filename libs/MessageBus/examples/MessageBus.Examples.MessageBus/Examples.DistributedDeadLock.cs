using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBus.Examples.MessageBus
{
    internal static class DistributedDeadlock
    {
        public static async Task ExecuteNonWorking(Application system)
        {
            // IMPORTANT:
            // this is an example on how to NOT use the messaging system.
            //
            // LoadFromSourceQuery will execute a sub query SubQuery which will execute another LoadFromSourceQuery
            // Because there is only one LoadFromSourceQuery handler which is busy waiting for the result
            // of SubQuery - which is busy waiting for another QueryA result.
            using IMessageBus eventSystem = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            eventSystem.RegisterQueryHandler(new LoadFromSourceHandler(eventSystem));
            eventSystem.RegisterQueryHandler(new SubQueryHandler(eventSystem));

            await PerformQueries(eventSystem);
        }

        public static async Task ExecuteWorkingButFlawed(Application system)
        {
            // IMPORTANT:
            // While this implementation fixes the same problem, it only does this
            // because there will be a second "LoadFromSourceHandler" available.
            // This is flawed because if multiple parallel queries get executed, the same
            // dead lock can happen.
            //
            // This is NOT a solution for the problem - it only covers the root problem.
            using IMessageBus eventSystem = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            eventSystem.RegisterQueryHandler(new LoadFromSourceHandler(eventSystem));
            eventSystem.RegisterQueryHandler(new LoadFromSourceHandler(eventSystem));
            eventSystem.RegisterQueryHandler(new SubQueryHandler(eventSystem));

            await PerformQueries(eventSystem);
        }

        private static async Task PerformQueries(IMessageBus eventSystem)
        {
            Console.WriteLine($"\t > Querying from SourceB ...");
            var sourceBResult = await eventSystem.FireQuery<LoadFromSourceQuery, LoadFromSourceResult>(new LoadFromSourceQuery(Source.SourceB), CancellationToken.None);
            Console.WriteLine($"\t > {sourceBResult.Result}");

            Console.WriteLine($"\t > Querying from SourceA");
            using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                var sourceAResult = await eventSystem.FireQuery<LoadFromSourceQuery, LoadFromSourceResult>(new LoadFromSourceQuery(Source.SourceA), tokenSource.Token);
                Console.WriteLine($"\t > {sourceAResult.Result}");
            }
            catch (Exception ex)
            {
                string exceptionString = ConsoleFormat.Format(ex.ToString(), ConsoleFormat.ForegroundColor.BrightBlack);
                Console.WriteLine($"\t > {ConsoleFormat.Format("Query failed", ConsoleFormat.ForegroundColor.Red)}: {exceptionString}");
            }
        }

        [Topic("Queries/QueryA")]
        private class LoadFromSourceQuery : IMessageQuery<LoadFromSourceResult>
        {
            public LoadFromSourceQuery(Source source)
            {
                Source = source;
            }

            public Source Source { get; }

            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class LoadFromSourceResult : IMessageQueryResult
        {
            public LoadFromSourceResult(MessageId messageId, string result)
            {
                MessageId = messageId;
                Result = result;
            }

            public string Result { get; }

            public MessageId MessageId { get; }
        }

        private class LoadFromSourceHandler : IAsyncMessageQueryHandler<LoadFromSourceQuery, LoadFromSourceResult>
        {
            private readonly IMessageBus _bus;

            public LoadFromSourceHandler(IMessageBus bus)
            {
                _bus = bus;
            }

            public Task<LoadFromSourceResult> HandleAsync(LoadFromSourceQuery query)
            {
                Console.WriteLine($"\t\t > {ConsoleFormat.Format("FromSourceHandler Begin", ConsoleFormat.ForegroundColor.Magenta)}");
                try
                {
                    switch (query.Source)
                    {
                        case Source.SourceA:
                            return HandleSourceA(query);
                        case Source.SourceB:
                            return HandleSourceB(query);
                        default:
                            throw new NotSupportedException();
                    }
                }
                finally
                {
                    Console.WriteLine($"\t\t > {ConsoleFormat.Format("FromSourceHandler End", ConsoleFormat.ForegroundColor.Magenta)}");
                }
            }

            private Task<LoadFromSourceResult> HandleSourceB(LoadFromSourceQuery query)
            {
                return Task.FromResult(new LoadFromSourceResult(query.MessageId, "Result from Source B"));
            }

            private async Task<LoadFromSourceResult> HandleSourceA(LoadFromSourceQuery query)
            {
                var subQuery = await _bus.FireQuery<SubQuery, SubQueryResult>(new SubQuery(), CancellationToken.None);
                string result = $"Result from Source A (sub query result: {subQuery.Result})";
                return new LoadFromSourceResult(query.MessageId, result);
            }
        }

        [Topic("Queries/SubQuery")]
        private class SubQuery : IMessageQuery<SubQueryResult>
        {
            public MessageId MessageId { get; } = MessageId.NewId();
        }

        private class SubQueryResult : IMessageQueryResult
        {
            public SubQueryResult(MessageId messageId, string result)
            {
                MessageId = messageId;
                Result = result;
            }

            public string Result { get; }

            public MessageId MessageId { get; }
        }

        private class SubQueryHandler : IAsyncMessageQueryHandler<SubQuery, SubQueryResult>
        {
            private readonly IMessageBus _bus;

            public SubQueryHandler(IMessageBus bus)
            {
                _bus = bus;
            }

            public async Task<SubQueryResult> HandleAsync(SubQuery query)
            {
                Console.WriteLine($"\t\t > {ConsoleFormat.Format("SubQueryHandler Begin", ConsoleFormat.ForegroundColor.Magenta)}");
                try
                {
                    var sourceResult = await _bus.FireQuery<LoadFromSourceQuery, LoadFromSourceResult>(new LoadFromSourceQuery(Source.SourceB), CancellationToken.None);
                    return new SubQueryResult(query.MessageId, sourceResult.Result);
                }
                finally
                {
                    Console.WriteLine($"\t\t > {ConsoleFormat.Format("SubQueryHandler End", ConsoleFormat.ForegroundColor.Magenta)}");
                }
            }
        }


        private enum Source
        {
            SourceA,

            SourceB
        }
    }
}
