using Prototypist.TaskChain.DataTypes;
using System.Threading;

namespace Prototypist.TaskChain
{

    internal class TreeNode<TChild>
    {
        public readonly TChild[] backing = new TChild[16];

        public TreeNode(int size)
        {
            backing = new TChild[size];
        }
    }

    internal class SetListNode<TValue> 
    {
        public readonly object value;
        public SetListNode<TValue> next;

        public SetListNode(TValue value)
        {
            this.value = value;
        }
    }


}
