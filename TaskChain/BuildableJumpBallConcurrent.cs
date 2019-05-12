using System;
using System.Threading;

namespace Prototypist.TaskChain
{
    //public class QueueingConcurrent
    //{
    //    protected abstract class Link
    //    {
    //        public volatile Link next;
    //        public TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
    //        public abstract Task Do();
    //    }

    //    protected class Link : Link
    //    {
    //        private readonly Action action;
    //        public override Task Do()
    //        {
    //            try
    //            {
    //                action();
    //                taskCompletionSource.SetResult(true);
    //            }
    //            catch (Exception e)
    //            {
    //                taskCompletionSource.SetException(e);
    //            }
    //            return taskCompletionSource.Task;
    //        }

    //        public Link(Action action) => this.action = action ?? throw new ArgumentNullException(nameof(action));

    //    }

    //    protected class LinkAsync : Link
    //    {
    //        private readonly Func<Task> func;
    //        public override async Task Do()
    //        {
    //            try
    //            {
    //                await func();
    //                taskCompletionSource.SetResult(true);
    //            }
    //            catch (Exception e)
    //            {
    //                taskCompletionSource.SetException(e);
    //                throw;
    //            }
    //        }

    //        public LinkAsync(Func<Task> func) => this.func = func ?? throw new ArgumentNullException(nameof(func));

    //    }

    //    private const int RUNNING = 1;
    //    private const int STOPPED = 0;
    //    private int running = STOPPED;
    //    protected volatile Link endOfChain;
    //    protected volatile Link lastRun;

    //    public QueueingConcurrent()
    //    {
    //        endOfChain = new Link(() => { });
    //        lastRun = endOfChain;
    //    }

    //    public Task Act(Action func)
    //    {
    //        return Run(new Link(func));
    //    }

    //    public async Task ActAsync(Func<Task> func)
    //    {
    //        await Run(new LinkAsync(func));
    //    }

    //    public Func<bool> GetTryEnqueue(Func<Task> func)
    //    {
    //        var link = new LinkAsync(func);
    //        return () =>
    //        {
    //            if (Interlocked.CompareExchange(ref endOfChain.next, link, null) == null)
    //            {
    //                return true;
    //            }
    //            return false;
    //        };
    //    }

    //    private Task Run(Link link)
    //    {
    //        while (true)
    //        {
    //            var myEndOfChain = endOfChain;
    //            if (Interlocked.CompareExchange(ref myEndOfChain.next, link, null) == null)
    //            {
    //                Interlocked.CompareExchange(ref endOfChain, link, myEndOfChain);
    //                return DoWork();
    //            }
    //            else
    //            {
    //                Interlocked.CompareExchange(ref endOfChain, myEndOfChain.next, myEndOfChain);
    //            }
    //        }

    //        async Task DoWork()
    //        {
    //            if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
    //            {
    //                var mine = lastRun.next;
    //                if (mine == null)
    //                {
    //                    running = STOPPED;
    //                    goto exit;
    //                }
    //                await mine.Do();
    //                lastRun = mine;
    //                running = STOPPED;
    //                TryProcess();
    //            }
    //        exit:
    //            await link.taskCompletionSource.Task;
    //        }
    //    }

    //    private async Task RunAsync(Link link)
    //    {
    //        while (true)
    //        {
    //            var myEndOfChain = endOfChain;
    //            if (Interlocked.CompareExchange(ref myEndOfChain.next, link, null) == null)
    //            {
    //                Interlocked.CompareExchange(ref endOfChain, link, myEndOfChain);
    //                await DoWork();
    //            }
    //            else
    //            {
    //                Interlocked.CompareExchange(ref endOfChain, myEndOfChain.next, myEndOfChain);
    //            }
    //        }

    //        async Task DoWork()
    //        {
    //            if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
    //            {
    //                var mine = lastRun.next;
    //                if (mine == null)
    //                {
    //                    running = STOPPED;
    //                    goto exit;
    //                }
    //                await mine.Do();
    //                lastRun = mine;
    //                running = STOPPED;
    //                TryProcess();
    //            }
    //        exit:
    //            await link.taskCompletionSource.Task;
    //        }
    //    }

    //    public void TryProcess()
    //    {
    //        if (Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED)
    //        {
    //            Task.Run(Process);
    //        }

    //        async void Process()
    //        {
    //            do
    //            {
    //                Link mine;
    //                while ((mine = lastRun.next) != null)
    //                {
    //                    await mine.Do();
    //                    lastRun = mine;
    //                }
    //                running = STOPPED;
    //            } while (lastRun.next != null && Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == STOPPED);
    //        }
    //    }

    //}


    public class BuildableJumpBallConcurrent<TValue> : JumpBallConcurrent<TValue>
    {
        private int built = 0;

        public BuildableJumpBallConcurrent() : base(default)
        {
        }

        public void Build(TValue value)
        {
            this.value = value;
            built = 1;
        }

        public override TValue EnqueRead()
        {
            SpinWait.SpinUntil(() => built == 1);
            return base.EnqueRead();
        }

        public override TValue Read()
        {
            SpinWait.SpinUntil(() => built == 1);
            return base.Read();
        }

        public override TValue Run(Func<TValue, TValue> func)
        {
            SpinWait.SpinUntil(() => built == 1);
            return base.Run(func);
        }

        public override TValue SetValue(TValue value)
        {
            SpinWait.SpinUntil(() => built == 1);
            return base.SetValue(value);
        }
    }
}

