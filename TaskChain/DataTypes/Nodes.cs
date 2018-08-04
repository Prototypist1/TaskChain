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
        public readonly TValue value;
        public SetListNode<TValue> next;

        public SetListNode(TValue value)
        {
            this.value = value;
        }
    }

    internal class BuildableListNode<TKey, TValue> : IValueNode<TValue>
    {
        public TKey key;
        public BuildableListNode<TKey, TValue> next;
        private object value;
        private const int TRUE = 1;
        private const int FALSE = 0;
        private int building = TRUE;
        private readonly ITaskManager taskManager;
        public readonly IActionChainer actionChainer;

        public TValue Value
        {
            get {
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                return (TValue)Volatile.Read(ref value);
            }
            set
            {
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                Volatile.Write(ref this.value, value);
            }
        }

        public BuildableListNode(TKey key, ITaskManager taskManager, TValue value) : this(key, taskManager)
        {
            this.value = value;
            building = FALSE;
        }

        public BuildableListNode(TKey key, ITaskManager taskManager)
        {
            this.taskManager = taskManager;
            actionChainer = taskManager.GetActionChainer();
            this.key = key;
        }

        public void Build(TValue res)
        {
            this.value = res;
            building = FALSE;
        }
    }

    public interface IValueNode<TValue>
    {
        TValue Value{get;set;}
    }

}
