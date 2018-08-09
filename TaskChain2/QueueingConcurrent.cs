using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public class QueueingConcurrent<TValue>
    {
        private volatile object value;

        private class Link
        {
            public Exception exception;
            public readonly Func<TValue, TValue> func;
            public ManualResetEventSlim manualReset = new ManualResetEventSlim();
            public volatile Link next;

            public Link(Func<TValue, TValue> func) => this.func = func ?? throw new ArgumentNullException(nameof(func));

            internal void Wait()
            {
                manualReset.Wait();
                if (exception != null) {
                    throw exception;
                }
            }
        }

        private volatile Link endOfChain = new Link(x => x);
        private const int RUNNING = 1;
        private const int STOPPED = 0;
        private int running = STOPPED;
        private volatile Link startOfChain;

        public QueueingConcurrent(TValue value) => this.value = value;

        public TValue GetValue()
        {
            return (TValue)value;
        }

        public  void SetValue(TValue value)
        {
            Act(x => value);
        }

        public void Act(Func<TValue, TValue> func) {
            var link =new Link(func);

            var oldEndofChain = endOfChain;
            while (true)
            {
                if (Interlocked.CompareExchange(ref oldEndofChain.next, link, null) == null)
                {
                    Interlocked.CompareExchange(ref startOfChain, link, null);
                    DoWork();
                    return;
                }
                oldEndofChain = Interlocked.CompareExchange(ref endOfChain, oldEndofChain.next, oldEndofChain).next;
            }

            async void DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    try
                    {
                        value = startOfChain.func((TValue)value);
                        startOfChain.manualReset.Set();
                    }
                    catch (Exception e)
                    {
                        startOfChain.exception = e;
                        startOfChain.manualReset.Set();
                    }
                    startOfChain = startOfChain.next;
                    running = STOPPED;
                    if (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                    {
                        await Task.Run(() =>
                        {
                            while (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                            {
                                do
                                {
                                    try
                                    {
                                        value = startOfChain.func((TValue)value);
                                        startOfChain.manualReset.Set();
                                    }
                                    catch (Exception e)
                                    {
                                        startOfChain.exception = e;
                                        startOfChain.manualReset.Set();
                                    }
                                    startOfChain = startOfChain.next;
                                } while (startOfChain != null);
                                running = STOPPED;
                            }
                        });
                    }
                }
                else
                {
                    link.Wait();
                }
            }
        }
    }
}
