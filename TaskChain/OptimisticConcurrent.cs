using System;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class OptimisticConcurrent<TValue>
        where TValue : ICopy<TValue>
    { 
        
        public class Envelope
        {
            public readonly TValue value;

            public Envelope(TValue value)
            {
                this.value = value;
            }
        }

        private Envelope value;

        public OptimisticConcurrent(TValue value)
        {
            this.value = new Envelope(value);
        }

        /// <summary>
        /// warning! update may be called multiple times!
        /// </summary>
        public TValue Update(Func<TValue, TValue> update) {

            while (true) {
                var myView = value;
                var res = new Envelope (update(myView.value == null ? default : myView.value.Copy()) );
                if (Interlocked.CompareExchange(ref value, res, myView) == myView) {
                    return res.value;
                }
            }
        }
        public TValue Read() => value.value;
    }

    public interface ICopy<TValue> {
        public TValue Copy();
    }
}

