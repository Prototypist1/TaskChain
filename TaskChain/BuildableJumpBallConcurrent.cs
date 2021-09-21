using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
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

        public override Task<TValue> RunAsync(Func<TValue, Task<TValue>> func)
        {
            SpinWait.SpinUntil(() => built == 1);
            return base.RunAsync(func);
        }

        public override TValue SetValue(TValue value)
        {
            SpinWait.SpinUntil(() => built == 1);
            return base.SetValue(value);
        }
    }
}

