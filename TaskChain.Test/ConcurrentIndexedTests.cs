
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class ConcurrentIndexedTests
    {
        [Theory]
        [InlineData(1)]
        public void AddUpdate(int i)
        {
            var thing = new ConcurrentIndexed<int, string>();

            thing.DoOrAdd(i, (s) => s + " world", "hello");
            Assert.Equal("hello", thing.GetOrThrow(i));
            thing.DoOrAdd(i, (s) => s + " world", "hello");
            Assert.Equal("hello world", thing.GetOrThrow(i));
        }

        [Theory]
        [InlineData(1)]
        public void TryRemove(int i)
        {
            var thing = new ConcurrentIndexed<int, string>();

            Assert.True( thing.TryAdd(i,  "world"));
            Assert.True(thing.TryRemove(i, out var value));
            Assert.Equal("world", value);
            Assert.True(thing.TryAdd(i, "world"));
        }

        [Theory]
        [InlineData(1)]
        public void AddUpdateOrThrow(int i)
        {
            var thing = new ConcurrentIndexed<int, string>();

            thing.AddOrThrow(i, "hello");
            Assert.Equal("hello", thing.GetOrThrow(i));
            Assert.Throws<Exception>(() =>
            {
                thing.AddOrThrow(i, "hello");

            });

            thing.UpdateOrThrow(i, (s) => s + " world");
            Assert.Equal("hello world", thing.GetOrThrow(i));
        }

        [Theory]
        [InlineData(1)]
        public void GetFallback(int i)
        {
            var thing = new ConcurrentIndexed<int, string>();

            Assert.Equal("not hello", thing.GetOrAdd(i, () => "not hello"));
            Assert.Equal("not hello", thing.GetOrAdd(i, () => "hello"));
        }

        [Theory]
        [InlineData(1)]
        public void UpdateFallback(int i)
        {
            var thing = new ConcurrentIndexed<int, string>();

            Assert.Equal("not hello", thing.DoOrAdd(i, (x) => x + " world", "not hello"));
            Assert.Equal("not hello", thing.GetOrThrow(i));
        }

        [Fact]
        public async Task AddUpdateParallel()
        {
            {
                var thing = new ConcurrentIndexed<int, string>();

                var tasks = new List<Task>();
                for (var i = 0; i < 100; i += 3)
                {
                    tasks.Add(Task.Run(() => AddUpdate(i)));
                    tasks.Add(Task.Run(() => GetFallback(i + 1)));
                    tasks.Add(Task.Run(() => UpdateFallback(i + 2)));
                }

                await Task.WhenAll(tasks.ToArray());
            }
        }

        [Fact]
        public void Enumerate()
        {
            var thing = new ConcurrentIndexed<int, string>();
            for (var i = 0; i < 100; i++)
            {
                thing.AddOrThrow(i, () => $"{i}");
            }
            var count = 0;
            foreach (var uhh in thing)
            {
                count++;
            }
            Assert.Equal(100, count);
        }

        [Fact]
        public void TryGet()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.False(target.TryGet(1,out var _));

            target.AddOrThrow(1, "1");

            Assert.True(target.TryGet(1, out var second));
            Assert.Equal("1", second);
        }

        [Fact]
        public void GetOrTrhow()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.ThrowsAny<Exception>(() => target.GetOrThrow(1));

            target.AddOrThrow(1, "1");

            Assert.Equal("1", target.GetOrThrow(1));
        }

        [Fact]
        public void SetOrThrow()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.ThrowsAny<Exception>(() => target.UpdateOrThrow(1, "1"));

            target.AddOrThrow(1, "1");
            target.UpdateOrThrow(1, "1-1");

            Assert.Equal("1-1", target.GetOrThrow(1));
        }

        [Fact]
        public void DoOrThrow()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.ThrowsAny<Exception>(() => target.DoOrThrow(1, x => x + "!"));

            target.AddOrThrow(1, "1");
            target.DoOrThrow(1, x => x + "!");

            Assert.Equal("1!", target.GetOrThrow(1));
        }

        [Fact]
        public void DoOrThrow_Func()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.ThrowsAny<Exception>(() => target.DoOrThrow(1, x => x + "!"));

            target.AddOrThrow(1, "1");
            string s = null;
            target.DoOrThrow(1, x => {
                s = x + "!";
                return x;
            });

            Assert.Equal("1!", s);
        }

        [Fact]
        public void Set()
        {
            var target = new ConcurrentIndexed<int, string>();

            target.Set(1, "1");
            Assert.Equal("1", target.GetOrThrow(1));
            target.Set(1, "1!");
            Assert.Equal("1!", target.GetOrThrow(1));
        }

        [Fact]
        public void GetOrAdd()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.Equal("1", target.GetOrAdd(1, "1"));
            Assert.Equal("1", target.GetOrAdd(1, "1!"));
        }

        [Fact]
        public void GetOrAdd_Func()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.Equal("1", target.GetOrAdd(1, () => "1"));
            Assert.Equal("1", target.GetOrAdd(1, () => "1!"));
        }

        [Fact]
        public void DoOrAdd()
        {
            var target = new ConcurrentIndexed<int, string>();

            target.DoOrAdd(1, x => "1!", "1");
            Assert.Equal("1", target.GetOrThrow(1));
            target.DoOrAdd(1, x => "1!", "1");
            Assert.Equal("1!", target.GetOrThrow(1));
        }

        [Fact]
        public void AddOrThrow()
        {
            var target = new ConcurrentIndexed<int, string>();

            target.AddOrThrow(1, "1");

            Assert.ThrowsAny<Exception>(() => target.AddOrThrow(1, "1!"));
        }

        [Fact]
        public void AddOrThrow_Func()
        {
            var target = new ConcurrentIndexed<int, string>();

            target.AddOrThrow(1, () => "1");

            Assert.ThrowsAny<Exception>(() => target.AddOrThrow(1, () => "1!"));
        }


        [Fact]
        public void UpdateOrThrow()
        {
            var target = new ConcurrentIndexed<int, string>();

            Assert.ThrowsAny<Exception>(() => target.UpdateOrThrow(1, x => x + "!"));
            target.Set(1, "1");
            Assert.Equal("1!", target.UpdateOrThrow(1, x => x + "!"));
        }


        [Fact]
        public void DoubleIterate()
        {
            for (int k = 0; k < 1000; k++)
            {
                var student = new ConcurrentIndexed<int,int>();
                for (int i = 0; i < 10; i++)
                {
                    student.GetOrAdd(i, i);
                }
                var count = 0;

                foreach (var x in student)
                {
                    foreach (var y in student)
                    {
                        count++;
                    }
                }
                Assert.Equal(100, count);
            }
        }

    }
}
