using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.DataTypes
{
    public class Concurrent<TValue>
    {
        public TValue Value { get; protected set; }

        protected readonly IActionChainer actionChainer;

        public Concurrent(TValue value, IActionChainer actionChainer)
        {
            //this.wrapper = new ValueHolder(this);
            this.Value = value;
            this.actionChainer = actionChainer;
        }

        public Concurrent(TValue value) : this(value, Chaining.taskManager.GetActionChainer())
        {
        }

        public virtual void Do(Func<TValue,TValue> action)
        {
            actionChainer.Run(() =>
            {
                Value = action(Value);
            });
        }

        public virtual TRes Do<TRes>(Func<TValue,(TValue, TRes)> action)
        {
            return actionChainer.Run(() =>
            {
                var x = action(Value);
                Value = x.Item1;
                return x.Item2;
            });
        }
    }

    public class Inner<TValue>
    {
        public TValue value;
    }

    public class Concurrent2<TValue>
    {

        private Inner<TValue> item;
        private Inner<TValue> oldItem;
        private readonly TaskManager taskManager;

        public TValue Value
        {
            get
            {
                return oldItem.value;
            }
        }

        public void Set(TValue value) {
            var myItem = new Inner<TValue> { value = value };
            var localOldItem = oldItem;
            if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem) {
                oldItem = myItem;
            }
            else {
                taskManager.SpinUntil(() =>
                {
                    localOldItem = oldItem;
                    if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem)
                    {
                        oldItem = myItem;
                        return true;
                    }
                    return false;
                });
            }
        }

        public virtual void Do(Func<TValue, TValue> action)
        {
            var myItem = new Inner<TValue>();
            var localOldItem = oldItem;
            if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem)
            {
                oldItem.value = action(oldItem.value);
                item = oldItem;
            }
            else
            {
                taskManager.SpinUntil(() =>
                {
                    localOldItem = oldItem;
                    if (Interlocked.CompareExchange(ref item, myItem, localOldItem) == localOldItem)
                    {
                        oldItem.value = action(oldItem.value);
                        item = oldItem;
                        return true;
                    }
                    return false;
                });
            }
        }

    }

    //public class ConcurrentListNode<TValue> : Concurrent<TValue>
    //{
    //    public ConcurrentListNode<TValue> next;

    //    public ConcurrentListNode( TValue value) : base(value)
    //    {
    //    }
    //}

    //public class BuildableConcurrentListNode<TKey, TValue> : BuildableConcurrent<TValue>
    //{
    //    public readonly TKey key;
    //    public BuildableConcurrentListNode<TKey, TValue> next;

    //    public BuildableConcurrentListNode(TKey key, ITaskManager taskManager) : base(taskManager)
    //    {
    //        this.key = key;
    //    }

    //    public BuildableConcurrentListNode(TKey key, TValue value) : base(value)
    //    {
    //        this.key = key;
    //    }

    //    public BuildableConcurrentListNode(TKey key,  ITaskManager taskManager, TValue value) : base(value, taskManager)
    //    {
    //        this.key = key;
    //    }
    //}
}
