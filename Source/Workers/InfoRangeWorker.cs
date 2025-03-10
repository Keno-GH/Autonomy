using System;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class InfoRangeWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
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

        private int CalculateFromInfoRange(string infoKey, bool fromMap, float leftLimit, float rightLimit, PriorityGiver giver, PriorityCalculationContext context)
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
}
