using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// Manages PriorityGiver evaluation for individual pawns
    /// Core responsibility: Convert colony/pawn conditions into work priority adjustments per pawn
    /// </summary>
    public class PriorityGiverManager : MapComponent
    {
        private InfoGiverManager infoGiverManager;
        private Autonomy.Systems.WorkPriorityAssignmentSystem workPrioritySystem;
        
        // Tick tracking for evaluation frequency
        private int ticksSinceLastUpdate = 0;
        private int ticksSinceLastUrgentUpdate = 0;
        
        private const int UPDATE_INTERVAL = 100; // Normal evaluation every 100 ticks (~1.7 seconds) - for testing
        private const int URGENT_UPDATE_INTERVAL = 10; // Urgent evaluation every 10 ticks (~0.17 seconds) - for testing

        public PriorityGiverManager(Map map) : base(map)
        {
            this.infoGiverManager = map.GetComponent<InfoGiverManager>();
            // Don't get workPrioritySystem here - get it lazily when needed
        }
        
        private Autonomy.Systems.WorkPriorityAssignmentSystem GetWorkPrioritySystem()
        {
            if (workPrioritySystem == null)
            {
                workPrioritySystem = map.GetComponent<Autonomy.Systems.WorkPriorityAssignmentSystem>();
            }
            return workPrioritySystem;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            ticksSinceLastUpdate++;
            ticksSinceLastUrgentUpdate++;
            
            // Check urgent PriorityGivers every 400 ticks
            if (ticksSinceLastUrgentUpdate >= URGENT_UPDATE_INTERVAL)
            {
                ticksSinceLastUrgentUpdate = 0;
                EvaluateUrgentPriorityGivers();
            }
            
            // Check all PriorityGivers every 2000 ticks
            if (ticksSinceLastUpdate >= UPDATE_INTERVAL)
            {
                ticksSinceLastUpdate = 0;
                EvaluateAllPriorityGivers();
            }
        }

        #region PriorityGiver Evaluation

        private void EvaluateUrgentPriorityGivers()
        {
            var priorityGivers = DefDatabase<PriorityGiverDef>.AllDefs.Where(pg => pg.isUrgent);
            
            foreach (var priorityGiver in priorityGivers)
            {
                try
                {
                    EvaluatePriorityGiverForAllPawns(priorityGiver, "[Autonomy-Priority-Urgent]");
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating urgent PriorityGiver {priorityGiver.defName}: {e.Message}");
                }
            }
        }

        private void EvaluateAllPriorityGivers()
        {
            var priorityGivers = DefDatabase<PriorityGiverDef>.AllDefs;
            
            foreach (var priorityGiver in priorityGivers)
            {
                try
                {
                    string prefix = priorityGiver.isUrgent ? "[Autonomy-Priority-Urgent]" : "[Autonomy-Priority]";
                    EvaluatePriorityGiverForAllPawns(priorityGiver, prefix);
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating PriorityGiver {priorityGiver.defName}: {e.Message}");
                }
            }
            
            // After all PriorityGivers are evaluated, recalculate work priorities
            var workPrioritySystem = GetWorkPrioritySystem();
            if (workPrioritySystem != null)
            {
                workPrioritySystem.RecalculateWorkPriorities();
            }
            else
            {
                Log.Error("[Autonomy] workPrioritySystem is NULL - cannot recalculate work priorities!");
            }
        }

        private void EvaluatePriorityGiverForAllPawns(PriorityGiverDef def, string logPrefix)
        {
            // Get all player pawns in the current map - each pawn gets individual evaluation
            var pawns = map.mapPawns.FreeColonists;
            
            foreach (var pawn in pawns)
            {
                try
                {
                    int priority = EvaluatePriorityGiverForPawn(def, pawn);
                    Log.Message($"{logPrefix} {def.label} for {pawn.NameShortColored}: {priority} priority");
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating PriorityGiver {def.defName} for pawn {pawn.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Evaluates a PriorityGiver for a specific pawn, returning the priority adjustment
        /// This is the core per-pawn evaluation that will eventually feed into work priority calculations
        /// </summary>
        public int EvaluatePriorityGiverForPawn(PriorityGiverDef def, Pawn pawn)
        {
            // Evaluate each condition for the specific pawn
            foreach (var condition in def.conditions)
            {
                try
                {
                    // Get the InfoGiver result for this condition with pawn context
                    float infoValue = infoGiverManager.GetLastResult(condition.infoDefName, condition, pawn);
                    
                    // Find matching priority range
                    foreach (var range in def.priorityRanges)
                    {
                        if (range.Contains(infoValue))
                        {
                            return range.GetRandomPriority();
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating condition for PriorityGiver {def.defName} for pawn {pawn.Name}: {e.Message}");
                }
            }
            
            // Return default priority if no conditions match
            return 3;
        }

        /// <summary>
        /// Gets all PriorityGiver results for a specific pawn
        /// This will be used later for work priority aggregation and ranking
        /// </summary>
        public Dictionary<string, int> GetAllPriorityResults(Pawn pawn)
        {
            var results = new Dictionary<string, int>();
            var priorityGivers = DefDatabase<PriorityGiverDef>.AllDefs;
            
            foreach (var priorityGiver in priorityGivers)
            {
                try
                {
                    int priority = EvaluatePriorityGiverForPawn(priorityGiver, pawn);
                    results[priorityGiver.defName] = priority;
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error getting priority result for {priorityGiver.defName} for pawn {pawn.Name}: {e.Message}");
                }
            }
            
            return results;
        }

        #endregion
    }
}