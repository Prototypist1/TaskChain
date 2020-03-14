
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

            AddOrUpdateInner(i, thing);
        }

        private static void AddOrUpdateInner(int i, MonsterIndexBackedIndex.View<int, string> thing)
        {
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
            GetOrAddInner(i, thing);
        }

        private static void GetOrAddInner(int i, MonsterIndexBackedIndex.View<int, string> thing)
        {
            Assert.Equal("not hello", thing.GetOrAdd(i, "not hello"));
            Assert.Equal("not hello", thing.GetOrAdd(i, "hello"));
        }

        [Theory]
        [InlineData(1)]
        public void TryGetValue(int i)
        {
            var target = new MonsterIndexBackedIndex.View<int, string>();

            TryGetValueInner(i, target);
        }

        private static void TryGetValueInner(int i, MonsterIndexBackedIndex.View<int, string> target)
        {
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

            GetOrThrowInner(i, target);
        }

        private static void GetOrThrowInner(int i, MonsterIndexBackedIndex.View<int, string> target)
        {
            Assert.ThrowsAny<Exception>(() => target.GetOrThrow(i));

            Assert.True(target.TryAdd(i, "1"));

            Assert.Equal("1", target.GetOrThrow(i));
        }

        [Fact]
        public async Task AddUpdateParallel()
        {



            var tasks = new List<Task>();
            for (var i = 0; i < 500 * 4; i += 4)
            {
                var j = i;
                tasks.Add(Task.Run(() => AddOrUpdate(j)));
                tasks.Add(Task.Run(() => GetOrAdd(j + 1)));
                tasks.Add(Task.Run(() => TryGetValue(j + 2)));
                tasks.Add(Task.Run(() => GetOrThrow(j + 3)));
            }

            await Task.WhenAll(tasks.ToArray());

        }

        [Fact]
        public async Task AddUpdateParallelSameView()
        {

            var thing = new MonsterIndexBackedIndex.View<int, string>();

            var tasks = new List<Task>();
            for (var i = 0; i < 500 * 4; i += 4)
            {
                var j = i;
                tasks.Add(Task.Run(() => AddOrUpdateInner(j, thing)));
                tasks.Add(Task.Run(() => GetOrAddInner(j + 1, thing)));
                tasks.Add(Task.Run(() => TryGetValueInner(j + 2, thing)));
                tasks.Add(Task.Run(() => GetOrThrowInner(j + 3, thing)));
            }

            await Task.WhenAll(tasks.ToArray());

        }
    }
}
