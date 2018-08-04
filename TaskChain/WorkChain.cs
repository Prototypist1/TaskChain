using System;
using System.Threading;

namespace Prototypist.TaskChain
{
    internal class WorkChain
    {
        private volatile Link endOfChain = new ActionLink(() => { }, () => { });
        private volatile Link startOfChain;

        public WorkChain()
        {
        }

        public void Enchain(Link link)
        {
            var oldEndofChain = endOfChain;
            while (true)
            {
                if (Interlocked.CompareExchange(ref oldEndofChain.next, link, null) == null)
                {
                    Interlocked.CompareExchange(ref startOfChain, link, null);
                    Interlocked.CompareExchange(ref endOfChain, oldEndofChain.next, oldEndofChain);
                    return;
                }
                Interlocked.CompareExchange(ref endOfChain, oldEndofChain.next, oldEndofChain);
                oldEndofChain = endOfChain;
            }
        }


        public void ProcessTask()
        {
            var ogStartofChain = startOfChain;
            if (ogStartofChain != null)
            {
                var ogNext = ogStartofChain.next;
                ogStartofChain.WorkerTryRun();
                Interlocked.CompareExchange(ref startOfChain, ogNext, ogStartofChain);
            }
        }

        internal void DoWork()
        {
            var ogStartofChain = startOfChain;
            var ogNext = default(Link);
            while (ogStartofChain != null)
            {
                ogNext = ogStartofChain.next;
                ogStartofChain.WorkerTryRun();
                Interlocked.CompareExchange(ref startOfChain, ogNext, ogStartofChain);
                ogStartofChain = startOfChain;
            }
        }
    }

}
