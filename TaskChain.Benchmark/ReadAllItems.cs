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

        [Params(1,50,100,300,500)]
        public int Items;
        [Params(2,4,8)]
        public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            random = new Random((int)DateTime.Now.Ticks);
            mine = new ConcurrentHashIndexedTree<HashTest2, string>();
            concurrentDictionary = new ConcurrentDictionary<HashTest2, string>();
            for (int x = 1; x <= Items; x++)
            {
                for (int y = 1; y <= Items; y++)
                {
                    mine.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                    concurrentDictionary.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                }
            }
        }

        [Benchmark]
        public void Mine()
        {
            Action read = () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    mine.TryGet(new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1)), out var _);
                }
            };

            List<Action> actions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }
            
            Parallel.Invoke(actions.ToArray());
            
        }

        [Benchmark]
        public void ConcurrentDictionary()
        {
            Action read = () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    concurrentDictionary.TryGetValue(new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1)), out var _);
                }
            };

            List<Action> actions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }

            Parallel.Invoke(actions.ToArray());
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
                for (int i = 0; i < 100; i++)
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
                for (int i = 0; i < 100; i++)
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
