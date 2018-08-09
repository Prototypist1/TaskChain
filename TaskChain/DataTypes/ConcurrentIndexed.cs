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
        private readonly RawConcurrentHashIndexed<TKey, TValue> backing = new RawConcurrentHashIndexed<TKey, TValue>();
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
                res = backing.GetNodeOrThrow(key).Value;
                return true;
            }
            catch {
                res = default;
                return false;
            }
        }

        public bool TryUpdate(TKey key, TValue newValue)
        {
            try
            {
                NoModificationDuringEnumeration();
                if (backing.TryGet(key, out var item))
                {
                    item.Set(newValue);
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
        public bool TryDo<TOut>(TKey key, Func<TValue,(TValue, TOut)> function, out TOut res) {
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
                var toAdd = new ConcurrentIndexedListNode3<TKey, TValue>(key, value);
                backing.SetOrAdd(toAdd);
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
                var toAdd = new ConcurrentIndexedListNode3<TKey, TValue>(key, fallback);
                return backing.GetOrAdd(toAdd).Value;
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
                var toAdd = new BuildableConcurrentIndexedListNode3<TKey, TValue>(key);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd)) {
                    toAdd.Build(fallback());
                }
                return current.Value;
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
                var toAdd = new BuildableConcurrentIndexedListNode3<TKey, TValue>(key);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    toAdd.Build(fallback());
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
                var toAdd = new ConcurrentIndexedListNode3<TKey, TValue>(key, fallback);
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
        public void DoOrAdd(TKey key, Func<TValue, TValue> action, TValue fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new ConcurrentIndexedListNode3<TKey, TValue>(key, fallback);
                var current = backing.GetOrAdd(toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    current.Do(action);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public void DoOrAdd(TKey key, Func<TValue, TValue> action, Func<TValue> fallback) {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new BuildableConcurrentIndexedListNode3<TKey, TValue>(key);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    toAdd.Build(fallback());
                }
                else
                {
                    current.Do(action);
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public bool DoOrAdd<TOut>(TKey key, Func<TValue, (TValue,TOut)> function, TValue fallback, out TOut res)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new ConcurrentIndexedListNode3<TKey, TValue>(key, fallback);
                var current = backing.GetOrAdd(toAdd);
                if (!ReferenceEquals(current, toAdd))
                {
                    res = current.Do(function);
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
        public bool DoOrAdd<TOut>(TKey key, Func<TValue, (TValue, TOut)> function, Func<TValue> fallback, out TOut res)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new BuildableConcurrentIndexedListNode3<TKey, TValue>(key);
                var current = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(current, toAdd))
                {
                    toAdd.Build(fallback());
                    res = default;
                    return false;
                }
                else
                {
                    res = current.Do(function);
                    return true;
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TOut DoAddIfNeeded<TOut>(TKey key, Func<TValue, (TValue, TOut)> function, TValue fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new ConcurrentIndexedListNode3<TKey, TValue>(key, fallback);
                var current = backing.GetOrAdd(toAdd);
                return toAdd.Do(function);
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }
        public TOut DoAddIfNeeded<TOut>(TKey key, Func<TValue, (TValue, TOut)> function, Func<TValue> fallback)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new BuildableConcurrentIndexedListNode3<TKey, TValue>(key);
                var res = backing.GetOrAdd(toAdd);
                if (ReferenceEquals(res,toAdd))
                {
                    toAdd.Build(fallback());
                }
                return res.Do(function);
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
                yield return new KeyValuePair<TKey, TValue>(item.key, item.Value);
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
        public static void DoOrThrow<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<TValue, TValue> action) {
            if (self.TryDo(key, action)) {
                return;
            }
            throw new Exception("No item found for that key");
        }
        public static TOut DoOrThrow<TKey, TValue,TOut>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<TValue, (TValue, TOut)> function) {
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
            self.DoOrAdd(key, x => {
                res = function(x);
                return res;
                } , fallback);
            return res;
        }
        public static TValue UpdateOrAdd<TKey, TValue>(this ConcurrentHashIndexedTree<TKey, TValue> self, TKey key, Func<TValue, TValue> function, Func<TValue> fallback) {
            var res = default(TValue);
            self.DoOrAdd(key, x =>
            {
                res = function(x);
                return res;
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
                res = function(x);
                return res;
            })){
                return res;
            }
            throw new Exception("No item found for that key");
        }
    }
}
