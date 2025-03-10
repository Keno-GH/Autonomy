using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class ImmunityWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            var pawnInfo = context.PawnInfo;

            if (!pawnInfo.TryGetValue("immunityGainSpeed", out float immunityGainSpeed) || 
            !pawnInfo.TryGetValue("severityGainSpeed", out float severityGainSpeed) || 
            !pawnInfo.TryGetValue("severityTendedSpeed", out float severityTendedSpeed))
            {
                return 0;
            }

            if (severityGainSpeed == 0)
            {
                return 0;
            }

            float calculatedSeverityGainSpeed = severityGainSpeed + severityTendedSpeed; // SeverityTendedSpeed is negative

            float adjustedImmunityGainSpeed = context.Pawn.InBed() ? immunityGainSpeed * 0.8f : immunityGainSpeed; // Assume we get immunity 20% slower when out of bed

            string[] priorityParts = giver.priority.Split('~');
            int minPriority = int.Parse(priorityParts[0]);
            int maxPriority = int.Parse(priorityParts[1]);

            if (adjustedImmunityGainSpeed < calculatedSeverityGainSpeed)
            {
                return maxPriority;
            }

            float difference = adjustedImmunityGainSpeed - calculatedSeverityGainSpeed;
            float ratio = difference / calculatedSeverityGainSpeed;
            int calculatedPriority = (int)(minPriority + (1 - ratio) * (maxPriority - minPriority));

            return calculatedPriority;
        }
    }
}
