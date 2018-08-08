using BenchmarkDotNet.Running;
using System;

namespace Prototypist.TaskChain.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary1 = BenchmarkRunner.Run<ReadAllItems>();
            var summary2 = BenchmarkRunner.Run<WriteAllItems>();
            //Console.WriteLine(summary1);
            Console.WriteLine(summary2);
            Console.ReadLine();
        }
    }
}
