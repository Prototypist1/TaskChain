using Prototypist.TaskChain.DataTypes;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace TaskChain.Test
{

    public class HashCollisionTests
    {
        public class HashTestHelper
        {
            private readonly int hashCode;
            private int nature;

            public HashTestHelper(int hashCode, int nature)
            {
                this.hashCode = hashCode;
                this.nature = nature;
            }

            public override bool Equals(object obj)
            {
                return obj is HashTestHelper test &&
                       nature == test.nature;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }

        [Fact]
        public void TryGet()
        {
            var target = new ConcurrentHashIndexedTree<HashTestHelper, int>();
            
            target.AddOrThrow(new HashTestHelper(1,1), 1);
            target.AddOrThrow(new HashTestHelper(1, 2), 2);
            target.AddOrThrow(new HashTestHelper(1, 3), 3);
            target.AddOrThrow(new HashTestHelper(1, 4), 4);
            target.AddOrThrow(new HashTestHelper(1, 5), 5);
            target.AddOrThrow(new HashTestHelper(1, 6), 6);

            Assert.True(target.TryGet(new HashTestHelper(1, 1), out var res1));
            Assert.Equal(1, res1);
            Assert.True(target.TryGet(new HashTestHelper(1, 2), out var res2));
            Assert.Equal(2, res2);
            Assert.True(target.TryGet(new HashTestHelper(1, 3), out var res3));
            Assert.Equal(3, res3);
            Assert.True(target.TryGet(new HashTestHelper(1, 4), out var res4));
            Assert.Equal(4, res4);
            Assert.True(target.TryGet(new HashTestHelper(1, 5), out var res5));
            Assert.Equal(5, res5);
            Assert.True(target.TryGet(new HashTestHelper(1, 6), out var res6));
            Assert.Equal(6, res6);
        }

    }
}
