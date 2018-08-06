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

        public struct ValueHolder:IDisposable
        {
            private readonly Concurrent<TValue> owner;

            public ValueHolder(Concurrent<TValue> owner)
            {
                this.owner = owner;
            }

            public TValue Value
            {
                get => owner.Value;
                set => owner.Value = value;
            }

            public void Dispose() { }
        }

        public virtual TValue Value
        {
            get
            {
                return value;
            }
            protected set
            {
                this.value = value;
            }
        }

        protected readonly IActionChainer actionChainer;
        protected TValue value;

        public Concurrent(TValue value, IActionChainer actionChainer)
        {
            this.value = value;
            this.actionChainer = actionChainer;
        }

        public Concurrent(TValue value):this(value,Chaining.taskManager.GetActionChainer())
        {
        }

        public void Do(Action<ValueHolder> action)
        {
            actionChainer.Run(() => {
                using (var wrapper = new ValueHolder(this)) {
                    action(wrapper);
                }
            });
        }

        public TRes Do<TRes>(Func<ValueHolder,TRes> action)
        {
            return actionChainer.Run(() => {
                using (var wrapper = new ValueHolder(this))
                {
                    return action(wrapper);
                }
            });
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
