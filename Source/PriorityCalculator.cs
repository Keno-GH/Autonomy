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
                var method = typeof(PriorityCalculator).GetMethod(def.giver, BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(PriorityGiver) && parameters[1].ParameterType == typeof(PriorityCalculationContext))
                    {
                        var handler = (Func<PriorityGiver, PriorityCalculationContext, int>)Delegate.CreateDelegate(typeof(Func<PriorityGiver, PriorityCalculationContext, int>), method);
                        conditionHandlers[def.defName] = handler;
                    }
                    else
                    {
                        Log.Error($"PriorityCalculator: Method {def.giver} parameter count or types do not match for PriorityGiverDef {def.defName}");
                    }
                }
                else
                {
                    Log.Error($"PriorityCalculator: Method {def.giver} not found for PriorityGiverDef {def.defName}");
                }
            }
        }

        public static (int priority, List<string> descriptions) GetPriority(WorkTypeDef workTypeDef, Map map, Pawn pawn, Dictionary<string, int> workDrivePreferences, Dictionary<string, float> pawnInfo)
        {
            var extension = workTypeDef.GetModExtension<PriorityGiverExtension>();
            if (extension == null)
            {
                return (0, new List<string>());
            }

            var mapInfo = InfoProvider.GetMapInfo(map, extension.priorityGivers);
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
                if (conditionHandlers.TryGetValue(giver.condition, out var handler))
                {
                    int giverPriority = handler(giver, context);
                    if (giverPriority != 0)
                    {
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
                }
                else
                {
                    Log.Error($"PriorityCalculator: No handler found for condition {giver.condition}");
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

        private static int HandleMapFilthy(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var mapInfo = context.MapInfo;

            if (!IsMapFilthy(mapInfo)) return 0;

            if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference)) return 0;

            float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
            float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
            if (workDrivePreference < minScore || workDrivePreference > maxScore) return 0;

            float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
            float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
            int minPriority = int.Parse(giver.priority.Split('~')[0]);
            int maxPriority = int.Parse(giver.priority.Split('~')[1]);
            int filthAmount = (int)MapFilthAmount(mapInfo);
            int basePriority =
                filthAmount > 200 ? maxPriority
                : filthAmount < 10 ? minPriority
                : minPriority + ((filthAmount - 10) * (maxPriority - minPriority) / 190);
            float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
            float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
            basePriority = (int)(basePriority * multiplier);
            if (basePriority < minPriority) basePriority = minPriority;
            else if (basePriority > maxPriority) basePriority = maxPriority;

            return basePriority;
        }

        private static int HandleThingsDeteriorating(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var mapInfo = context.MapInfo;

            if (!mapInfo.TryGetValue("thingsDeteriorating", out float thingsDeteriorating) || thingsDeteriorating <= 0) return 0;

            if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference)) return 0;

            float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
            float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
            if (workDrivePreference < minScore || workDrivePreference > maxScore) return 0;

            float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
            float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
            int minPriority = int.Parse(giver.priority.Split('~')[0]);
            int maxPriority = int.Parse(giver.priority.Split('~')[1]);

            int basePriority = 
                (int)thingsDeteriorating > 25 ? maxPriority
                : (int)thingsDeteriorating < 1 ? minPriority
                : minPriority + (((int)thingsDeteriorating - 1) * (maxPriority - minPriority) / 24);

            float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
            float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
            basePriority = (int)(basePriority * multiplier);

            if (basePriority < minPriority) basePriority = minPriority;
            else if (basePriority > maxPriority) basePriority = maxPriority;

            return basePriority;
        }

        private static int HandleThingsNeedRefueling(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var mapInfo = context.MapInfo;

            if (!mapInfo.TryGetValue("refuelableThingsNeedingRefuel", out float refuelableThingsNeedingRefuel) || refuelableThingsNeedingRefuel <= 0) return 0;

            if (!workDrivePreferences.TryGetValue(giver.type, out int workDrivePreference)) return 0;

            float minScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[0]);
            float maxScore = float.Parse(giver.workPreferenceScoreRange.Split('~')[1]);
            if (workDrivePreference < minScore || workDrivePreference > maxScore) return 0;

            float minMultiplier = float.Parse(giver.typeMultiplier.Split('~')[0]);
            float maxMultiplier = float.Parse(giver.typeMultiplier.Split('~')[1]);
            int minPriority = int.Parse(giver.priority.Split('~')[0]);
            int maxPriority = int.Parse(giver.priority.Split('~')[1]);

            int basePriority = 
            refuelableThingsNeedingRefuel > 10 ? maxPriority
            : refuelableThingsNeedingRefuel < 0 ? minPriority
            : minPriority + ((int)(refuelableThingsNeedingRefuel - 0) * (maxPriority - minPriority) / 10);

            float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
            float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
            basePriority = (int)(basePriority * multiplier);

            if (basePriority < minPriority) basePriority = minPriority;
            else if (basePriority > maxPriority) basePriority = maxPriority;

            return basePriority;
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

        private static int HandleTending(PriorityGiver giver, PriorityCalculationContext context)
        {
            var pawnInfo = context.PawnInfo;

            if (!pawnInfo.TryGetValue("injuriesCount", out float needsTending) || needsTending <= 0)
            {
                return 0;
            }

            string[] priorityParts = giver.priority.Split('~');
            int minPriority = int.Parse(priorityParts[0]);
            int maxPriority = int.Parse(priorityParts[1]);

            int calculatedPriority = 
                needsTending > 10 ? maxPriority
                : needsTending < 0 ? minPriority
                : minPriority + ((int)(needsTending - 0) * (maxPriority - minPriority) / 10);

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

            if ((giver.condition == "MinorPassion" && isMinorPassion) || (giver.condition == "MajorPassion" && isMajorPassion))
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

                if (giver.condition == "MajorPassion" && conditionPriorities.ContainsKey("MinorPassion"))
                {
                    int minorPassionPriority = conditionPriorities["MinorPassion"];
                    conditionPriorities.Remove("MinorPassion");
                    return basePriority - minorPassionPriority;
                }

                conditionPriorities[giver.condition] = basePriority;
                return basePriority;
            }

            return 0;
        }

        private static int HandleComparedStats(PriorityGiver giver, PriorityCalculationContext context)
        {
            var workDrivePreferences = context.WorkDrivePreferences;
            var mapInfo = context.MapInfo;
            var pawn = context.Pawn;

            float comparisonStatValue;
            float pawnStatValue = pawn.GetStatValue(StatDef.Named(giver.stat));
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

            return calculatedPriority;
        }

        private static bool IsMapFilthy(Dictionary<string, float> mapInfo)
        {
            return mapInfo.ContainsKey("filthInHome") && mapInfo["filthInHome"] > 10;
        }

        private static float MapFilthAmount(Dictionary<string, float> mapInfo)
        {
            return mapInfo.ContainsKey("filthInHome") ? mapInfo["filthInHome"] : 0;
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
