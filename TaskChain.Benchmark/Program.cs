using BenchmarkDotNet.Running;
using System;

namespace Prototypist.TaskChain.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ReadAllItems>();
            Console.WriteLine(summary);
            Console.ReadLine();
        }
    }
}
