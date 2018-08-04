using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public partial class TaskManager
    {
        private class Processor
        {
            private readonly WorkChain workChain;
            volatile int awake = FALSE;
            volatile int workToDo = FALSE;

            public Processor(WorkChain workChain)
            {
                this.workChain = workChain;
            }

            public bool TryAwake()
            {
                workToDo = TRUE;
                if (Interlocked.CompareExchange(ref awake, TRUE, FALSE) == FALSE)
                {
                    Task.Run(() =>
                    {
                        do
                        {
                            workToDo = FALSE;
                            workChain.DoWork();
                            awake = FALSE;
                        } while (workToDo == TRUE && Interlocked.CompareExchange(ref awake, TRUE, FALSE) == FALSE);
                    });
                    return true;
                }
                return false;
            }
        }
    }

}
