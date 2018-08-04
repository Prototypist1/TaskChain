using System;
using System.Threading;

namespace Prototypist.TaskChain
{

    internal class ItemLock :  IActionChainer
    {
        private const int TRUE = 1;
        private const int FALSE = 0;
        
        private readonly TaskManager taskManager;
        private volatile int running = FALSE;

        public ItemLock(TaskManager taskManager)
        {
            this.taskManager = taskManager;
        }

        public void Run(Action action)
        {
            taskManager.SpinUntil(() => TryRun(action));
        }

        public T Run<T>(Func<T> func)
        {
            var res = default(T);
            taskManager.SpinUntil(() => TryRun(func, out res));
            return res;
        }

        public bool TryRun(Action action)
        {
            if (Interlocked.CompareExchange(ref running, TRUE, FALSE) == FALSE)
            {
                try
                {
                    action();
                }
                finally
                {
                    Interlocked.Exchange(ref running, FALSE);
                }
                return true;
            }
            return false;
        }

        public bool TryRun<T>(Func<T> func, out T result)
        {
            if (Interlocked.CompareExchange(ref running, TRUE, FALSE) == FALSE)
            {
                try
                {
                    result = func();
                }
                finally
                {
                    Interlocked.Exchange(ref running, FALSE);
                }
                return true;
            }
            result = default(T);
            return false;
        }

        public IActionChainer GetActionChainer()
        {
            return taskManager.GetActionChainer();
        }
    }


}
