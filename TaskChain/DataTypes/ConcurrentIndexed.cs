using Prototypist.TaskChain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain.DataTypes
{



    // this would be way simpler if I did not creat so many crazy methods...
    public class ConcurrentIndexed<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        TreeNode<TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>> tree = new TreeNode<TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>>(64);
        private readonly ITaskManager taskManager;

        public ConcurrentIndexed()
        {
            this.taskManager = Chaining.taskManager;
        }

        public ConcurrentIndexed(IEnumerable<Tuple<TKey, TValue>> x) : this()
        {
            foreach (var pair in x)
            {
                AddOrThrow(pair.Item1, pair.Item2);
            }
        }

        #region Help

        private BuildableListNode<TKey, TValue> GetNodeOrThrow(TKey key)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            return tree.backing[a].backing[b].backing[c];
        }

        #endregion

        public TValue GetOrThrow(TKey key)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Value;
                }
                at = at.next;
            };
        }

        public bool TryGet(TKey key, out TValue res)
        {
            try
            {
                res = GetOrThrow(key);
                return true;
            }
            catch
            {
                res = default(TValue);
                return false;
            }
        }

        public void SetOrThrow(TKey key, TValue value)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    at.Do(x=>x.Value = value);
                    return;
                }
                at = at.next;
            };
        }

        public void DoOrThrow(TKey key, Action<BuildableConcurrent<TValue>.ValueHolder> action)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    at.Do((x) =>  action(x));
                    return;
                }
                at = at.next;
            };
        }

        public TOut DoOrThrow<TOut>(TKey key, Func<BuildableConcurrent<TValue>.ValueHolder, TOut> function)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Do(x => function(x));
                }
                at = at.next;
            };
        }

        public void Set(TKey key, TValue value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager, value);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    at.Do(x=>x.Value = value);
                    return;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return;
                }
                at = at.next;
            };
        }

        public TValue GetOrAdd(TKey key, TValue fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return fallback;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Value;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return fallback;
                }
                at = at.next;
            };
        }

        public TValue GetOrAdd(TKey key, Func<TValue> fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                var res = fallback();
                mine.Build(res);
                return res;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Value;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    var res = fallback();
                    mine.Build(res);
                    return res;
                }
                at = at.next;
            };
        }

        public void DoOrAdd(TKey key, Action<BuildableConcurrent<TValue>.ValueHolder> action, TValue fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    at.Do(x => action(x));
                    return;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return;
                }
                at = at.next;
            };
        }

        public void AddOrThrow(TKey key, TValue value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager, value);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    throw new Exception("Key already has a value");
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return;
                }
                at = at.next;
            };
        }

        public void AddOrThrow(TKey key, Func<TValue> value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                mine.Build(value());
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    throw new Exception("Key already has a value");
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    mine.Build(value());
                    return;
                }
                at = at.next;
            };
        }

        public bool TryAdd(TKey key, TValue value)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return true;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return true;
                }
                at = at.next;
            };
        }

        public TValue UpdateOrAdd(TKey key, Func<TValue, TValue> function, TValue fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return fallback;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Do(x =>
                    {
                        var res = function(x.Value);
                        x.Value = res;
                        return res;
                    });
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return fallback;
                }
                at = at.next;
            };
        }

        public TValue UpdateOrAdd(TKey key, Func<TValue, TValue> function, Func<TValue> fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                var res = fallback();
                mine.Build(res);
                return res;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Do(x =>
                    {
                        var res = function(x.Value);
                        x.Value = res;
                        return res;
                    });
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    var res = fallback();
                    mine.Build(res);
                    return res;
                }
                at = at.next;
            };
        }

        public TValue UpdateOrThrow(TKey key, Func<TValue, TValue> function)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Do(x =>
                    {
                        var res = function(x.Value);
                        x.Value = res;
                        return res;
                    });
                }
                if (Volatile.Read(ref at.next) == null)
                {
                    throw new Exception("Key not found");
                }
                at = at.next;
            };
        }

        public TOut DoAddIfNeeded<TOut>(TKey key, Func<BuildableConcurrent<TValue>.ValueHolder, TOut> function, TValue fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return mine.Do(x => function(x));
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Do(x => function(x));
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    return at.Do(x => function(x));
                }
                at = at.next;
            };
        }

        public TOut DoAddIfNeeded<TOut>(TKey key, Func<BuildableConcurrent<TValue>.ValueHolder, TOut> function, Func<TValue> fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode<TKey, TValue>>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode<TKey, TValue>>(4), null);
            var mine = new BuildableListNode<TKey, TValue>(key, taskManager);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                mine.Build(fallback());
                return mine.Do(x => function(x));
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.Do(x => function(x));
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    mine.Build(fallback());
                    return at.Do(x => function(x));
                }
                at = at.next;
            }
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var l1 in tree.backing)
            {
                if (l1 != null)
                {
                    foreach (var l2 in l1.backing)
                    {
                        if (l2 != null)
                        {
                            foreach (var l3 in l2.backing)
                            {
                                if (l3 != null)
                                {
                                    var at = l3;
                                    while (at != null)
                                    {
                                        yield return new KeyValuePair<TKey, TValue>(at.key,  at.Value);
                                        at = at.next;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class SortaDictionaryExtensions
    {
        public static void Set<TKey, TValue>(this ConcurrentIndexed<TKey, TValue> inner, TKey key, TValue value)
        {
            inner.DoOrThrow(key, (data) =>
            {
                data.Value = value;
            });
        }
    }

}
