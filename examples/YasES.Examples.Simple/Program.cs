using System;
using YasES.Core;

namespace YasES.Examples.Simple
{
    class Program
    {
        static void Main(string[] args)
        {
            IEventStore eventStore = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                .Build();

            using (eventStore)
            {
                const string bucketId = "myBucket";
                const string streamId = "myStream";

                // add a new event
                CommitAttempt commit = new EventCollector()
                    .Add(new EventMessage("MyEventName", payload: Memory<byte>.Empty))
                    .BuildCommit(StreamIdentifier.SingleStream(bucketId, streamId));
                eventStore.Events.Commit(commit);

                // read back the event
                ReadPredicate predicate = ReadPredicateBuilder.Forwards(StreamIdentifier.SingleStream(bucketId, streamId));
                foreach (var @event in eventStore.Events.Read(predicate))
                    Console.WriteLine($"Event Name: {@event.EventName}, Created On: {@event.CreationDateUtc:o}, Commited On: {@event.CommitTimeUtc:o}");
            }
        }
    }
}
