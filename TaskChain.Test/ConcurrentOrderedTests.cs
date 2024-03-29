﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class ConcurrentOrderedTests
    {
        [Fact]
        public async Task AddStuff()
        {
            for (int k = 0; k < 1000; k++)
            {
                var student = new ConcurrentArrayList<int>();

                var tasks = new List<Task>();
                for (var i = 0; i < 20; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var r = new Random(i);
                        for (var j = 0; j < 100; j++)
                        {
                            student.EnqueAdd(r.Next(0, 100));
                        }
                    }));
                }

                await Task.WhenAll(tasks.ToArray());

                Assert.Equal(2000, student.Count);
            }
        }

        [Fact]
        public void SimpleAddStuff()
        {
            for (int k = 0; k < 1000; k++)
            {
                var student = new ConcurrentArrayList<int>();


                var r = new Random();
                for (var j = 0; j < 100; j++)
                {
                    student.EnqueAdd(r.Next(0, 100));
                }

                Assert.Equal(100, student.Count);
            }
        }

        [Fact]
        public void Scramble()
        {
            for (int k = 0; k < 1000; k++)
            {
                var student = new ConcurrentArrayList<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

                Parallel.For(0, 20, (i) =>
                {
                    var r = new Random(i);
                    for (var j = 0; j < 100; j++)
                    {
                        student[r.Next(0, 10)] = student[r.Next(0, 10)];
                    }
                });
            }
        }

        // this is a bad test
        // ConcurrentArrayList's itterator makes a copy of the list and itterates on that
        // you would have to try to get while that copy is being made for it to fail
        //
        //[Fact]
        //public void UpdateAndIterate()
        //{
        //    for (int k = 0; k < 1000; k++)
        //    {
        //        var student = new ConcurrentArrayList<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        //        Assert.ThrowsAny<Exception>(() =>
        //        {
        //            foreach (var item in student)
        //            {
        //                student[0] = item;
        //            }
        //        });
        //    }
        //}

        [Fact]
        public void ReadAndIterate()
        {
            for (int k = 0; k < 1000; k++)
            {
                var student = new ConcurrentArrayList<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

                foreach (var item in student)
                {
                    var harmless = student[0];
                }
            }
        }


        [Fact]
        public void DoubleIterate()
        {
            for (int k = 0; k < 1000; k++)
            {
                var student = new ConcurrentArrayList<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
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
