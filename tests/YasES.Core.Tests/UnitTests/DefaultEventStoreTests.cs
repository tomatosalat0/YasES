﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YasES.Core.Tests.UnitTests
{
    [TestClass]
    public class DefaultEventStoreTests
    {
        [TestMethod]
        public void DefaultEventStoreUsesContainerData()
        {
            IEventReadWrite storage = new TestEventReadWrite();
            IEventStore eventStore = Wireup.Init()
                .ConfigureContainer((container) =>
                {
                    container.Register<IEventReadWrite>(storage);
                })
                .Build();

            Assert.AreSame(storage, eventStore.Events);
        }

        [TestMethod]
        public void DefaultEventStoreDisposesContainer()
        {
            DisposeTrack tracker = new DisposeTrack();
            IEventReadWrite storage = new TestEventReadWrite();
            IEventStore eventStore = Wireup.Init()
                .ConfigureContainer((container) =>
                {
                    container.Register<DisposeTrack>(tracker);
                    container.Register<IEventReadWrite>(storage);
                })
                .Build();
            eventStore.Dispose();

            Assert.AreEqual(1, tracker.DisposeCalls);
        }

        private class TestEventReadWrite : IEventReadWrite
        {
            public void Commit(CommitAttempt attempt)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadEventMessage> Read(ReadPredicate predicate)
            {
                throw new NotImplementedException();
            }
        }

        private class DisposeTrack : IDisposable
        {
            public int DisposeCalls;

            public void Dispose()
            {
                DisposeCalls++;
            }
        }
    }
}
