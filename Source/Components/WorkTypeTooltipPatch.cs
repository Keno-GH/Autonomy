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
                
                // WorkType-specific PriorityGivers
                foreach (var priorityGiverResult in priorityResult.PriorityGiverResults)
                {
                    if (priorityGiverResult.Priority != 0)
                    {
                        string sign = priorityGiverResult.Priority > 0 ? "+" : "";
                        sb.AppendLine($"- {priorityGiverResult.Description}: {sign}{priorityGiverResult.Priority}");
                    }
                }

                // Show only the FIRST WorkGiver that has priority changes to avoid clutter
                var firstWorkGiverWithPriority = priorityResult.WorkGiverResults
                    .FirstOrDefault(kvp => kvp.Value.PriorityGiverResults.Any(pgr => pgr.Priority != 0));
                    
                if (firstWorkGiverWithPriority.Key != null)
                {
                    var workGiver = firstWorkGiverWithPriority.Key;
                    var workGiverResult = firstWorkGiverWithPriority.Value;
                    
                    sb.AppendLine($"** {workGiver.label}");
                    
                    foreach (var priorityGiverResult in workGiverResult.PriorityGiverResults)
                    {
                        if (priorityGiverResult.Priority != 0)
                        {
                            string sign = priorityGiverResult.Priority > 0 ? "+" : "";
                            string line = $"- {priorityGiverResult.Description}: {sign}{priorityGiverResult.Priority}";
                            
                            if (priorityGiverResult.IsDeduplication)
                            {
                                // Grey out deduplicated entries
                                line = $"<color=grey>{line} (applies only once)</color>";
                            }
                            
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