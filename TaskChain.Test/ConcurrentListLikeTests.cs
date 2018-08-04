using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class ConcurrentListLikeTests
    {
        [Fact]
        public async Task AddStuff()
        {
            var student = new ConcurrentListLike<int>();

            var tasks = new List<Task>();
            for (var i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var r = new Random(i);
                    for (var j = 0; j < 100; j++)
                    {
                        student.Add(r.Next(0, 100));
                    }
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            Assert.Equal(2000, student.Count);
        }

        [Fact]
        public void Scramble()
        {
            var student = new ConcurrentListLike<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            Parallel.For(0, 20, (i) =>
            {
                var r = new Random(i);
                for (var j = 0; j < 100; j++)
                {
                    student[r.Next(0, 10)] = student[r.Next(0, 10)];
                }
            });
        }

        [Fact]
        public void UpdateAndIterate()
        {
            var student = new ConcurrentListLike<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            // this just need to not throw
            foreach (var item in student.ToArray())
            {
                student[0] = item;
            }

        }

        [Fact]
        public void ReadAndIterate()
        {
            var student = new ConcurrentListLike<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            foreach (var item in student.ToArray())
            {
                var harmless = student[0];
            }
        }


        [Fact]
        public void DoubleIterate()
        {
            var student = new ConcurrentListLike<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            var count = 0;

            foreach (var x in student.ToArray())
            {
                foreach (var y in student.ToArray())
                {
                    count++;
                }
            }
            Assert.Equal(100, count);
        }

    }
}
