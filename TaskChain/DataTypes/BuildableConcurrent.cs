using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public class BuildableConcurrent<TValue>: Concurrent<TValue>
    {

        private const int TRUE = 1;
        private const int FALSE = 0;
        private int building = TRUE;
        private readonly ITaskManager taskManager;
        
        public override TValue Value
        {
            get
            {
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                return (TValue)Volatile.Read(ref value);
            }
            protected set
            {
                taskManager.SpinUntil(() => building == FALSE || Volatile.Read(ref building) == FALSE);
                Volatile.Write(ref this.value, value);
            }
        }
        
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
        
        public void Build(TValue res)
        {
            this.value = res;
            building = FALSE;
        }
    }
}
