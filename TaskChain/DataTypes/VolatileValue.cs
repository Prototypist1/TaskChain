using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    public abstract class BaseVolatileValue<TValue>
    {
        private object value;

        public TValue Value
        {
            get => (TValue)Volatile.Read(ref value);
            set => Volatile.Write(ref this.value, value);
        }

        public BaseVolatileValue(TValue value)
        {
            this.value = value;
        }
    }

    public class VolatileValue<TValue> : BaseVolatileValue<TValue>
    {
        public readonly IActionChainer actionChainer;

        public VolatileValue(IActionChainer actionChainer, TValue value) : base(value)
        {
            this.actionChainer = actionChainer ?? throw new ArgumentNullException(nameof(actionChainer));
        }
    }
}
