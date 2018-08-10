using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class ConcurrentArrayList<TValue> : IReadOnlyList<TValue>
    {
        private readonly RawConcurrentArrayList<QueueingConcurrent<TValue>> backing = new RawConcurrentArrayList<QueueingConcurrent<TValue>>();
        private const long enumerationAdd = 1_000_000_000;
        private long enumerationCount = 0;


        public ConcurrentArrayList()
        {
        }

        public ConcurrentArrayList(IEnumerable<TValue> items)
        {
            // this could probably be optimized...
            this.EnqueAddSet(items);
        }

        private void NoModificationDuringEnumeration()
        {
            var res = Interlocked.Increment(ref enumerationCount);
            if (res >= enumerationAdd)
            {
                throw new Exception("No modification during enumeration");
            }
        }

        public bool TryGet(int i, out TValue value)
        {
            try
            {
                if (backing.TryGet(i, out var res))
                {
                    value = res.GetValue();
                    return true;
                }
                value = default;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool TrySet(int i, TValue value)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(i, out var res))
                {
                    res.Act(x => value);
                    return true;
                }
                value = default;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool TryDo(int i, Func<TValue,TValue> action)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(i, out var res))
                {
                    res.Act(action);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void EnqueAdd(TValue value)
        {
            try
            {
                NoModificationDuringEnumeration();
                backing.EnqueAdd(new QueueingConcurrent<TValue>(value));
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public TValue this[int index]
        {
            get
            {
                if (TryGet(index, out var res))
                {
                    return res;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (TrySet(index, value))
                {
                    return;
                }
                throw new IndexOutOfRangeException();
            }
        }

        public int Count => backing.Count;

        public IEnumerator<TValue> GetEnumerator()
        {
            Interlocked.Add(ref enumerationCount, enumerationAdd);
            SpinWait.SpinUntil(()=> Volatile.Read(ref enumerationCount) % enumerationAdd == 0);
            foreach (var item in backing)
            {
                yield return item.GetValue();
            }
            Interlocked.Add(ref enumerationCount, -enumerationAdd);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class ConcurrentArrayArrayExtensions
    {
        public static void Update<TValue>(this ConcurrentArrayList<TValue> self, int index, Func<TValue, TValue> func)
        {
            if (self.TryDo(index, x => func(x)))
            {
                return;
            }
            throw new IndexOutOfRangeException();
        }
        public static void EnqueAddSet<TValue>(this ConcurrentArrayList<TValue> self, IEnumerable<TValue> collection)
        {
            foreach (var item in collection)
            {
                self.EnqueAdd(item);
            }
        }
    }
}
