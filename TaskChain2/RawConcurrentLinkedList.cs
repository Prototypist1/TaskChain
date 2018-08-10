using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Prototypist.TaskChain
{
    public class RawConcurrentLinkedList<TValue>: IReadOnlyCollection<TValue>
    {
        protected volatile Link startOfChain;
        protected volatile Link endOfChain = new Link();
        private int count = 0;

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

        public void Add(TValue value) {
            var link = new Link(value);
            while (true)
            {
                if (Interlocked.CompareExchange(ref endOfChain.next, link, null) == null)
                {
                    endOfChain = endOfChain.next;
                    Interlocked.Increment(ref count);
                    Interlocked.CompareExchange(ref startOfChain, link, null);
                    return;
                }
            }
        }

        public IEnumerator<TValue> GetEnumerator() {
            var at = startOfChain;
            yield return at.Value;
            while (at.next != null) {
                at = at.next;
                yield return at.Value;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

