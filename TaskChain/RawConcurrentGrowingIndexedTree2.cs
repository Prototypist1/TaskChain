using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public class RawConcurrentGrowingIndexedTree2<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {

        private class Value 
        {
            public Value next;
            public readonly int hash;
            public readonly TKey key;
            public readonly TValue value;

            public Value(int hash, TKey key, TValue value)
            {
                this.hash = hash;
                this.key = key;
                this.value = value;
            }
        }

        private struct Orchard {
            public readonly object[] items;
            public readonly int sizeInBit;
            public readonly int mask;

            public Orchard(object[] items, int size, int mask)
            {
                this.items = items ?? throw new ArgumentNullException(nameof(items));
                this.sizeInBit = size;
                this.mask = mask;
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

        private string PlacementDebugger()
        {
            var strings = new List<string>();
            for (int i = 0; i < orchard.items.Length; i++)
            {
                PlacementDebugger(orchard.items[i], 2, new string[] { IntToString(i, 4) }, strings);
            }
            return string.Join(Environment.NewLine, strings);
        }

        private void PlacementDebugger(object target,int width, string[] indexes, List<string> strings)
        {
            if (target == null)
            {
                return;
            }

            if (target is object[] objects) {
                for (int i = 0; i < objects.Length; i++)
                {
                    var nextIndex = indexes.Select(x => x).ToList();
                    nextIndex.Add(IntToString(i, width));
                    PlacementDebugger(objects[i], 2, nextIndex.ToArray(), strings);
                }
                return;
            }

            if (target is Value value) {
                if (!IntToString(value.hash, 32).EndsWith(string.Join("", indexes.Reverse())))
                {
                    strings.Add(string.Join(", ", indexes) + " : " + IntToString(value.hash, 32));
                }
            }
        }

        public TValue this[TKey key] => GetOrThrow(key);

        Orchard orchard = new Orchard(new object[16],4,0b1111);
        private const int arrayMask = 0b11;
        private const int sizeInBit = 2;

        public bool ContainsKey(TKey key) => TryGetValue(key, out var _);
        
        public TValue GetOrThrow(TKey key)
        {
            if (TryGetValue(key, out var res)) return res;
            throw new KeyNotFoundException($"{key.ToString()} not found");
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Value existingValue;
            Value toAdd;
            object[] newIndex, localIndex;
            object nextAt;

            int totalSizeInBits = 0, localOffset;

            var localOrchard = orchard;
            var array = localOrchard.items;

            var hash = key.GetHashCode();

            var at = array[hash & localOrchard.mask];
            
            if (at is object[])
            {
                array = at as object[];
                totalSizeInBits += localOrchard.sizeInBit;
                at = array[(hash >> totalSizeInBits) & arrayMask];
                goto WhereToOffOrchard;
            }

            if (at is null)
            {
                toAdd = new Value(hash, key, value);
                if ((at = Interlocked.CompareExchange(ref localOrchard.items[hash  & localOrchard.mask], toAdd, null)) == null)
                {
                    Interlocked.Increment(ref count);
                    return toAdd.value;
                }
                goto WhereToWithToAdd;
            }

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                newIndex = new object[4];
                localOffset = localOrchard.sizeInBit;
                localIndex = newIndex;
                toAdd = new Value(hash, key, value);
                while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask)) {
                    var nextLocalIndex = new object[4];
                    localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                    localIndex = nextLocalIndex;
                    localOffset += sizeInBit;
                }
                localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
                localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
                nextAt = Interlocked.CompareExchange(ref localOrchard.items[hash  & localOrchard.mask], newIndex, at);
                if (ReferenceEquals(nextAt, at))
                {
                    Interlocked.Increment(ref count);
                    return toAdd.value;
                }
                at = nextAt;
                // we actually know this is an array
                goto WhereToWithToAdd;
            }
            throw new Exception("code should never get here");


        WhereToWithToAdd:
            if (at is object[])
            {
                array = at as object[];
                totalSizeInBits += localOrchard.sizeInBit;
                at = array[(hash >> totalSizeInBits) & arrayMask];
                goto WhereToWithToAddOffOrchard;
            }

            if (at is null)
            {
                if ((at = Interlocked.CompareExchange(ref localOrchard.items[hash & localOrchard.mask], toAdd, null)) == null)
                {
                    Interlocked.Increment(ref count);
                    return toAdd.value;
                }
                goto WhereToWithToAdd;
            }

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                newIndex = new object[4];
                localOffset = localOrchard.sizeInBit;
                localIndex = newIndex;
                while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask))
                {
                    var nextLocalIndex = new object[4];
                    localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                    localIndex = nextLocalIndex;
                    localOffset += sizeInBit;
                }
                localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
                localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
                nextAt = Interlocked.CompareExchange(ref localOrchard.items[hash & localOrchard.mask], newIndex, at);
                if (ReferenceEquals(nextAt, at))
                {
                    Interlocked.Increment(ref count);
                    return toAdd.value;
                }
                at = nextAt;
                // we actually know this is an array
                goto WhereToWithToAdd;
            }
            throw new Exception("code should never get here");

        WhereToWithToAddOffOrchard:
            if (at is object[])
            {
                array = at as object[];
                totalSizeInBits += sizeInBit;
                at = array[(hash >> totalSizeInBits) & arrayMask];
                goto WhereToWithToAddOffOrchard;
            }

            if (at == null)
            {
                goto IsNullWithToAddOffOrcard;
            }

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                goto IsValueWithToAdd;
            }

            throw new Exception("this should never happen");

        WhereToOffOrchard:
            if (at is object[])
            {
                array = at as object[];
                totalSizeInBits += sizeInBit;
                at = array[(hash >> totalSizeInBits) & arrayMask];
                goto WhereToOffOrchard;
            }

            if (at == null)
            {
                goto IsNullOffOrcard;
            }

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                goto IsValueOffOrcard;
            }
            throw new Exception("this should never happen");

        IsNullOffOrcard:
            toAdd = new Value(hash, key, value);
        IsNullWithToAddOffOrcard:
            if ((at = Interlocked.CompareExchange(ref array[(hash >> totalSizeInBits) & arrayMask], toAdd, null)) == null)
            {
                Interlocked.Increment(ref count);
                return toAdd.value;
            }
            goto WhereToWithToAddOffOrchard;

        IsValueOffOrcard:
            toAdd = new Value(hash, key, value);
        IsValueWithToAdd:
            newIndex = new object[4];
            localOffset = totalSizeInBits + sizeInBit;
            localIndex = newIndex;
            while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask))
            {
                var nextLocalIndex = new object[4];
                localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                localIndex = nextLocalIndex;
                localOffset += sizeInBit;
            }
            localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
            localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
            nextAt = Interlocked.CompareExchange(ref array[(hash >> totalSizeInBits) & arrayMask], newIndex, at);
            if (ReferenceEquals(nextAt, at))
            {
                Interlocked.Increment(ref count);
                return toAdd.value;
            }
            at = nextAt;
            // we actually know this is an array
            goto WhereToWithToAddOffOrchard;

        HashMatch:
            if (existingValue.key.Equals(key))
            {
                return existingValue.value;
            }
            while (existingValue.next != null)
            {
                existingValue = existingValue.next;
                if (existingValue.key.Equals(key))
                {
                    return existingValue.value;
                }
            }
            toAdd = new Value(hash, key, value);
            while (null == Interlocked.CompareExchange(ref existingValue.next, toAdd, null))
            {
                existingValue = existingValue.next;
                if (existingValue.key.Equals(key))
                {
                    return existingValue.value;
                }
            }
            return toAdd.value;
        }

        public bool TryGetValue(TKey key, out TValue res)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Value existingValue;

            int totalSizeInBits = 0;

            var localOrchard = orchard;
            var array = localOrchard.items;

            var hash = key.GetHashCode();

            var at = array[hash & localOrchard.mask];

            if ((array = at as object[]) != null)
            {
                totalSizeInBits += localOrchard.sizeInBit;
            IsArray:
                at = array[(hash >> totalSizeInBits) & arrayMask];
                if (at is object[])
                {
                    totalSizeInBits += sizeInBit;
                    array = at as object[];
                    goto IsArray;
                }
                if (at == null)
                {
                    res = default;
                    return false;
                }
                if ((existingValue = at as Value) != null)
                {
                    if (existingValue.hash == hash)
                    {
                        goto HashMatch;
                    }
                    res = default;
                    return false;
                }
                throw new Exception("this should never happen");
            }

            if (at is null)
            {
                res = default;
                return false;
            }

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }

                res = default;
                return false;
            }
            throw new Exception("code should never get here");

        HashMatch:
            if (existingValue.key.Equals(key))
            {
                res = existingValue.value;
                return true;
            }
            while (existingValue.next != null)
            {
                existingValue = existingValue.next;
                if (existingValue.key.Equals(key))
                {
                    res = existingValue.value;
                    return true;
                }
            }
            res = default;
            return false;
        }

        private IEnumerable<KeyValuePair<TKey, TValue>> Iterate(object thing)
        {
            
            if (thing is Value value)
            {
                yield return new KeyValuePair<TKey, TValue>(value.key,value.value);
            }

            if (thing is object[] things)
            {
                foreach (var item in Iterate(things))
                {
                    yield return item;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var thing in orchard.items)
            {
                foreach (var item in Iterate(thing))
                {
                    yield return item;
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


        #region Debugging

        private void CheckCount(TKey key)
        {
            //var subCound = SubCount(orchard.items);
            //if (subCound != count)
            //{
            //    var error = 0;
            //}
            //var pd = PlacementDebugger();
            //if (pd != "")
            //{
            //    var error = 0;
            //}

            if (!TryGetValue(key, out var _))
            {
                var db = PlacementDebugger();
            }
        }

        private int SubCount(object thing)
        {
            if (thing is null)
            {
                return 0;
            }

            if (thing is Value value)
            {
                return 1 + SubCount(value.next);
            }

            if (thing is object[] things)
            {
                return things.Sum(x => SubCount(x));
            }

            throw new Exception("bug");
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private static string IntToString(int i, int bits)
        {
            var res = "";
            for (int j = bits - 1; j >= 0; j--)
            {
                res += i >> j & 1;
            }
            return res;
        }


        #endregion

    }
}

