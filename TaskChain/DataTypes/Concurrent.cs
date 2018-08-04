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

        public TValue Value
        {
            get
            {
                return (TValue)Volatile.Read(ref value);
            }
            private set
            {
                Volatile.Write(ref this.value, value);
            }
        }

        private readonly IActionChainer actionChainer;
        private object value;
        
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


    public class BuildableConcurrent<TValue>
    {

        private const int TRUE = 1;
        private const int FALSE = 0;
        private int building = TRUE;
        private readonly ITaskManager taskManager;
        private readonly IActionChainer actionChainer;
        private object value;

        public struct ValueHolder : IDisposable
        {
            private readonly BuildableConcurrent<TValue> owner;

            public ValueHolder(BuildableConcurrent<TValue> owner)
            {
                this.owner = owner;
            }

            public TValue Value
            {
                get => owner.Value;
                set => owner.Value = value;
            }

            public void Dispose(){}
        }

        public TValue Value
        {
            get
            {
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                return (TValue)Volatile.Read(ref value);
            }
            private set
            {
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                Volatile.Write(ref this.value, value);
            }
        }
        
        public BuildableConcurrent(TValue value, ITaskManager taskManager)
        {
            this.value = value;
            building = FALSE;
            this.taskManager = taskManager;
            this.actionChainer = taskManager.GetActionChainer();
        }

        public BuildableConcurrent(ITaskManager taskManager)
        {
            this.taskManager = taskManager;
            this.actionChainer = taskManager.GetActionChainer();
        }

        public BuildableConcurrent(TValue value) : this(value, Chaining.taskManager)
        {
        }

        public void Do(Action<ValueHolder> action)
        {
            actionChainer.Run(() => {
                using (var wrapper = new ValueHolder(this))
                {
                    action(wrapper);
                }
            });
        }

        public TRes Do<TRes>(Func<ValueHolder, TRes> action)
        {
            return actionChainer.Run(() => {
                using (var wrapper = new ValueHolder(this))
                {
                    return action(wrapper);
                }
            });
        }

        public void Build(TValue res)
        {
            this.value = res;
            building = FALSE;
        }
    }

    public class BuildableListNode<TKey, TValue> : BuildableConcurrent<TValue>
    {
        public readonly TKey key;
        public BuildableListNode<TKey, TValue> next;

        public BuildableListNode(TKey key, ITaskManager taskManager) : base(taskManager)
        {
            this.key = key;
        }

        public BuildableListNode(TKey key, TValue value) : base(value)
        {
            this.key = key;
        }

        public BuildableListNode(TKey key,  ITaskManager taskManager, TValue value) : base(value, taskManager)
        {
            this.key = key;
        }
    }
}
