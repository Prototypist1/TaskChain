using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{
    // TODO redo, more custom
    /// <summary>
    /// A list without remove
    /// </summary>
    public class ConcurrentOrdered<TValue>: IReadOnlyList<TValue>
    {
        private readonly IActionChainer actionChainer;
        private readonly List<ItemShell> backing = new List<ItemShell>();

        private ConcurrentOrdered(IEnumerable<TValue> old, IActionChainer actionChainer)
        {
            this.actionChainer = actionChainer;
            actionChainer.Run(() =>
            {
                foreach (var item in old)
                {
                    backing.Add(new ItemShell(item, actionChainer.GetActionChainer()));
                }
            });
        }

        private ConcurrentOrdered(IActionChainer actionChainer)
        {
            this.actionChainer = actionChainer;
        }

        public ConcurrentOrdered(IEnumerable<TValue> old) :
            this(old, Chaining.taskManager.GetActionChainer())
        {
        }

        public ConcurrentOrdered() :
            this(Chaining.taskManager.GetActionChainer())
        {
        }

        public int Count
        {
            get
            {
                return actionChainer.Run(() => backing.Count);
            }
        }

        public void Update(int index, Func<TValue, TValue> func)
        {
            backing[index].Do(x => x.Set(func(x.Get())));
        }

        public void Add(TValue value)
        {
            actionChainer.Run(() =>
            {
                var entry = new ItemShell(value, actionChainer.GetActionChainer());
                backing.Add(entry);
            });
        }

        public TValue this[int index]
        {
            get { return backing[index].Get(); }
            set { backing[index].Do(x => x.Set(value)); }
        }

        private class ItemShell
        {
            private readonly Item item;
            private readonly IActionChainer actionChainer;

            public ItemShell(TValue value, IActionChainer actionChainer)
            {
                item = new Item(value);
                this.actionChainer = actionChainer;
            }

            public TValue Get()
            {
                return item.Get();
            }

            public class Item
            {
                public Item(TValue value)
                {
                    this.value = value;
                }

                // this wishes it could be volitile
                private object value;

                public TValue Get()
                {
                    // reads are atomic so this is ok 
                    return (TValue)Volatile.Read(ref value);
                }
                public void Set(TValue value)
                {
                    Volatile.Write(ref this.value, value);
                }
            }

            internal void Do(Action<Item> action)
            {
                actionChainer.Run(() => action(item));
            }

            internal TOut Do<TOut>(Func<Item, TOut> function)
            {
                return actionChainer.Run(() => function(item));
            }
        }

        public void AddSet(IEnumerable<TValue> collection)
        {
            actionChainer.Run(() =>
            {
                foreach (var value in collection)
                {
                    var entry = new ItemShell(value, actionChainer.GetActionChainer());
                    backing.Add(entry);
                }
            });
        }

        public TValue[] ToArray()
        {
            return actionChainer.Run(() => backing.Select(x => x.Get()).ToArray());
        }

        public bool Contains<T>(T t)
        {
            if (t == null)
            {
                return actionChainer.Run(() => backing.Any(x => x.Get() == null));
            }
            return actionChainer.Run(() => backing.Any(x => t.Equals(x.Get())));
        }

        public IEnumerator<TValue> GetEnumerator() {
            var count = Count;
            int i = 0;
            while (i < Count)
            {
                for (; i < count; i++)
                {
                    yield return this[i];
                }
                count = Count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
