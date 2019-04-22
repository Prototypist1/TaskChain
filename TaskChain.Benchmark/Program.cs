using BenchmarkDotNet.Running;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.Benchmark
{
    class Program
    {
        private const int AcountCount = 100000;
        private const int Range = 1000;
        private const int Runs = 100;
        private const int WarmUpRuns = 10;

        static void Main(string[] _)
        {
            //var summary = BenchmarkRunner.Run<ReadAllItems>();
            //var summary = BenchmarkRunner.Run<WriteAllItems>();
            //var summary = BenchmarkRunner.Run<ParallelUpdate>();
            //var summary = BenchmarkRunner.Run<InterlockedTest>();


            Console.WriteLine("Tree read      " + BenchmarkTreeRead(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Classics read  " + BenchmarkClassicRead(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Tree write     " + BenchmarkTreeWrite(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Classics write " + BenchmarkClassicWrite(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Tree           " + BenchmarkTree(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Classics       " + BenchmarkClassic(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Mine           " + BenchmarkMine(AcountCount, Range, Runs, WarmUpRuns));
            Console.WriteLine("Growing        " + BenchmarkGrowing(AcountCount, Range, Runs, WarmUpRuns));
            //Console.ReadLine();
        }


        static string BenchmarkMine(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new RawConcurrentIndexed<int, int>();

            List<Action> actions = null; ;

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var roll = rand.Next(2);
                    if (roll == 0)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.GetOrAdd(new RawConcurrentIndexed<int, int>.KeyValue( number, number));
                        });
                    }
                    else if (roll == 1)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.TryGetValue(number, out var _);
                        });
                    }
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkGrowing(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new RawConcurrentGrowingIndex<int, int>();

            List<Action> actions = null;;

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var roll = rand.Next(2);
                    if (roll == 0)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.GetOrAdd(number, number);
                        });
                    }
                    else if (roll == 1)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.TryGetValue(number, out var _);
                        });
                    }
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkTree(int acountCount,  int range, int runs, int warmUpRuns) {
            var rand = new Random();

            var tree = new RawConcurrentIndexedTree<int, int>();

            List<Action> actions = null;

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var roll = rand.Next(2);
                    if (roll == 0)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.GetOrAdd(number, number);
                        });
                    }
                    else if (roll == 1)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.TryGetValue(number, out var _);
                        });
                    }
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkTreeRead(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new RawConcurrentIndexedTree<int, int>();

            List<Action> actions = null;

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    {
                        var number = rand.Next(range);
                        tree.GetOrAdd(number, number);
                    }
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.TryGetValue(number, out var _);
                        });
                    }
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkTreeWrite(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new RawConcurrentIndexedTree<int, int>();

            List<Action> actions = null;

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var number = rand.Next(range);
                    actions.Add(() =>
                    {
                        tree.GetOrAdd(number, number);
                    });
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();
        }


        static string BenchmarkClassicRead(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new ConcurrentDictionary<int, int>();

            var actions = new List<Action>();

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    {
                        var number = rand.Next(range);
                        tree.GetOrAdd(number, number);
                    }
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.TryGetValue(number, out var _);
                        });
                    }
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkClassicWrite(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new ConcurrentDictionary<int, int>();

            var actions = new List<Action>();

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var number = rand.Next(range);
                    actions.Add(() =>
                    {
                        tree.GetOrAdd(number, number);
                    });
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkClassic(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            var tree = new ConcurrentDictionary<int, int>();

            var actions = new List<Action>();

            Action stepUp = () =>
            {
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var roll = rand.Next(2);
                    if (roll == 0)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.GetOrAdd(number, number);
                        });
                    }
                    else if (roll == 1)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.TryGetValue(number, out var _);
                        });
                    }
                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();
        }
    }
}
