using BenchmarkDotNet.Attributes;
using Prototypist.TaskChain.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypist.TaskChain.Benchmark
{
    [RPlotExporter, RankColumn]
    public class ReadAllItems
    {
        //private ConcurrentDictionary<Guid, string> thing;
        private RawConcurrentGrowingIndexedTree3<Guid, string> thing;
        private List<List<Guid>> items;
        
        [Params(100,10000,1000000)]
        public int Items;
        [Params(1)]
        public int Threads;
        [Params(1)]
        public int sizeInBit;


        [GlobalSetup]
        public void Setup()
        {
            //thing = new ConcurrentDictionary<Guid, string>();
            thing = new RawConcurrentGrowingIndexedTree3<Guid, string>(sizeInBit);

            items = new List<List<Guid>>();
            for (var x = 0; x < Threads; x++)
            {
                var myList = new List<Guid>();
                for (var y = 0; y < Items; y++)
                {
                    var guid = Guid.NewGuid();
                    thing.GetOrAdd(guid, guid.ToString());
                    myList.Add(guid);
                }
                items.Add(myList);
            }

            Task.Delay(2000).Wait();

        }

        [Benchmark]
        public void Read()
        {
            void read(List<Guid> items)
            {
                foreach (var item in items)
                {
                    thing.TryGetValue(item, out var _);
                }
            }

            var actions = new List<Action>();
            foreach (var item in items)
            {
                actions.Add(() => read(item));
            }

            Parallel.Invoke(actions.ToArray());
        }

        //[Benchmark]
        public void Write()
        {
            void write()
            {
                for (int i = 0; i < 1000; i++)
                {
                    var guid = Guid.NewGuid();
                    thing.GetOrAdd(guid, guid.ToString());
                }
                
            }

            var actions = new List<Action>();
            foreach (var item in items)
            {
                actions.Add(write);
            }

            Parallel.Invoke(actions.ToArray());
        }
    }

    public class InterlockedTest {

        [Params(4)]
        public int Threads;
        [Params(10000)]
        public int Reps;

        private readonly object o = new object();
        private int i1 = 0;
        private List<Action> lockActions;
        private int i2;
        private List<Action> interlockActions;

        [GlobalSetup]
        public void Setup()
        {
            void lockAdd()
            {
                lock (o) {
                    i1++;
                }
            }

            lockActions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                lockActions.Add(lockAdd);
            }

            void interlockAdd()
            {
                Interlocked.Increment(ref i2);
            }

            interlockActions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                interlockActions.Add(interlockAdd);
            }

        }


        [Benchmark]
        public void Lock()
        {
            Parallel.Invoke(lockActions.ToArray());
        }

        [Benchmark]
        public void InterLocked()
        {
            Parallel.Invoke(interlockActions.ToArray());
        }
    }

    public class ParallelUpdate
    {

        private ConcurrentIndexed<int, string> mine;
        private ConcurrentDictionary<int, string> concurrentDictionary;
        private Random random;
        private List<Action> cdActions;
        private List<Action> myActions;
        
        [Params(1,10)]
        public int RandomRange;
        [Params(1,4)]
        public int Threads;
        [Params(10000)]
        public  int Reps;

        [GlobalSetup]
        public void Setup()
        {
            random = new Random((int)DateTime.Now.Ticks);
            mine = new ConcurrentIndexed<int, string>();
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

        [Benchmark]
        public void ConcurrentDictionary()
        {
            Parallel.Invoke(cdActions.ToArray());
        }
    }

    [RPlotExporter, RankColumn]
    public class WriteAllItems
    {

        private RawConcurrentIndexed<Guid, Guid> origninal;
        private RawConcurrentGrowingIndex<Guid, Guid> growning;
        private RawConcurrentIndexedTree<Guid, Guid> tree;
        private ConcurrentDictionary<Guid, Guid> concurrentDictionary;

        
        [Params(500)]
        public int Items;
        [Params(4)]
        public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            tree = new RawConcurrentIndexedTree<Guid, Guid>();
            growning = new RawConcurrentGrowingIndex<Guid, Guid>();
            concurrentDictionary = new ConcurrentDictionary<Guid, Guid>();
            origninal = new RawConcurrentIndexed<Guid, Guid>();
        }

        private void AddToOriginal()
        {
            for (var i = 0; i < Items; i++)
            {
                origninal.GetOrAdd(new RawConcurrentIndexed<Guid, Guid>.KeyValue( Guid.NewGuid(), Guid.NewGuid()));
            }
        }

        //[Benchmark]
        public void Original()
        {
            var actions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                actions.Add(AddToOriginal);
            }

            Parallel.Invoke(actions.ToArray());
        }

        private void AddToGrowing()
        {
            for (var i = 0; i < Items; i++)
            {
                growning.GetOrAdd(Guid.NewGuid(), Guid.NewGuid());
            }
        }

        //[Benchmark]
        public void Growing()
        {
            var actions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                actions.Add(AddToGrowing);
            }

            Parallel.Invoke(actions.ToArray());

        }


        private void AddToTree() {
            for (var i = 0; i < Items; i++)
            {
                tree.GetOrAdd(Guid.NewGuid(), Guid.NewGuid());
            }
        }

        [Benchmark]
        public void Tree()
        {
            var actions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                actions.Add(AddToTree);
            }

            Parallel.Invoke(actions.ToArray());

        }

        private void AddToConcurrentDictionary()
        {
            for (var i = 0; i < Items; i++)
            {
                concurrentDictionary.GetOrAdd(Guid.NewGuid(), Guid.NewGuid());
            }
        }

        //[Benchmark]
        public void ConcurrentDictionary()
        {
            var actions = new List<Action>();
            for (int i = 0; i < Threads; i++)
            {
                actions.Add(AddToConcurrentDictionary);
            }

            Parallel.Invoke(actions.ToArray());
        }
    }
}
