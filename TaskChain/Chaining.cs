using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prototypist.TaskChain
{
    public static class Chaining
    {
        public static TaskManager taskManager = new TaskManager();

        public static void For(int start, int stop, Action<int> action)
        {
            taskManager.For(start, stop, action);
        }

        public static void Run(Action[] action)
        {
            taskManager.Run(action);
        }
        
        public static void Foreach<T>(IEnumerable<T> input, Action<T> action)
        {
            taskManager.Foreach(input, action);
        }

        public static void ChainingForeach<T>(this IEnumerable<T> input, Action<T> action)
        {
            taskManager.Foreach(input, action);
        }

        public static Tout[] ChainingSelect<Tin, Tout>(this IEnumerable<Tin> input, Func<Tin, Tout> action)
        {
            return taskManager.Run(input.Select<Tin, Func<Tout>>(x => () => action(x)).ToArray());
        }
        

    }

}
