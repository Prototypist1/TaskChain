using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class ConcurrentLinkedList<TValue> : IReadOnlyList<TValue>
    {
        protected volatile Link start = new Link() { deleted = 1 };
        protected volatile Link endOfChain;
        private int count = 0;

        public ConcurrentLinkedList()
        {
            endOfChain = start;
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
            public int deleted;

            public Link(TValue value) => this.value = value;

            public Link()
            {
            }
        }

        /// <summary>
        /// count seems kind of fundimentally unsafe
        /// even if it is right for an instance 
        /// by the time you use it it could be wrong
        /// 
        /// I think I'd like to remove it
        /// and make this an IEnummerable instaed of a IReadOnlyList
        /// </summary>
        public int Count {
            get {
                return count;
            }
        }

        public bool TryGetFirst(out TValue first)
        {
            var target = start;
            while (target.deleted == 1) {
                target = target.next;
                if (target == null)
                {
                    first = default;
                    return false;
                }
            }
            first = target.Value;
            return true;
        }

        public bool TryGetLast(out TValue last)
        {
            // try to use GetEndOfCahin
            var target = GetEndOfCahin();
            if (target == start)
            {
                last = default;
                return false;
            }
            if (target.deleted == 0) {
                last = target.Value;
                return true;
            }
            
            // but the end of the chain might be deleted
            // in that case, loop over the chain
            var at = start;
            var res = false;
            last = default;
            do
            {
                if (at.deleted == 0)
                {
                    res = true;
                    last = at.Value;
                }
                at = at.next;
            } while (at != null);
            return res;
        }

        private Link Get(int i) {
            if (i < 0) {
                throw new IndexOutOfRangeException($"index: {i} is not expected to be to be less than 0");
            }
            var at = start;
            var myIndex = 0;
            do
            {
                if (myIndex == i)
                {
                    return at;
                }
                if (at.deleted == 0)
                {
                    myIndex++;
                }
                at = at.next;
            } while (at != null);
            throw new IndexOutOfRangeException($"index: {i} requested, only {myIndex} items avaible");
        }

        /// <summary>
        /// removes the first entry that matches values
        /// </summary>
        public bool Remove(TValue value) {
        TOP:
            var at = start;
            if (at.deleted == 1 && at.next != null) {
                if (Interlocked.CompareExchange(ref start, at.next, at) == at) {
                    goto TOP;
                }
            }

            Link last = null;
            while (at != null)
            {
                if (at.deleted == 1)
                { 
                    // don't delete the end
                    // someone could be adding after it
                    if (at.next != null && last != null)
                    {
                        last.next = at.next;
                    }
                }
                else if (at.Value.Equals(value) && Interlocked.CompareExchange(ref at.deleted, 1, 0) == 0)
                {
                    Interlocked.Decrement(ref count);
                    return true;
                }
                last = at;
                at = at.next;
            }
            return false;
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
            var at = start;
            while (at != null)
            {
                if (Interlocked.CompareExchange(ref at.deleted, 1, 0) == 0)
                {
                    Interlocked.Decrement(ref count);
                    if (at.next != null) {
                        Interlocked.CompareExchange(ref start, at.next, at);
                    }
                    return true;
                }
                else if (at.next != null)
                {
                    Interlocked.CompareExchange(ref start, at.next, at);
                }
                at = at.next;
            }
            return false;
            //while (true)
            //{

            //    var startSnapShot = beforeStart;
            //    if (startSnapShot.next == null)
            //    {
            //        return false;
            //    }
            //    if (Interlocked.CompareExchange(ref beforeStart, startSnapShot.next, startSnapShot) == startSnapShot)
            //    {
            //        Interlocked.Decrement(ref count);
            //        return true;
            //    }
            //    //startSnapShot = beforeStart;
            //}
        }


        public bool RemoveStart(out TValue res) {

            //while (true)
            //{
            //    var startSnapShot = beforeStart;
            //    if (startSnapShot.next == null)
            //    {
            //        res = default;
            //        return false;
            //    }
            //    if (Interlocked.CompareExchange(ref beforeStart, startSnapShot.next, startSnapShot) == startSnapShot)
            //    {
            //        Interlocked.Decrement(ref count);
            //        res = startSnapShot.next.Value;
            //        return true;
            //    }
            //}

            var at = start;
            while (at != null)
            {
                if (Interlocked.CompareExchange(ref at.deleted, 1, 0) == 0)
                {
                    Interlocked.Decrement(ref count);
                    if (at.next != null)
                    {
                        Interlocked.CompareExchange(ref start, at.next, at);
                    }
                    res = at.Value;
                    return true;
                }
                else if (at.next != null)
                {
                    Interlocked.CompareExchange(ref start, at.next, at);
                }
                at = at.next;
            }
            res = default;
            return false;
        }

        public IEnumerator<TValue> GetEnumerator() {
            var at = start;
            while (at != null) {
                if (at.deleted == 0)
                {
                    yield return at.Value;
                }
                at = at.next;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

