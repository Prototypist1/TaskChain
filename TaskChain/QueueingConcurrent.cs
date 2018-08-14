using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{

    public abstract class QueueingConcurrent
    {
        public abstract Func<bool> GetTryEnqueue(Func<object, Task<object>> func);
        public abstract void TryStart();

        private class FirstComeFirstServe
        {
            public Task<object[]> main;
        }

        private class CrossSyncLink
        {
            private readonly Task<bool> GotAll;
            private readonly FirstComeFirstServe firstComeFirstServe;
            private readonly Func<object[], object[]> func;
            private readonly Task<object>[] inputs;
            private readonly int i;
            public TaskCompletionSource<object> MyValue = new TaskCompletionSource<object>();

            public CrossSyncLink(FirstComeFirstServe firstComeFirstServe, Func<object[], object[]> func, Task<object>[] inputs, int i, TaskCompletionSource<object> myValue, Task<bool> gotAll)
            {
                this.firstComeFirstServe = firstComeFirstServe ?? throw new ArgumentNullException(nameof(firstComeFirstServe));
                this.func = func ?? throw new ArgumentNullException(nameof(func));
                this.inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
                this.i = i;
                MyValue = myValue ?? throw new ArgumentNullException(nameof(myValue));
                GotAll = gotAll ?? throw new ArgumentNullException(nameof(gotAll));
            }

            public async Task<object> Do(object value)
            {
                if (await GotAll)
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
                    return (await firstComeFirstServe.main)[i]; ;
                }
                return value;
            }
        }

        // ⚠⚠⚠ super unsafe, nothing like type checking
        public static Task<object[]> MultiEnqueue(QueueingConcurrent[] concurrents, Func<object[],object[]> function) {
            
            
            var firstComeFirstServe = new FirstComeFirstServe();

            var myValues = new TaskCompletionSource<object>[concurrents.Length];

            for (int i = 0; i < concurrents.Length; i++)
            {
                myValues[i] = new TaskCompletionSource<object>();
            }

            var inputs = myValues.Select(x => x.Task).ToArray();

            bool done = false;
            while (!done){
                var gotAll = new TaskCompletionSource<bool>();
                
                var tryEnqueues = new Func<bool>[concurrents.Length];
                for (int i = 0; i < concurrents.Length; i++)
                {
                    tryEnqueues[i] = concurrents[i].GetTryEnqueue(new CrossSyncLink(firstComeFirstServe, function, inputs, i, myValues[i], gotAll.Task).Do);
                }

                done=tryEnqueues.All(x => x());
                gotAll.SetResult(done);
            }

            foreach (var concurrent in concurrents)
            {
                concurrent.TryStart();
            }

            return firstComeFirstServe.main;
        }
    }


    


    public class QueueingConcurrent<TValue>: QueueingConcurrent
    {
        protected volatile object value;

        protected abstract class AbstractLink
        {
            public volatile AbstractLink next;
            public TaskCompletionSource<TValue> taskCompletionSource = new TaskCompletionSource<TValue>();
            public abstract Task<TValue> Do(TValue value);
        }

        protected class Link : AbstractLink
        {
            private readonly Func<TValue, TValue> func;
            public override Task<TValue> Do(TValue value)
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

        protected class LinkAsync : AbstractLink
        {
            private readonly Func<TValue, Task<TValue>> func;
            public override async Task<TValue> Do(TValue value)
            {
                try
                {
                    var res = await func(value);
                    taskCompletionSource.SetResult(res);
                    return res;
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                    throw;
                }
            }

            public LinkAsync(Func<TValue, Task<TValue>> func) => this.func = func ?? throw new ArgumentNullException(nameof(func));

        }
        
        private const int RUNNING = 1;
        private const int STOPPED = 0;
        private int running = STOPPED;
        protected volatile AbstractLink startOfChain;
        protected volatile AbstractLink endOfChain = new Link(x => x);

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
        public async Task<TValue> ActAsync(Func<TValue, Task<TValue>> func)
        {
            return await Run(new LinkAsync(func));
        }

        public virtual Task<TValue> EnqueRead()
        {
            return Run(new Link(x => x));
        }
        
        private Task<TValue> Run(AbstractLink link)
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
                    value = startOfChain.Do((TValue)value).Result;
                    startOfChain = startOfChain.next;
                    running = STOPPED;
                    TryStart();
                }
                return link.taskCompletionSource.Task;
            }
        }

        // goodbye type safety 👋
        public override Func<bool> GetTryEnqueue(Func<object, Task<object>> func) {
            var link = new LinkAsync(async (x)=> (TValue)await func(x));
            var localEnd = endOfChain;
            return () =>
            {
                if (Interlocked.CompareExchange(ref localEnd.next, link, null) == null)
                {
                    endOfChain = endOfChain.next;
                    return true;
                }
                return false;
            };
        }
        
        private async Task<TValue> RunAsync(AbstractLink link)
        {
            while (true)
            {
                if (Interlocked.CompareExchange(ref endOfChain.next, link, null) == null)
                {
                    endOfChain = endOfChain.next;
                    Interlocked.CompareExchange(ref startOfChain, link, null);
                    return await DoWork();
                }
            }

            async Task<TValue> DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    value = await startOfChain.Do((TValue)value);
                    startOfChain = startOfChain.next;
                    running = STOPPED;
                    TryStart();
                }
                return await link.taskCompletionSource.Task;
            }
        }

        public override void TryStart()
        {
            if (startOfChain != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(Process);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

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

