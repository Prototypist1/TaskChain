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

    public class ConcurrentIndexed<TKey, TValue> :  IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>> backing = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>();
        private const long enumerationAdd = 1_000_000_000;
        private long enumerationCount = 0;

        private void NoModificationDuringEnumeration()
        {
            var res = Interlocked.Increment(ref enumerationCount);
            if (res >= enumerationAdd)
            {
                throw new Exception("No modification during enumeration");
            }
        }

        public bool ContainsKey(TKey key) {
            return backing.Contains(key);
        }

        public bool TryGet(TKey key, out TValue res)
        {
            try
            {
                res = backing.GetNodeOrThrow(key).value.Read();
                return true;
            }
            catch {
                res = default;
                return false;
            }
        }

        public async Task<bool> TryUpdate(TKey key, TValue newValue)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item))
                {
                    await item.value.SetValue(newValue);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public async Task<bool> TryDo(TKey key, Func<TValue, TValue> action)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item))
                {
                    await item.value.Act(action);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void Set(TKey key, TValue value) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, new QueueingConcurrent<TValue>(value));
                var res = backing.GetOrAdd(toAdd);
                if (!ReferenceEquals(toAdd, res)) {
                    res.value.SetValue(value);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TValue GetOrAdd(TKey key, TValue fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, new QueueingConcurrent<TValue>(fallback));
                return backing.GetOrAdd(toAdd).value.Read();
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TValue GetOrAdd(TKey key, Func<TValue> fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var buildable = new BuildableQueueingConcurrent<TValue>();
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, buildable);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd)) {
                    buildable.Build(fallback());
                }
                return current.value.Read();
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
                var buildable = new BuildableQueueingConcurrent<TValue>();
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, buildable);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    buildable.Build(fallback());
                }
                else {
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
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, new QueueingConcurrent<TValue>(fallback));
                var current = backing.GetOrAdd(toAdd);
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
        public Task<TValue> DoOrAdd(TKey key, Func<TValue, TValue> action, TValue fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, new QueueingConcurrent<TValue>(fallback));
                var current = backing.GetOrAdd(toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    return current.value.Act(action);
                }
                return Task.FromResult(fallback);
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public Task<TValue> DoOrAdd(TKey key, Func<TValue, TValue> action, Func<TValue> fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var buildable = new BuildableQueueingConcurrent<TValue>();
                var toAdd = new RawConcurrentIndexed<TKey, QueueingConcurrent<TValue>>.KeyValue(key, buildable);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    var fallbackValue = fallback();
                    buildable.Build(fallbackValue);
                    return Task.FromResult(fallbackValue);
                }
                else
                {
                    return current.value.Act(action);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

    
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            Interlocked.Add(ref enumerationCount, enumerationAdd);
            SpinWait.SpinUntil(() => Volatile.Read(ref enumerationCount) % enumerationAdd == 0);
            foreach (var item in backing)
            {
                yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value.Read());
            }
            Interlocked.Add(ref enumerationCount, -enumerationAdd);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

    public static class ConcurrentHashIndexedTreeExtensions {
        public static async Task UpdateOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, TValue newValue)
        {
            if (await self.TryUpdate(key, newValue))
            {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static TValue GetOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key) {
            if (self.TryGet(key, out var res)) {
                return res;
            }
            throw new Exception("No item found for that key");
        }
        public static async Task DoOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, Func<TValue, TValue> action) {
            if (await self.TryDo(key, action)) {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static bool TryAdd<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, TValue value) {
            try
            {
                self.AddOrThrow(key, value);
                return true;
            }
            catch {
                return false;
            }
        }
        public static async Task<TValue> UpdateOrThrow<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> self, TKey key, Func<TValue, TValue> function) {
            var res = default(TValue);
            if (await self.TryDo(key, x =>
            {
                res = function(x);
                return res;
            })){
                return res;
            }
            throw new Exception("No item found for that key");
        }
    }
}
