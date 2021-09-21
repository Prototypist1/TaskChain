﻿using System;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class OptimisticConcurrent<TValue>
        where TValue : ICopy<TValue>
    { 
        
        public class Envolope
        {
            public readonly TValue value;
        }

        private Envolope value;

        /// <summary>
        /// warning! update may be called multiple times!
        /// </summary>
        public TValue Update(Func<TValue, Envolope> update) {

            while (true) {
                var myView = value;
                var res = update(myView.value.Copy());
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
