using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class ComparedStatsWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var mapInfo = context.MapInfo;
            var pawn = context.Pawn;

            float comparisonStatValue;
            string statKey = giver.useAverage ? $"average_{giver.stat}" : $"bestAt_{giver.stat}";

            if (!string.IsNullOrEmpty(giver.onlyForEnabled))
            {
                WorkTypeDef workTypeDef = DefDatabase<WorkTypeDef>.GetNamed(giver.onlyForEnabled, errorOnFail: false);
                if (workTypeDef != null && !pawn.workSettings.WorkIsActive(workTypeDef))
                {
                    return 0; // Skip if the pawn does not have the required work type enabled
                }
                statKey = $"average_{giver.stat}_{workTypeDef.defName}";
            }

            if (!mapInfo.TryGetValue(statKey, out comparisonStatValue)) return 0;

            float pawnStatValue = pawn.GetStatValue(StatDef.Named(giver.stat), true);
            if (!giver.useAverage && pawnStatValue < comparisonStatValue) return 0;

            if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference)) return 0;

            float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
            float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
            if (workDrivePreference < minScore || workDrivePreference > maxScore) return 0;

            float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
            float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);

            float statDifference = pawnStatValue - comparisonStatValue;

            int calculatedPriority = 0;
            if (!string.IsNullOrEmpty(giver.priority))
            {
                int minPriority = int.Parse(giver.priority.Split('~')[0]);
                int maxPriority = int.Parse(giver.priority.Split('~')[1]);
                float statRatio = statDifference / comparisonStatValue;
                calculatedPriority = (int)(minPriority + (statRatio * (maxPriority - minPriority)));

                float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
                float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
                calculatedPriority = (int)(calculatedPriority * multiplier);

                if (calculatedPriority < minPriority) calculatedPriority = minPriority;
                else if (calculatedPriority > maxPriority) calculatedPriority = maxPriority;
            }
            else
            {
                foreach (var rangeData in giver.rangeDatas)
                {
                    float minStatDifference = float.Parse(rangeData.validRange.Split('~')[0]);
                    float maxStatDifference = float.Parse(rangeData.validRange.Split('~')[1]);
                    if (statDifference < minStatDifference || statDifference > maxStatDifference) continue;

                    int minPriority = int.Parse(rangeData.priority.Split('~')[0]);
                    int maxPriority = int.Parse(rangeData.priority.Split('~')[1]);
                    float statRatio = statDifference / comparisonStatValue;
                    calculatedPriority = (int)(minPriority + (statRatio * (maxPriority - minPriority)));

                    float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
                    float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
                    calculatedPriority = (int)(calculatedPriority * multiplier);

                    if (calculatedPriority < minPriority) calculatedPriority = minPriority;
                    else if (calculatedPriority > maxPriority) calculatedPriority = maxPriority;

                    break;
                }
            }

            // Log.Message($"HandleComparedStats: Calculated priority for pawn {pawn.Name} with stat {giver.stat} is {calculatedPriority}, after comparing with his {pawnStatValue} to {comparisonStatValue} using {giver.useAverage} average, with a difference of {statDifference}");

            return calculatedPriority;
        }
    }
}
