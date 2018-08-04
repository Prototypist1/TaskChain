using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{
    public class TaskManagerTests
    {
        [Fact]
        public async Task BunchOWork()
        {
            var taskManager = new TaskManager();

            var workItemsEach = 1000;
            var bools = new bool[10 * workItemsEach];
            var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            await Task.WhenAll(numbers.Select(number => Task.Run(() =>
            {
                var actions = new List<Action>();
                for (int i = 0; i < workItemsEach; i++)
                {
                    var j = i;
                    actions.Add(() =>
                    {
                        int k = 0;
                        for (; k < (workItemsEach * number) + j;)
                        {
                            k++;
                        }
                        bools[k] = true;
                    });
                }

                taskManager.Run(actions.ToArray());
            })));

            for (int i = 0; i < bools.Length; i++)
            {

                Assert.True(bools[i]);
            }
        }
    }

}
