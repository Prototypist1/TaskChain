using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class ConcurrentLinkedList<TValue> : IReadOnlyList<TValue>
    {
        protected Link BoforeStart = new Link();
        protected volatile Link endOfChain;
        private int count = 0;

        public ConcurrentLinkedList()
        {
            endOfChain = BoforeStart;
        }

        protected class Link
        {
            public TValue Value {
                get {
                    return (TValue)value;
                }
                set {
                    this.value = value;
                }
            }
            private volatile object value;
            public volatile Link next;

            public Link(TValue value) => this.value = value;

            public Link()
            {
            }
        }

        public int Count {
            get {
                return count;
            }
        }

        public bool TryGetFirst(out TValue first)
        {
            var target = BoforeStart.next;
            if (target == null) {
                first = default;
                return false;
            }
            first = target.Value;
            return true;
        }

        public bool TryGetLast(out TValue last)
        {
            var target = endOfChain;
            if (target == null)
            {
                last = default;
                return false;
            }
            last = target.Value;
            return true;
        }

        private Link Get(int i) {
            if (i < 0) {
                throw new IndexOutOfRangeException($"index: {i} is not expected to be to be less than 0");
            }
            var at = BoforeStart.next;
            var myIndex = 0;
            while (true) { 
                if (myIndex == i) {
                    return at;
                }
                if (at.next == null )
                {
                    throw new IndexOutOfRangeException($"index: {i} requested, only {myIndex} items avaible");
                }
                at = at.next;
                myIndex++;
            }
        }

        public TValue this[int index] { get {
                return Get(index).Value;
            }
            set {
                Get(index).Value = value;
            }
        }

        public void Add(TValue value) {
            var link = new Link(value);
            while (true)
            {
                var myEndOfChain = endOfChain;
                if (Interlocked.CompareExchange(ref myEndOfChain.next, link, null) == null)
                {
                    endOfChain = endOfChain.next;
                    Interlocked.Increment(ref count);
                    return;
                }
            }
        }

        public bool RemoveStart() {
            while (true)
            {
                var toRemove = BoforeStart;
                if (toRemove.next == null)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref BoforeStart, toRemove.next, toRemove) == toRemove)
                {
                    Interlocked.Decrement(ref count);

                    return true;
                }
            }
        }

        public IEnumerator<TValue> GetEnumerator() {
            var at = BoforeStart.next;
            if (at != null) {
                yield return at.Value;
                while (at.next != null) {
                    at = at.next;
                    yield return at.Value;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

