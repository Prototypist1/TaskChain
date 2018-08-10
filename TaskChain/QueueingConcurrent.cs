using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{

    public class QueueingConcurrent<TValue>
    {
        protected volatile object value;

        protected class Link
        {
            public readonly Func<TValue, TValue> func;
            public volatile Link next;
            public TaskCompletionSource<TValue> taskCompletionSource = new TaskCompletionSource<TValue>();
            public void Done(TValue res)
            {
                taskCompletionSource.SetResult(res);
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

        public virtual TValue Read()
        {
            return (TValue)value;
        }

        public Task<TValue> SetValue(TValue value)
        {
            return Act(x => value);
        }

        public Task<TValue> Act(Func<TValue, TValue> func)
        {
            return Run(new Link(func));
        }

        public virtual Task<TValue> EnqueRead()
        {
            return Run(new Link(x => x));
        }

        private Task<TValue> Run(Link link)
        {
            while (true)
            {
                if (Interlocked.CompareExchange(ref endOfChain.next, link, null) == null)
                {
                    endOfChain = endOfChain.next;
                    Interlocked.CompareExchange(ref startOfChain, link, null);
                    return DoWork();
                }
            }

            Task<TValue> DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    try
                    {
                        value = startOfChain.func((TValue)value);
                        startOfChain.Done((TValue)value);
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
                                        startOfChain.Done((TValue)value);
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
                return link.taskCompletionSource.Task;
            }
        }
    }
}

