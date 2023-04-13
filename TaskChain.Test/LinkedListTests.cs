using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class LinkedListTests
    {

        [Fact]
        public void AddRemoveAndCount() {
            for (int i = 0; i < 10000; i++)
            {
                var random = new Random();
                var removed = 0;
                var subject = new ConcurrentLinkedList<int>();

                var toAdd = random.Next(0, 1000);
                var toRemove = random.Next(0, 1000);

                var actions = new List<Action>();

                for (int j = 0; j < toAdd; j++)
                {
                    actions.Add(() =>  subject.Add(0));
                }

                for (int j = 0; j < toRemove; j++)
                {
                    actions.Add(() =>
                    {
                        if (subject.RemoveStart())
                        {
                            Interlocked.Increment(ref removed);
                        }
                    });
                }

                Parallel.Invoke( actions.OrderBy(x => random.NextDouble()).ToArray());

                Assert.Equal(toAdd-removed, subject.Count);
                Assert.Equal(toAdd - removed, subject.Count());

            }
        }

        [Fact]
        public void AddRemove() {
            for (int i = 0; i < 1000; i++)
            {
                var random = new Random();
                var subject = new ConcurrentLinkedList<int>();

                var addCount = 1000;

                var toAdd = new int[addCount].Select(_ => random.Next(0, 1000)).ToArray();
                var toRemove = new int[addCount].Select(_ => random.Next(0, 1000)).ToArray();

                var actions = new List<Action>();

                for (int j = 0; j < toAdd.Length; j++)
                {
                    var target = toAdd[j];
                    actions.Add(() => subject.Add(target));
                }

                for (int j = 0; j < toRemove.Length; j++)
                {
                    var target = toRemove[j];
                    subject.Add(target);
                    actions.Add(() => subject.Remove(target));
                }

                Parallel.Invoke(actions.OrderBy(x => random.NextDouble()).ToArray());

                Assert.Equal(addCount, subject.Count);
            }
        }
    }
}
