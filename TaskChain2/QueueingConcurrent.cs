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
            public readonly Func<TValue, TValue> func;
            public volatile Link next;
            public virtual void Done()
            {
            }
            public virtual void Done(Exception e)
            {
            }

            public Link(Func<TValue, TValue> func) => this.func = func ?? throw new ArgumentNullException(nameof(func));

            internal virtual void Wait()
            {
            }
        }

        private class WaitingLink : Link
        {
            public Exception exception;

            public override void Done()
            {

                lock (this)
                {
                    done = true;
                    Monitor.Pulse(this);
                }
            }
            public override void Done(Exception e)
            {
                exception = e;
                Done();
            }
            private bool done = false;

            public WaitingLink(Func<TValue, TValue> func) : base(func) { }

            internal override void Wait()
            {
                if (!done)
                {
                    lock (this)
                    {
                        if (!done)
                        {
                            Monitor.Wait(this);
                        }
                    }
                }
                if (exception != null)
                {
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

        public void SetValue(TValue value)
        {
            Act(x => value);
        }

        public void Act(Func<TValue, TValue> func)
        {
            var link = new Link(func);
            Run(link);
            return;
        }

        public void Wait()
        {
            var link = new WaitingLink(x => x);
            Run(link);
            return;
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

                else
                {
                    link.Wait();
                }
            }
        }
    }
}

