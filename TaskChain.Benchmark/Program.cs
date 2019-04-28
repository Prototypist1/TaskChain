using BenchmarkDotNet.Running;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.Benchmark
{
    class Program
    {
        private const int AcountCount = 1000000;
        private const int Range = 100000;
        private const int Runs = 50;
        private const int WarmUpRuns = 5;

        static void Main(string[] _)
        {
            //var summary = BenchmarkRunner.Run<ReadAllItems>();
            //var summary = BenchmarkRunner.Run<WriteAllItems>();
            //var summary = BenchmarkRunner.Run<ParallelUpdate>();
            //var summary = BenchmarkRunner.Run<InterlockedTest>();

            Console.WriteLine("Classics read 100         " + BenchmarkGrowingTreeRead(100, Runs, WarmUpRuns));
            Console.WriteLine("Classics read 1000        " + BenchmarkGrowingTreeRead(1000, Runs, WarmUpRuns));
            Console.WriteLine("Classics read 10000       " + BenchmarkGrowingTreeRead(10000, Runs, WarmUpRuns));
            Console.WriteLine("Classics read 100000      " + BenchmarkGrowingTreeRead(100000, Runs, WarmUpRuns));
            Console.WriteLine("Classics read 1000000     " + BenchmarkGrowingTreeRead(1000000, Runs, WarmUpRuns));

            //Console.WriteLine("Growing tree read  " + BenchmarkGrowingTreeRead(AcountCount, Runs, WarmUpRuns));
            //Console.WriteLine("Tree read          " + BenchmarkTreeRead(AcountCount, Runs, WarmUpRuns));
            //Console.WriteLine("Classics read      " + BenchmarkClassicRead(AcountCount, Runs, WarmUpRuns));
            //Console.WriteLine("growing Tree write " + BenchmarkGrowingTreeWrite(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Tree write         " + BenchmarkTreeWrite(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Classics write     " + BenchmarkClassicWrite(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Growing Tree       " + BenchmarkGrowingTree(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Tree               " + BenchmarkTree(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Classics           " + BenchmarkClassic(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Mine               " + BenchmarkMine(AcountCount, Range, Runs, WarmUpRuns));
            //Console.WriteLine("Growing            " + BenchmarkGrowing(AcountCount, Range, Runs, WarmUpRuns));
            //Console.ReadLine();
        }


        static string BenchmarkMine(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null; ;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentIndexed<int, int>();
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    var roll = rand.Next(2);
                    if (roll == 0)
                    {
                        var number = rand.Next(range);
                        actions.Add(() =>
                        {
                            tree.GetOrAdd(new RawConcurrentIndexed<int, int>.KeyValue(number, number));
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


            List<Action> actions = null; ;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentGrowingIndex<int, int>();
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
        static string BenchmarkGrowingTree(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentGrowingIndexedTree<int, int>();
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

        static string BenchmarkTree(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();

            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentIndexedTree<int, int>();
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

        static string BenchmarkGrowingTreeRead(int acountCount, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentGrowingIndexedTree<Guid, string>();
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {

                    var guid = Guid.NewGuid();
                    tree.GetOrAdd(guid, guid.ToString());

                    actions.Add(() =>
                    {
                        tree.TryGetValue(guid, out var _);
                    });

                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkTreeRead(int acountCount, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentIndexedTree<Guid, string>();
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {

                    var guid = Guid.NewGuid();
                    tree.GetOrAdd(guid, guid.ToString());

                    actions.Add(() =>
                    {
                        tree.TryGetValue(guid, out var _);
                    });

                }
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();

        }

        static string BenchmarkGrowingTreeWrite(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentGrowingIndexedTree<int, int>();
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


        static string BenchmarkTreeWrite(int acountCount, int range, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentIndexedTree<int, int>();
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


        static string BenchmarkClassicRead(int acountCount, int runs, int warmUpRuns)
        {
            var rand = new Random();


            var actions = new List<Action>();

            Action stepUp = () =>
            {
                var tree = new ConcurrentDictionary<Guid, string>();
                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {

                    var guid = Guid.NewGuid();
                    tree.GetOrAdd(guid, guid.ToString());

                    actions.Add(() =>
                    {
                        tree.TryGetValue(guid, out var _);
                    });

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


            var actions = new List<Action>();

            Action stepUp = () =>
            {
                var tree = new ConcurrentDictionary<int, int>();
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


            var actions = new List<Action>();

            Action stepUp = () =>
            {
                var tree = new ConcurrentDictionary<int, int>();
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

                actions.Add(() => {

                    var db = tree;
                });
            };

            Action run = () =>
            {
                Parallel.Invoke(actions.ToArray());
            };

            return MyStupidBenchmarker.Benchmark(stepUp, run, runs, warmUpRuns).ToString();
        }
    }
}
