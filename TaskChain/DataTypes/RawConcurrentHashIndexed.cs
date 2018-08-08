using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class RawConcurrentHashIndexed<TKey, TValue> : IEnumerable<ConcurrentIndexedListNode2<TKey, TValue>>
    {
        private const int Size = 128;
        private readonly TreeNode<ConcurrentIndexedListNode2<TKey, TValue>> tree = new TreeNode<ConcurrentIndexedListNode2<TKey, TValue>>(Size);

        public bool Contains(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var ata = tree.backing[a];
            while (ata != null)
            {
                if (hash == ata.hash && key.Equals(ata.key))
                {
                    return true;
                }
                ata = ata.next;
            }
            return false;
        }

        public ConcurrentIndexedListNode2<TKey, TValue> GetNodeOrThrow(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var at = tree.backing[a];
            while (true)
            {
                if (hash == at.hash && key.Equals(at.key))
                {
                    return at;
                }
                at = at.next;
            }
        }

        public ConcurrentIndexedListNode2<TKey, TValue> GetOrAdd(ConcurrentIndexedListNode2<TKey, TValue> node)
        {
            var hash = node.hash;
            var a = ((uint)hash) % Size;
            var at = tree.backing[a]; 
            if (at == null && Interlocked.CompareExchange(ref tree.backing[a], node, null) == null) {
                return node;
            }
            while (true)
            {
                if (hash == at.hash && node.key.Equals(at.key))
                {
                    return at;
                }
                if (at.next == null && Interlocked.CompareExchange(ref at.next, node, null) == null)
                {
                    return node;
                }
                at = at.next;
            };
        }

        public bool TryGet(TKey key, out ConcurrentIndexedListNode2<TKey, TValue> res)
        {
            try
            {
                res = GetNodeOrThrow(key);
                return true;
            }
            catch
            {
                res = default;
                return false;
            }
        }

        public IEnumerator<ConcurrentIndexedListNode2<TKey, TValue>> GetEnumerator()
        {
            foreach (var l1 in tree.backing)
            {
                if (l1 != null)
                {
                    var at = l1;
                    while (at != null)
                    {
                        yield return at;
                        at = at.next;
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}