using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    public interface IPriorityWorker
    {
        int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context);
    }
}
