using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Prototypist.TaskChain.Test
{

    public class QueingConcurrentTests
    {
        [Fact]
        public void Count()
        {
            var target = new QueueingConcurrent<int>(0);

            int plusOne(int x) => x + 1;
            Parallel.For(0, 1000, (i) =>
            {
                target.Act(plusOne);
            });
            
            Assert.Equal(1000, target.EnqueRead().Result);
        }
    }
}
