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
            var thing = new RawConcurrentIndexed<string, string>();
            var str = Guid.NewGuid().ToString();
            thing.GetOrAdd(str, str);

            Assert.Equal(str, thing[str]);
        }

        [Fact]
        public void AddAndGetBack()
        {
            var thing = new RawConcurrentIndexed<string, string>();
            var toAdds = new string[1000];
            for (int i = 0; i < toAdds.Length; i++)
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
            var thing = new RawConcurrentIndexed<string, string>();
            var toAdds = new string[1000];
            for (int i = 0; i < toAdds.Length; i++)
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
        public void Growing3AddAndGetBack()
        {
            for (int k = 0; k < 100; k++)
            {

                var thing = new RawConcurrentIndexed<string, string>();
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
            for (int n = 0; n < 100; n++)
            {
                var thing = new RawConcurrentIndexed<string, string>();
                var toAdds = new string[10000];
                for (int i = 0; i < toAdds.Length; i++)
                {
                    toAdds[i] = Guid.NewGuid().ToString();
                }

                Parallel.Invoke(toAdds.Select<string, Action>(x =>
                     () => thing.GetOrAdd(x, x)).ToArray());

                Parallel.Invoke(toAdds.Select<string, Action>(x =>
                    () => Assert.Equal(x, thing[x])).ToArray());
            }
        }

        [Fact]
        public void Growing2AddAndGetBack()
        {
            var thing = new RawConcurrentIndexed<string, string>();
            var toAdds = new string[1000];
            for (int i = 0; i < toAdds.Length; i++)
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
            for (int n = 0; n < 100; n++)
            {
                var thing = new RawConcurrentIndexed<string, string>();
                var toAdds = new string[10000];
                for (int i = 0; i < toAdds.Length; i++)
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
}
