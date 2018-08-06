using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class RawConcurrentHashIndexed<TKey, TValue> : IEnumerable<IndexedListNode<TKey, TValue>>
    {
        private const int Size = 64;
        private readonly TreeNode<IndexedListNode<TKey, TValue>> tree = new TreeNode<IndexedListNode<TKey, TValue>>(Size);

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
        public IndexedListNode<TKey, TValue> GetNodeOrThrow(TKey key)
        {
            var hash = key.GetHashCode();
            var a = ((uint)hash) % Size;
            var at = tree.backing[a];
            x:
            if (hash == at.hash && key.Equals(at.key))
            {
                return at;
            }
            at = at.next;
            goto x;
        }
        public IndexedListNode<TKey, TValue> GetOrAdd(IndexedListNode<TKey, TValue> node)
        {
            var hash = node.key.GetHashCode();
            var a = ((uint)hash) % Size;
            var at = Interlocked.CompareExchange(ref tree.backing[a], node, null);
            if (at == null)
            {
                return node;
            }
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