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
                WorkTypeDef = workTypeDef, // Set the WorkTypeDef
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

                // Retrieve the worker for this condition
                IPriorityWorker worker = PriorityWorkerRegistry.GetWorker(def.workerClass);
                if (worker == null)
                {
                    Log.Error($"PriorityCalculator: No worker found for condition {giver.condition}");
                    continue;
                }

                int giverPriority = worker.CalculatePriority(giver, context);
                
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
    }

    public class PriorityCalculationContext
    {
        public Dictionary<string, int> WorkDrivePreferences { get; set; }
        public Dictionary<string, float> MapInfo { get; set; }
        public Dictionary<string, float> PawnInfo { get; set; }
        public Pawn Pawn { get; set; }
        public WorkTypeDef WorkTypeDef { get; set; } // Added WorkTypeDef
        public Dictionary<string, int> ConditionPriorities { get; set; }
    }
}
