using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.Benchmark
{
    public static class MyStupidBenchmarker
    {

        public class BenchmarkResult
        {
            public readonly IReadOnlyList<long> times;
            public readonly double average;
            public readonly double std;

            public BenchmarkResult(List<long> times)
            {
                this.times = times;
                double sumOfSqrs = 0;
                foreach (var value in times)
                {
                    average += value;
                }
                average = (average / times.Count);

                foreach (var value in times)
                {
                    sumOfSqrs += Math.Pow((value - average), 2);
                }
                std = Math.Sqrt(sumOfSqrs / (times.Count - 1));
            }

            public override string ToString()
            {
                return $"average: {average/ TimeSpan.TicksPerMillisecond}, std: {std/ TimeSpan.TicksPerMillisecond}"; 
            }
        }

        public static BenchmarkResult Benchmark(Action setup, Action run, int runs, int warmupRuns) {
            for (int i = 0; i < warmupRuns; i++)
            {
                setup();
                run();
            }

            var times = new List<long>();
            for (int i = 0; i < runs; i++)
            {
                setup();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                run();
                watch.Stop();
                times.Add(watch.ElapsedTicks);
            }

            return new BenchmarkResult(times);
        }
    }
}
