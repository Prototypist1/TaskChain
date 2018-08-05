using Prototypist.TaskChain.DataTypes;
using System.Threading;

namespace Prototypist.TaskChain
{

    public class TreeNode<TChild>
    {
        public readonly TChild[] backing = new TChild[16];

        public TreeNode(int size)
        {
            backing = new TChild[size];
        }
    }

    public class IndexedListNode<TKey, TValue>
    {
        public readonly TKey key;
        public readonly TValue value;
        public IndexedListNode<TKey, TValue> next;

        public IndexedListNode(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
