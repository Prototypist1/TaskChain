using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class RawConcurrentIndexedTree<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private class KeyValue : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            public readonly KeyValue[] next;
            public readonly TKey key;
            public readonly int hash;
            public readonly TValue value;

            public KeyValue(TKey key, TValue value, int hash)
            {
                this.key = key;
                this.value = value;
                this.hash = hash;
                this.next = new KeyValue[0b1<< width];
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                yield return new KeyValuePair<TKey, TValue>(key, value);
                foreach (var item in this.next)
                {
                    if (item != null)
                    {
                        foreach (var innerItem in item)
                        {
                            yield return innerItem;
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private const int width= 4;
        private const int mask = 0b0000_0000_1111;
        private volatile int count;

        public int Count
        {
            get
            {
                return count;
            }
        }

        public TValue this[TKey key] => GetOrThrow(key);

        KeyValue root = new KeyValue(default,default,0);

        public bool ContainsKey(TKey key)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashKey = hash;

            while(true)
            {
                at = at.next[hashKey & mask];
                if (at == null) {
                    return false;
                }
                if (hash == at.hash && key.Equals(at.key))
                {
                    return true;
                }
                hashKey >>= width;
            }
        }

        public TValue GetOrThrow(TKey key)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashKey = hash;

            while (true)
            {
                at = at.next[hashKey & mask];
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at.value;
                }
                hashKey >>= width;
            }
        }

        public TValue GetOrAdd(TKey key,TValue value)
        {
            var hash = key.GetHashCode();
            var hashKey = hash;
            var node = new KeyValue(key, value, hash);

            var at = root;

            while (true)
            {
                at = Interlocked.CompareExchange(ref at.next[hashKey & mask], node, null);
                if ((at == null) || (hash == at.hash && key.Equals(at.key))) {
                    if (at == null)
                    {
                        Interlocked.Increment(ref count);
                        return node.value;
                    }
                    return at.value;
                }
                hashKey >>= width;
            }
        }

        public bool TryGetValue(TKey key, out TValue res)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashKey = hash;

            while (true)
            {
                at = at.next[hashKey & mask];
                if ((at == null) || (hash == at.hash && key.Equals(at.key)))
                {
                    if (at == null)
                    {
                        res = default;
                        return false;
                    }
                    res = at.value;
                    return true;
                }
                hashKey >>= width;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var l1 in root)
            {
                yield return l1;
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                foreach (var item in this)
                {
                    yield return item.Key;
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var item in this)
                {
                    yield return item.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}

