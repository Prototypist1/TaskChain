using System;
using System.Threading;

namespace Prototypist.TaskChain
{

    internal class ActionLink : Link
    {
        private readonly Action action;
        private readonly Action onComplete;

        public ActionLink(Action action, Action onComplete)
        {
            this.action = action;
            this.onComplete = onComplete;
        }

        public override void WorkerTryRun()
        {
            if (Interlocked.CompareExchange(ref started, TRUE, FALSE) == FALSE)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    onComplete();
                }
            }
        }

        public override void MainTryRun()
        {
            if (Interlocked.CompareExchange(ref started, TRUE, FALSE) == FALSE)
            {
                
                action();
                onComplete();
                
            }
        }
    }


}
