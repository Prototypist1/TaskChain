using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class ConcurrentSet<T> : IEnumerable<T>
    {

        TreeNode<TreeNode<TreeNode<SetListNode<T>>>> tree = new TreeNode<TreeNode<TreeNode<SetListNode<T>>>>(64);

        public ConcurrentSet()
        {
        }

        public ConcurrentSet(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                TryAdd(item);
            }
        }

        public bool Contains(T value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, value.GetHashCode()));
            var a = hash % 64;
            var ata = tree.backing[a];
            if (ata == null) {
                return false;
            }
            var b = (hash % 1024) / 64;
            var atb = ata.backing[b];
            if (atb== null)
            {
                return false;
            }
            var c = (hash % 4096) / 1024;
            var atc = atb.backing[c];
            while (atc != null)
            {
                if (object.Equals( atc.value,  value))
                {
                    return true;
                }
                atc = atc.next;
            }
            return false;
        }

        public bool ContainsOrAdd(T value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, value.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<SetListNode<T>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<SetListNode<T>>(4), null);
            var mine = new SetListNode<T>(value);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return false;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (object.Equals(at.value, value))
                {
                    return true;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return false;
                }
                at = at.next;
            };
        }

        public T GetOrAdd(T value)
        {
            
            var hash = Math.Abs(Math.Max(-int.MaxValue, value.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<SetListNode<T>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<SetListNode<T>>(4), null);
            var mine = new SetListNode<T>(value);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return value;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (object.Equals(at.value, value))
                {
                    return (T)at.value;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return value;
                }
                at = at.next;
            };
        }

        public bool TryAdd(T value)
        {
            return !ContainsOrAdd(value);
        }


        

        public void AddOrThrow(T value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, value.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<SetListNode<T>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<SetListNode<T>>(4), null);
            var mine = new SetListNode<T>(value);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (object.Equals(at.value, value))
                {
                    throw new Exception($"item already added: {value}");
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return;
                }
                at = at.next;
            };
        }

        public IEnumerator<T> GetEnumerator()
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
                                        yield return (T)at.value;
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
}