using System;
using System.Collections.Generic;
using System.Linq;

namespace MessageBus.Messaging.InProcess
{
    internal class LockedList<T>
    {
        private readonly List<T> _values = new List<T>();
        private readonly object _lock = new object();

        public void Add(T item)
        {
            Write(list => list.Add(item));
        }

        public void Write(Action<List<T>> execute)
        {
            lock (_lock)
            {
                execute(_values);
            }
        }

        public TResult Write<TResult>(Func<List<T>, TResult> execute)
        {
            lock (_lock)
            {
                return execute(_values);
            }
        }

        public TResult Read<TResult>(Func<IReadOnlyList<T>, TResult> execute)
        {
            lock (_lock)
            {
                return execute(_values);
            }
        }

        public void RemoveWhere(Predicate<T> predicate)
        {
            Write((list) => list.RemoveAll(predicate));
        }

        public int ForEach(Func<T, bool> predicate, Action<T> execute)
        {
            return Read<int>((list) =>
            {
                int result = 0;
                foreach (var p in list.Where(predicate))
                {
                    result++;
                    execute(p);
                }
                return result;
            });
        }
    }
}
