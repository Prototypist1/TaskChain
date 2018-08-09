using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class ConcurrentArrayList<TValue> : IReadOnlyList<TValue>
        where TValue : class
    {
        private readonly QueueingConcurrent<TValue[][]> backing = new QueueingConcurrent<TValue[][]>(new TValue[5][]);
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
                    Interlocked.CompareExchange(ref lastCount, lastCountCache + 1, lastCountCache);
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
                return backing.GetValue()[myOuterIndex][myInnerIndex] != null;
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
            if (backing.GetValue().Length <= myOuterIndex)
            {
                // expand
                backing.Act(x =>
                {
                    if (x.Length <= myOuterIndex)
                    {
                        var replace = new TValue[x.Length + outerStep][];
                        for (var i = 0; i < x.Length; i++)
                        {
                            replace[i] = x[i];
                        }
                        return replace;
                    }
                    return x;
                });
            }
            Interlocked.CompareExchange(ref backing.GetValue()[myOuterIndex], new TValue[innerSize], null);
            backing.GetValue()[myOuterIndex][myInnerIndex] = value;
            Interlocked.CompareExchange(ref lastCount, myIndex + 1, myIndex);
        }

        public bool TryGet(int i, out TValue value)
        {
            try
            {
                var myOuterIndex = i / innerSize;
                var myInnerIndex = i % innerSize;
                value = backing.GetValue()[myOuterIndex][myInnerIndex];
                return value != null;
            }
            catch
            {
                value = default(TValue);
                return false;
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
