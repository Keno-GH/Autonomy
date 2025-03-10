using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class BaseTypeWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;

            if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference))
                return 0;

            float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
            float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
            if (workDrivePreference < minScore || workDrivePreference > maxScore)
                return 0;

            float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
            float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
            int basePriority = int.Parse(giver.priority);
            float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
            float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);

            return (int)(basePriority * multiplier);
        }
    }
}
