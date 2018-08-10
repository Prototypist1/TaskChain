using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{

    public class QueueingConcurrent<TValue>
    {
        private volatile object value;

        protected class Link
        {
            public readonly Func<TValue, TValue> func;
            public volatile Link next;
            public TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            public void Done()
            {
                taskCompletionSource.SetResult(true);
            }
            public void Done(Exception e)
            {
                taskCompletionSource.SetException(e);
            }

            public Link(Func<TValue, TValue> func) => this.func = func ?? throw new ArgumentNullException(nameof(func));

        }

        protected volatile Link endOfChain = new Link(x => x);
        private const int RUNNING = 1;
        private const int STOPPED = 0;
        private int running = STOPPED;
        protected volatile Link startOfChain;

        public QueueingConcurrent(TValue value) => this.value = value;

        public virtual TValue GetValue()
        {
            return (TValue)value;
        }

        public void SetValue(TValue value)
        {
            Act(x => value);
        }

        public Task Act(Func<TValue, TValue> func)
        {
            var link = new Link(func);
            Run(link);
            return link.taskCompletionSource.Task;
        }

        public void WaitForIdle()
        {
            if (startOfChain != null)
            {
                return;
            }

            var link = new Link(x => x);
            Run(link);
            link.taskCompletionSource.Task.Wait();
        }

        private void Run(Link link)
        {
            while (true)
            {
                if (Interlocked.CompareExchange(ref endOfChain.next, link, null) == null)
                {
                    endOfChain = endOfChain.next;
                    Interlocked.CompareExchange(ref startOfChain, link, null);
                    DoWork();
                    return;
                }
            }

            void DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    try
                    {
                        value = startOfChain.func((TValue)value);
                        startOfChain.Done();
                    }
                    catch (Exception e)
                    {

                        startOfChain.Done(e);
                    }
                    startOfChain = startOfChain.next;
                    running = STOPPED;
                    if (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                    {
                        Task.Run(() =>
                        {
                            do
                            {
                                do
                                {
                                    try
                                    {
                                        value = startOfChain.func((TValue)value);
                                        startOfChain.Done();
                                    }
                                    catch (Exception e)
                                    {

                                        startOfChain.Done(e);
                                    }
                                    startOfChain = startOfChain.next;
                                } while (startOfChain != null);
                                running = STOPPED;
                            } while (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED);
                        });
                    }
                }
            }
        }
    }

    public class BuildableQueueingConcurrent<TValue> : QueueingConcurrent<TValue>
    {
        ManualResetEventSlim eventSlim = new ManualResetEventSlim();
        private volatile object build;
        public BuildableQueueingConcurrent() : base(default)
        {
            startOfChain = new Link(x =>
            {
                eventSlim.Wait();
                return (TValue)build;
            });
            endOfChain = startOfChain;
        }

        public void Build(TValue value) {
            build = value;
            eventSlim.Set();
        }

        public override TValue GetValue()
        {
            eventSlim.Wait();
            return base.GetValue();
        }
    }
}

