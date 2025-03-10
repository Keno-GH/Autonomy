using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
namespace Autonomy
{
    public static class PriorityWorkerRegistry
    {
        private static readonly Dictionary<string, IPriorityWorker> workers = new Dictionary<string, IPriorityWorker>();

        public static void RegisterWorker(string conditionName, IPriorityWorker worker)
        {
            workers[conditionName] = worker;
        }

        public static IPriorityWorker GetWorker(string conditionName)
        {
            workers.TryGetValue(conditionName, out var worker);
            return worker;
        }
    }
}
