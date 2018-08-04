using System;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class ConcurrentSetTests {
        [Fact]
        public void Contains() {
            var target = new ConcurrentSet<int>();

            Assert.False(target.Contains(1));

            target.ContainsOrAdd(1);

            Assert.True(target.Contains(1));
        }

        [Fact]
        public void ContainsOrAdd()
        {
            var target = new ConcurrentSet<int>();

            Assert.False(target.ContainsOrAdd(1));
            Assert.True(target.ContainsOrAdd(1));
        }

        [Fact]
        public void TryAdd()
        {
            var target = new ConcurrentSet<int>();

            Assert.True(target.TryAdd(1));
            Assert.False(target.TryAdd(1));
        }

        [Fact]
        public void ToEnumerable()
        {
            var thing = new ConcurrentSet<int>();
            for (var i = 0; i < 100; i++)
            {
                thing.TryAdd(i);
            }
            var count = 0;
            foreach (var uhh in thing.ToEnumerable())
            {
                count++;
            }
            Assert.Equal(100, count);
        }


        [Fact]
        public void GetOrAdd()
        {
            var thing = new ConcurrentSet<Tuple<int>>();

            var t1 = new Tuple<int>(1);
            var res= thing.GetOrAdd(t1);
            Assert.Same(res, t1);

            var t2 = new Tuple<int>(1);
            var res2 = thing.GetOrAdd(t2);
            Assert.Same(res2, t1);

        }
    }
}