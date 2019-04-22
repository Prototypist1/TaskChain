using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public class RawConcurrentGrowingIndex<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {

        private class KeyValue : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            public readonly KeyValue[] next;
            public readonly TKey key;
            public readonly uint hash;
            public readonly TValue value;
            public readonly bool dummy;

            public KeyValue(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
                this.hash = (uint)key.GetHashCode();
                this.next = new KeyValue[2];
                this.dummy = false;
            }

            public KeyValue()
            {
                this.next = new KeyValue[2];
                this.dummy = true;
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                if (!dummy)
                {
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                    if (next[0] != null)
                    {
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

            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private int reindexing =0;
        private volatile int count;
        private int HashLength;
        private KeyValue[] tree;

        public RawConcurrentGrowingIndex() {
            HashLength = 7;
            tree = new KeyValue[0b1 << (HashLength-1)];
            for (int i = 0; i < tree.Length; i++)
            {
                var toAdd = new KeyValue();
                toAdd.next[0] = new KeyValue();
                toAdd.next[1] = new KeyValue();
                tree[i] = toAdd;
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public TValue this[TKey key] => GetOrThrow(key);

        public bool ContainsKey(TKey key)
        {
            var hash = key.GetHashCode();
            var myTree = tree;
            var a = ((uint)hash) % myTree.Length;
            var ata = myTree[a];
            var atInHash = HashLength;
            do
            {
                if (!ata.dummy && hash == ata.hash && key.Equals(ata.key))
                {
                    return true;
                }
                atInHash += 1;
                ata = ata.next[(hash >> atInHash) & 0b1];
            } while (ata != null);
            return false;
        }

        public TValue GetOrThrow(TKey key)
        {
            var hash = (uint)key.GetHashCode();
            var myTree = tree;
            var a = hash % myTree.Length;
            var at = myTree[a];
            var atInHash = HashLength;
            while (true)
            {
                if (!at.dummy && hash == at.hash && key.Equals(at.key))
                {
                    return at.value;
                }
                atInHash += 1;
                at = at.next[(hash >> atInHash) & 0b1];
            }
        }

        private void AddToEnd(KeyValue node)
        {
            var hash = node.hash;
            var myTree = tree;
            var a = ((uint)hash) % myTree.Length;
            var at = myTree[a];
            var atInHash = HashLength;
            if (at == null && Interlocked.CompareExchange(ref myTree[a], node, null) == null)
            {
                return;
            }
            while (true)
            {
                atInHash += 1;
                var bit = (hash >> atInHash) & 0b1;
                if (at.next[bit] == null && Interlocked.CompareExchange(ref at.next[bit], node, null) == null)
                {
                    return;
                }
                at = at.next[bit];
            };
        }

        public TValue GetOrAdd(TKey key,TValue value)
        {
            var node = new KeyValue(key, value);
            // sometime we re-index
            // we could even do it on another thread
            if (count > tree.Length && Interlocked.CompareExchange(ref reindexing,1,0) ==0)
            {
                // don't even wait
                Task.Run(() =>
                {
                    for (int i = 0; i < tree.Length; i++)
                    {
                        if (!tree[i].dummy)
                        {
                            AddToEnd(new KeyValue(tree[i].key, tree[i].value));
                        }
                    }

                    var mySize = tree.Length;
                    var next = new KeyValue[mySize * 2];
                    for (int i = 0; i < tree.Length; i++)
                    {
                        next[i] = tree[i].next[0];
                        Interlocked.CompareExchange(ref next[i].next[0], new KeyValue(), null);
                        Interlocked.CompareExchange(ref next[i].next[1], new KeyValue(), null);
                        next[mySize + i] = tree[i].next[1];
                        Interlocked.CompareExchange(ref next[mySize + i].next[0], new KeyValue(), null);
                        Interlocked.CompareExchange(ref next[mySize + i].next[1], new KeyValue(), null);
                    }

                    // now i need to set two things at once but I can't do that
                    // I just need everyone to use a 'myTree' and myTree.Length
                    tree = next;

                    reindexing = 0;
                });
            }

            var hash = node.hash;
            var myTree = tree;
            var a = ((uint)hash) % myTree.Length;
            var at = myTree[a];
            var atInHash = HashLength;
            if (at == null && Interlocked.CompareExchange(ref myTree[a], node, null) == null)
            {
                Interlocked.Increment(ref count);
                return node.value;
            }
            while (true)
            {
                if (!at.dummy && hash == at.hash && node.key.Equals(at.key))
                {
                    return at.value;
                }
                atInHash += 1;
                var bit = (hash >> atInHash) & 0b1;
                if (at.next[bit] == null && Interlocked.CompareExchange(ref at.next[bit], node, null) == null)
                {
                    Interlocked.Increment(ref count);
                    return node.value;
                }
                at = at.next[bit];
            };
        }

        public bool TryGetValue(TKey key, out TValue res)
        {
            var hash = key.GetHashCode();
            var myTree = tree;
            var a = ((uint)hash) % myTree.Length;
            var at = myTree[a];
            var atInHash = HashLength;
            while (true)
            {
                if (at == null)
                {
                    res = default;
                    return false;
                }
                if (!at.dummy && hash == at.hash && key.Equals(at.key))
                {
                    res = at.value;
                    return true;
                }
                atInHash += 1;
                at = at.next[(hash >> atInHash) & 0b1];
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {

            var keys = new HashSet<TKey>();

            foreach (var l1 in tree)
            {
                if (l1 != null)
                {
                    foreach (var item in l1)
                    {
                        if (keys.Add(item.Key))
                        {
                            yield return item;
                        }
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

