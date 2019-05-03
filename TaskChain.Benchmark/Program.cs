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
        private const int Runs = 10;
        private const int WarmUpRuns = 1;

        static void Main(string[] _)
        {

            //foreach (var i in new[] { 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000 }){
            //    var tree = new RawConcurrentGrowingIndexedTree2<Guid, string>();
            //    var guidList = new List<Guid>();
            //    for (int ii = 0; ii < i; ii++)
            //    {
            //        guidList.Add(Guid.NewGuid());
            //    }
            //    foreach (var guid in guidList)
            //    {
            //        tree.GetOrAdd(guid, guid.ToString());
            //    }

            //    var watch = System.Diagnostics.Stopwatch.StartNew();
            //    Parallel.Invoke(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.Select(x =>
            //    {
            //        Action a = () =>
            //        {
            //            for (int j = x; j < guidList.Count; j += 12)
            //            {

            //                tree.TryGetValue(guidList[j], out var _);
            //            }
            //        };
            //        return a;
            //    }).ToArray());

            //    watch.Stop();
            //    Console.WriteLine($"reads: {i}, depth per read: {((double)tree.depth)/i}, took: {((double)watch.ElapsedTicks*1_000_000) / (TimeSpan.TicksPerMillisecond* i) }");
            //}


            //var summary = BenchmarkRunner.Run<ReadAllItems>();
            //var summary = BenchmarkRunner.Run<WriteAllItems>();
            //var summary = BenchmarkRunner.Run<ParallelUpdate>();
            //var summary = BenchmarkRunner.Run<InterlockedTest>();
            //Console.WriteLine("JustReadFromAnArrayALot 1000000         " + JustReadFromAnArrayALot(100000000, 1, 0));
            //Console.WriteLine("Growing tree read 100         " + BenchmarkGrowingTreeRead(100, Runs, WarmUpRuns));
            //Console.WriteLine("Growing tree read 1000        " + BenchmarkGrowingTreeRead(1000, Runs, WarmUpRuns));
            //Console.WriteLine("Growing tree read 10000       " + BenchmarkGrowingTreeRead(10000, Runs, WarmUpRuns));
            //Console.WriteLine("Growing tree read 100000      " + BenchmarkGrowingTreeRead(100000, Runs, WarmUpRuns));
            //Console.WriteLine("Growing tree read 1000000     " + BenchmarkGrowingTreeRead(1000000, 1, 0));

            Console.WriteLine("Growing tree 2 read 100         " + BenchmarkGrowingTree2Read(100, Runs, WarmUpRuns));
            Console.WriteLine("Growing tree 2 read 1000        " + BenchmarkGrowingTree2Read(1000, Runs, WarmUpRuns));
            Console.WriteLine("Growing tree 2 read 10000       " + BenchmarkGrowingTree2Read(10000, Runs, WarmUpRuns));
            Console.WriteLine("Growing tree 2 read 100000      " + BenchmarkGrowingTree2Read(100000, Runs, WarmUpRuns));
            Console.WriteLine("Growing tree 2 read 1000000     " + BenchmarkGrowingTree2Read(1000000, Runs, WarmUpRuns));


            //Console.WriteLine("Classic read 100         " + BenchmarkClassicRead(100, Runs, WarmUpRuns));
            //Console.WriteLine("Classic read 1000        " + BenchmarkClassicRead(1000, Runs, WarmUpRuns));
            //Console.WriteLine("Classic read 10000       " + BenchmarkClassicRead(10000, Runs, WarmUpRuns));
            //Console.WriteLine("Classic read 100000      " + BenchmarkClassicRead(100000, Runs, WarmUpRuns));
            //Console.WriteLine("Classic read 1000000     " + BenchmarkClassicRead(1000000, Runs, WarmUpRuns));

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


        private class ArrayHolder {
            public Memory<string> array;
        }

        static string JustReadFromAnArrayALot(int acountCount, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;
            ArrayHolder ah = new ArrayHolder();

            Action stepUp = () =>
            {
                var tree = new string[100];
                for (int i = 0; i < 100; i++)
                {
                    tree[i] = "thing " + i;
                }
                ah.array = tree;

                Action action = () =>
                {
                    var _ = ah.array.Span[(int)rand.Next(100)];
                };

                actions = new List<Action>();
                for (int i = 0; i < acountCount; i++)
                {
                    actions.Add(action);
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


        static string BenchmarkGrowingTree2Read(int acountCount, int runs, int warmUpRuns)
        {
            var rand = new Random();


            List<Action> actions = null;

            Action stepUp = () =>
            {
                var tree = new RawConcurrentGrowingIndexedTree2<Guid, string>();
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
