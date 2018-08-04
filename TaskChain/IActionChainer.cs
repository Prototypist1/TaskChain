using System;

namespace Prototypist.TaskChain
{
    public interface IActionChainer
    {
        IActionChainer GetActionChainer();
        void Run(Action action);
        T Run<T>(Func<T> func);
    }
}