using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
