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

        private RawConcurrentIndexed<HashTest2, string> mine;
        private RawConcurrentGrowingIndex<HashTest2, string> growingIndexed;
        private ConcurrentDictionary<HashTest2, string> concurrentDictionary;
        private Dictionary<HashTest2, string> dictionary;
        private RawConcurrentIndexedTree<HashTest2, string> tree;
        private Random random;
        private HashTest2[] HashItems;

        [Params(500)]
        public int Items;
        [Params(1)]
        public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            random = new Random((int)DateTime.Now.Ticks);
            mine = new RawConcurrentIndexed<HashTest2, string>();
            growingIndexed = new RawConcurrentGrowingIndex<HashTest2, string>();
            concurrentDictionary = new ConcurrentDictionary<HashTest2, string>();
            dictionary = new Dictionary<HashTest2, string>();
            tree = new RawConcurrentIndexedTree<HashTest2, string>();
            for (var x = 1; x <= Items; x++)
            {
                for (var y = 1; y <= Items; y++)
                {
                    mine.GetOrAdd(new RawConcurrentIndexed<HashTest2, string>.KeyValue( new HashTest2(x, y), x + ", " + y));
                    concurrentDictionary.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                    growingIndexed.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                    dictionary[new HashTest2(x, y)] = x + ", " + y;
                    tree.GetOrAdd(new HashTest2(x, y), x + ", " + y);
                }
            }

            HashItems = new HashTest2[1000];
            for (var i = 0; i < 1000; i++)
            {
                HashItems[i] =new HashTest2(random.Next(1, Items + 1), random.Next(1, Items + 1));
            }
        }

        //[Benchmark]
        public void Simple()
        {
            void read()
            {
                foreach (var item in HashItems)
                {
                    dictionary.TryGetValue(item, out var _);
                }
            }

            var actions = new List<Action>();
            for (var i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }

            Parallel.Invoke(actions.ToArray());
        }

        //[Benchmark]
        public void Mine()
        {
            void read()
            {
                foreach (var item in HashItems)
                {
                    mine.TryGetValue(item, out var _);
                }
            }

            var actions = new List<Action>();
            for (var i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }

            Parallel.Invoke(actions.ToArray());
        }

        //[Benchmark]
        public void RawConcurrentGrowingIndex()
        {
            void read()
            {
                foreach (var item in HashItems)
                {
                    growingIndexed.TryGetValue(item, out var _);
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
        public void Tree()
        {
            void read()
            {
                foreach (var item in HashItems)
                {
                    tree.TryGetValue(item, out var _);
                }
            }

            var actions = new List<Action>();
            for (var i = 0; i < Threads; i++)
            {
                actions.Add(read);
            }

            Parallel.Invoke(actions.ToArray());
        }

        //[Benchmark]
        public void ConcurrentDictionary()
        {
            void read()
            {
                foreach (var item in HashItems)
                {
                    concurrentDictionary.TryGetValue(item, out var _);
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
