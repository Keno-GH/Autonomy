using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Autonomy
{
    public class PriorityGiverDef : Def
    {
        public string workerClass;
        public List<string> arguments;
        public bool allowedForSlaves;
    }
}
