
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class BigBackedeIndexedTests
    {
        [Theory]
        [InlineData(1)]
        public void AddOrUpdate(int i)
        {
            var thing = new MonsterIndexBackedIndex.View<int, string>();

            thing.AddOrUpdate(i, "hello");
            Assert.Equal("hello", thing.GetOrThrow(i));
            thing.AddOrUpdate(i, "world hello");
            Assert.Equal("world hello", thing.GetOrThrow(i));
        }

        [Theory]
        [InlineData(1)]
        public void GetOrAdd(int i)
        {
            var thing = new MonsterIndexBackedIndex.View<int, string>();

            Assert.Equal("not hello", thing.GetOrAdd(i, "not hello"));
            Assert.Equal("not hello", thing.GetOrAdd(i, "hello"));
        }


        [Theory]
        [InlineData(1)]
        public void TryGetValue(int i)
        {
            var target = new MonsterIndexBackedIndex.View<int, string>();

            Assert.False(target.TryGetValue(i, out var _));

            Assert.True(target.TryAdd(i, "1"));

            Assert.True(target.TryGetValue(i, out var second));
            Assert.Equal("1", second);
        }

        [Theory]
        [InlineData(1)]
        public void GetOrThrow(int i)
        {
            var target = new MonsterIndexBackedIndex.View<int, string>();

            Assert.ThrowsAny<Exception>(() => target.GetOrThrow(i));

            Assert.True(target.TryAdd(i, "1"));

            Assert.Equal("1", target.GetOrThrow(i));
        }

        [Fact]
        public async Task AddUpdateParallel()
        {
            {
                var thing = new MonsterIndexBackedIndex.View<int, string>();

                var tasks = new List<Task>();
                for (var i = 0; i < 100*4; i += 4)
                {
                    tasks.Add(Task.Run(() => AddOrUpdate(i)));
                    tasks.Add(Task.Run(() => GetOrAdd(i + 1)));
                    tasks.Add(Task.Run(() => TryGetValue(i + 2)));
                    tasks.Add(Task.Run(() => GetOrThrow(i + 3)));
                }

                await Task.WhenAll(tasks.ToArray());
            }
        }








    }
}
