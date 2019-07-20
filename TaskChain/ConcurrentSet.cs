using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain
{

    public class ConcurrentSet<T> : IReadOnlyCollection<T>
    {

        private readonly RawConcurrentIndexed<T, T> backing = new RawConcurrentIndexed<T, T>();
        private const long enumerationAdd = 1_000_000_000;
        private long enumerationCount = 0;

        public int Count => backing.Count;

        private void NoModificationDuringEnumeration()
        {
            var res = Interlocked.Increment(ref enumerationCount);
            if (res >= enumerationAdd)
            {
                throw new Exception("No modification during enumeration");
            }
        }

        public bool Contains(T value)
        {
            return backing.ContainsKey(value);
        }

        // TODO some of these are extensions
        public T GetOrAdd(T value)
        {
            try
            {
                NoModificationDuringEnumeration();

                return backing.GetOrAdd(value, value);
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }

        }

        public void AddOrThrow(T value)
        {
            try
            {
                NoModificationDuringEnumeration();
                var res = backing.TryAdd(value, value);
                if (!res)
                {
                    throw new Exception("Item already added");
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public bool TryAdd(T value)
        {
            try
            {
                NoModificationDuringEnumeration();
                return backing.TryAdd(value, value);
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            Interlocked.Add(ref enumerationCount, enumerationAdd);
            while (Volatile.Read(ref enumerationCount) % enumerationAdd != 0)
            {
                // TODO do tasks?
            }
            foreach (var item in backing)
            {
                yield return item.Value;
            }
            Interlocked.Add(ref enumerationCount, -enumerationAdd);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}