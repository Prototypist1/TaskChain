using BenchmarkDotNet.Attributes;
using Prototypist.TaskChain.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.Benchmark
{


    public class HashTest2
    {
        private readonly int x;
        private readonly int y;

        public HashTest2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            return obj is HashTest2 test &&
                   x == test.x &&
                   y == test.y;
        }

        public override int GetHashCode()
        {
            unchecked {
                return x + y;
            }
        }
    }

    public class HashTest
    {
        private readonly int hashCode;
        private int nature;

        public HashTest(int hashCode, int nature)
        {
            this.hashCode = hashCode;
            this.nature = nature;
        }

        public override bool Equals(object obj)
        {
            return obj is HashTest test &&
                   nature == test.nature;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }


    [RPlotExporter, RankColumn]
    public class ConcurrentHashIndexedTreeBenchmarks
    {


        private Dictionary<int, int> dict;

        private RawConcurrentHashIndexed<int, int> rawConcurrentHashIndexed;

        private ConcurrentHashIndexedTree<int, int> concurrentHashIndexedTree;
        private ConcurrentHashIndexedTree<HashTest, int> concurrentHashIndexedTree2;
        private ConcurrentDictionary<HashTest, int> concurrentDictionary2;
        private ConcurrentDictionary<int, int> concurrentDictionary;
        //private Tuple<int, int>[][] data;

        //[Params(0,1,2,3,4,5)]
        [Params(1)]
        public int Items;
        //[Params(4)]
        //public int Threads;

        [GlobalSetup]
        public void Setup()
        {


            concurrentHashIndexedTree2 = new ConcurrentHashIndexedTree<HashTest, int>();
            for (var i = 1; i <= Items; i++)
            {
                concurrentHashIndexedTree2.GetOrAdd(new HashTest(1, i), i);
            }

            concurrentDictionary2 = new ConcurrentDictionary<HashTest, int>();
            for (var i = 1; i <= Items; i++)
            {
                concurrentDictionary2.GetOrAdd(new HashTest(1, i), i);
            }

            rawConcurrentHashIndexed = new RawConcurrentHashIndexed<int, int>();
            rawConcurrentHashIndexed.GetOrAdd(new ConcurrentIndexedListNode3<int, int>(1, 1));

            dict = new Dictionary<int, int>
            {
                [1] = 1
            };

            concurrentHashIndexedTree = new ConcurrentHashIndexedTree<int, int>();
            concurrentHashIndexedTree.Set(1, 1);
            //concurrentHashIndexedTree.GetOrAdd(new IndexedListNode<int, Concurrent<int>>(1, new Concurrent<int>(1)));
            //concurrentHashIndexedTree.GetOrAdd(new IndexedListNode<int, int>(1,1));
            concurrentDictionary = new ConcurrentDictionary<int, int>
            {
                [1] = 1
            };
            //var r = new Random((int)DateTime.Now.Ticks);
            //data = new Tuple<int, int>[Threads][];
            //for (int i = 0; i < Threads; i++)
            //{
            //    data[i] = new Tuple<int, int>[Items];
            //    for (int j = 0; j < Items; j++)
            //    {
            //        data[i][j] = new Tuple<int, int>(r.Next(1, 1000), r.Next(1, 1000));
            //    }
            //}

        }

        //[Benchmark]
        public void A()
        {
            rawConcurrentHashIndexed.GetNodeOrThrow(1);
        }

        //[Benchmark]
        public void B()
        {
            dict.TryGetValue(1, out var _);
        }

        [Benchmark]
        public void AddOrUpdateHashIndexedTree()
        {
            //Parallel.ForEach(data, (list) =>
            //{
            //    foreach (var (x,y) in list)
            //    {
            //        concurrentHashIndexedTree.UpdateOrAdd(x, z=>z+y,y);
            //    }
            //});
            concurrentHashIndexedTree2.TryGet(new HashTest(1, 1),out var _);
            //concurrentHashIndexedTree.GetNodeOrThrow(1);//.value.Value;
            //var db = x.Value;
        }

        [Benchmark]
        public void AddOrUpdateDictionary()
        {
            //Parallel.ForEach(data, (list) =>
            //{
            //    foreach (var (x, y) in list)
            //    {
            //        concurrentDictionary.AddOrUpdate(x, y, (_,z) => z + y );
            //    }
            //});
            concurrentDictionary2.TryGetValue(new HashTest(1, 1), out var _);
        }
    }
}
