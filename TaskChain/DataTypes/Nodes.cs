using Prototypist.TaskChain.DataTypes;
using System;
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

    public class ConcurrentIndexedListNode<TKey, TValue>
    {
        public readonly int hash;
        public readonly TKey key;
        public ConcurrentIndexedListNode<TKey, TValue> next;

        public ConcurrentIndexedListNode(TKey key, TValue value, IActionChainer actionChainer)
        {
            this.key = key;
            this.Value = value;
            this.hash = key.GetHashCode();
            this.actionChainer = actionChainer;
        }

        public ConcurrentIndexedListNode(TKey key, TValue value) : this(key, value, Chaining.taskManager.GetActionChainer())
        {
        }

        public virtual TValue Value { get; protected set; }

        protected readonly IActionChainer actionChainer;

        public void Do(Func<TValue, TValue> action)
        {
            actionChainer.Run(() =>
            {
                Value = action(Value);
            });
        }

        public TRes Do<TRes>(Func<TValue, (TValue, TRes)> action)
        {
            return actionChainer.Run(() =>
            {
                var x = action(Value);
                Value = x.Item1;
                return x.Item2;
            });
        }
    }


    public class ConcurrentIndexedListNode2<TKey, TValue>
    {
        public readonly int hash;
        public readonly TKey key;
        public ConcurrentIndexedListNode2<TKey, TValue> next;
        protected Inner<TValue> item;
        protected Inner<TValue> oldItem;
        protected readonly ITaskManager taskManager;

        public virtual TValue Value
        {
            get
            {
                return oldItem.value;
            }
        }

        public ConcurrentIndexedListNode2(TKey key, TValue value, ITaskManager taskManager)
        {
            this.key = key;
            var item = new Inner<TValue> (value);
            this.item = item;
            this.oldItem = item;
            this.hash = key.GetHashCode();
            this.taskManager = taskManager;
        }

        public ConcurrentIndexedListNode2(TKey key, TValue value) : this(key, value, Chaining.taskManager)
        {
        }
        
        public void Set(TValue value)
        {
            var myItem = new Inner<TValue> { value = value };
            taskManager.SpinUntil(() =>
            {
                var localOldItem = oldItem;
                if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem)
                {
                    oldItem = myItem;
                    return true;
                }
                return false;
            });

        }

        public void Do(Func<TValue, TValue> action)
        {
            var myItem = new Inner<TValue>();

            taskManager.SpinUntil(() =>
            {
                var localOldItem = oldItem;
                if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem)
                {
                    oldItem.value = action(oldItem.value);
                    item = oldItem;
                    return true;
                }
                return false;
            });
        }

        public TRes Do<TRes>(Func<TValue, (TValue, TRes)> action)
        {
            var myItem = new Inner<TValue>();
            TRes res = default;
            taskManager.SpinUntil(() =>
            {
                var localOldItem = oldItem;
                if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem)
                {
                    var x = action(oldItem.value);
                    oldItem.value = x.Item1;
                    item = oldItem;
                    res = x.Item2;
                    return true;
                }
                return false;
            });
            return res;

        }
    }


    public class ConcurrentIndexedListNode3<TKey, TValue>
    {
        private readonly object mutex;
        public readonly TKey key;
        protected TValue value;
        public readonly int hash;
        public ConcurrentIndexedListNode3<TKey, TValue> next;

        public virtual TValue Value
        {
            get
            {
                return value;
            }
        }

        public ConcurrentIndexedListNode3(TKey key, TValue value)
        {
            this.mutex = new object();
            this.key = key;
            this.hash = key.GetHashCode();
            this.value = value;
        }
        

        public void Set(TValue value)
        {
            lock (mutex) {
                this.value = value;
            }
        }

        public void Do(Func<TValue, TValue> action)
        {
            lock (mutex)
            {
                value = action(value);
            }
        }

        public TRes Do<TRes>(Func<TValue, (TValue, TRes)> action)
        {
            lock (mutex)
            {
                var x = action(value);
                value = x.Item1;
                return x.Item2;
            }
        }
    }

    public class BuildableConcurrentIndexedListNode<TKey, TValue> : ConcurrentIndexedListNode<TKey, TValue>
    {
        private const int TRUE = 1;
        private const int FALSE = 0;
        private int building = TRUE;
        private readonly ITaskManager taskManager;

        public override TValue Value
        {
            get
            {
                taskManager.SpinUntil(() => building == TRUE);
                return base.Value;
            }
            protected set => base.Value = value;
        }

        public BuildableConcurrentIndexedListNode(TKey key, TValue value, ITaskManager taskManager) : base(key, value, taskManager.GetActionChainer())
        {
            building = FALSE;
            this.taskManager = taskManager;
        }

        public BuildableConcurrentIndexedListNode(TKey key, ITaskManager taskManager) : base(key, default, taskManager.GetActionChainer())
        {
            this.taskManager = taskManager;
        }

        public BuildableConcurrentIndexedListNode(TKey key, TValue value) : this(key, value, Chaining.taskManager)
        {
        }

        public BuildableConcurrentIndexedListNode(TKey key) : this(key, Chaining.taskManager)
        {
        }

        public void Build(TValue res)
        {
            this.Value = res;
            building = FALSE;
        }


    }


    public class BuildableConcurrentIndexedListNode2<TKey, TValue> : ConcurrentIndexedListNode2<TKey, TValue>
    {
        private const int TRUE = 1;
        private const int FALSE = 0;
        private int building = TRUE;
        protected Inner<TValue> oldItemCache; 

        public override TValue Value
        {
            get
            {
                taskManager.SpinUntil(() => building == FALSE);
                return base.Value;
            }
        }


        public BuildableConcurrentIndexedListNode2(TKey key, ITaskManager taskManager) : base(key, default, taskManager)
        {
            this.oldItemCache = oldItem;
            this.oldItem = new Inner<TValue>();
        }
        
        public BuildableConcurrentIndexedListNode2(TKey key) : this(key, Chaining.taskManager)
        {
        }

        public void Build(TValue res)
        {
            this.item.value = res;
            building = FALSE;
            this.oldItem = oldItemCache;
        }
    }

    public class BuildableConcurrentIndexedListNode3<TKey, TValue> : ConcurrentIndexedListNode3<TKey, TValue>
    {
        private ManualResetEventSlim manualReset = new ManualResetEventSlim();

        public override TValue Value
        {
            get
            {
                manualReset.Wait();
                return base.Value;
            }
        }
        
        public BuildableConcurrentIndexedListNode3(TKey key) : base(key, default)
        {
        }
        
        public void Build(TValue res)
        {
            value = res;
            manualReset.Set();
        }
    }
}
