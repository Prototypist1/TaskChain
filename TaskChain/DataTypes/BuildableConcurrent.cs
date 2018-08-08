using System;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class BuildableConcurrent<TValue>: Concurrent<TValue>
    {

        private const int TRUE = 1;
        private const int FALSE = 0;
        private int building = TRUE;
        private readonly ITaskManager taskManager;

        public BuildableConcurrent(TValue value, ITaskManager taskManager):base(value, taskManager.GetActionChainer())
        {
            building = FALSE;
            this.taskManager = taskManager;
        }

        public BuildableConcurrent(ITaskManager taskManager) : base(default, taskManager.GetActionChainer())
        {
            this.taskManager = taskManager;
        }

        public BuildableConcurrent(TValue value) : this(value, Chaining.taskManager)
        {
        }

        public BuildableConcurrent() : this(Chaining.taskManager)
        {
        }

        public void Build(TValue res)
        {
            this.Value = res;
            building = FALSE;
        }

        public override void Do(Func<TValue, TValue> action) {
            taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
            base.Do(action);
        }

        public override TRes Do<TRes>(Func<TValue,(TValue, TRes)> action) {
            taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
            return base.Do(action);
        }
    }
}
