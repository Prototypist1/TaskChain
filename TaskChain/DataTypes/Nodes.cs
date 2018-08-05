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

    //public class SetListNode<TValue> : TreeListNode<TValue, SetListNode<TValue>>
    //{
    //    public SetListNode(TValue value): base(value)
    //    {
    //    }
    //}

    //public class IndexedListNode<TKey, TValue, TNext> : TreeListNode<TValue, TNext>
    //    where TNext : IndexedListNode<TKey, TValue, TNext>
    //{

    //    public IndexedListNode(TKey key, TValue value): base(value)
    //    {
    //        this.key = key;
    //    }
    //}


}
