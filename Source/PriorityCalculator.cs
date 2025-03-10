using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using System.Runtime.InteropServices;

namespace Autonomy
{
    public static class PriorityCalculator
    {
        private static Dictionary<string, Func<PriorityGiver, PriorityCalculationContext, int>> conditionHandlers;

        static PriorityCalculator()
        {
            LoadPriorityGivers();
        }

        private static void LoadPriorityGivers()
        {
            conditionHandlers = new Dictionary<string, Func<PriorityGiver, PriorityCalculationContext, int>>();
            foreach (var def in DefDatabase<PriorityGiverDef>.AllDefs)
            {
                MethodInfo method = typeof(PriorityCalculator).GetMethod(def.giver, BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                {
                    Log.Error($"PriorityCalculator: Method {def.giver} not found for PriorityGiverDef {def.defName}");
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(PriorityGiver) || parameters[1].ParameterType != typeof(PriorityCalculationContext))
                {
                    Log.Error($"PriorityCalculator: Method {def.giver} parameter count or types do not match for PriorityGiverDef {def.defName}");
                    continue;
                }
                
                var handler = (Func<PriorityGiver, PriorityCalculationContext, int>)Delegate.CreateDelegate(typeof(Func<PriorityGiver, PriorityCalculationContext, int>), method);
                conditionHandlers[def.defName] = handler;
                
                
            }
        }

        public static (int priority, List<string> descriptions) GetPriority(WorkTypeDef workTypeDef, Map map, Pawn pawn, Dictionary<string, int> workDrivePreferences, Dictionary<string, float> pawnInfo, Dictionary<string, float> mapInfo)
        {
            var extension = workTypeDef.GetModExtension<PriorityGiverExtension>();
            if (extension == null)
            {
                return (0, new List<string>());
            }

            if (mapInfo.ContainsKey("noMap"))
            {
                return (0, new List<string>());
            }
            
            int priority = 0;
            List<string> descriptions = new List<string>();

            // Build the unified context once.
            PriorityCalculationContext context = new PriorityCalculationContext
            {
                WorkDrivePreferences = workDrivePreferences,
                MapInfo = mapInfo,
                PawnInfo = pawnInfo,
                Pawn = pawn,
                ConditionPriorities = new Dictionary<string, int>()
            };

            foreach (var giver in extension.priorityGivers)
            {
                // Get the PriorityGiverDef for the current giver
                var def = DefDatabase<PriorityGiverDef>.GetNamed(giver.condition, false);
                if (def == null)
                {
                    Log.Error($"PriorityCalculator: No PriorityGiverDef found for condition {giver.condition}");
                    continue;
                }

                // Check if the pawn is a slave and if the def is not allowed for slaves
                if (pawn.IsSlave && !def.allowedForSlaves)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(giver.mayRequire))
                {
                    if (!ModsConfig.IsActive(giver.mayRequire) && !ModsConfig.ActiveModsInLoadOrder.Any(mod => mod.Name == giver.mayRequire))
                    {
                        continue;
                    }
                }

                if (!conditionHandlers.TryGetValue(giver.condition, out var handler))
                {
                    Log.Error($"PriorityCalculator: No handler found for condition {giver.condition}");
                    continue;
                }

                int giverPriority = handler(giver, context);
                
                if (giverPriority == 0)
                {
                    continue;
                }
                priority += giverPriority;

                if (giver.rangeDatas == null)
                {
                    descriptions.Add($"{giver.description} : {giverPriority}");
                }
                else
                {
                    foreach (var rangeData in giver.rangeDatas)
                    {
                        int minPriority = int.Parse(rangeData.priority.Split('~')[0]);
                        int maxPriority = int.Parse(rangeData.priority.Split('~')[1]);
                        if (giverPriority >= minPriority && giverPriority <= maxPriority)
                        {
                            descriptions.Add($"{rangeData.description} : {giverPriority}");
                            break;
                        }
                    }
                }
                
            }

            return (priority, descriptions);
        }

        private static int HandleBaseType(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;

            if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference)) return 0;

            float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
            float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
            if (workDrivePreference < minScore || workDrivePreference > maxScore) return 0;

