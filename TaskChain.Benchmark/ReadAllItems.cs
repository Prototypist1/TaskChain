using BenchmarkDotNet.Attributes;
using Prototypist.TaskChain.DataTypes;
using System.Collections.Concurrent;

namespace Prototypist.TaskChain.Benchmark
{
    [RPlotExporter, RankColumn]
    public class ReadAllItems
    {

        private ConcurrentHashIndexedTree<HashTest2, string> mine;
        private ConcurrentDictionary<HashTest2, string> concurrentDictionary;

        [Params(100,300,500)]
        public int Items;
        //[Params(2,4,8)]
        //public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            
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
        public void MineSeries()
        {
            for (int x = 1; x <= Items; x++)
            {
                for (int y = 1; y <= Items; y++)
                {
                    mine.TryGet(new HashTest2(x, y), out var _);
                }
            }
        }

        [Benchmark]
        public void ConcurrentDictionarySeries()
        {
            for (int x = 1; x <= Items; x++)
            {
                for (int y = 1; y <= Items; y++)
                {
                    concurrentDictionary.TryGetValue(new HashTest2(x, y), out var _);
                }
            }
        }
    }
}
