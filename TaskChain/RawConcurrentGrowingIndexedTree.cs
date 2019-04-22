using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
            private int resize = 0;

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

            private bool Resize(int startingHashPosition,out KeyValue result)
            {

                // 'lock' the item we are resizing
                // we never unlock it, don't want it to be resized again
                if (Interlocked.CompareExchange(ref resize,1,0)==0) {

                    // allocat new array
                    var newArray = new KeyValue[1 << (this.sizeInBits + 1)];

                    // create the new KeyValue
                    result = new KeyValue(this.key, this.value, this.hash, newArray, this.sizeInBits + 1);

                    // fill the old structure with dummies
                    this.FillWithDummies(startingHashPosition);

                    // re insert the whole structure
                    this.InsertAll(startingHashPosition, result);

                    return true;
                }
                result = default;
                return false;

            }

            private void FillWithDummies(int startingHashPosition)
            {
                for (int i = 0; i < next.Length; i++)
                {
                    var child = Interlocked.CompareExchange(ref next[i], new KeyValue((this.hash & ~((1 << (32 - startingHashPosition)) - 1)) | (i << (32 - startingHashPosition - this.sizeInBits)), new KeyValue[1], 0), null);
                    if (child != null) {
                        child.FillWithDummies(startingHashPosition + this.sizeInBits);
                    }
                }
            }

            private void InsertAll(int startingHashPosition, KeyValue insertIn)
            {
                for (int i = 0; i < next.Length; i++)
                {
                    var toInsert = next[i];

                    if (!(toInsert.dummy && toInsert.next[0] is KeyValue))
                    {
                        ForceAddItem(startingHashPosition, insertIn, toInsert);
                    }

                    toInsert.InsertAll(startingHashPosition, insertIn);
                }
            }

            private void ForceAddItem(int startingHashPosition, KeyValue insertIn, KeyValue toInsert)
            {


                var node = toInsert.dummy ? toInsert : new KeyValue(toInsert.key, toInsert.value, toInsert.hash);

                var hash = key.GetHashCode();
                var hashPosition = startingHashPosition;

                var at = insertIn;

                while ((at = Interlocked.CompareExchange(ref at.next[(hash >> (32 - hashPosition - at.sizeInBits)) & ((1 << at.sizeInBits) - 1)], node, null)) is KeyValue)
                {
                    hashPosition += at.sizeInBits;
                }
            }
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
        KeyValue root = new KeyValue(default, default, 0,new KeyValue[2],1);

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
                at = at.next[(hash >> (32 - hashPosition - at.sizeInBits)) & ((1 << at.sizeInBits) - 1)];
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at.value;
                }
                hashPosition += at.sizeInBits;
            }
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            var hash = key.GetHashCode();
            var hashPosition = 0;

            var at = root;

            // if it is just a get we want to avoid allocation
            while (at.next[(hash >> (32 - hashPosition - at.sizeInBits)) & ((1 << at.sizeInBits) - 1)] is KeyValue next)
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
                at = Interlocked.CompareExchange(ref at.next[(hash >> (32 - hashPosition - at.sizeInBits)) & ((1 << at.sizeInBits) - 1)], node, null);
                if ((at == null) || (hash == at.hash && key.Equals(at.key)))
                {
                    if (at == null)
                    {
                        Interlocked.Increment(ref count);
                        return node.value;
                    }
                    return at.value;
                }
                hashPosition += at.sizeInBits;
            }
        }

        public bool TryGetValue(TKey key, out TValue res)
        {
            var at = root;
            var hash = key.GetHashCode();
            var hashPosition = 0;

            while (true)
            {
                at = at.next[(hash >> (32 - hashPosition - at.sizeInBits)) & ((1 << at.sizeInBits) - 1)];
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
                hashPosition += at.sizeInBits;
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

