using System;
using System.Collections.Generic;

namespace Prototypist.TaskChain
{
    public interface ITaskManager
    {
        IActionChainer GetActionChainer();
        void Enque(Action action);
        void For(int start, int stop, Action<int> action);
        void Foreach<T>(IEnumerable<T> input, Action<T> action);
        void Run(Action[] actions);
        Tout[] Run<Tout>(Func<Tout>[] funcs);
        void SpinUntil(Func<bool> condition);
        IEnumerable<Tout> TaskManagerSelect<Tin, Tout>(IEnumerable<Tin> input, Func<Tin, Tout> action);
    }
}