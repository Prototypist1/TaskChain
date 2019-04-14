using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class RawConcurrentIndexed<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        public class KeyValue: IEnumerable<KeyValuePair<TKey, TValue>>
        {
            public readonly KeyValue[] next;
            public readonly TKey key;
            public readonly int hash;
            public readonly TValue value;

            public KeyValue(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
                this.hash = key.GetHashCode();
                this.next = new KeyValue[2];
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                yield return new KeyValuePair<TKey, TValue>(key, value);
                if (next[0] != null) {
                    foreach (var item in next[0])
                    {
                        yield return item;
                    }
                }
                if (next[1] != null)
                {
                    foreach (var item in next[1])
                    {
                        yield return item;
                    }
                }

            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private volatile int count;
        private int GrowSize = 2* Size;
        private const int HashLength = 7;
        private const int Size = 0b1 << HashLength;
        private readonly KeyValue[] tree = new KeyValue[Size];

        public int Count {
            get {
                return count;
            }
        }
        
        public TValue this[TKey key] => GetNodeOrThrow(key).value;

        public bool ContainsKey(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var ata = tree[a];
            var atInHash = HashLength;
            while (ata != null)
            {
                if (hash == ata.hash && key.Equals(ata.key))
                {
                    return true;
                }
                ata = ata.next[(hash >> ++atInHash) & 0b1];
            }
            return false;
        }

        public KeyValue GetNodeOrThrow(TKey key)
        {
            var hash = (uint)key.GetHashCode();
            var a = hash % Size;
            var at = tree[a];
            var atInHash = HashLength;
            while (true)
            {
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at;
                }
                at = at.next[(hash >> ++atInHash) & 0b1];
            }
        }

        // TODO you are here
        // we don't have add or throw??
        // we don't have set??
        // we should expand the index

        public KeyValue GetOrAdd(KeyValue node)
        {
            
            var hash = node.hash;
            var a = ((uint)hash) % Size;
            var at = tree[a];
            var atInHash = HashLength;
            if (at == null && Interlocked.CompareExchange(ref tree[a], node, null) == null)
            {
                Interlocked.Increment(ref count);
                return node;
            }
            while (true)
            {
                if (hash == at.hash && node.key.Equals(at.key))
                {
                    return at;
                }
                var bit = (hash >> ++atInHash) & 0b1;
                if (at.next[bit] == null && Interlocked.CompareExchange(ref at.next[bit], node, null) == null)
                {
                    Interlocked.Increment(ref count);
                    return node;
                }
                at = at.next[bit];
            };
        }

        public bool TryGet(TKey key, out KeyValue res)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var at = tree[a];
            var atInHash = HashLength;
            while (true)
            {
                if (at == null)
                {
                    res = default;
                    return false;
                }
                if (hash == at.hash && key.Equals(at.key))
                {
                    res = at;
                    return true;
                }
                at = at.next[(hash >> ++atInHash) & 0b1];
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var at = tree[a];
            var atInHash = HashLength;
            while (true)
            {
                if (at == null)
                {
                    value = default;
                    return false;
                }
                if (hash == at.hash && key.Equals(at.key))
                {
                    value = at.value;
                    return true;
                }
                at = at.next[(hash >> ++atInHash) & 0b1];
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var l1 in tree)
            {
                if (l1 != null)
                {
                    foreach (var item in l1) {
                        yield return item;
                    }
                }
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

        public IEnumerable<TValue> Values {
            get {
                foreach (var item in this)
                {
                    yield return item.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}

