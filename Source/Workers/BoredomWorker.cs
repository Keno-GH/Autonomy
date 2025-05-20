using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine; // Added for Mathf
using System; // Added for Parse methods

namespace Autonomy.Workers
{
    public class BoredomWorker : IPriorityWorker
    {
        // Represents the number of InfoProvider update cycles of wandering for max boredom effect.
        // InfoProvider.GetPawnInfo is called via MyMapComponent.MapComponentTick(), which has an interval of 1000 game ticks.
        // ticksSinceLastWork is incremented by 1000 for each such cycle.
        // So, 40 cycles = 40 * 1000 = 40000 game ticks (approx. 16 in-game hours if 1 hour = 2500 ticks).
        private const int MAX_WANDERING_CYCLES_FOR_BOREDOM_EFFECT = 40;

        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            if (context.Pawn == null || context.PawnInfo == null)
            {
                return 0;
            }

            if (!context.PawnInfo.TryGetValue("ticksSinceLastWork", out float totalWanderingTicksFloat))
            {
                // Log.Warning($"BoredomWorker: ticksSinceLastWork not found for pawn {context.Pawn.Name.ToStringShort}.");
                return 0;
            }

            // Convert total accumulated wandering ticks (e.g., 1000, 2000) into the number of update cycles.
            // Each cycle corresponds to one 1000-tick interval where the pawn was wandering.
            int wanderingCycles = (int)(totalWanderingTicksFloat / 1000f);

            string[] priorityParts = giver.priority.Split('~');
            if (priorityParts.Length != 2)
            {
                Log.Error($"BoredomWorker: Invalid priority format for giver.condition '{giver.condition}': {giver.priority}");
                return 0;
            }

            int minGiverPriority;
            int maxGiverPriority;
            try
            {
                minGiverPriority = int.Parse(priorityParts[0]);
                maxGiverPriority = int.Parse(priorityParts[1]);
            }
            catch (FormatException e)
            {
                Log.Error($"BoredomWorker: Could not parse priority parts for giver.condition '{giver.condition}': {giver.priority} - {e.Message}");
                return 0;
            }

            int basePriority;
            if (wanderingCycles <= 0)
            {
                basePriority = minGiverPriority;
            }
            else if (wanderingCycles >= MAX_WANDERING_CYCLES_FOR_BOREDOM_EFFECT)
            {
                basePriority = maxGiverPriority;
            }
            else
            {
                float boredomRatio = (float)wanderingCycles / MAX_WANDERING_CYCLES_FOR_BOREDOM_EFFECT;
                basePriority = minGiverPriority + (int)(boredomRatio * (maxGiverPriority - minGiverPriority));
            }

            int calculatedPriority = basePriority;

            // Apply work drive preference multiplier if defined
            if (!string.IsNullOrEmpty(giver.type) &&
                !string.IsNullOrEmpty(giver.workPreferenceScoreRange) &&
                !string.IsNullOrEmpty(giver.typeMultiplier) &&
                context.WorkDrivePreferences != null)
            {
                if (!context.WorkDrivePreferences.TryGetValue(giver.type, out int workDrivePreference))
                {
                    // Log.Warning($"BoredomWorker: Work drive preference not found for type '{giver.type}' for pawn {context.Pawn.Name.ToStringShort}. Using base priority.");
                    // If no specific preference, multiplier doesn't apply, or use a default. Here, we just use basePriority.
                }
                else
                {
                    string[] scoreRangeParts = giver.workPreferenceScoreRange.Split('~');
                    string[] multiplierParts = giver.typeMultiplier.Split('~');

                    if (scoreRangeParts.Length == 2 && multiplierParts.Length == 2)
                    {
                        try
                        {
                            float minScore = float.Parse(scoreRangeParts[0]);
                            float maxScore = float.Parse(scoreRangeParts[1]);
                            float minMultiplier = float.Parse(multiplierParts[0]);
                            float maxMultiplier = float.Parse(multiplierParts[1]);

                            // Clamp workDrivePreference to the score range for ratio calculation
                            float clampedWorkDrivePreference = Mathf.Clamp(workDrivePreference, minScore, maxScore);

                            float ratioMultiplier = 0f;
                            if (maxScore - minScore != 0)
                            {
                                ratioMultiplier = (clampedWorkDrivePreference - minScore) / (maxScore - minScore);
                            }
                            else if (clampedWorkDrivePreference == minScore) // Handle case where minScore == maxScore
                            {
                                ratioMultiplier = 0f; // Or 0.5f, depending on desired behavior for a single-point range
                            }
                            // If minScore == maxScore and clampedWorkDrivePreference != minScore, it's effectively outside a point range.
                            // The XML uses wide ranges like -10~10, so this is unlikely with current setup.

                            float finalMultiplier = minMultiplier + ratioMultiplier * (maxMultiplier - minMultiplier);
                            calculatedPriority = (int)(basePriority * finalMultiplier);
                        }
                        catch (FormatException e)
                        {
                            Log.Error($"BoredomWorker: Could not parse score range or multiplier for giver.condition '{giver.condition}' - {e.Message}");
                            // Fallback to basePriority if parsing fails
                            calculatedPriority = basePriority;
                        }
                    }
                    else
                    {
                        Log.Warning($"BoredomWorker: Invalid workPreferenceScoreRange or typeMultiplier format for giver.condition '{giver.condition}'. Using base priority.");
                    }
                }
            }
            
            // Ensure the final priority is within the giver's defined min/max range
            calculatedPriority = Mathf.Clamp(calculatedPriority, minGiverPriority, maxGiverPriority);

            return calculatedPriority;
        }
    }
}
