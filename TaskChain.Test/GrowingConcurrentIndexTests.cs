using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class GrowingConcurrentIndexTests
    {
        [Fact]
        public void Add() {
            var thing = new RawConcurrentGrowingIndex<string, string>();
            var str = Guid.NewGuid().ToString();
            thing.GetOrAdd(str, str);

            Assert.Equal(str, thing[str]);
        }

        [Fact]
        public void AddAndGetBack()
        {
            var thing = new RawConcurrentIndexedTree<string, string>();
            var toAdds = new string[1000];
            for (int i = 0; i < 1000; i++)
            {
                toAdds[i] = Guid.NewGuid().ToString();
            }
            foreach (var toAdd in toAdds)
            {
                thing.GetOrAdd(toAdd, toAdd);
            }
            foreach (var toAdd in toAdds)
            {
                Assert.Equal(toAdd, thing[toAdd]);
            }
        }


        [Fact]
        public void GrowingAddAndGetBack()
        {
            var thing = new RawConcurrentGrowingIndexedTree<string, string>();
            var toAdds = new string[1000];
            for (int i = 0; i < 1000; i++)
            {
                toAdds[i] = Guid.NewGuid().ToString();
            }
            foreach (var toAdd in toAdds)
            {
                thing.GetOrAdd(toAdd, toAdd);
            }
            foreach (var toAdd in toAdds)
            {
                Assert.Equal(toAdd, thing[toAdd]);
            }
        }



        [Fact]
        public void MemTest() {

            int[] array = new int[100];
            var memory = new Memory<int>(array);

            var actions = new List<Action>();

            for (int i = 0; i < 100; i++)
            {
                var j = i;
                actions.Add(() =>
                {
                    memory.Span[j] = j;
                });
            }

            Parallel.Invoke(actions.ToArray());
            
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, memory.Span[i]);
            }
        }

        [Fact]
        public void Growing3AddAndGetBack()
        {
            for (int k = 0; k < 100; k++)
            {

                var thing = new RawConcurrentGrowingIndexedTree3<string, string>();
                var toAdds = new string[1000];
                for (int i = 0; i < 1000; i++)
                {
                    toAdds[i] = Guid.NewGuid().ToString();
                }
                foreach (var toAdd in toAdds)
                {
                    thing.GetOrAdd(toAdd, toAdd);
                }
                foreach (var toAdd in toAdds)
                {
                    Assert.Equal(toAdd, thing[toAdd]);
                }
            }
        }


        [Fact]
        public void Growing3AddAndGetBackParallel()
        {
            var thing = new RawConcurrentGrowingIndexedTree3<string, string>();
            var toAdds = new string[100000];
            for (int i = 0; i < 100000; i++)
            {
                toAdds[i] = Guid.NewGuid().ToString();
            }

            Parallel.Invoke(toAdds.Select<string, Action>(x =>
                 () => thing.GetOrAdd(x, x)).ToArray());

            Parallel.Invoke(toAdds.Select<string, Action>(x =>
                () => Assert.Equal(x, thing[x])).ToArray());
        }

        [Fact]
        public void Growing2AddAndGetBack()
        {
            var thing = new RawConcurrentGrowingIndexedTree2<string, string>();
            var toAdds = new string[1000];
            for (int i = 0; i < 1000; i++)
            {
                toAdds[i] = Guid.NewGuid().ToString();
            }
            foreach (var toAdd in toAdds)
            {
                thing.GetOrAdd(toAdd, toAdd);
            }
            foreach (var toAdd in toAdds)
            {
                Assert.Equal(toAdd, thing[toAdd]);
            }
        }

        [Fact]
        public void Growing2AddAndGetBackParallel()
        {
            var thing = new RawConcurrentGrowingIndexedTree2<string, string>();
            var toAdds = new string[10000];
            for (int i = 0; i < 10000; i++)
            {
                toAdds[i] = Guid.NewGuid().ToString();
            }

            Parallel.Invoke(toAdds.Select<string, Action>(x =>
                 () => thing.GetOrAdd(x, x)).ToArray());

            Parallel.Invoke(toAdds.Select<string, Action>(x =>
                () => Assert.Equal(x, thing[x])).ToArray());
        }
    }
}
