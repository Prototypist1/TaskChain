﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{

    public static class QueueingConcurrent
    {

        private class FirstComeFirstServe<TInputs>
        {
            public TaskCompletionSource<TInputs> main = new TaskCompletionSource<TInputs>();
            public object first = null;
        }

        private class CrossSyncLink<T,TInputs>
        {
            private Task<bool> GotAll { get; }
            private FirstComeFirstServe<TInputs> FirstComeFirstServe { get; }
            private Func<TInputs, TInputs> Func { get; }
            private Task<TInputs> Inputs { get; }
            private Func<TInputs, T> Covert { get; }
            private TaskCompletionSource<T> MyValue { get; }

            public CrossSyncLink(FirstComeFirstServe<TInputs> firstComeFirstServe, Func<TInputs, TInputs> func, Task<TInputs> inputs, Func<TInputs,T> covert, TaskCompletionSource<T> myValue, Task<bool> gotAll)
            {
                FirstComeFirstServe = firstComeFirstServe ?? throw new ArgumentNullException(nameof(firstComeFirstServe));
                Func = func ?? throw new ArgumentNullException(nameof(func));
                Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
                Covert = covert ?? throw new ArgumentNullException(nameof(covert));
                MyValue = myValue ?? throw new ArgumentNullException(nameof(myValue));
                GotAll = gotAll ?? throw new ArgumentNullException(nameof(gotAll));
            }

            public async Task<T> Do(T value)
            {
                if (await GotAll)
                {
                    MyValue.SetResult(value);
                    if (Interlocked.CompareExchange(ref FirstComeFirstServe.first, FirstComeFirstServe.main, null) == null)
                    {

                        try
                        {
                            FirstComeFirstServe.main.SetResult(Func(await Inputs));
                        }
                        catch (Exception e)
                        {
                            FirstComeFirstServe.main.SetException(e);
                        }
                    }
                    return Covert(await FirstComeFirstServe.main.Task); ;
                }
                return value;
            }
        }

        public static Task<Tuple<T1, T2>> Act<T1, T2>(QueueingConcurrent<T1> concurrent1, QueueingConcurrent<T2> concurrent2, Func<Tuple<T1,T2>,Tuple<T1,T2>> function) {

            var firstComeFirstServe = new FirstComeFirstServe<Tuple<T1, T2>>();

            var myValues1 = new TaskCompletionSource<T1>();
            var myValues2 = new TaskCompletionSource<T2>();
            
            async Task<Tuple<T1, T2>> GetInputs() {
                return new Tuple<T1, T2>(await myValues1.Task, await myValues2.Task);
            }

            var inputs = GetInputs();
            
            var done = false;
            while (!done)
            {
                var gotAll = new TaskCompletionSource<bool>();
                
                var tryEnqueues1 = concurrent1.GetTryEnqueue(new CrossSyncLink<T1,Tuple<T1,T2>>(firstComeFirstServe, function, inputs, x=>x.Item1, myValues1, gotAll.Task).Do);
                var tryEnqueues2 = concurrent2.GetTryEnqueue(new CrossSyncLink<T2, Tuple<T1, T2>>(firstComeFirstServe, function, inputs, x => x.Item2, myValues2, gotAll.Task).Do);
                
                done = tryEnqueues1() && tryEnqueues2();
                gotAll.SetResult(done);
            }

            concurrent1.TryProcess();
            concurrent2.TryProcess();
            
            return firstComeFirstServe.main.Task;
        }

        public static Task<Tuple<T1, T2, T3>> Act<T1, T2, T3>(QueueingConcurrent<T1> concurrent1, QueueingConcurrent<T2> concurrent2, QueueingConcurrent<T3> concurrent3, Func<Tuple<T1, T2, T3>, Tuple<T1, T2, T3>> function)
        {

            var firstComeFirstServe = new FirstComeFirstServe<Tuple<T1, T2, T3>>();

            var myValues1 = new TaskCompletionSource<T1>();
            var myValues2 = new TaskCompletionSource<T2>();
            var myValues3 = new TaskCompletionSource<T3>();

            async Task<Tuple<T1, T2, T3>> GetInputs()
            {
                return new Tuple<T1, T2, T3>(await myValues1.Task, await myValues2.Task, await myValues3.Task);
            }

            var inputs = GetInputs();

            var done = false;
            while (!done)
            {
                var gotAll = new TaskCompletionSource<bool>();

                var tryEnqueues1 = concurrent1.GetTryEnqueue(new CrossSyncLink<T1, Tuple<T1, T2, T3>>(firstComeFirstServe, function, inputs, x => x.Item1, myValues1, gotAll.Task).Do);
                var tryEnqueues2 = concurrent2.GetTryEnqueue(new CrossSyncLink<T2, Tuple<T1, T2, T3>>(firstComeFirstServe, function, inputs, x => x.Item2, myValues2, gotAll.Task).Do);
                var tryEnqueues3 = concurrent3.GetTryEnqueue(new CrossSyncLink<T3, Tuple<T1, T2, T3>>(firstComeFirstServe, function, inputs, x => x.Item3, myValues3, gotAll.Task).Do);

                done = tryEnqueues1() && tryEnqueues2() && tryEnqueues3();
                gotAll.SetResult(done);
            }

            concurrent1.TryProcess();
            concurrent2.TryProcess();
            concurrent3.TryProcess();

            return firstComeFirstServe.main.Task;
        }

    }
    
    public class QueueingConcurrent<TValue>
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
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
                return taskCompletionSource.Task;
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
        protected volatile AbstractLink endOfChain;
        protected volatile AbstractLink lastRun;

        public QueueingConcurrent(TValue value) {
            this.value = value;
            endOfChain = new Link(x => x);
            lastRun = endOfChain;
        }

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
        
        public Func<bool> GetTryEnqueue(Func<TValue, Task<TValue>> func)
        {
            var link = new LinkAsync(func);
            return () =>
            {
                if (Interlocked.CompareExchange(ref endOfChain.next, link, null) == null)
                {
                    return true;
                }
                return false;
            };
        }

        private Task<TValue> Run(AbstractLink link)
        {
            while (true)
            {
                var myEndOfChain = endOfChain;
                if (Interlocked.CompareExchange(ref myEndOfChain.next, link, null) == null)
                {
                    Interlocked.CompareExchange(ref endOfChain, link, myEndOfChain);
                    return DoWork();
                }
                else {
                    Interlocked.CompareExchange(ref endOfChain, myEndOfChain.next, myEndOfChain);
                }
            }

            async Task<TValue> DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    var mine = lastRun.next;
                    if (mine == null)
                    {
                        running = STOPPED;
                        goto exit;
                    }
                    value = await mine.Do((TValue)value);
                    lastRun = mine;
                    running = STOPPED;
                    TryProcess();
                }
                exit:
                return await link.taskCompletionSource.Task;
            }
        }
        
        private async Task<TValue> RunAsync(AbstractLink link)
        {
            while (true)
            {
                var myEndOfChain = endOfChain;
                if (Interlocked.CompareExchange(ref myEndOfChain.next, link, null) == null)
                {
                    Interlocked.CompareExchange(ref endOfChain, link, myEndOfChain);
                    return await DoWork();
                }
                else
                {
                    Interlocked.CompareExchange(ref endOfChain, myEndOfChain.next, myEndOfChain);
                }
            }

            async Task<TValue> DoWork()
            {
                if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
                {
                    var mine = lastRun.next;
                    if (mine == null)
                    {
                        running = STOPPED;
                        goto exit;
                    }
                    value = await mine.Do((TValue)value);
                    lastRun = mine;
                    running = STOPPED;
                    TryProcess();
                }
                exit:
                return await link.taskCompletionSource.Task;
            }
        }

        public void TryProcess()
        {
            if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
            {
                Task.Run(Process);
            }

            async void Process()
            {
                do
                {
                    AbstractLink mine;
                    while ((mine = lastRun.next) != null)
                    {
                        value = await mine.Do((TValue)value);
                        lastRun = mine;
                    } 
                    running = STOPPED;
                } while (lastRun.next != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED);
            }
        }
        
    }
}

