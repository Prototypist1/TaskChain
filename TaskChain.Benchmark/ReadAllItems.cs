using BenchmarkDotNet.Attributes;
using Prototypist.TaskChain.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.Benchmark
{
    [RPlotExporter, RankColumn]
    public class ReadAllItems
    {

        private ConcurrentHashIndexedTree<HashTest2, string> mine;
        private ConcurrentDictionary<HashTest2, string> concurrentDictionary;
        private Random random;

        [Params(1, 50, 100, 300, 500)]
        public int Items;
        [Params(2, 4, 8)]
        public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            random = new Random((int)DateTime.Now.Ticks);
            mine = new ConcurrentHashIndexedTree<HashTest2, string>();
            concurrentDictionary = new ConcurrentDictionary<HashTest2, string>();
            for (var x = 1; x <= Items; x++)
            {
                for (var y = 1; y <= Items; y++)
                {
                    mine.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                    concurrentDictionary.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                }
            }
        }

        [Benchmark]
        public void Mine()
        {
            void read()
            {
                for (var i = 0; i < 1000; i++)
                {
                    mine.TryGet(new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1)), out var _);
                }
            }

            var actions = new List<Action>();
            for (var i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }

            Parallel.Invoke(actions.ToArray());

        }

        [Benchmark]
        public void ConcurrentDictionary()
        {
            void read()
            {
                for (var i = 0; i < 1000; i++)
                {
                    concurrentDictionary.TryGetValue(new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1)), out var _);
                }
            }

            var actions = new List<Action>();
            for (var i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }

            Parallel.Invoke(actions.ToArray());
        }
    }

    public class ParallelUpdate
    {

        private ConcurrentHashIndexedTree<int, string> mine;
        private ConcurrentDictionary<int, string> concurrentDictionary;
        private Random random;
        private List<Action> cdActions;
        private List<Action> myActions;

        //[Params(1, 50, 100, 300, 500)]
        [Params(1)]
        public int RandomRange;
        [Params(4)]
        public int Threads;
        [Params(10000)]
        public  int Reps;

        [GlobalSetup]
        public void Setup()
        {
            random = new Random((int)DateTime.Now.Ticks);
            mine = new ConcurrentHashIndexedTree<int, string>();
            concurrentDictionary = new ConcurrentDictionary<int, string>();

            void cdRead()
            {
                for (var i = 0; i < Reps; i++)
                {
                    concurrentDictionary[random.Next(1, RandomRange + 1)] = "";
                }
            }

            cdActions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                cdActions.Add(cdRead);
            }

            void myRead()
            {
                for (var i = 0; i < Reps; i++)
                {
                    mine.Set(random.Next(1, RandomRange + 1), "");
                }
            }

            myActions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                myActions.Add(myRead);
            }

        }

        [Benchmark]
        public void Mine()
        {
            Parallel.Invoke(myActions.ToArray());
        }

        //[Benchmark]
        public void ConcurrentDictionary()
        {
            Parallel.Invoke(cdActions.ToArray());
        }

        //[Benchmark]
        public void ChainingMine()
        {
            Chaining.Run(myActions.ToArray());
        }

        //[Benchmark]
        public void ChainingConcurrentDictionary()
        {
            Chaining.Run(cdActions.ToArray());
        }
    }

    [RPlotExporter, RankColumn]
    public class WriteAllItems
    {

        private ConcurrentHashIndexedTree<HashTest2, string> mine;
        private ConcurrentDictionary<HashTest2, string> concurrentDictionary;
        private Random random;

        //[Params(1, 50, 100, 300, 500)]
        [Params(10)]
        public int Items;
        //[Params(2, 4, 8)]
        //public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            random = new Random((int)DateTime.Now.Ticks);
            mine = new ConcurrentHashIndexedTree<HashTest2, string>();
            concurrentDictionary = new ConcurrentDictionary<HashTest2, string>();
            //for (int x = 1; x <= Items; x++)
            //{
            //    for (int y = 1; y <= Items; y++)
            //    {
            //        mine.GetOrAdd(new HashTest2(x, y), x + ", " + y);
            //        concurrentDictionary.GetOrAdd(new HashTest2(x, y), x + ", " + y);
            //    }
            //}
        }

        [Benchmark]
        public void Mine()
        {
            //Action read = () =>
            //{
            for (var i = 0; i < 100; i++)
            {
                mine.Set(new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1)), "");
            }
            //};

            //List<Action> actions = new List<Action>();
            //for (int i = 0; i < Threads; i++)
            //{
            //    actions.Add(read);
            //}

            //Parallel.Invoke(actions.ToArray());

        }

        //[Benchmark]
        public void ConcurrentDictionary()
        {
            //Action read = () =>
            //{
            for (var i = 0; i < 100; i++)
            {
                concurrentDictionary[new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1))] = "";
            }
            //};

            //List<Action> actions = new List<Action>();
            //for (int i = 0; i < Threads; i++)
            //{
            //    actions.Add(read);
            //}

            //Parallel.Invoke(actions.ToArray());
        }
    }
}
