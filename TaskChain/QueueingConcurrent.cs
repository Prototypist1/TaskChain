using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{

    public class QueueingConcurrent
    {

    }


    protected class CrossSyncLink
    {
        public class FirstComeFirstServe
        {
            public Task<object[]> main;
        }

        private readonly FirstComeFirstServe firstComeFirstServe;
        private readonly Func<object[], object[]> func;
        private readonly Task<object>[] inputs;
        public volatile Link next;
        private readonly int i;
        public TaskCompletionSource<object> MyValue = new TaskCompletionSource<object>();
        public TaskCompletionSource<TValue> taskCompletionSource = new TaskCompletionSource<TValue>();

        public CrossSyncLink(FirstComeFirstServe firstComeFirstServe, Func<object[], object[]> func, Task<object>[] inputs, int i)
        {
            this.firstComeFirstServe = firstComeFirstServe ?? throw new ArgumentNullException(nameof(firstComeFirstServe));
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            this.i = i;
        }

        public async Task<TValue> Do(object value)
        {

            MyValue.SetResult(value);
            var thing = new TaskCompletionSource<object[]>();
            if (Interlocked.CompareExchange(ref firstComeFirstServe.main, thing.Task, null) == null)
            {
                try
                {
                    thing.SetResult(func(await Task.WhenAll(inputs)));
                }
                catch (Exception e)
                {
                    thing.SetException(e);
                }
            }
            var res = (TValue)(await firstComeFirstServe.main)[i];
            taskCompletionSource.SetResult(res);
            return res;
        }

    }


    public class QueueingConcurrent<TValue>: QueueingConcurrent
    {
        protected volatile object value;

        protected class Link
        {
            private readonly Func<TValue, TValue> func;
            public volatile Link next;
            public TaskCompletionSource<TValue> taskCompletionSource = new TaskCompletionSource<TValue>();
            public Task<TValue> Do(TValue value)
            {
                try
                {
                    var res = func(value);
                    taskCompletionSource.SetResult(res);
                    return taskCompletionSource.Task;
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                    return taskCompletionSource.Task;
                }
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

            async Task<TValue> DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    value = await startOfChain.Do((TValue)value);
                    startOfChain = startOfChain.next;
                    running = STOPPED;
                    if (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(Process);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
                return await link.taskCompletionSource.Task;

                async void Process()
                {
                    do
                    {
                        do
                        {
                            value = await startOfChain.Do((TValue)value);
                            startOfChain = startOfChain.next;
                        } while (startOfChain != null);
                        running = STOPPED;
                    } while (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED);
                }
            }
        }
    }
}

