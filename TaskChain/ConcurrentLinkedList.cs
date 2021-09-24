using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class ConcurrentLinkedList<TValue> : IReadOnlyList<TValue>
    {
        protected volatile Link beforeStart = new Link();
        protected volatile Link endOfChain;
        private int count = 0;

        public ConcurrentLinkedList()
        {
            endOfChain = beforeStart;
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
            var target = beforeStart.next;
            if (target == null) {
                first = default;
                return false;
            }
            first = target.Value;
            return true;
        }

        public bool TryGetLast(out TValue last)
        {
            var target = GetEndOfCahin();
            if (target == beforeStart)
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
            var at = beforeStart.next;
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
                var myEndOfChain = GetEndOfCahin();
                if (Interlocked.CompareExchange(ref myEndOfChain.next, link, null) == null)
                {
                    Interlocked.Increment(ref count);
                    return;
                }
            }
        }

        private Link GetEndOfCahin() {
            while (true) {
                var myEndOfChain = endOfChain;
                if (myEndOfChain.next == null)
                {
                    return myEndOfChain;
                }
                Interlocked.CompareExchange(ref endOfChain, myEndOfChain.next, myEndOfChain);
            }
        }

        public bool RemoveStart() 
        {
            while (true)
            {
                var startSnapShot = beforeStart;
                if (startSnapShot.next == null)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref beforeStart, startSnapShot.next, startSnapShot) == startSnapShot)
                {
                    Interlocked.Decrement(ref count);
                    return true;
                }
                startSnapShot = beforeStart;
            }
        }


        public bool RemoveStart(out TValue res) {

            while (true)
            {
                var startSnapShot = beforeStart;
                if (startSnapShot.next == null)
                {
                    res = default;
                    return false;
                }
                if (Interlocked.CompareExchange(ref beforeStart, startSnapShot.next, startSnapShot) == startSnapShot)
                {
                    Interlocked.Decrement(ref count);
                    res = startSnapShot.next.Value;
                    return true;
                }
            }
        }

        public IEnumerator<TValue> GetEnumerator() {
            var at = beforeStart.next;
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

