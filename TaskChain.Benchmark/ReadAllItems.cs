using BenchmarkDotNet.Attributes;
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
        private RawConcurrentIndexed<Guid, string> thing;
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
            thing = new RawConcurrentIndexed<Guid, string>(sizeInBit);

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


    [RPlotExporter, RankColumn]
    public class HashCollisionTest
    {
        private RawConcurrentIndexed<SimpleHash, string> thing;
        private SimpleHash simpleHash0, simpleHash1, simpleHash2, simpleHashNot;

        [GlobalSetup]
        public void Setup()
        {
            thing = new RawConcurrentIndexed<SimpleHash, string>(2);
            simpleHash0 = new SimpleHash(0b_0000_00_00_00_00_00_00_00_00_00_00_00_00_00_00);
            thing.GetOrAdd(simpleHash0, "0-0");

            simpleHash1 = new SimpleHash(0b_0001_01_00_00_00_00_00_00_00_00_00_00_00_00_00);
            thing.GetOrAdd(new SimpleHash(0b_0001_00_00_00_00_00_00_00_00_00_00_00_00_00_00), "1-0");
            thing.GetOrAdd(simpleHash1, "1-1");

            simpleHashNot = new SimpleHash(0b_0010_01_00_01_00_00_10_00_00_00_00_00_00_00_00);

            //simpleHash2 = new SimpleHash(0b_0010_10_00_00_00_00_00_00_00_00_00_00_00_00_00);
            //thing.GetOrAdd(new SimpleHash(0b_0010_00_00_00_00_00_00_00_00_00_00_00_00_00_00), "2-0");
            //thing.GetOrAdd(new SimpleHash(0b_0010_01_00_00_00_00_00_00_00_00_00_00_00_00_00), "2-1");
            //thing.GetOrAdd(simpleHash2, "1-2");
        }

        [Benchmark]
        public void SameBucket2()
        {
            thing.TryGetValue(simpleHash1, out var _);
        }

        [Benchmark]
        public void NotThere()
        {
            thing.TryGetValue(simpleHashNot, out var _);
        }

        //[Benchmark]
        //public void SameBucket3()
        //{
        //    thing.TryGetValue(simpleHash2, out var _);
        //}

        [Benchmark]
        public void Single()
        {
            thing.TryGetValue(simpleHash0, out var _);
        }




    }

    [RPlotExporter, RankColumn]
    public class HashCollisionTestSystem
    {
        private ConcurrentDictionary<SimpleHash, string> thing;
        private SimpleHash simpleHash0;
        private SimpleHash simpleHash1, simpleHashNot;

        [GlobalSetup]
        public void Setup()
        {
            thing = new ConcurrentDictionary<SimpleHash, string>();
            simpleHash0 = new SimpleHash(0);
            thing.GetOrAdd(simpleHash0, "0-0");

            simpleHash1 = new SimpleHash(1);
            thing.GetOrAdd(simpleHash1, "1-18");
            for (int i = 0; i < 1; i++)
            {
                thing.GetOrAdd(new SimpleHash(1), Guid.NewGuid().ToString());
            }

            simpleHashNot = new SimpleHash(2);

            //thing.GetOrAdd(new SimpleHash((31*19)+5), "1-19");
            //thing.GetOrAdd(new SimpleHash((31*20)+5), "1-20");
            //thing.GetOrAdd(new SimpleHash((31*21)+5), "1-21");
            //thing.GetOrAdd(new SimpleHash((31*22)+5), "1-22");
            //thing.GetOrAdd(new SimpleHash((31*23)+5), "1-23");
            //thing.GetOrAdd(new SimpleHash((31*24)+5), "1-24");
            //thing.GetOrAdd(new SimpleHash((31*25)+5), "1-25");
            //thing.GetOrAdd(new SimpleHash((31*26)+5), "1-26");
            //thing.GetOrAdd(new SimpleHash((31*27)+5), "1-27");
            //thing.GetOrAdd(new SimpleHash((31*28)+5), "1-28");
            //thing.GetOrAdd(new SimpleHash((31*29)+5), "1-29");
            //thing.GetOrAdd(new SimpleHash((31*30)+5), "1-30");
            //thing.GetOrAdd(new SimpleHash((31*31)+5), "1-31");
        }

        [Benchmark]
        public void SameBucket2()
        {

            thing.TryGetValue(simpleHash1, out var _);
        }

        [Benchmark]
        public void NotThere()
        {
            thing.TryGetValue(simpleHashNot, out var _);
        }

        [Benchmark]
        public void Single()
        {
            thing.TryGetValue(simpleHash0, out var _);
        }

    }

    public class SimpleHash {
        private readonly int hash;

        public SimpleHash(int hash)
        {
            this.hash = hash;
        }

        public override int GetHashCode()
        {
            return hash;
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

        //private RawConcurrentIndexed<Guid, Guid> origninal;
        private RawConcurrentIndexed<Guid, Guid> growning;
        //private RawConcurrentIndexedTree<Guid, Guid> tree;
        private ConcurrentDictionary<Guid, Guid> concurrentDictionary;

        
        [Params(500)]
        public int Items;
        [Params(4)]
        public int Threads;

        [GlobalSetup]
        public void Setup()
        {
            //tree = new RawConcurrentIndexedTree<Guid, Guid>();
            growning = new RawConcurrentIndexed<Guid, Guid>();
            concurrentDictionary = new ConcurrentDictionary<Guid, Guid>();
            //origninal = new RawConcurrentIndexed<Guid, Guid>();
        }

        //private void AddToOriginal()
        //{
        //    for (var i = 0; i < Items; i++)
        //    {
        //        origninal.GetOrAdd(new RawConcurrentIndexed<Guid, Guid>.KeyValue( Guid.NewGuid(), Guid.NewGuid()));
        //    }
        //}

        //[Benchmark]
        //public void Original()
        //{
        //    var actions = new List<Action>();
        //    for (int i = 0; i < Threads; i++)
        //    {
        //        actions.Add(AddToOriginal);
        //    }

        //    Parallel.Invoke(actions.ToArray());
        //}

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


        //private void AddToTree() {
        //    for (var i = 0; i < Items; i++)
        //    {
        //        tree.GetOrAdd(Guid.NewGuid(), Guid.NewGuid());
        //    }
        ////}

        //[Benchmark]
        //public void Tree()
        //{
        //    var actions = new List<Action>();
        //    for (int i = 0; i < Threads; i++)
        //    {
        //        actions.Add(AddToTree);
        //    }

        //    Parallel.Invoke(actions.ToArray());

        //}

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
