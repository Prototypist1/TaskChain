﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{

    public class JumpBallConcurrent<TValue>
    {
        protected TValue value;

        private const int RUNNING = 1;
        private const int STOPPED = 0;
        private int running = STOPPED;

        public JumpBallConcurrent(TValue value)
        {
            this.value = value;
        }

        public virtual TValue Read()
        {
            return (TValue)value;
        }

        public virtual TValue SetValue(TValue value)
        {
            Modify(x => value);
            return value;
        }

        public virtual void Modify(Func<TValue, TValue> func)
        {
            SpinWait.SpinUntil(()=>Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == RUNNING);

            value = func(value);
            running = STOPPED;
        }

        public virtual T Run<T>(Func<TValue, T> func)
            where T: TValue
        {
            SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == RUNNING);

            var res = func(value);
            value = res;
            running = STOPPED;
            return res;
        }

        public virtual async Task<TValue> RunAsync(Func<TValue, Task<TValue>> func)
        {
            SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == RUNNING);

            var res = await func(value);
            value = res;
            running = STOPPED;
            return res;
        }

        public virtual void Act(Action<TValue> action)
        {
            SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref running, RUNNING, STOPPED) == RUNNING);
            action(value);
            running = STOPPED;
        }

        public virtual TValue EnqueRead()
        {
            return Run(x => x);
        }
    }
}

