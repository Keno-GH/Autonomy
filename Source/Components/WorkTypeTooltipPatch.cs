using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// Patches work type tooltips to show autonomous priority calculations
    /// Displays how each pawn calculated their work priorities based on PriorityGiver results
    /// </summary>
    [HarmonyPatch]
    public static class WorkTypeTooltipPatch
    {
        /// <summary>
        /// Patch the work type tooltip to include priority calculation details
        /// </summary>
        [HarmonyPatch(typeof(WidgetsWork), "TipForPawnWorker")]
        [HarmonyPostfix]
        public static void TipForPawnWorker_Postfix(ref string __result, Pawn p, WorkTypeDef wDef, bool incapableBecauseOfCapacities)
        {
            try
            {
                if (p?.Map == null) return;

                var workPrioritySystem = p.Map.GetComponent<Autonomy.Systems.WorkPriorityAssignmentSystem>();
                if (workPrioritySystem == null) return;

                var priorityResult = workPrioritySystem.GetWorkTypePriorityResult(p, wDef);
                if (priorityResult == null || priorityResult.TotalPriority == 0) return;

                // Build the autonomous priority section
                var sb = new StringBuilder(__result);
                sb.AppendLine();
                sb.AppendLine();
                
                // Priority calculation header
                sb.AppendLine($"Priority calculated: {priorityResult.TotalPriority}");
                
                // WorkType-specific PriorityGivers (shown once at WorkType level)
                foreach (var priorityGiverResult in priorityResult.PriorityGiverResults)
                {
                    if (priorityGiverResult.Priority != 0)
                    {
                        string sign = priorityGiverResult.Priority > 0 ? "+" : "";
                        sb.AppendLine($"- {priorityGiverResult.Description}: {sign}{priorityGiverResult.Priority}");
                    }
                }

                // Collect all unique PriorityGivers from WorkGivers (deduplicated for tooltip display)
                var shownPriorityGivers = new System.Collections.Generic.HashSet<string>();
                
                foreach (var kvp in priorityResult.WorkGiverResults)
                {
                    var workGiver = kvp.Key;
                    var workGiverResult = kvp.Value;
                    
                    // Only show WorkGivers that have non-zero priority results
                    if (!workGiverResult.PriorityGiverResults.Any(pgr => pgr.Priority != 0))
                        continue;
                    
                    // Collect lines for this WorkGiver first (to see if we have anything to show after deduplication)
                    var workGiverLines = new System.Collections.Generic.List<string>();
                    
                    foreach (var priorityGiverResult in workGiverResult.PriorityGiverResults)
                    {
                        if (priorityGiverResult.Priority != 0)
                        {
                            // Only show each PriorityGiver once per WorkType (tooltip deduplication)
                            string key = $"{priorityGiverResult.Description}_{priorityGiverResult.Priority}";
                            if (shownPriorityGivers.Contains(key))
                                continue;
                            
                            shownPriorityGivers.Add(key);
                            
                            string sign = priorityGiverResult.Priority > 0 ? "+" : "";
                            string line = $"- {priorityGiverResult.Description}: {sign}{priorityGiverResult.Priority}";
                            
                            workGiverLines.Add(line);
                        }
                    }
                    
                    // Only show the WorkGiver header if we have lines to display
                    if (workGiverLines.Any())
                    {
                        sb.AppendLine($"** {workGiver.label}");
                        foreach (var line in workGiverLines)
                        {
                            sb.AppendLine(line);
                        }
                    }
                }

                __result = sb.ToString();
            }
            catch (System.Exception e)
            {
                Log.Error($"[Autonomy] Error adding priority tooltip for {p?.Name} {wDef?.label}: {e.Message}");
            }
        }
    }
}