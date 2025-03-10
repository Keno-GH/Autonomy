using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class PassionWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var pawnInfo = context.PawnInfo;
            var conditionPriorities = context.ConditionPriorities;

            if (!pawnInfo.TryGetValue(giver.skill, out float skillLevel) || !pawnInfo.TryGetValue($"{giver.skill}_passion", out float skillPassion)) return 0;

            bool isMajorPassion = skillPassion == (float)Passion.Major;
            bool isMinorPassion = skillPassion == (float)Passion.Minor;
            // Vanilla Skills Expanded
            bool isApathyPassion = pawnInfo.TryGetValue($"has_{giver.skill}_apathyPassion", out float hasApathyPassion) && hasApathyPassion == 1;
            bool isNaturalPassion = pawnInfo.TryGetValue($"has_{giver.skill}_naturalPassion", out float hasNaturalPassion) && hasNaturalPassion == 1;
            bool isCriticalPassion = pawnInfo.TryGetValue($"has_{giver.skill}_criticalPassion", out float hasCriticalPassion) && hasCriticalPassion == 1;
            

            if (
            (giver.condition == "MinorPassion" && isMinorPassion) 
            || (giver.condition == "MajorPassion" && isMajorPassion)
            || (giver.condition == "ApathyPassion" && isApathyPassion)
            || (giver.condition == "NaturalPassion" && isNaturalPassion)
            || (giver.condition == "CriticalPassion" && isCriticalPassion)
            )
            {
                if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference)) return 0;

                float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
                float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
                if (workDrivePreference < minScore || workDrivePreference > maxScore) return 0;

                float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
                float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
                int basePriority = int.Parse(giver.priority);
                float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
                float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
                basePriority = (int)(basePriority * multiplier);

                if (basePriority == 0) return 0;
                
                // This attemmpts to avoid over prioritizing the same worktype when it has multiple passions, but currently
                // is not working and I am too tired to fix it.
                var conditionChecks = new Dictionary<string, string[]>
                {
                    { "MinorPassion", new[] { "ApathyPassion" } },
                    { "MajorPassion", new[] { "MinorPassion", "ApathyPassion" } },
                    { "NaturalPassion", new[] { "MajorPassion", "MinorPassion", "ApathyPassion" } },
                    { "CriticalPassion", new[] { "NaturalPassion", "MajorPassion", "MinorPassion", "ApathyPassion" } },
                };

                if (conditionChecks.TryGetValue(giver.condition, out var prioritiesToCheck))
                {
                    foreach (var condition in prioritiesToCheck)
                    {
                        if (conditionPriorities.TryGetValue(condition, out int conditionPriority))
                        {
                            conditionPriorities.Remove(condition);
                            return basePriority - conditionPriority;
                        }
                    }
                }

                conditionPriorities[giver.condition] = basePriority;
                return basePriority;
            }

            return 0;
        }
    }
}
