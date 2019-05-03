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
        public int depth;


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

        private class Orchard
        {
            public readonly object[][] items;
            public readonly int sizeInBit;
            public readonly int mask;

            public Orchard(object[][] items, int size, int mask)
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

        public TValue this[TKey key] => GetOrThrow(key);

        private Orchard orchard = new Orchard(new object[][] {
            new object[arraySize],new object[arraySize],new object[arraySize],new object[arraySize],
            new object[arraySize],new object[arraySize],new object[arraySize],new object[arraySize],
            new object[arraySize],new object[arraySize],new object[arraySize],new object[arraySize],
            new object[arraySize],new object[arraySize],new object[arraySize],new object[arraySize]
        }, 4, 0b1111);
        private const int arrayMask = 0b1;
        private const int sizeInBit = 1;
        private const int arraySize = 2;
        private const int resizingSize = 0b1111111;

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
            int localOffset;

            var hash = key.GetHashCode();

            var localOrchard = orchard;
            var array = localOrchard.items[hash & localOrchard.mask];
            var at = array[(hash >> localOrchard.sizeInBit) & arrayMask];
            int totalSizeInBits = localOrchard.sizeInBit;

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
            newIndex = new object[arraySize];
            localOffset = totalSizeInBits + sizeInBit;
            localIndex = newIndex;
            while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask))
            {
                var nextLocalIndex = new object[arraySize];
                localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                localIndex = nextLocalIndex;
                localOffset += sizeInBit;
            }
            localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
            localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
            nextAt = Interlocked.CompareExchange(ref array[(hash >> totalSizeInBits) & arrayMask], newIndex, at);
            if (ReferenceEquals(nextAt, at))
            {
                if ((Interlocked.Increment(ref count) & resizingSize) == 0b0)
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    TryResize();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
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
            while (null != Interlocked.CompareExchange(ref existingValue.next, toAdd, null))
            {
                existingValue = existingValue.next;
                if (existingValue.key.Equals(key))
                {
                    return existingValue.value;
                }
            }
            if ((Interlocked.Increment(ref count) & resizingSize) == 0b0)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                TryResize();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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

            var hash = key.GetHashCode();

            var localOrchard = orchard;
            var array = localOrchard.items[hash & localOrchard.mask];
            var at = array[(hash >> localOrchard.sizeInBit) & arrayMask];
            int totalSizeInBits = localOrchard.sizeInBit;

        IsArray:
            if (at is object[])
            {
                Interlocked.Increment(ref depth);
                totalSizeInBits += sizeInBit;
                array = at as object[];
                at = array[(hash >> totalSizeInBits) & arrayMask];
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
                yield return new KeyValuePair<TKey, TValue>(value.key, value.value);
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

        #region Resize

        // we resize if thie first two layors are full and all of it's kids are full
        // we never want and add to have to be done to the new orchard and the old orchard
        // by only resizing with full arrays we ensure no one will add to those arrays

        // more over these items can not be changed
        // is they need to be arrays
        // values can turn in to arrays

        private int at = 0;
        private int resizing = 0;

        private async Task<bool> TryResize()
        {
            return await Task.Run(() =>
            {
                if (Interlocked.CompareExchange(ref resizing, 1, 0) == 0)
                {
                    if (CanResize())
                    {
                        var array = new object[orchard.items.Length * arraySize][];

                        for (int i = 0; i < orchard.items.Length; i++)
                        {
                            var target = orchard.items[i] as object[];
                            for (int j = 0; j < target.Length; j++)
                            {
                                array[(j * orchard.items.Length) + i] = (object[])target[j];
                            }
                        }
                        orchard = new Orchard(array, orchard.sizeInBit + sizeInBit, (orchard.mask << sizeInBit) | arrayMask);

                        at = 0;
                        resizing = 0;
                        return true;
                    }
                    else
                    {
                        resizing = 0;
                    }
                }
                return false;
            });
        }

        private bool CanResize()
        {
            var filledIn = 0;
            while (at < orchard.items.Length)
            {
                var target = orchard.items[at];
                for (int i = 0; i < target.Length; i++)
                {
                    var innerTarget = target[i];

                    if (innerTarget is null)
                    {
                        filledIn++;
                        var toAdd = new object[arraySize];
                        innerTarget = Interlocked.CompareExchange(ref target[i], toAdd, null);
                    }

                    if (innerTarget is Value value)
                    {
                        filledIn++;
                        var toAdd = new object[arraySize];
                        toAdd[(value.hash >> (orchard.sizeInBit + arrayMask)) & arrayMask] = value;
                        Interlocked.CompareExchange(ref target[i], toAdd, value);
                    }

                    if (filledIn >= resizingSize)
                    {
                        return false;
                    }
                }

                at++;
            }
            return true;
        }



        #endregion

        #region Debugging


        private string PlacementDebugger()
        {
            var strings = new List<string>();
            for (int i = 0; i < orchard.items.Length; i++)
            {
                PlacementDebugger(orchard.items[i], sizeInBit, new string[] { IntToString(i, orchard.sizeInBit) }, strings);
            }
            return string.Join(Environment.NewLine, strings);
        }

        private void PlacementDebugger(object target, int width, string[] indexes, List<string> strings)
        {
            if (target == null)
            {
                return;
            }

            if (target is object[] objects)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    var nextIndex = indexes.Select(x => x).ToList();
                    nextIndex.Add(IntToString(i, width));
                    PlacementDebugger(objects[i], sizeInBit, nextIndex.ToArray(), strings);
                }
                return;
            }

            if (target is Value value)
            {
                if (!IntToString(value.hash, 32).EndsWith(string.Join("", indexes.Reverse())))
                {
                    strings.Add(string.Join(", ", indexes) + " : " + IntToString(value.hash, 32));
                }
            }
        }


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

