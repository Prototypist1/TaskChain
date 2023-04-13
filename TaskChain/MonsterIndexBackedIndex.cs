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
            // these are just to keep everything from being garbage collected
            // we use weak references on Value otherwise nothing would ever get garbage collected
            private readonly ConcurrentLinkedList<TValue> values = new ConcurrentLinkedList<TValue>();
            private readonly ConcurrentLinkedList<TKey> keys = new ConcurrentLinkedList<TKey>();

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
                    else if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        value = (TValue)at.value.Target;
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


                Value at;

                while (true)
                {
                    at = Interlocked.CompareExchange(ref backing[hashCode & size], mine, null);

                    if (at == null)
                    {
                        values.Add(value);
                        keys.Add(key);
                        return true;
                    }
                    else if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        return false;
                    }
                    else if (!at.value.IsAlive)
                    {
                        if (at != Interlocked.CompareExchange(ref backing[hashCode & size], at.next, at))
                        {
                            break;
                        }
                    }
                    else { 
                        break;
                    }
                }

                while (true)
                {
                    if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                    {
                        values.Add(value);
                        keys.Add(key);
                        return true;
                    }

                    at = at.next;

                    if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        return false;
                    }
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

                Value at;

                while (true)
                {
                    at = Interlocked.CompareExchange(ref backing[hashCode & size], mine, null);

                    if (at == null)
                    {
                        values.Add(value);
                        keys.Add(key);
                        return;
                    }
                    else if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        at.value = new WeakReference(value);
                        values.Add(value);
                        keys.Add(key);
                        return;
                    }
                    else if (!at.value.IsAlive)
                    {
                        if (at != Interlocked.CompareExchange(ref backing[hashCode & size], at.next, at))
                        {
                            break;
                        }
                    }
                    else {
                        break;
                    }
                }

                while (true)
                {
                    if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                    {
                        values.Add(value);
                        keys.Add(key);
                        return;
                    }

                    at = at.next;

                    if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        at.value = new WeakReference(value);
                        values.Add(value);
                        keys.Add(key);
                        return;
                    }

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

                Value at;

                while (true)
                {
                    at = Interlocked.CompareExchange(ref backing[hashCode & size], mine, null);

                    if (at == null)
                    {
                        values.Add(value);
                        keys.Add(key);
                        return value;
                    }
                    else if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        return (TValue)at.value.Target;
                    }
                    else if (!at.value.IsAlive)
                    {
                        if (at != Interlocked.CompareExchange(ref backing[hashCode & size], at.next, at))
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }   
                }

                while (true)
                {
                    if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                    {
                        values.Add(value);
                        keys.Add(key);
                        return value;
                    }

                    at = at.next;

                    if (at.hash == hashCode && at.viewId == viewId && at.value.IsAlive && at.key.Target.Equals(key))
                    {
                        return (TValue)at.value.Target;
                    }
                }
            }

            // TODO you could do remove
            // concurrent dict has a good API for it
            // you remove the key and the value
            // or you remove the key and it outs the value

            // TODO make a HashSet that shares the same backing

            // and now I want to rewrite this
            // the size is prime
            // when you go to insert of lookup
            // key the hashcode % prime
            // and walk by that 
            // idk
            // what do I know is this is bugged
            // Ants hangs when I use it

            // ...and this thing is a memory nightmare
            // you have to manually take stuff out or else it'll never be garbage collected 
        }

        private class Value
        {
            public readonly int viewId;
            public Value next;
            public readonly int hash;
            public readonly WeakReference key;
            public WeakReference value;

            public Value(int hash, object key, object value, int viewId)
            {
                this.hash = hash;
                this.key = new WeakReference(key);
                this.value = new WeakReference(value);
                this.viewId = viewId;
            }
        }
    }
}

