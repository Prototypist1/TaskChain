using Prototypist.TaskChain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.DataTypes
{

    public class ConcurrentIndexed<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly RawConcurrentGrowingIndexedTree3<TKey, JumpBallConcurrent<TValue>> backing = new RawConcurrentGrowingIndexedTree3<TKey, JumpBallConcurrent<TValue>>();
        private const long enumerationAdd = 1_000_000_000;
        private long enumerationCount = 0;

        public int Count => backing.Count;

        private void NoModificationDuringEnumeration()
        {
            var res = Interlocked.Increment(ref enumerationCount);
            if (res >= enumerationAdd)
            {
                throw new Exception("No modification during enumeration");
            }
        }

        public bool ContainsKey(TKey key)
        {
            return backing.ContainsKey(key);
        }

        public TValue this[TKey key] {
            get => backing[key].Read();
            set =>Set(key, value);
        }

        public bool TryGet(TKey key, out TValue res)
        {
            try
            {
                res = backing[key].Read();
                return true;
            }
            catch
            {
                res = default;
                return false;
            }
        }

        public bool TryUpdate(TKey key, TValue newValue)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGetValue(key, out var item))
                {
                    item.SetValue(newValue);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public bool TryDo(TKey key, Func<TValue, TValue> action)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGetValue(key, out var item))
                {
                    item.Run(action);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void Set(TKey key, TValue value)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new JumpBallConcurrent<TValue>(value);
                var res = backing.GetOrAdd(key,toAdd);
                if (!ReferenceEquals(toAdd, res))
                {
                    res.SetValue(value);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TValue GetOrAdd(TKey key, TValue fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                return backing.GetOrAdd(key, new JumpBallConcurrent<TValue>(fallback)).Read();
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TValue GetOrAdd(TKey key, Func<TValue> fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var buildable = new BuildableJumpBallConcurrent<TValue>();
                var current = backing.GetOrAdd(key, buildable);
                if (ReferenceEquals(current, buildable))
                {
                    buildable.Build(fallback());
                }
                return current.Read();
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void AddOrThrow(TKey key, Func<TValue> fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var buildable = new BuildableJumpBallConcurrent<TValue>();
                var current = backing.GetOrAdd(key, buildable);
                if (ReferenceEquals(current, buildable))
                {
                    buildable.Build(fallback());
                }
                else
                {
                    throw new Exception("Item already exits");
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void AddOrThrow(TKey key, TValue fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new JumpBallConcurrent<TValue>(fallback);
                var current = backing.GetOrAdd(key, toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    throw new Exception("Item already exits");
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TValue DoOrAdd(TKey key, Func<TValue, TValue> action, TValue fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new JumpBallConcurrent<TValue>(fallback);
                var current = backing.GetOrAdd(key,toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    return current.Run(action);
                }
                return fallback;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TValue DoOrAdd(TKey key, Func<TValue, TValue> action, Func<TValue> fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var buildable = new BuildableJumpBallConcurrent<TValue>();
                var current = backing.GetOrAdd(key,buildable);
                if (ReferenceEquals(current, buildable))
                {
                    var fallbackValue = fallback();
                    buildable.Build(fallbackValue);
                    return fallbackValue;
                }
                else
                {
                    return current.Run(action);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                Interlocked.Add(ref enumerationCount, enumerationAdd);
                SpinWait.SpinUntil(() => Volatile.Read(ref enumerationCount) % enumerationAdd == 0);
                foreach (var item in backing)
                {
                    yield return item.Key;
                }
                Interlocked.Add(ref enumerationCount, -enumerationAdd);
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {

                Interlocked.Add(ref enumerationCount, enumerationAdd);
                SpinWait.SpinUntil(() => Volatile.Read(ref enumerationCount) % enumerationAdd == 0);
                foreach (var item in backing)
                {
                    yield return item.Value.Read();
                }
                Interlocked.Add(ref enumerationCount, -enumerationAdd);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Interlocked.Add(ref enumerationCount, enumerationAdd);
            SpinWait.SpinUntil(() => Volatile.Read(ref enumerationCount) % enumerationAdd == 0);
            foreach (var item in backing)
            {
                yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value.Read());
            }
            Interlocked.Add(ref enumerationCount, -enumerationAdd);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool TryGetValue(TKey key, out TValue value) => throw new NotImplementedException();
    }

    public static class ConcurrentHashIndexedTreeExtensions
    {
        public static void UpdateOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, TValue newValue)
        {
            if (self.TryUpdate(key, newValue))
            {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static TValue GetOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key)
        {
            if (self.TryGet(key, out var res))
            {
                return res;
            }
            throw new Exception("No item found for that key");
        }
        public static void DoOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, Func<TValue, TValue> action)
        {
            if (self.TryDo(key, action))
            {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static bool TryAdd<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, TValue value)
        {
            try
            {
                self.AddOrThrow(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static TValue UpdateOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, Func<TValue, TValue> function)
        {
            var res = default(TValue);
            if (self.TryDo(key, x =>
            {
                res = function(x);
                return res;
            }))
            {
                return res;
            }
            throw new Exception("No item found for that key");
        }
    }
}
