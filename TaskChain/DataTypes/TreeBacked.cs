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



    internal class BuildableListNode<TKey,TValue>
        {
            public TKey key;
            public BuildableListNode<TKey, TValue> next;
            private InnerHave<TValue> innerHave;
            private const int TRUE = 1;
            private const int FALSE = 0;
            private int building = TRUE;
            private readonly ITaskManager taskManager;

            public BuildableListNode(TKey key, ITaskManager taskManager, TValue value) : this(key, taskManager)
            {
                this.innerHave = new InnerHave<TValue>(taskManager.GetActionChainer(), value);
                building = FALSE;
            }

            public BuildableListNode(TKey key, ITaskManager taskManager)
            {
                this.taskManager = taskManager;
                this.key = key;
            }

            public void Build(TValue res)
            {
                innerHave = new InnerHave<TValue>(taskManager.GetActionChainer(), res);
                building = FALSE;
            }

            public InnerHave<TValue> GetInner()
            {
                // save that volatile read if we can
                // idk is that even worth it ?
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                return innerHave;
            }
        }
    
}
