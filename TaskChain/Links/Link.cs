using System;

namespace Prototypist.TaskChain
{
    internal abstract class Link
    {
        protected const int TRUE = 1;
        protected const int FALSE = 0;
        public volatile Link next;
        protected volatile int started = FALSE;
        public abstract void WorkerTryRun();
        public abstract void MainTryRun();
        public volatile Exception exception;
    }
}
