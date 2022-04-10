using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public interface IBeforeCommitHook
    {
        CommitAttempt BeforeCommit(CommitAttempt attempt);
    }

    public static class EventStoreBuilderBeforeCommitHookExtensions
    {
        public static EventStoreBuilder WithBeforeCommitHook(this EventStoreBuilder builder, IBeforeCommitHook hook)
        {
            if (hook is null) throw new ArgumentNullException(nameof(hook));
            builder.ConfigureServices((services) =>
            {
                IEventReadWrite existing = services.Resolve<IEventReadWrite>();
                IEventReadWrite overwritten = new BeforeCommitStore(existing, hook);
                services.RegisterSingleton<IEventReadWrite>(overwritten);
            });
            return builder;
        }

        private class BeforeCommitStore : IEventReadWrite
        {
            private readonly IBeforeCommitHook _hook;
            private readonly IEventReadWrite _inner;

            public BeforeCommitStore(IEventReadWrite inner, IBeforeCommitHook hook)
            {
                _inner = inner;
                _hook = hook;
            }

            public void Commit(CommitAttempt attempt)
            {
                CommitAttempt afterHook = _hook.BeforeCommit(attempt);
                _inner.Commit(afterHook);
            }

            public IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate)
            {
                return _inner.Read(predicate);
            }
        }
    }
}
