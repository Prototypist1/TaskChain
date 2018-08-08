using BenchmarkDotNet.Running;
using System;

namespace Prototypist.TaskChain.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<ReadAllItems>();
            //var summary = BenchmarkRunner.Run<WriteAllItems>();
            var summary = BenchmarkRunner.Run<ParallelUpdate>();
            Console.WriteLine(summary);
            Console.ReadLine();
        }
    }
}
