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

            // workPreferenceScoreRange and typeMultiplier are used to adjust the priority based on pawn's work drive preferences.
            // minScore/maxScore define the range of a pawn's work drive preference (e.g., -10 to 10 for SocialPreference).
            // minMultiplier/maxMultiplier define how the basePriority should be scaled based on that preference.
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
                // These are the raw values from the XML's 'priority' attribute.
                // For interpolation, the first value is treated as the priority at the 'leftLimit' of the 'range',
                // and the second value is the priority at the 'rightLimit' of the 'range'.
                // Example: priority="0~-25" means minPriority=0, maxPriority=-25.
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

            // Interpolation logic:
            // Calculates a basePriority by mapping the 'value' (from infoKey) within the 'range' (leftLimit~rightLimit)
            // to a point within the 'priority' (minPriority~maxPriority from XML).

            if (isDecreasingValue)
            {
                // Handles ranges like "40~0".
                // If value is beyond the limits, it's clamped to the respective priority.
                // Otherwise, it interpolates.
                // Example: range="40~0", priority="-25~0", value=8
                // leftLimit=40, rightLimit=0
                // XML_minPriority=-25, XML_maxPriority=0
                // basePriority = XML_maxPriority - (int)((value - rightLimit) * (XML_maxPriority - XML_minPriority) / (leftLimit - rightLimit))
                // basePriority = 0 - (int)((8 - 0) * (0 - (-25)) / (40 - 0))
                // basePriority = 0 - (int)(8 * 25 / 40) = 0 - (int)(5) = -5
                basePriority =
                value < rightLimit ? maxPriority // Value is less than the 'end' of a decreasing range (e.g., value < 0 for 40~0)
                : value > leftLimit ? minPriority // Value is greater than the 'start' of a decreasing range (e.g., value > 40 for 40~0)
                : maxPriority - (int)((value - rightLimit) * (maxPriority - minPriority) / (leftLimit - rightLimit));
            }
            else
            {   
                // Handles ranges like "0~40".
                // If value is beyond the limits, it's clamped to the respective priority.
                // Otherwise, it interpolates.
                // Example: range="0~40", priority="0~-25", value=8
                // leftLimit=0, rightLimit=40
                // XML_minPriority=0, XML_maxPriority=-25
                // basePriority = XML_minPriority + (int)((value - leftLimit) * (XML_maxPriority - XML_minPriority) / (rightLimit - leftLimit))
                // basePriority = 0 + (int)((8 - 0) * (-25 - 0) / (40 - 0))
                // basePriority = 0 + (int)(8 * -25 / 40) = 0 + (int)(-5) = -5
                basePriority = 
                value > rightLimit ? maxPriority // Value is greater than the 'end' of an increasing range (e.g., value > 40 for 0~40)
                : value < leftLimit ? minPriority // Value is less than the 'start' of an increasing range (e.g., value < 0 for 0~40)
                : minPriority + (int)((value - leftLimit) * (maxPriority - minPriority) / (rightLimit - leftLimit));
            }

            if (hasWorkDrivePreference)
            {
                float ratio = (workDrivePreference - minScore) / (maxScore - minScore);
                float multiplier = minMultiplier + ratio * (maxMultiplier - minMultiplier);
                basePriority = (int)(basePriority * multiplier);
            }

            // Final Clamping:
            // The calculated basePriority (after interpolation and potential work drive multiplication)
            // is clamped to be within the numerical min and max of the original priority string.
            // IMPORTANT: For this clamping to work as intended (e.g., ensuring a value of 0 for TempDiffHot=0
            // when priority is "-25~0"), the priority string in XML should be numerically ordered
            // from smallest to largest (e.g., "-25~0", not "0~-25").
            // If priority was "0~-25" (parsedMin=0, parsedMax=-25):
            //   A calculated basePriority of 0 would be clamped: 0 > -25 (parsedMax) is true, so basePriority becomes -25. (Incorrect for TempDiffHot=0)
            // If priority is "-25~0" (parsedMin=-25, parsedMax=0):
            //   A calculated basePriority of 0 would NOT be clamped: 0 < -25 is false, 0 > 0 is false. So basePriority remains 0. (Correct for TempDiffHot=0)
            
            // Store the parsed min/max from the XML string for clarity in clamping.
            int parsedXmlMinPriority = int.Parse(giver.priority.Split('~')[0]);
            int parsedXmlMaxPriority = int.Parse(giver.priority.Split('~')[1]);

            if (parsedXmlMinPriority < parsedXmlMaxPriority) // e.g. "-25~0"
            {
                if (basePriority < parsedXmlMinPriority) basePriority = parsedXmlMinPriority;
                else if (basePriority > parsedXmlMaxPriority) basePriority = parsedXmlMaxPriority;
            }
            else // e.g. "0~-25"
            {
                if (basePriority < parsedXmlMaxPriority) basePriority = parsedXmlMaxPriority; // Check against the numerically smaller value
                else if (basePriority > parsedXmlMinPriority) basePriority = parsedXmlMinPriority; // Check against the numerically larger value
            }
            return basePriority;
        }
    }
}