            float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
            float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
            int basePriority = int.Parse(giver.priority);
            float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
            float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);

            return (int)(basePriority * multiplier);
        }

        private static int HandleSurroundingsFilthy(PriorityGiver giver, PriorityCalculationContext context)
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

        private static int HandleImmunity(PriorityGiver giver, PriorityCalculationContext context)
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

        private static int HandleSkillAffinity(PriorityGiver giver, PriorityCalculationContext context)
        {
            var pawnInfo = context.PawnInfo;

            if (!pawnInfo.TryGetValue(giver.skill, out float skillLevel)) return 0;

            foreach (var rangeData in giver.rangeDatas)
            {
                float minSkillLevel = float.Parse(rangeData.validRange.Split('~')[0]);
                float maxSkillLevel = float.Parse(rangeData.validRange.Split('~')[1]);
                if (skillLevel < minSkillLevel || skillLevel > maxSkillLevel) continue;

                int minPriority = int.Parse(rangeData.priority.Split('~')[0]);
                int maxPriority = int.Parse(rangeData.priority.Split('~')[1]);
                float skillRatio = (skillLevel - minSkillLevel) / (maxSkillLevel - minSkillLevel);
                int skillPriority = (int)(minPriority + (skillRatio * (maxPriority - minPriority)));
                int calculatedPriority = skillPriority < minPriority ? minPriority : skillPriority > maxPriority ? maxPriority : skillPriority;

                return calculatedPriority;
            }

            return 0;
        }

        private static int HandlePassion(PriorityGiver giver, PriorityCalculationContext context)
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

        private static int HandleInfoRange(PriorityGiver giver, PriorityCalculationContext context)
        {

            if (giver.infoRange == null)
            {
                Log.Error($"HandleInfoRange: giver.infoRange is null. A giver with infoRange condition must have an infoRange with infoKey, fromMap and range defined.");
                return 0;
            }

            if (string.IsNullOrEmpty(giver.infoRange.infoKey))
            {
                Log.Error($"HandleInfoRange: giver.infoRange.infoKey is null or empty. A giver with infoRange condition must have an infoKey defined.");
                return 0;
            }

            if (string.IsNullOrEmpty(giver.infoRange.range))
            {
                Log.Error($"HandleInfoRange: giver.infoRange.range is null or empty. A giver with infoRange condition must have a range defined.");
                return 0;
            }

            if (!giver.infoRange.fromMap && context.PawnInfo.ContainsKey("noPawn"))
            {
                return 0;
            }

            string[] rangeParts = giver.infoRange.range.Split('~');

            if (rangeParts.Length != 2)
            {
                Log.Error($"HandleInfoRange: giver.infoRange.range is invalid. A giver with infoRange condition must have a range defined as 'min~max'.");
                return 0;
            }

            float leftLimit = float.Parse(rangeParts[0]);
            float rightLimit = float.Parse(rangeParts[1]);
            return CalculateFromInfoRange(giver.infoRange.infoKey, giver.infoRange.fromMap, leftLimit, rightLimit, giver, context);
        }

        private static int HandleComparedStats(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var mapInfo = context.MapInfo;
            var pawn = context.Pawn;

            float comparisonStatValue;
            float pawnStatValue = pawn.GetStatValue(StatDef.Named(giver.stat), true);
            if (giver.useAverage)
            {
                if (!mapInfo.TryGetValue($"average_{giver.stat}", out comparisonStatValue)) return 0;
            }
            else
            {
                if (!mapInfo.TryGetValue($"bestAt_{giver.stat}", out comparisonStatValue)) return 0;
                if (pawnStatValue < comparisonStatValue) return 0;
            }


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
        private static int CalculateFromInfoRange(string infoKey, bool fromMap, float leftLimit, float rightLimit, PriorityGiver giver, PriorityCalculationContext context)
        {
            if (giver == null)
            {
                Log.Error("CalculateFromInfoRange: giver is null");
                return 0;
            }

            if (giver.exclusiveTo != null)
            {
                if (context.PawnInfo.TryGetValue(giver.exclusiveTo, out float exclusiveValue))
                {
                    if (exclusiveValue == 0)
                    {
                        return 0;
                    }
                }
            }

            var info = fromMap ? context.MapInfo : context.PawnInfo;
            if (info == null)
            {
                Log.Error($"CalculateFromInfoRange: info dictionary is null for infoKey {infoKey}");
                return 0;
            }

            if (!info.TryGetValue(infoKey, out float value))
            {
                Log.Warning($"CalculateFromInfoRange: infoKey {infoKey} not found");
                return 0;
            }

            float minScore = 0, maxScore = 0, minMultiplier = 0, maxMultiplier = 0;

            bool hasWorkDrivePreference;
            int workDrivePreference = 0;
            if (string.IsNullOrEmpty(giver.type))
            {
                hasWorkDrivePreference = false;
            }
            else
            {
                hasWorkDrivePreference = context.WorkDrivePreferences.TryGetValue(giver.type, out workDrivePreference);
                if (!hasWorkDrivePreference)
                {
                    Log.Error($"CalculateFromInfoRange: Work drive preference not found for type {giver.type}");
                    return 0;
                }
            }

            if (hasWorkDrivePreference)
            {
                try
                {
                    minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
                    maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
                    if (workDrivePreference < minScore || workDrivePreference > maxScore) hasWorkDrivePreference = false;

                    minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
                    maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
                }
                catch (Exception ex)
                {
                    Log.Error($"CalculateFromInfoRange: Error parsing work preference score range or type multiplier for giver {giver.type}: {ex.Message}");
                    return 0;
                }
            }

            int minPriority, maxPriority;
            try
            {
                minPriority = int.Parse(giver.priority.Split('~')[0]);
                maxPriority = int.Parse(giver.priority.Split('~')[1]);
            }
            catch (Exception ex)
            {
                Log.Error($"CalculateFromInfoRange: Error parsing priority range for giver {giver.type}: {ex.Message}");
                return 0;
            }

            bool isDecreasingValue = leftLimit > rightLimit;
            int basePriority;

            if (isDecreasingValue)
            {
                basePriority =
                value < rightLimit ? maxPriority
                : value > leftLimit ? minPriority
                : maxPriority - (int)((value - rightLimit) * (maxPriority - minPriority) / (leftLimit - rightLimit));
            }
            else
            {   
                basePriority = 
                value > rightLimit ? maxPriority
                : value < leftLimit ? minPriority
                : minPriority + (int)((value - leftLimit) * (maxPriority - minPriority) / (rightLimit - leftLimit));
            }

            if (hasWorkDrivePreference)
            {
                float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
                float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
                basePriority = (int)(basePriority * multiplier);
            }

            if (basePriority < minPriority) basePriority = minPriority;
            else if (basePriority > maxPriority) basePriority = maxPriority;

            return basePriority;
        }
    }

    public class PriorityCalculationContext
    {
        public Dictionary<string, int> WorkDrivePreferences { get; set; }
        public Dictionary<string, float> MapInfo { get; set; }
        public Dictionary<string, float> PawnInfo { get; set; }
        public Pawn Pawn { get; set; }
        public Dictionary<string, int> ConditionPriorities { get; set; }
    }
}
