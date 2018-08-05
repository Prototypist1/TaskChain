using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{

    public class RawConcurrentArrayArray<TValue> : IReadOnlyList<TValue>
        where TValue : class
    {
        private readonly Concurrent<TValue[][]> backing = new Concurrent<TValue[][]>(new TValue[5][]);
        private int leadingCount = -1;
        private int lastCount = 0;
        private readonly int innerSize = 20;
        private readonly int outerStep = 5;

        public int Count
        {
            get
            {
                var lastCountCache = Volatile.Read(ref lastCount);
                while (Has(lastCountCache))
                {
                    Interlocked.CompareExchange(ref lastCount, lastCountCache+1, lastCountCache);
                    lastCountCache = Volatile.Read(ref lastCount);
                }
                return lastCount;


            }
        }

        public bool Has(int i)
        {
            try
            {
                var myOuterIndex = i / innerSize;
                var myInnerIndex = i % innerSize;
                return backing.Value[myOuterIndex][myInnerIndex] != null;
            }
            catch
            {
                return false;
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
        }

        public void EnqueAdd(TValue value)
        {
            var myIndex = Interlocked.Increment(ref leadingCount);
            var myOuterIndex = myIndex / innerSize;
            var myInnerIndex = myIndex % innerSize;
            if (backing.Value.Length <= myOuterIndex)
            {
                // expand
                backing.Do(x =>
                {
                    if (backing.Value.Length <= myOuterIndex)
                    {
                        var replace = new TValue[backing.Value.Length + outerStep][];
                        for (int i = 0; i < backing.Value.Length; i++)
                        {
                            replace[i] = backing.Value[i];
                        }
                        x.Value = replace;
                    }
                });
            }
            Interlocked.CompareExchange(ref backing.Value[myOuterIndex], new TValue[innerSize], null);
            backing.Value[myOuterIndex][myInnerIndex] = value;
            Interlocked.CompareExchange(ref lastCount, myIndex+1, myIndex);
        }

        public bool TryGet(int i, out TValue value)
        {
            try { 
                var myOuterIndex = i / innerSize;
                var myInnerIndex = i % innerSize;
                value = backing.Value[myOuterIndex][myInnerIndex];
                return value != null;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ConcurrentArrayArray<TValue> : IReadOnlyList<TValue>
    {
        private readonly RawConcurrentArrayArray<Concurrent<TValue>> backing = new RawConcurrentArrayArray<Concurrent<TValue>>();
        private const long enumerationAdd = 1_000_000_000;
        private long enumerationCount = 0;


        public ConcurrentArrayArray()
        {
        }

        public ConcurrentArrayArray(IEnumerable<TValue> items)
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
                    value = res.Value;
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
                    res.Do(x => x.Value = value);
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
        public bool TryDo(int i, Action<Concurrent<TValue>.ValueHolder> action)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(i, out var res))
                {
                    res.Do(action);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool TryDo<TOut>(int i, Func<Concurrent<TValue>.ValueHolder, TOut> func, out TOut result)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(i, out var res))
                {
                    result = res.Do(func);
                    return true;
                }
                result = default;
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
                backing.EnqueAdd(new Concurrent<TValue>(value));
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

    public static class ConcurrentArrayArrayExtensions
    {
        public static void Update<TValue>(this ConcurrentArrayArray<TValue> self, int index, Func<TValue, TValue> func)
        {
            if (self.TryDo(index, x => x.Value = func(x.Value)))
            {
                return;
            }
            throw new IndexOutOfRangeException();
        }
        public static void EnqueAddSet<TValue>(this ConcurrentArrayArray<TValue> self, IEnumerable<TValue> collection)
        {
            foreach (var item in collection)
            {
                self.EnqueAdd(item);
            }
        }
    }
}
