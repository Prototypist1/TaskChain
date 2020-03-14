using System;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class MonsterIndexBackedIndex
    {
        private const int size = 0b1111_1111_1111_1111_1111;
        private static int count = 0;
        private static readonly Value[] backing = new Value[size];

        public class View<TKey, TValue>
        {

            private readonly int viewId;

            public View()
            {
                viewId = Interlocked.Increment(ref count);
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                var hashCode = key.GetHashCode();

                var at = backing[hashCode & size];

                while (true)
                {
                    if (at == null)
                    {
                        value = default;
                        return false;
                    }
                    else if (at.hash == hashCode && at.viewId == viewId && at.key.Equals(key))
                    {
                        value = (TValue)at.value;
                        return true;
                    }

                    at = at.next;
                }
            }

            public TValue GetOrThrow(TKey key)
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }
                throw new KeyNotFoundException(key.ToString());
            }

            public bool TryAdd(TKey key, TValue value)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }


                var hashCode = key.GetHashCode();

                var mine = new Value(hashCode, key, value, viewId);

                var at = Interlocked.CompareExchange(ref backing[hashCode & size], mine, null);

                if (at == null)
                {
                    return true;
                }
                else if (at.hash == hashCode && at.viewId == viewId && at.key.Equals(key))
                {
                    return false;
                }

                while (true)
                {
                    if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                    {
                        return true;
                    }

                    if (at.next.hash == hashCode && at.next.viewId == viewId && at.next.key.Equals(key))
                    {
                        return false;
                    }

                    at = at.next;
                }
            }

            public void AddOrUpdate(TKey key, TValue value)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }


                var hashCode = key.GetHashCode();

                var mine = new Value(hashCode, key, value, viewId);

                var at = Interlocked.CompareExchange(ref backing[hashCode & size], mine, null);

                if (at == null)
                {
                    return;
                }
                else if (at.hash == hashCode && at.viewId == viewId && at.key.Equals(key))
                {
                    at.value = value;
                    return;
                }

                while (true)
                {
                    if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                    {
                        return;
                    }

                    if (at.next.hash == hashCode && at.next.viewId == viewId && at.next.key.Equals(key))
                    {
                        at.next.value = value;
                        return;
                    }

                    at = at.next;
                }
            }

            public TValue GetOrAdd(TKey key, TValue value)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }


                var hashCode = key.GetHashCode();

                var mine = new Value(hashCode, key, value, viewId);

                var at = Interlocked.CompareExchange(ref backing[hashCode & size], mine, null);

                if (at == null)
                {
                    return value;
                }
                else if (at.hash == hashCode && at.viewId == viewId && at.key.Equals(key))
                {
                    return (TValue)at.value;
                }

                while (true)
                {
                    if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                    {
                        return value;
                    }

                    if (at.next.hash == hashCode && at.next.viewId == viewId && at.next.key.Equals(key))
                    {
                        return (TValue)at.next.value;
                    }

                    at = at.next;
                }
            }
        }

        private class Value
        {
            public readonly int viewId;
            public Value next;
            public readonly int hash;
            public readonly object key;
            public object value;

            public Value(int hash, object key, object value, int viewId)
            {
                this.hash = hash;
                this.key = key;
                this.value = value;
                this.viewId = viewId;
            }
        }
    }
}

