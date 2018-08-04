using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.DataTypes
{
    public class ConcurrentFloat
    {
        private readonly IActionChainer actionChainer;
        private float value;
        
        private ConcurrentFloat(float value, IActionChainer actionChainer)
        {
            this.value = value;
            this.actionChainer = actionChainer;
        }

        public ConcurrentFloat(float value):this(value,Chaining.taskManager.GetActionChainer())
        {
        }

        public void Add(float shift)
        {
            actionChainer.Run(() => Volatile.Write(ref value, Get() + shift));
        }

        public float Get()
        {
            return Volatile.Read(ref value);
        }
    }
}
