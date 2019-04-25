using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public class RawConcurrentGrowingIndexedTree<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        // think about nodes owning width
        // auto resizing
        // having sizing based on level:
        // top index is other indexes are smaller
        private class KeyValue : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            public readonly KeyValue[] next;
            public readonly TKey key;
            public readonly int hash;
            public readonly TValue value;
            public readonly bool dummy;
            public readonly int sizeInBits;

            public KeyValue(TKey key, TValue value, int hash)
            {
                this.key = key;
                this.value = value;
                this.hash = hash;
                this.sizeInBits = 1;
                this.next = new KeyValue[1<<sizeInBits];
                this.dummy = false;
            }

            public KeyValue(TKey key, TValue value, int hash, KeyValue[] next, int sizeInBits)
            {
                this.key = key;
                this.value = value;
                this.hash = hash;
                this.sizeInBits = sizeInBits;
                this.next = next;
                this.dummy = false;
            }

            public KeyValue(int hash, KeyValue[] next, int sizeInBits)
            {
                this.hash = hash;
                this.sizeInBits = sizeInBits;
                this.next = next;
                this.dummy = true;
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                yield return new KeyValuePair<TKey, TValue>(key, value);
                for (int i = 0; i < next.Length; i++)
                {
                    var item = next[i];
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

            // we only resize when all our childrne exist
            // we only resize if none of the children are dummies
            // we only resize when all our children are the same size
            
            public KeyValue Resize(int startingHashPosition,int addedWidth)
            {
                // allocat new array
                var newArray = new KeyValue[1 << (sizeInBits + addedWidth)];

                // create the new KeyValue
                var result = new KeyValue(this.key, this.value, this.hash, newArray, sizeInBits + addedWidth);

                // fill the old structure with dummies
                for (int i = 0; i < next.Length; i++)
                {
                    var child = next[i];
                    for (int j = 0; j < child.next.Length; j++)
                    {
                        Interlocked.CompareExchange(ref child.next[j], new KeyValue((child.hash & ~((1 << (32 - startingHashPosition - this.sizeInBits)) - 1)) | (j << (32 - startingHashPosition - this.sizeInBits - child.sizeInBits)), new KeyValue[1], 0), null);
                        newArray[(i * child.next.Length) + j] = child.next[j];
                    }
                }

                // re insert the whole structure
                for (int i = 0; i < next.Length; i++)
                {
                    ForceAddItem(startingHashPosition, result, next[i]);
                }

                return result;
            }

            private void ForceAddItem(int startingHashPosition, KeyValue insertIn, KeyValue toInsert)
            {
                var node = new KeyValue(toInsert.key, toInsert.value, toInsert.hash);

                var hash = node.hash;

                var at = insertIn;
                var hashPosition = startingHashPosition + at.sizeInBits;

                while ((at = Interlocked.CompareExchange(ref at.next[(hash >> (32 - hashPosition)) & ((1 << at.sizeInBits) - 1)], node, null)) is KeyValue)
                {
                    hashPosition += at.sizeInBits;
                }
            }

            public bool CanResize(out int addedWidth) {
                if (dummy)
                {
                    addedWidth = default;
                    return false;
                }

                for (int i = 0; i < next.Length; i++)
                {
                    if (next[i] == null)
                    {
                        addedWidth = default;
                        return false;
                    }
                    if (next[i].dummy)
                    {
                        addedWidth = default;
                        return false;
                    }
                    if (next[i].sizeInBits != next[0].sizeInBits)
                    {
                        addedWidth = default;
                        return false;
                    }
                }
                addedWidth = next[0].sizeInBits;
                return true;
            }

            public void RemoveExpiredDummies()
            {
                for (int i = 0; i < next.Length; i++)
                {
                    if (next[i] != null)
                    {
                        while (next[i].dummy && next[i].next[0] != null)
                        {
                            next[i] = next[i].next[0];
                        }
                        next[i].RemoveExpiredDummies();
                    }
                }
            }
        }

        private string PlacementDebugger() {
            var strings = new List<string>();
            PlacementDebugger(root, new string[0], strings);
            return string.Join(Environment.NewLine, strings);
        }

        private void PlacementDebugger(KeyValue target,  string[] indexes, List<string> strings) {
            if (target == null)
            {
                return;
            }

            if (!IntToString(target.hash, 32).StartsWith(string.Join("", indexes)))
            {
                strings.Add(string.Join(", ", indexes) + " : " + IntToString(target.hash, 32) + " " + target.dummy);
            }

            for (int i = 0; i < target.next.Length; i++)
            {
                var nextIndex = indexes.Select(x => x).ToList();
                nextIndex.Add(IntToString(i, target.sizeInBits));
                PlacementDebugger(target.next[i], nextIndex.ToArray(), strings);
            }
        }

        private static string IntToString(int i,int bits) {
            var res = "";
            for (int j = bits - 1; j >= 0; j--)
            {
                res += i >> j & 1;
            }
            return res;
        }

        private int count = 0;

        public int Count
        {
            get
            {
                return count;
            }
        }

        public TValue this[TKey key] => GetOrThrow(key);

        // probably should be bigger
        KeyValue root = new KeyValue(default, default, 0,new KeyValue[1024],10);

        public bool ContainsKey(TKey key)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashPosition = 0;

            while (true)
            {
                at = at.next[(hash >> (32-hashPosition-at.sizeInBits)) & ((1 << at.sizeInBits)-1)];

                if (at == null)
                {
                    return false;
                }

                if (hash == at.hash && key.Equals(at.key))
                {
                    return true;
                }
                hashPosition += at.sizeInBits;
            }
        }

        public TValue GetOrThrow(TKey key)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashPosition = 0;

            while (true)
            {
                hashPosition += at.sizeInBits;
                at = at.next[(hash >> (32 - hashPosition)) & ((1 << at.sizeInBits) - 1)];
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at.value;
                }
            }
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            var hash = key.GetHashCode();
            

            var at = root;
            var hashPosition = at.sizeInBits;

            // if it is just a get we want to avoid allocation
            while (at.next[(hash >> (32 - hashPosition)) & ((1 << at.sizeInBits) - 1)] is KeyValue next)
            {
                at = next;
                hashPosition += at.sizeInBits;

                if (hash == at.hash && key.Equals(at.key))
                {
                    return at.value;
                }
            }

            var node = new KeyValue(key, value, hash);

            while (true)
            {
                at = Interlocked.CompareExchange(ref at.next[(hash >> (32 - hashPosition)) & ((1 << at.sizeInBits) - 1)], node, null);

                if (at == null)
                {
                    var counter = Interlocked.Increment(ref count);
                    if (counter > 1024 && (counter & (counter - 1)) == 0 && Interlocked.CompareExchange(ref cleanUpLock, 1, 0) == 0)
                    {
                        Task.Run(CleanUp);
                    }
                    return node.value;
                }

                if (hash == at.hash && key.Equals(at.key))
                {
                    return at.value;
                }

                hashPosition += at.sizeInBits;
            }
        }

        public bool TryGetValue(TKey key, out TValue res)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashPosition = at.sizeInBits;

            while (true)
            {
                at = at.next[(hash >> (32 - hashPosition)) & ((1 << at.sizeInBits) - 1)];

                if (at == null)
                {
                    res = default;
                    return false;
                }

                if (hash == at.hash && key.Equals(at.key))
                {

                    res = at.value;
                    return true;
                }

                hashPosition += at.sizeInBits;
            }
        }

        private int cleanUpLock = 0;
        private void CleanUp() {
            root.RemoveExpiredDummies();

            ConsiderResizing(ref root, 0);

            cleanUpLock = 0;
        }

        private void ConsiderResizing(ref KeyValue keyValues,int startingHashPosition) {
            if (keyValues == null) {
                return;
            }

            while (keyValues.CanResize(out var addedWidth)) {
                keyValues = keyValues.Resize(startingHashPosition,addedWidth);
            }
            for (int i = 0; i < keyValues.next.Length; i++)
            {
                ConsiderResizing(ref keyValues.next[i], startingHashPosition + keyValues.sizeInBits);
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

