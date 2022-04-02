using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using YasES.Core;
using YasES.Persistance.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Persistance.Sqlite.Tests.UnitTests
{
    [TestClass]
    public class SqliteEventReadWriteTests
    {
        [TestMethod]
        public void SqliteEventReadWriteCreatesSchemaIfRequested()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);

            Assert.IsTrue(engine is IStorageInitialization);
            IStorageInitialization initializer = engine as IStorageInitialization;
            initializer.Initialize();

            AssertSchemaExsists(factory);
        }

        private void AssertSchemaExsists(IConnectionFactory connectionFactory)
        {
            using (IDbConnection connection = connectionFactory.Open())
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='Events';";
                string value = command.ExecuteScalar().ToString();
                Assert.AreEqual("Events", value);
            }
        }

        [TestMethod]
        public void SqliteEventReadWriteCreatesSchemaOnlyOnce()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);

            Assert.IsTrue(engine is IStorageInitialization);
            IStorageInitialization initializer = engine as IStorageInitialization;
            initializer.Initialize();

            AssertSchemaExsists(factory);

            initializer.Initialize();

            AssertSchemaExsists(factory);
        }

        [TestMethod]
        public void SqliteEventReadWriteWritesCommitToStorage()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            CommitAttempt commit = new CommitAttempt(
                StreamIdentifier.SingleStream("bucket", "stream"),
                CommitId.NewId(),
                new[] { new EventMessage("Event", Memory<byte>.Empty) }
            );
            engine.Commit(commit);
        }

        [TestMethod]
        public void SqliteEventReaderReturnsEmptyIfStreamDoesNotExist()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(StreamIdentifier.SingleStream("bucket", "stream"))).ToList();
            Assert.AreEqual(0, messages.Count);
        }

        [TestMethod]
        public void SqliteEventReadWriteReturnsCommitedEvents()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier identifier = StreamIdentifier.SingleStream("bucket", "stream");
            CommitId commitId = CommitId.NewId();
            CommitAttempt commit = new CommitAttempt(
                identifier,
                commitId,
                new[] { new EventMessage("Event", Memory<byte>.Empty) }
            );
            engine.Commit(commit);

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(StreamIdentifier.SingleStream("bucket", "stream"))).ToList();
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("Event", messages[0].EventName);
            Assert.AreEqual(identifier, messages[0].StreamIdentifier);
            Assert.AreEqual(commitId, messages[0].CommitId);
        }

        [TestMethod]
        public void SqliteEventReaderReturnsAllEventsInOrderForwardReadIfManyExists()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier identifier = StreamIdentifier.SingleStream("bucket", "stream");
            CommitId commitId = CommitId.NewId();
            List<IEventMessage> sendMessages = Enumerable.Range(0, 2000).Select(p =>
            {
                return new EventMessage("Event", new Dictionary<string, object>() { ["index"] = p }, Memory<byte>.Empty);
            }).Cast<IEventMessage>().ToList();

            CommitAttempt commit = new CommitAttempt(
                identifier,
                commitId,
                sendMessages
            );
            engine.Commit(commit);

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(identifier)).ToList();
            Assert.AreEqual(2000, messages.Count);
            Assert.AreEqual(0, Convert.ToInt32(messages[0].Headers["index"]));
            Assert.AreEqual(1999, Convert.ToInt32(messages.Last().Headers["index"]));
        }

        [TestMethod]
        public void SqliteWithConnectionPoolWillKeepAtLeastOneConnectionOpen()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using DbConnectionPool pool = new DbConnectionPool(factory, factory.ConnectionClose);
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(pool);
            engine.Initialize();

            StreamIdentifier identifier = StreamIdentifier.SingleStream("bucket", "stream");
            CommitId commitId = CommitId.NewId();
            List<IEventMessage> sendMessages = Enumerable.Range(0, 2000).Select(p =>
            {
                return new EventMessage("Event", new Dictionary<string, object>() { ["index"] = p }, Memory<byte>.Empty);
            }).Cast<IEventMessage>().ToList();

            CommitAttempt commit = new CommitAttempt(
                identifier,
                commitId,
                sendMessages
            );
            engine.Commit(commit);

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(identifier)).ToList();
            Assert.AreEqual(1, pool.GetReadyConnections());
        }

        [TestMethod]
        public void SqliteEventReaderReturnsAllEventsInOrderBackwardsReadIfManyExists()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier identifier = StreamIdentifier.SingleStream("bucket", "stream");
            CommitId commitId = CommitId.NewId();
            List<IEventMessage> sendMessages = Enumerable.Range(0, 2000).Select(p =>
            {
                return new EventMessage("Event", new Dictionary<string, object>() { ["index"] = p }, Memory<byte>.Empty);
            }).Cast<IEventMessage>().ToList();

            CommitAttempt commit = new CommitAttempt(
                identifier,
                commitId,
                sendMessages
            );
            engine.Commit(commit);

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Backwards(identifier)).ToList();
            Assert.AreEqual(2000, messages.Count);
            Assert.AreEqual(1999, Convert.ToInt32(messages[0].Headers["index"]));
            Assert.AreEqual(0, Convert.ToInt32(messages.Last().Headers["index"]));
        }

        [TestMethod]
        public void SqliteEventReaderReturnsOnlyEventsWithTheSpecifiedNames()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier identifier = StreamIdentifier.SingleStream("bucket", "stream");
            CommitId commitId = CommitId.NewId();
            List<IEventMessage> sendMessages = Enumerable.Range(0, 100).Select(p =>
            {
                return new EventMessage($"Event{p % 10}", new Dictionary<string, object>() { ["index"] = p }, Memory<byte>.Empty);
            }).Cast<IEventMessage>().ToList();

            CommitAttempt commit = new CommitAttempt(
                identifier,
                commitId,
                sendMessages
            );
            engine.Commit(commit);

            ReadPredicate predicate = ReadPredicateBuilder.Custom()
                .FromSingleStream(identifier)
                .ReadForwards()
                .OnlyIncluding(new HashSet<string>() { "Event0", "Event1" })
                .WithoutCheckpointLimit()
                .Build();
            List<IReadEventMessage> messages = engine.Read(predicate).ToList();
            Assert.AreEqual(20, messages.Count);
            Assert.AreEqual(0, Convert.ToInt32(messages[0].Headers["index"]));
        }

        [TestMethod]
        public void SqliteEventReaderReturnsAllEventsExcludedTheSpecifiedNames()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier identifier = StreamIdentifier.SingleStream("bucket", "stream");
            CommitId commitId = CommitId.NewId();
            List<IEventMessage> sendMessages = Enumerable.Range(0, 100).Select(p =>
            {
                return new EventMessage($"Event{p % 10}", new Dictionary<string, object>() { ["index"] = p }, Memory<byte>.Empty);
            }).Cast<IEventMessage>().ToList();

            CommitAttempt commit = new CommitAttempt(
                identifier,
                commitId,
                sendMessages
            );
            engine.Commit(commit);

            ReadPredicate predicate = ReadPredicateBuilder.Custom()
                .FromSingleStream(identifier)
                .ReadForwards()
                .AllExcluding(new HashSet<string>() { "Event0", "Event1" })
                .WithoutCheckpointLimit()
                .Build();
            List<IReadEventMessage> messages = engine.Read(predicate).ToList();
            Assert.AreEqual(80, messages.Count);
            Assert.AreEqual(2, Convert.ToInt32(messages[0].Headers["index"]));
        }

        [TestMethod]
        public void SqliteEventReaderReturnsOnlyEventsOfSpecifiedStream()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("bucket1", "stream1");
            StreamIdentifier stream2 = StreamIdentifier.SingleStream("bucket1", "stream2");
            StreamIdentifier stream3 = StreamIdentifier.SingleStream("bucket2", "stream1");
            engine.Commit(new CommitAttempt(
                stream1,
                CommitId.NewId(),
                new[] { new EventMessage("Event1", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream2,
                CommitId.NewId(),
                new[] { new EventMessage("Event2", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream3,
                CommitId.NewId(),
                new[] { new EventMessage("Event3", Memory<byte>.Empty) }
            ));

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(stream1)).ToList();
            Assert.AreEqual(1, messages.Count);
        }

        [TestMethod]
        public void SqliteEventReaderReturnsOnlyEventsOfSpecifiedMultipleStreams()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("bucket", "stream1");
            StreamIdentifier stream2 = StreamIdentifier.SingleStream("bucket", "stream2");
            StreamIdentifier stream3 = StreamIdentifier.SingleStream("bucket", "stream3");
            engine.Commit(new CommitAttempt(
                stream1,
                CommitId.NewId(),
                new[] { new EventMessage("Event1", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream2,
                CommitId.NewId(),
                new[] { new EventMessage("Event2", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream3,
                CommitId.NewId(),
                new[] { new EventMessage("Event3", Memory<byte>.Empty) }
            ));

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(stream1, stream2)).ToList();
            Assert.AreEqual(2, messages.Count);
        }


        [TestMethod]
        public void SqliteEventReaderReturnsAllStreamsOfSingleBucket()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("bucket1", "stream1");
            StreamIdentifier stream2 = StreamIdentifier.SingleStream("bucket1", "stream2");
            StreamIdentifier stream3 = StreamIdentifier.SingleStream("bucket2", "stream1");
            engine.Commit(new CommitAttempt(
                stream1,
                CommitId.NewId(),
                new[] { new EventMessage("Event1", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream2,
                CommitId.NewId(),
                new[] { new EventMessage("Event2", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream3,
                CommitId.NewId(),
                new[] { new EventMessage("Event3", Memory<byte>.Empty) }
            ));

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(StreamIdentifier.AllStreams("bucket1"))).ToList();
            Assert.AreEqual(2, messages.Count);
        }

        [TestMethod]
        public void SqliteEventWriterIgnoresMultipleCommitsWithSameCommitId()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("bucket1", "stream1");
            CommitAttempt commit = new CommitAttempt(
                stream1,
                CommitId.NewId(),
                new[] { new EventMessage("Event1", Memory<byte>.Empty) }
            );

            engine.Commit(commit);
            engine.Commit(commit);
        }

        [TestMethod]
        public void SqliteEventReadWriterIsCaseSensitive()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("bucket1", "stream1");
            StreamIdentifier stream2 = StreamIdentifier.SingleStream("bucket1", "Stream1");
            engine.Commit(new CommitAttempt(
                stream1,
                CommitId.NewId(),
                new[] { new EventMessage("Event1", Memory<byte>.Empty) }
            ));

            engine.Commit(new CommitAttempt(
                stream2,
                CommitId.NewId(),
                new[] { new EventMessage("Event2", Memory<byte>.Empty) }
            ));

            List<IReadEventMessage> messages = engine.Read(ReadPredicateBuilder.Forwards(stream1)).ToList();
            Assert.AreEqual(1, messages.Count);
        }


        [TestMethod]
        public void SqliteEventReadWriterReturnsEventsWithSameCorrelationId()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("test", "test1");
            string correlationId = Guid.NewGuid().ToString();

            engine.Commit(stream1, new EventMessage("MyEvent1", new Dictionary<string, object>() { [CommonMetaData.CorrelationId] = correlationId }, Memory<byte>.Empty));
            engine.Commit(stream1, new EventMessage("MyEvent1", new Dictionary<string, object>() { [CommonMetaData.CorrelationId] = Guid.NewGuid().ToString() }, Memory<byte>.Empty));

            var predicate = ReadPredicateBuilder.Custom()
                .FromSingleStream(stream1)
                .ReadForwards()
                .IncludeAllEvents()
                .HavingTheCorrelationId(correlationId)
                .Build();

            List<IReadEventMessage> read = engine.Read(predicate).ToList();
            Assert.AreEqual(1, read.Count);
            Assert.AreEqual(correlationId, read[0].Headers[CommonMetaData.CorrelationId]);
        }

        [TestMethod]
        public void SqliteEventReadWriteEscapesStringValues()
        {
            SqliteConnectionFactory factory = new SqliteConnectionFactory("Data Source=EventSource_InMemory_Test;Mode=Memory;Cache=Shared;");
            using IDbConnection connectionPersistance = factory.Open();
            SqlitePersistanceEngine engine = new SqlitePersistanceEngine(factory);
            engine.Initialize();

            StreamIdentifier stream1 = StreamIdentifier.SingleStream("te'st", "te'--st1");
            string correlationId = "'--'";

            engine.Commit(stream1, new EventMessage("MyEv'ent1", new Dictionary<string, object>()
            {
                [CommonMetaData.CorrelationId] = correlationId,
                ["my'key"] = "my'value"
            }, Memory<byte>.Empty));

            Assert.AreEqual(1, engine.Read(ReadPredicateBuilder.Forwards(stream1)).Count());
            Assert.AreEqual(1, engine.Read(
                ReadPredicateBuilder.Custom().FromSingleStream(stream1).ReadForwards().OnlyIncluding("MyEv'ent1").WithoutCheckpointLimit().Build()
            ).Count());
            Assert.AreEqual(1, engine.Read(
                ReadPredicateBuilder.Custom().FromSingleStream(stream1).ReadForwards().AllExcluding("MyEv'ent2").WithoutCheckpointLimit().Build()
            ).Count());
            Assert.AreEqual(1, engine.Read(
                ReadPredicateBuilder.Custom().FromSingleStream(stream1).ReadForwards().IncludeAllEvents().HavingTheCorrelationId(correlationId).Build()
            ).Count());
        }
    }
}
