using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Prototypist.TaskChain
{

    public partial class TaskManager : ITaskManager
    {

        private const int TRUE = 1;
        private const int FALSE = 0;
        
        private WorkChain workChain = new WorkChain();
        private Processor[] processors = new Processor[Environment.ProcessorCount - 1];

        public void For(int start, int stop, Action<int> action)
        {
            var actions = new Action[stop - start];
            for (int i = 0; i < stop - start; i++)
            {
                var j = i;
                actions[j] = () => action(j + start);
            }
            Run(actions);
        }

        public void Foreach<T>(IEnumerable<T> input, Action<T> action)
        {
            Run(input.Select<T, Action>(x => () => action(x)).ToArray());
        }

        public IEnumerable<Tout> TaskManagerSelect<Tin, Tout>(IEnumerable<Tin> input, Func<Tin, Tout> action)
        {
            return Run(input.Select<Tin, Func<Tout>>(x => () => action(x)).ToArray());
        }

        /// <summary>
        /// action will be run in the background at somepoint
        /// </summary>
        public void Enque(Action action)
        {
            var actionTask = new ActionLink(action, () => { });

            workChain.Enchain(actionTask);

            AwakeProcessor();
        }


        /// <summary>
        /// all actions will be completed before method returns 
        /// </summary>
        public void Run(Action[] actions)
        {
            if (actions.Length == 0)
            {
                return;
            }

            int complected = 0;
            var tasks = actions.Select(x => new ActionLink(x, () => Interlocked.Increment(ref complected))).ToArray();

            bool allDone() => Volatile.Read(ref complected) == actions.Length;
            
            if (tasks.Length >1) {
                Link lastTask = tasks[0];
                for (var i = 1  ; i < tasks.Length; i++)
                {
                    lastTask.next = tasks[i];
                    lastTask = lastTask.next;
                }
            }

            RunInner(allDone, tasks);

            for (var i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].exception != null)
                {
                    throw new AggregateException(tasks[i].exception);
                }
            }
        }

        private void RunInner(Func<bool> allDone, Link[] tasks)
        {

            workChain.Enchain(tasks.First());

            AwakeProcessors();

            foreach (var actionTask in tasks)
            {
                actionTask.MainTryRun();
            }

            SpinWait.SpinUntil(() =>
            {
                if (allDone()) { return true; }
                workChain.ProcessTask();
                return false;
            });
        }

        public Tout[] Run<Tout>(Func<Tout>[] funcs)
        {
            if (funcs.Length == 0)
            {
                return new Tout[0];
            }

            var complected = 0;
            var tasks = funcs.Select(x => new FunctionLink<Tout>(x, () => Interlocked.Increment(ref complected))).ToArray();

            bool allDone() => Volatile.Read(ref complected) == funcs.Length;

            if (tasks.Length > 1)
            {
                Link lastTask = tasks[0];
                for (var i = 1; i < tasks.Length; i++)
                {
                    lastTask.next = tasks[i];
                    lastTask = lastTask.next;
                }
            }

            RunInner(allDone, tasks);

            var results = new Tout[funcs.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].exception != null)
                {
                    throw new AggregateException(tasks[i].exception);
                }
                results[i] = tasks[i].GetResult();
            }
            return results;
        }

        public void SpinUntil(Func<bool> condition)
        {
            while (!condition())
            {
                workChain.ProcessTask();
            };
        }

        private void AwakeProcessors()
        {
            for (var i = 0; i < processors.Length; i++)
            {
                var toAdd = new Processor(this.workChain);
                Interlocked.CompareExchange(ref processors[i], toAdd, null);
                processors[i].TryAwake();
            }
        }

        private void AwakeProcessor()
        {
            for (var i = 0; i < processors.Length; i++)
            {
                var toAdd = new Processor(this.workChain);
                Interlocked.CompareExchange(ref processors[i], toAdd, null);
                if (processors[i].TryAwake())
                {
                    return;
                }
            }
        }

        public IActionChainer GetActionChainer()
        {
            return new ItemLock(this);
        }
    }

}
