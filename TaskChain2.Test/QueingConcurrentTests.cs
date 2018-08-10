using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{

    public class QueingConcurrentTests
    {
        [Fact]
        public void Count()
        {
            var target = new QueueingConcurrent<int>(0);

            int plusOne(int x) => x + 1;
            Task t = Task.CompletedTask;
            for (int i = 0; i < 1000; i++)
            {
                t = target.Act(plusOne);
            }
            t.Wait();

            Assert.Equal(1000, target.GetValue());
        }

        //[Fact]
        //public void CountIncrement()
        //{
        //    var value = 0;

        //    var array = new Action[1000];
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        array[i] = () => Interlocked.Increment(ref value);
        //    }

        //    Parallel.Invoke(array);

        //    Assert.Equal(1000, value);
        //}

        //[Fact]
        //public void CountLock()
        //{
        //    var value = 0;
        //    object o = new object();

        //    var array = new Action[1000];
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        array[i] = () =>
        //        {
        //            lock (o)
        //            {
        //                value++;
        //            }
        //        };
        //    }

        //    Parallel.Invoke(array);

        //    Assert.Equal(1000, value);
        //}

        //[Fact]
        //public void Inseries()
        //{
        //    var value = 0;
        //    object o = new object();

        //    var array = new Action[1000];
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        array[i] = () =>
        //        {
        //            value++;
        //        };
        //    }

        //    foreach (var item in array)
        //    {
        //        item();
        //    }

        //    Assert.Equal(1000, value);
        //}

        //[Fact]
        //public void Baseline()
        //{
        //    var value = 0;
        //    object o = new object();

        //    var array = new Action[1000];
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        array[i] = () =>
        //        {
        //            value++;
        //        };
        //    }

        //}
    }
}
