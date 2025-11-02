using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Autonomy.Systems
{
    /// <summary>
    /// Work priority assignment and ranking system
    /// 
    /// Priority Pipeline:
    /// 1. Sum PriorityGiver results per WorkGiver for each pawn
    /// 2. Sum WorkGiver priorities → WorkType priorities 
    /// 3. Sum WorkType-specific PriorityGivers to WorkType total
    /// 4. Rank all WorkTypes: Top 20% = Priority 1, Next 20% = Priority 2, etc.
    /// 5. Apply final priorities to pawn work settings
    /// 
    /// Priority Level Mapping:
    /// - Manual Priorities ON: 1, 2, 3, 4, disabled (5 levels - preferred)
    /// - Manual Priorities OFF: enabled, enabled, disabled, disabled, disabled (fallback)
    /// 
    /// Deduplication: When a PriorityGiver affects multiple WorkGivers in the same WorkType,
    /// apply the priority bonus only once to prevent double-counting.
    /// </summary>
    public class WorkPriorityAssignmentSystem : MapComponent
    {
        private PriorityGiverManager priorityGiverManager;
        
        // Cached priority calculations for tooltip display
        private Dictionary<Pawn, Dictionary<WorkTypeDef, WorkTypePriorityResult>> pawnWorkTypePriorities 
            = new Dictionary<Pawn, Dictionary<WorkTypeDef, WorkTypePriorityResult>>();

        public WorkPriorityAssignmentSystem(Map map) : base(map)
        {
            this.priorityGiverManager = map.GetComponent<PriorityGiverManager>();
        }

        /// <summary>
        /// Calculate and apply work priorities for all pawns
        /// Called after PriorityGiver evaluation cycles
        /// </summary>
        public void RecalculateWorkPriorities()
        {
            var pawns = map.mapPawns.FreeColonists;
            
            foreach (var pawn in pawns)
            {
                try
                {
                    CalculateAndApplyPriorityForPawn(pawn);
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error calculating work priorities for {pawn.Name}: {e.Message}");
                }
            }
        }

        private void CalculateAndApplyPriorityForPawn(Pawn pawn)
        {
            var workTypePriorities = new Dictionary<WorkTypeDef, WorkTypePriorityResult>();
            
            // Step 1-3: Calculate priority for ALL WorkTypes (excluding disabled ones)
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (workType.workGiversByPriority.Any() && !pawn.WorkTypeIsDisabled(workType))
                {
                    var result = CalculateWorkTypePriority(pawn, workType);
                    // Include ALL work types, even those with 0 priority
                    workTypePriorities[workType] = result;
                }
            }
            
            // Step 4: Rank WorkTypes and assign priority levels
            AssignPriorityLevels(pawn, workTypePriorities);
            
            // Cache results for tooltip display
            pawnWorkTypePriorities[pawn] = workTypePriorities;
        }

        private WorkTypePriorityResult CalculateWorkTypePriority(Pawn pawn, WorkTypeDef workType)
        {
            var result = new WorkTypePriorityResult
            {
                WorkType = workType,
                WorkGiverResults = new Dictionary<WorkGiverDef, WorkGiverPriorityResult>()
            };

            var usedPriorityGivers = new HashSet<string>(); // Track to prevent double-counting

            // Step 1: Calculate priority for each WorkGiver
            int workGiverSum = 0;
            foreach (var workGiver in workType.workGiversByPriority)
            {
                var workGiverResult = CalculateWorkGiverPriority(pawn, workGiver, usedPriorityGivers);
                result.WorkGiverResults[workGiver] = workGiverResult;
                workGiverSum += workGiverResult.TotalPriority;
            }

            // Step 2: Sum WorkGiver priorities to WorkType
            result.WorkGiverSum = workGiverSum;

            // Step 3: Add WorkType-specific PriorityGivers
            var workTypePriorityGivers = GetPriorityGiversForWorkType(workType);
            foreach (var priorityGiver in workTypePriorityGivers)
            {
                if (!usedPriorityGivers.Contains(priorityGiver.defName))
                {
                    try
                    {
                        int priority = priorityGiverManager.EvaluatePriorityGiverForPawn(priorityGiver, pawn);
                        var priorityResult = new PriorityGiverResult
                        {
                            PriorityGiver = priorityGiver,
                            Priority = priority,
                            Description = GetPriorityDescription(priorityGiver, priority),
                            IsDeduplication = false
                        };
                        result.PriorityGiverResults.Add(priorityResult);
                        usedPriorityGivers.Add(priorityGiver.defName);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[Autonomy] Error evaluating WorkType PriorityGiver {priorityGiver.defName}: {e.Message}");
                    }
                }
            }

            result.TotalPriority = result.WorkGiverSum + result.PriorityGiverResults.Sum(p => p.Priority);
            
            // Only log if there's actually a priority change
            if (result.TotalPriority != 0)
            {
                Log.Message($"[Autonomy] {pawn.NameShortColored} - {workType.labelShort}: Total priority {result.TotalPriority}");
            }
            
            return result;
        }

        private WorkGiverPriorityResult CalculateWorkGiverPriority(Pawn pawn, WorkGiverDef workGiver, HashSet<string> usedPriorityGivers)
        {
            var result = new WorkGiverPriorityResult
            {
                WorkGiver = workGiver,
                PriorityGiverResults = new List<PriorityGiverResult>()
            };

            var workGiverPriorityGivers = GetPriorityGiversForWorkGiver(workGiver);
            
            foreach (var priorityGiver in workGiverPriorityGivers)
            {
                try
                {
                    int priority = priorityGiverManager.EvaluatePriorityGiverForPawn(priorityGiver, pawn);
                    bool isDeduplication = usedPriorityGivers.Contains(priorityGiver.defName);
                    
                    var priorityResult = new PriorityGiverResult
                    {
                        PriorityGiver = priorityGiver,
                        Priority = priority,
                        Description = GetPriorityDescription(priorityGiver, priority),
                        IsDeduplication = isDeduplication
                    };
                    
                    result.PriorityGiverResults.Add(priorityResult);
                    
                    if (!isDeduplication)
                    {
                        usedPriorityGivers.Add(priorityGiver.defName);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating WorkGiver PriorityGiver {priorityGiver.defName}: {e.Message}");
                }
            }

            result.TotalPriority = result.PriorityGiverResults.Where(p => !p.IsDeduplication).Sum(p => p.Priority);
            
            return result;
        }

        private void AssignPriorityLevels(Pawn pawn, Dictionary<WorkTypeDef, WorkTypePriorityResult> workTypePriorities)
        {
            if (!workTypePriorities.Any()) return;

            // Sort WorkTypes by total priority (descending)
            var sortedWorkTypes = workTypePriorities
                .OrderByDescending(kvp => kvp.Value.TotalPriority)
                .ToList();

            int totalWorkTypes = sortedWorkTypes.Count;
            bool useManualPriorities = Current.Game.playSettings.useWorkPriorities;

            for (int i = 0; i < sortedWorkTypes.Count; i++)
            {
                var workType = sortedWorkTypes[i].Key;
                int priorityLevel = CalculatePriorityLevel(i, totalWorkTypes, useManualPriorities);
                
                // Apply priority to pawn's work settings
                pawn.workSettings.SetPriority(workType, priorityLevel);
                
                Log.Message($"[Autonomy] {pawn.NameShortColored} - {workType.labelShort}: Assigned priority {priorityLevel}");
            }
        }

        private int CalculatePriorityLevel(int rank, int totalWorkTypes, bool useManualPriorities)
        {
            if (useManualPriorities)
            {
                // 5 levels: 1, 2, 3, 4, 0 (disabled)
                float percentile = (float)rank / totalWorkTypes;
                if (percentile < 0.2f) return 1; // Top 20%
                if (percentile < 0.4f) return 2; // Next 20%
                if (percentile < 0.6f) return 3; // Next 20%
                if (percentile < 0.8f) return 4; // Next 20%
                return 0; // Bottom 20% - disabled
            }
            else
            {
                // 2 levels: enabled, disabled (fallback for manual priorities off)
                float percentile = (float)rank / totalWorkTypes;
                return percentile < 0.6f ? 3 : 0; // Top 60% enabled, bottom 40% disabled
            }
        }

        private List<PriorityGiverDef> GetPriorityGiversForWorkType(WorkTypeDef workType)
        {
            var result = DefDatabase<PriorityGiverDef>.AllDefs
                .Where(pg => pg.targetWorkTypes.Contains(workType.defName))
                .ToList();
                
            return result;
        }

        private List<PriorityGiverDef> GetPriorityGiversForWorkGiver(WorkGiverDef workGiver)
        {
            return DefDatabase<PriorityGiverDef>.AllDefs
                .Where(pg => pg.targetWorkGivers.Contains(workGiver.defName))
                .ToList();
        }

        private string GetPriorityDescription(PriorityGiverDef priorityGiver, int priority)
        {
            // Find the matching priority range description
            foreach (var range in priorityGiver.priorityRanges)
            {
                if (range.PriorityRangeParsed.Includes(priority))
                {
                    return range.description;
                }
            }
            return $"Priority {priority}";
        }

        /// <summary>
        /// Get priority calculation results for tooltip display
        /// </summary>
        public WorkTypePriorityResult GetWorkTypePriorityResult(Pawn pawn, WorkTypeDef workType)
        {
            if (pawnWorkTypePriorities.TryGetValue(pawn, out var workTypePriorities))
            {
                workTypePriorities.TryGetValue(workType, out var result);
                return result;
            }
            return null;
        }
    }

    #region Data Structures

    public class WorkTypePriorityResult
    {
        public WorkTypeDef WorkType;
        public Dictionary<WorkGiverDef, WorkGiverPriorityResult> WorkGiverResults;
        public List<PriorityGiverResult> PriorityGiverResults = new List<PriorityGiverResult>();
        public int WorkGiverSum;
        public int TotalPriority;
    }

    public class WorkGiverPriorityResult
    {
        public WorkGiverDef WorkGiver;
        public List<PriorityGiverResult> PriorityGiverResults;
        public int TotalPriority;
    }

    public class PriorityGiverResult
    {
        public PriorityGiverDef PriorityGiver;
        public int Priority;
        public string Description;
        public bool IsDeduplication; // True if this priority was already counted for another WorkGiver
    }

    #endregion
}