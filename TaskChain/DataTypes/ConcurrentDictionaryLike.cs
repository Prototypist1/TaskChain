using Prototypist.TaskChain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prototypist.TaskChain
{

    public abstract class BaseInnerHave<TValue>
    {
        private object value;

        public TValue Value
        {
            get => (TValue)Volatile.Read(ref value);
            set => Volatile.Write(ref this.value, value);
        }

        public BaseInnerHave(TValue value)
        {
            this.value = value;
        }
    }

    public class InnerHave<TValue> : BaseInnerHave<TValue>
    {
        public readonly IActionChainer actionChainer;

        public InnerHave(IActionChainer actionChainer, TValue value) : base(value)
        {
            this.actionChainer = actionChainer ?? throw new ArgumentNullException(nameof(actionChainer));
        }
    }
    
    // this would be way simpler if I did not creat so many crazy methods...
    public class ConcurrentDictionaryLike<TKey, TValue> : TreeBacked<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        TreeNode<TreeNode<TreeNode<BuildableListNode>>> tree = new TreeNode<TreeNode<TreeNode<BuildableListNode>>>(64);
        private readonly ITaskManager taskManager;

        public ConcurrentDictionaryLike()
        {
            this.taskManager = Chaining.taskManager;
        }

        public ConcurrentDictionaryLike(IEnumerable<Tuple<TKey, TValue>> x):this()
        {
            foreach (var pair in x)
            {
                AddOrThrow(pair.Item1, pair.Item2);
            }
        }

        #region Help

        private BuildableListNode GetNodeOrThrow(TKey key)
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
                    return at.GetInner().Value;
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
            catch {
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
                    at.GetInner().Value = value;
                    return;
                }
                at = at.next;
            };
        }

        public void DoOrThrow(TKey key, Action<BaseInnerHave<TValue>> action)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    var inner = at.GetInner();
                    inner.actionChainer.Run(() => action(inner));
                    return;
                }
                at = at.next;
            };
        }

        public TOut DoOrThrow<TOut>(TKey key, Func<BaseInnerHave<TValue>, TOut> function)
        {
            var at = GetNodeOrThrow(key);
            while (true)
            {
                if (at.key.Equals(key))
                {
                    var inner = at.GetInner();
                    return inner.actionChainer.Run(() => function(inner));
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager, value);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    at.GetInner().Value = value;
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return fallback;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    return at.GetInner().Value;
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager);
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
                    return at.GetInner().Value;
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

        public void DoOrAdd(TKey key, Action<BaseInnerHave<TValue>> action, TValue fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    var innner = at.GetInner();
                    innner.actionChainer.Run(() => action(innner));
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager, value);
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager);
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager);
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                return fallback;
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    var inner = at.GetInner();
                    return inner.actionChainer.Run(() =>
                    {
                        var res = function(inner.Value);
                        inner.Value = res;
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
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager);
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
                    var inner = at.GetInner();
                    return inner.actionChainer.Run(() =>
                    {
                        var res = function(inner.Value);
                        inner.Value = res;
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
                    var inner = at.GetInner();
                    return inner.actionChainer.Run(() =>
                    {
                        var res = function(inner.Value);
                        inner.Value = res;
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

        public TOut DoAddIfNeeded<TOut>(TKey key, Func<BaseInnerHave<TValue>, TOut> function, TValue fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager, fallback);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                var innner = mine.GetInner();
                return innner.actionChainer.Run(() => function(innner));
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    var innner = at.GetInner();
                    return innner.actionChainer.Run(() => function(innner));
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    var innner = mine.GetInner();
                    return innner.actionChainer.Run(() => function(innner));
                }
                at = at.next;
            };
        }

        public TOut DoAddIfNeeded<TOut>(TKey key, Func<BaseInnerHave<TValue>, TOut> function, Func<TValue> fallback)
        {
            var hash = Math.Abs(Math.Max(-int.MaxValue, key.GetHashCode()));
            var a = hash % 64;
            var b = (hash % 1024) / 64;
            var c = (hash % 4096) / 1024;
            Interlocked.CompareExchange(ref tree.backing[a], new TreeNode<TreeNode<BuildableListNode>>(16), null);
            Interlocked.CompareExchange(ref tree.backing[a].backing[b], new TreeNode<BuildableListNode>(4), null);
            var mine = new BuildableListNode(key, taskManager);
            if (Interlocked.CompareExchange(ref tree.backing[a].backing[b].backing[c], mine, null) == null)
            {
                mine.Build(fallback());
                var innner = mine.GetInner();
                return innner.actionChainer.Run(() => function(innner));
            }
            var at = tree.backing[a].backing[b].backing[c];
            while (true)
            {
                if (at.key.Equals(key))
                {
                    var innner = at.GetInner();
                    return innner.actionChainer.Run(() => function(innner));
                }
                if (Interlocked.CompareExchange(ref at.next, mine, null) == null)
                {
                    mine.Build(fallback());
                    var innner = mine.GetInner();
                    return innner.actionChainer.Run(() => function(innner));
                }
                at = at.next;
            }
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
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
                                        var inner = at.GetInner();
                                        yield return new KeyValuePair<TKey, TValue>(at.key, inner.Value);
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
        public static void Set<TKey, TValue>(this ConcurrentDictionaryLike<TKey, TValue> inner, TKey key, TValue value)
        {
            inner.DoOrThrow(key, (data) =>
            {
                data.Value = value;
            });
        }
    }

}
