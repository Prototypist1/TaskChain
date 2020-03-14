using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class QueueingConcurrentActTest
    {

        //[Fact]
        //public async Task Test() {

        //    var r = new Random();

        //    var list = new QueueingConcurrent<double>[100];

        //    for (int i = 0; i < list.Length; i++)
        //    {
        //        list[i] = new QueueingConcurrent<double>(0);
        //    }

        //    var actions = new List<(double, QueueingConcurrent<double>, QueueingConcurrent<double>)>();

        //    for (int i = 0; i < 10000; i++)
        //    {
        //        var x = (
        //            (r.NextDouble() * 10) - 5,
        //            list[r.Next(0, list.Length)],
        //            list[r.Next(0, list.Length)]);
        //        actions.Add(x);
        //    }


        //    var running = new  ConcurrentLinkedList<Task<Tuple<double, double>>>();

        //    var run = 0;

        //    Parallel.ForEach(actions, action =>
        //    {
        //        running.Add(QueueingConcurrent.Act(action.Item2, action.Item3, (x) => {
        //            Interlocked.Increment(ref run);
        //            return new Tuple<double, double>(x.Item1 - action.Item1, x.Item2 + action.Item1);
        //        }));
        //    });

        //    await Task.Delay(10000);

        //    foreach (var item in list)
        //    {
        //        await item.Act(x => x);
        //    } 

        //    await Task.WhenAll(running.ToArray());

        //    foreach (var action in actions)
        //    {
        //        await action.Item2.Act(x => x + action.Item1);
        //        await action.Item3.Act(x => x - action.Item1);
        //    }

        //    foreach (var item in list)
        //    {
        //        Assert.Equal(0, item.Read());
        //    }
        //}
    }
}
