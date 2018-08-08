using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class RawConcurrentHashIndexedTree<TKey, TValue> : IEnumerable<ConcurrentIndexedListNode<TKey, TValue>>
    {
        private const int Size = 64;
        private const int BSize = 1024;
        private const int CSize = 4096;
        private readonly TreeNode<TreeNode<TreeNode<ConcurrentIndexedListNode<TKey, TValue>>>> tree = new TreeNode<TreeNode<TreeNode<ConcurrentIndexedListNode<TKey, TValue>>>>(Size);

        public bool Contains(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var ata = tree.backing[a];
            if (ata == null)
            {
                return false;
            }
            var b = (hash % BSize) / Size;
            var atb = ata.backing[b];
            if (atb == null)
            {
                return false;
            }
            var c = (hash % CSize) / BSize;
            var atc = atb.backing[c];
            while (atc != null)
            {
                if (hash == atc.hash && key.Equals(atc.key))
                {
                    return true;
                }
                atc = atc.next;
            }
            return false;
        }
        public ConcurrentIndexedListNode<TKey, TValue> GetNodeOrThrow(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var b = (hash % BSize) / Size;
            var c = (hash % CSize) / BSize;
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at;
                }
                at = at.next;
            };
        }
        public ConcurrentIndexedListNode<TKey, TValue> GetOrAdd(ConcurrentIndexedListNode<TKey, TValue> node)
        {
            var hash = node.key.GetHashCode();
            var a = ((uint)hash) % Size;
            var b = (hash % BSize) / Size;
            var c = (hash % CSize) / BSize;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<ConcurrentIndexedListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<ConcurrentIndexedListNode<TKey, TValue>>(4), null);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], node, null) == null)
            {
                return node;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (hash == at.hash && node.key.Equals(at.key))
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
                res = GetNodeOrThrow(key).Value;
                return true;
            }
            catch
            {
                res = default;
                return false;
            }
        }

        public IEnumerator<ConcurrentIndexedListNode<TKey, TValue>> GetEnumerator()
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

}