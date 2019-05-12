using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public class RawConcurrentGrowingIndexedTree3<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
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

        private struct PassThrough {
            public PassThrough(Memory<object> memory)
            {
                this.memory = memory;
            }

            public readonly Memory<object> memory;
        }

        private class Orchard
        {
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

        public TValue this[TKey key] => GetOrThrow(key);

        private Orchard orchard;
        private readonly int arrayMask;
        private readonly int sizeInBit;
        private readonly int arraySize;
        private int targetOrchardSize;

        // TODO you are here
        // at large size we are full of nearly empty list
        // list spawn more lists
        // I think we need to maintain a next list and use memory more aggressively

        // I sometimes miss resizing
        // if too many items are added while we are resizing
        // when the new orchard is swaped in it is smaller than the count so we never grow again
        // if we are bigger then double the current target try to double the target if you get it enqueue

        public RawConcurrentGrowingIndexedTree3() : this(2) { }

        public RawConcurrentGrowingIndexedTree3(int sizeInBit)
        {
            this.arrayMask = (1 << sizeInBit) - 1;
            this.sizeInBit = sizeInBit;
            this.arraySize = 1 << sizeInBit;
            this.orchard = new Orchard(new object[16], 4, 0b1111);
            this.targetOrchardSize = orchard.items.Length;
        }

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
            int localOffset, size;
            Span<object> array;

            var hash = key.GetHashCode();

            var localOrchard = orchard;
            int totalSizeInBits = 32 - localOrchard.sizeInBit;
            var at = localOrchard.items[(hash >> (totalSizeInBits)) & localOrchard.mask];

#pragma warning disable CS0164 // This label has not been referenced
        WhereTo:
#pragma warning restore CS0164 // This label has not been referenced

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                toAdd = new Value(hash, key, value);
                newIndex = new object[arraySize];
                localOffset = totalSizeInBits - sizeInBit;
                localIndex = newIndex;
                while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask))
                {
                    var nextLocalIndex = new object[arraySize];
                    localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                    localIndex = nextLocalIndex;
                    localOffset -= sizeInBit;
                }
                localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
                localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
                nextAt = Interlocked.CompareExchange(ref localOrchard.items[(hash >> (totalSizeInBits)) & localOrchard.mask], newIndex, at);
                if (ReferenceEquals(nextAt, at))
                {
                    size = targetOrchardSize;
                    if (Interlocked.Increment(ref count) > (size >> 2))
                    {
                        Resize(size);
                    }
                    return toAdd.value;
                }
                at = nextAt;
                // we actually know this is an array
                goto WhereToWithToAdd;
            }

            if (at == null)
            {
                toAdd = new Value(hash, key, value);
                if ((at = Interlocked.CompareExchange(ref localOrchard.items[(hash >> (totalSizeInBits)) & localOrchard.mask], toAdd, null)) == null)
                {
                    size = targetOrchardSize;
                    if (Interlocked.Increment(ref count) > (size >> 2))
                    {
                        Resize(size);
                    }
                    return toAdd.value;
                }
                goto WhereToWithToAdd;
            }

            if (at is object[])
            {
                array = new Span<object>(at as object[]);
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }

            if (at is Memory<object>)
            {
                array = ((Memory<object>)at).Span;
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }

            throw new Exception("must be one of those!");

        WhereToWithToAdd:

            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                newIndex = new object[arraySize];
                localOffset = totalSizeInBits - sizeInBit;
                localIndex = newIndex;
                while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask))
                {
                    var nextLocalIndex = new object[arraySize];
                    localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                    localIndex = nextLocalIndex;
                    localOffset -= sizeInBit;
                }
                localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
                localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
                nextAt = Interlocked.CompareExchange(ref localOrchard.items[(hash >> (totalSizeInBits)) & localOrchard.mask], newIndex, at);
                if (ReferenceEquals(nextAt, at))
                {
                    size = targetOrchardSize;
                    if (Interlocked.Increment(ref count) > (size >> 2))
                    {
                        Resize(size);
                    }
                    return toAdd.value;
                }
                at = nextAt;
                // we actually know this is an array
                goto WhereToWithToAdd;
            }

            if (at == null)
            {
                if ((at = Interlocked.CompareExchange(ref localOrchard.items[(hash >> (totalSizeInBits)) & localOrchard.mask], toAdd, null)) == null)
                {
                    size = targetOrchardSize;
                    if (Interlocked.Increment(ref count) > (size >> 2))
                    {
                        Resize(size);
                    }
                    return toAdd.value;
                }
                goto WhereToWithToAdd;
            }

            if (at is object[])
            {
                array = new Span<object>(at as object[]);
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }

            if (at is Memory<object>)
            {
                array = ((Memory<object>)at).Span;
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }
            
            throw new Exception("must be one of those!");

        WhereToOffOrchard:
            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                goto IsValueOffOrcard;
            }

            if (at == null)
            {
                goto IsNullOffOrcard;
            }

            if (at is object[])
            {
                array = new Span<object>(at as object[]);
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }

            if (at is Memory<object>)
            {
                array = ((Memory<object>)at).Span;
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }

            if (at is PassThrough) {
                array = ((PassThrough)at).memory.Span;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToOffOrchard;
            }

            throw new Exception("this should never happen");


        WhereToWithToAddOffOrchard:
            if ((existingValue = at as Value) != null)
            {
                if (existingValue.hash == hash)
                {
                    goto HashMatch;
                }
                goto IsValueWithToAdd;
            }

            if (at == null)
            {
                goto IsNullWithToAddOffOrcard;
            }

            if (at is object[])
            {
                array = new Span<object>(at as object[]);
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToWithToAddOffOrchard;
            }

            if (at is Memory<object>)
            {
                array = ((Memory<object>)at).Span;
                totalSizeInBits -= sizeInBit;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToWithToAddOffOrchard;
            }

            if (at is PassThrough)
            {
                array = ((PassThrough)at).memory.Span;
                at = array[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereToWithToAddOffOrchard;
            }

            throw new Exception("this should never happen");

        IsNullOffOrcard:
            toAdd = new Value(hash, key, value);
        IsNullWithToAddOffOrcard:
            if ((at = Interlocked.CompareExchange(ref array[(hash >> (totalSizeInBits)) & arrayMask], toAdd, null)) == null)
            {
                size = targetOrchardSize;
                if (Interlocked.Increment(ref count) > (size >> 2))
                {
                    Resize(size);
                }
                return toAdd.value;
            }
            goto WhereToWithToAddOffOrchard;

        IsValueOffOrcard:
            toAdd = new Value(hash, key, value);
        IsValueWithToAdd:
            newIndex = new object[arraySize];
            localOffset = totalSizeInBits - sizeInBit;
            localIndex = newIndex;
            while (((existingValue.hash >> (localOffset)) & arrayMask) == ((toAdd.hash >> (localOffset)) & arrayMask))
            {
                var nextLocalIndex = new object[arraySize];
                localIndex[((existingValue.hash >> (localOffset)) & arrayMask)] = nextLocalIndex;
                localIndex = nextLocalIndex;
                localOffset -= sizeInBit;
            }
            localIndex[(existingValue.hash >> (localOffset)) & arrayMask] = existingValue;
            localIndex[(toAdd.hash >> (localOffset)) & arrayMask] = toAdd;
            nextAt = Interlocked.CompareExchange(ref array[(hash >> (totalSizeInBits)) & arrayMask], newIndex, at);
            if (ReferenceEquals(nextAt, at))
            {
                size = targetOrchardSize;
                if (Interlocked.Increment(ref count) > (size >> 2))
                {
                    Resize(size);
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
            size = targetOrchardSize;
            if (Interlocked.Increment(ref count) > (size >> 2))
            {
                Resize(size);
            }
            return toAdd.value;
        }

        public bool TryGetValue(TKey key, out TValue res)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var hash = key.GetHashCode();

            var localOrchard = orchard;
            int totalSizeInBits = 32 - localOrchard.sizeInBit;
            var at = localOrchard.items[(hash >> (totalSizeInBits)) & localOrchard.mask];

        WhereTo:

            if (at is Value existingValue) {
                if (existingValue.hash == hash)
                {
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
                }
                res = default;
                return false;
            }

            if (at is null)
            {
                res = default;
                return false;
            }

            if (at is object[] objects) {
                totalSizeInBits -= sizeInBit;
                at = objects[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereTo;
            }

            if (at is Memory<object> mem) {
                totalSizeInBits -= sizeInBit;
                at = mem.Span[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereTo;
            }

            if (at is PassThrough pass) {
                at = pass.memory.Span[(hash >> (totalSizeInBits)) & arrayMask];
                goto WhereTo;
            }

            throw new Exception("must be one of those!");
            
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

        private QueueingConcurrent<int> resizer = new QueueingConcurrent<int>(0);


        private void Resize(int size)
        {
            if (Interlocked.CompareExchange(ref targetOrchardSize, size * arraySize, size) == size)
            {
                resizer.Act(x =>
                {
                    var nextOrcard = new object[orchard.items.Length * arraySize];
                    for (var at = 0; at < orchard.items.Length; at++)
                    {
                        var target = orchard.items[at];

                        if (target is null)
                        {
                            var toAdd = new Memory<object>(nextOrcard, at * arraySize, arraySize);
                            target = Interlocked.CompareExchange(ref orchard.items[at], toAdd, null);
                        }

                        if (target is Value value)
                        {
                            var toAdd = new Memory<object>(nextOrcard, at * arraySize, arraySize);
                            toAdd.Span[(value.hash >> (32 - orchard.sizeInBit - sizeInBit)) & arrayMask] = value;
                            target = Interlocked.CompareExchange(ref orchard.items[at], toAdd, value);
                        }

                        if (target is object[] array)
                        {
                            for (var j = 0; j < array.Length; j++)
                            {
                                var innerTarget = array[j];

                                if (innerTarget is null)
                                {
                                    var toAdd = new PassThrough(new Memory<object>(nextOrcard, at * arraySize, arraySize));
                                    innerTarget = Interlocked.CompareExchange(ref array[j], toAdd, null);
                                }

                                if (innerTarget is Value innerValue)
                                {
                                    var toAdd = new PassThrough(new Memory<object>(nextOrcard, at * arraySize, arraySize));
                                    toAdd.memory.Span[j] = innerValue;
                                    innerTarget = Interlocked.CompareExchange(ref array[j], toAdd, innerValue);
                                }

                                if (innerTarget is Memory<object>)
                                {
                                    throw new Exception("bug");
                                }

                                if (innerTarget is PassThrough)
                                {
                                    throw new Exception("bug");
                                }

                                if (innerTarget is object[])
                                {
                                    nextOrcard[(at * arraySize) + j] = array[j];
                                }
                            }
                        }

                        if (target is Memory<object>)
                        {
                            throw new Exception("bug");
                        }

                        if (target is PassThrough)
                        {
                            throw new Exception("bug");
                        }
                    }
                    orchard = new Orchard(nextOrcard, orchard.sizeInBit + sizeInBit, (orchard.mask << sizeInBit) | arrayMask);
                    return x;
                });
            }  
        }


        #endregion

        #region Debugging

        private string PlacementDebugger()
        {
            var strings = new List<string>();
            var myOrcard = orchard;
            for (int i = 0; i < myOrcard.items.Length; i++)
            {
                PlacementDebugger(myOrcard.items[i], sizeInBit, new string[] { IntToString(i, myOrcard.sizeInBit) }, strings);
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
                if (!IntToStringReverse(value.hash, 32).StartsWith(string.Join("", indexes)))
                {
                    strings.Add(string.Join(", ", indexes) + " : " + IntToString(value.hash, 32));
                }
            }
        }


        private void CheckCount()
        {
            var subCound = SubCount(orchard.items);
            if (subCound < count)
            {
                var error = 0;
            }
            //var pd = PlacementDebugger();
            //if (pd != "")
            //{
            //    var error = 0;
            //}

            //if (!TryGetValue(key, out var _))
            //{
            //    var db = PlacementDebugger();
            //}
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

            if (thing is Memory<object> mem)
            {
                var res = 0;
                for (int i = 0; i < mem.Length; i++)
                {
                    res += SubCount(mem.Span[i]);
                }
                return res;
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

        private static string IntToStringReverse(int i, int bits)
        {
            var res = "";
            for (int j = 1; j <= bits; j++)
            {
                res += i >> (32 - j) & 1;
            }
            return res;
        }


        #endregion
    }
}

