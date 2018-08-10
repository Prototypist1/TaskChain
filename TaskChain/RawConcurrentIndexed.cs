using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class RawConcurrentIndexed<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        public class KeyValue
        {
            public KeyValue next;
            public readonly TKey key;
            public readonly int hash;
            public readonly TValue value;

            public KeyValue(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
                this.hash = key.GetHashCode();
            }
        }

        private volatile int count;
        private const int Size = 128;
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
            while (ata != null)
            {
                if (hash == ata.hash && key.Equals(ata.key))
                {
                    return true;
                }
                ata = ata.next;
            }
            return false;
        }

        public KeyValue GetNodeOrThrow(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var at = tree[a];
            while (true)
            {
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at;
                }
                at = at.next;
            }
        }

        public KeyValue GetOrAdd(KeyValue node)
        {
            var hash = node.hash;
            var a = ((uint)hash) % Size;
            var at = tree[a];
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
                if (at.next == null && Interlocked.CompareExchange(ref at.next, node, null) == null)
                {
                    Interlocked.Increment(ref count);
                    return node;
                }
                at = at.next;
            };
        }

        public bool TryGet(TKey key, out KeyValue res)
        {
            try
            {
                res = GetNodeOrThrow(key);
                return true;
            }
            catch
            {
                res = default;
                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            try
            {
                value = GetNodeOrThrow(key).value;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var l1 in tree)
            {
                if (l1 != null)
                {
                    var at = l1;
                    while (at != null)
                    {
                        yield return new KeyValuePair<TKey, TValue>(at.key, at.value);
                        at = at.next;
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

