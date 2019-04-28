using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public class BuildableQueueingConcurrent<TValue> : QueueingConcurrent<TValue>
    {
        readonly ManualResetEventSlim eventSlim = new ManualResetEventSlim();
        private volatile object build;
        public BuildableQueueingConcurrent() : base(default)
        {
            lastRun = new Link(x =>
            {
                eventSlim.Wait();
                return (TValue)build;
            });
            endOfChain = lastRun;
        }

        public void Build(TValue value) {
            build = value;
            this.value = value;
            eventSlim.Set();
        }

        public override TValue Read()
        {
            eventSlim.Wait();
            return base.Read();
        }

        public override Task<TValue> EnqueRead()
        {
            eventSlim.Wait();
            return base.EnqueRead();
        }
    }
}

