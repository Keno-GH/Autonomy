using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class SurroundingsFilthyWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            if (!context.PawnInfo.TryGetValue("cleanlinessSurroundingMe", out float cleanliness) || cleanliness >= 0)
            {
                return 0;
            }

            string[] priorityParts = giver.priority.Split('~');
            int minPriority = int.Parse(priorityParts[0]);
            int maxPriority = int.Parse(priorityParts[1]);

            // Helper function to compute base priority solely from cleanliness.
            float CalculateCleanlinessPriority()
            {
                if (cleanliness < -10)
                {
                    return maxPriority;
                }
                if (cleanliness > 0)
                {
                    return minPriority;
                }

                return minPriority + ((cleanliness + 10) * (maxPriority - minPriority) / 10f);
            }

            // If the giver type is missing or there's no corresponding work drive preference,
            // use only the cleanliness value.
            if (string.IsNullOrEmpty(giver.type) || !context.WorkDrivePreferences.TryGetValue(giver.type, out int workDrivePreference))
            {
                return (int)CalculateCleanlinessPriority();
            }

            string[] scoreRangeParts = giver.workPreferenceScoreRange.Split('~');
            float minScore = float.Parse(scoreRangeParts[0]);
            float maxScore = float.Parse(scoreRangeParts[1]);

            if (workDrivePreference < minScore || workDrivePreference > maxScore)
            {
                return 0;
            }

            string[] multiplierParts = giver.typeMultiplier.Split('~');
            float minMultiplier = float.Parse(multiplierParts[0]);
            float maxMultiplier = float.Parse(multiplierParts[1]);

            float basePriority = CalculateCleanlinessPriority();

            float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
            float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);

            int finalPriority = (int)(basePriority * multiplier);
            finalPriority = Math.Max(minPriority, Math.Min(finalPriority, maxPriority));

            return finalPriority;
        }
    }
}
