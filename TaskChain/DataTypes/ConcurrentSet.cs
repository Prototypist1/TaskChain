using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class RawConcurrentHashIndexedTree<TKey,TValue> : IEnumerable<IndexedListNode<TKey, TValue>>
    {
        private readonly TreeNode<TreeNode<TreeNode<IndexedListNode<TKey,TValue>>>> tree = new TreeNode<TreeNode<TreeNode<IndexedListNode<TKey, TValue>>>>(64);
        
        public bool Contains(TKey key)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var ata = tree.backing[a];
            if (ata == null)
            {
                return false;
            }
            var b = (hash % 1024) / 64;
            var atb = ata.backing[b];
            if (atb == null)
            {
                return false;
            }
            var c = (hash % 4096) / 1024;
            var atc = atb.backing[c];
            while (atc != null)
            {
                if (object.Equals(atc.key, key))
                {
                    return true;
                }
                atc = atc.next;
            }
            return false;
        }
        private IndexedListNode<TKey, TValue> GetNodeOrThrow(TKey key)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            return tree.backing[a].backing[b].backing[c];
        }
        public IndexedListNode<TKey, TValue> GetOrAdd(IndexedListNode<TKey, TValue> node)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, node.key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<IndexedListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<IndexedListNode<TKey, TValue>>(4), null);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], node, null) == null)
            {
                return node;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (object.Equals(at.key, node.key))
                {
                    return at;
                }
                if (Interlocked.CompareExchange(ref at.next, node, null) == null)
                {
                    return node;
                }
                at = at.next;
            };
        }
        public bool TryGet(TKey key, out TValue res)
        {
            try
            {
                res = GetNodeOrThrow(key).value;
                return true;
            }
            catch
            {
                res = default;
                return false;
            }
        }
        
        public IEnumerator<IndexedListNode<TKey, TValue>> GetEnumerator()
        {
            foreach (var l1 in tree.backing)
            {
                if (l1 != null)
                {
                    foreach (var l2 in l1.backing)
                    {
                        if (l2 != null)
                        {
                            foreach (var l3 in l2.backing)
                            {
                                if (l3 != null)
                                {
                                    var at = l3;
                                    while (at != null)
                                    {
                                        yield return at;
                                        at = at.next;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly RawConcurrentHashIndexedTree<T, BuildableConcurrent<T>> backing = new RawConcurrentHashIndexedTree<T, BuildableConcurrent<T>>();
        private const long enumerationAdd = 1_000_000_000;
        private long enumerationCount =0;

        private void NoModificationDuringEnumeration() {
            var res = Interlocked.Increment(ref enumerationCount);
            if (res >= enumerationAdd)
            {
                throw new Exception("No modification during enumeration");
            }
        }

        public bool Contains(T value)
        {
            return backing.Contains(value);
        }

        // TODO some of these are extensions

        public T GetOrAdd(T value)
        {
            try
            {
                NoModificationDuringEnumeration();

                return backing.GetOrAdd(new IndexedListNode<T, BuildableConcurrent<T>>(value, new BuildableConcurrent<T>(value))).value.Value;
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }

        }

        public void AddOrThrow(T value)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<T, BuildableConcurrent<T>>(value, new BuildableConcurrent<T>(value));
                var res = backing.GetOrAdd(toAdd);
                if (!object.ReferenceEquals(res, toAdd)){
                    throw new Exception("Item already added");
                }
            }
            finally
            {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public bool TryAdd(T value)
        {
            try
            {
                NoModificationDuringEnumeration();
                var toAdd = new IndexedListNode<T, BuildableConcurrent<T>>(value, new BuildableConcurrent<T>(value));
                var res = backing.GetOrAdd(toAdd);
                return object.ReferenceEquals(res, toAdd);
            }
            finally {
                Interlocked.Decrement(ref enumerationCount);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            Interlocked.Add(ref enumerationCount, enumerationAdd);
            while (Volatile.Read(ref enumerationCount) % enumerationAdd != 0) {
                // TODO do tasks?
            }
            foreach (var item in backing)
            {
                yield return item.value.Value;
            }
            Interlocked.Add(ref enumerationCount, -enumerationAdd);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
}