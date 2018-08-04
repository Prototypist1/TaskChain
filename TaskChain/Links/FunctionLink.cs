using System;
using System.Threading;

namespace Prototypist.TaskChain
{

    internal class FunctionLink<T> : Link
    {
        private readonly Func<T> func;
        private readonly Action onComplete;
        private volatile object result;

        public T GetResult()
        {
            return (T)result;
        }

        public FunctionLink(Func<T> func, Action onComplete)
        {
            this.func = func;
            this.onComplete = onComplete;
        }

        public override void WorkerTryRun()
        {
            if (Interlocked.CompareExchange(ref started, TRUE, FALSE) == FALSE)
            {
                try
                {
                    result = func();
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
                // the main thread can just throw 
                result = func();
                onComplete();
            }
        }
    }


}
