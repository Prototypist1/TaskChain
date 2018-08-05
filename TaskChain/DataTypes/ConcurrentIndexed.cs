using Prototypist.TaskChain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{

    public class ConcurrentHashIndexedTree<TKey, TValue> :  IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly RawConcurrentHashIndexedTree<TKey, BuildableConcurrent<TValue>> backing = new RawConcurrentHashIndexedTree<TKey, BuildableConcurrent<TValue>>();
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

        public bool TryGet(TKey key, out TValue res)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item)) {
                    res = item.Value;
                    return true;
                }
                res = default;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool TryUpdate(TKey key, TValue newValue)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item))
                {
                    item.Do(x => x.Value = newValue);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool TryDo(TKey key, Action<Concurrent<TValue>.ValueHolder> action)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item))
                {
                    item.Do(action);
                    return true;
                }
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool TryDo<TOut>(TKey key, Func<Concurrent<TValue>.ValueHolder, TOut> function, out TOut res) {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item))
                {
                    res= item.Do(function);
                    return true;
                }
                res = default;
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
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>(value));
                var item = backing.GetOrAdd(toAdd);
                item.value.Do(x => x.Value = value);
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
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>(fallback));
                return backing.GetOrAdd(toAdd).value.Value;
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
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>());
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd)) {
                    toAdd.value.Build(fallback());
                }
                return current.value.Value;
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
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>());
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    toAdd.value.Build(fallback());
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
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>(fallback));
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
        public void DoOrAdd(TKey key, Action<Concurrent<TValue>.ValueHolder> action, TValue fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>(fallback));
                var current = backing.GetOrAdd(toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    current.value.Do(action);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void DoOrAdd(TKey key, Action<Concurrent<TValue>.ValueHolder> action, Func<TValue> fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>());
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    toAdd.value.Build(fallback());
                }
                else
                {
                    current.value.Do(action);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool DoOrAdd<TOut>(TKey key, Func<Concurrent<TValue>.ValueHolder,TOut> function, TValue fallback, out TOut res)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>(fallback));
                var current = backing.GetOrAdd(toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    res = current.value.Do(function);
                    return true;
                }
                res = default;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool DoOrAdd<TOut>(TKey key, Func<Concurrent<TValue>.ValueHolder, TOut> function, Func<TValue> fallback, out TOut res)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>());
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    toAdd.value.Build(fallback());
                    res = default;
                    return false;
                }
                else
                {
                    res = current.value.Do(function);
                    return true;
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TOut DoAddIfNeeded<TOut>(TKey key, Func<Concurrent<TValue>.ValueHolder, TOut> function, TValue fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>(fallback));
                var current = backing.GetOrAdd(toAdd);
                return toAdd.value.Do(function);
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TOut DoAddIfNeeded<TOut>(TKey key, Func<Concurrent<TValue>.ValueHolder, TOut> function, Func<TValue> fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<TKey, BuildableConcurrent<TValue>>(key, new BuildableConcurrent<TValue>());
                var res = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(res,toAdd))
                {
                    toAdd.value.Build(fallback());
                }
                return res.value.Do(function);
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
    
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            Interlocked.Add(ref enumerationCount, enumerationAdd);
            while (Volatile.Read(ref enumerationCount) % enumerationAdd != 0)
            {
                // TODO do tasks?
            }
            foreach (var item in backing)
            {
                yield return new KeyValuePair<TKey, TValue>(item.key, item.value.Value);
            }
            Interlocked.Add(ref enumerationCount, -enumerationAdd);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

    public static class ConcurrentHashIndexedTreeExtensions {
        public static void UpdateOrThrow<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, TValue newValue)
        {
            if (self.TryUpdate(key, newValue))
            {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static TValue GetOrThrow<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key) {
            if (self.TryGet(key, out var res)) {
                return res;
            }
            throw new Exception("No item found for that key");
        }
        public static void DoOrThrow<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Action<Concurrent<TValue>.ValueHolder> action) {
            if (self.TryDo(key, action)) {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static TOut DoOrThrow<TKey, TValue,TOut>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<Concurrent<TValue>.ValueHolder, TOut> function) {
            if (self.TryDo(key, function, out var res))
            {
                return res;
            }
            throw new Exception("No item found for that key");
        }
        //public static void AddOrThrow<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, TValue value) {
        //    var res = self.TryAdd(key, value);
        //    if (!res)
        //    {
        //        throw new Exception("No item found for that key");
        //    }
        //}
        public static bool TryAdd<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, TValue value) {
            try
            {
                self.AddOrThrow(key, value);
                return true;
            }
            catch {
                return false;
            }
        }
        public static TValue UpdateOrAdd<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<TValue, TValue> function, TValue fallback) {
            var res = fallback;
            self.DoOrAdd(key, x =>
            {
                res = function(x.Value);
                x.Value = res;
            },fallback);
            return res;
        }
        public static TValue UpdateOrAdd<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<TValue, TValue> function, Func<TValue> fallback) {
            var res = default(TValue);
            self.DoOrAdd(key, x =>
            {
                res = function(x.Value);
                x.Value = res;
            },()=> {
                res = fallback();
                return res;
                });
            return res;
        }
        public static TValue UpdateOrThrow<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<TValue, TValue> function) {
            var res = default(TValue);
            if (self.TryDo(key, x =>
            {
                res = function(x.Value);
                x.Value = res;
            })){
                return res;
            }
            throw new Exception("No item found for that key");
        }
    }
}
