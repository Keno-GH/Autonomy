using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

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
        
        private const int UPDATE_INTERVAL = 200; // Normal evaluation every 2000 ticks (~33 seconds)
        private const int URGENT_UPDATE_INTERVAL = 40; // Urgent evaluation every 400 ticks (~6.7 seconds)

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
            // Get all spawned player pawns in the current map - each pawn gets individual evaluation
            // Use FreeColonistsSpawned to avoid processing pawns in caravans/cryptosleep/etc.
            // Create a copy with ToList() to prevent "Collection was modified" errors
            var pawns = map.mapPawns.FreeColonistsSpawned.ToList();
            
            foreach (var pawn in pawns)
            {
                try
                {
                    int priority = EvaluatePriorityGiverForPawn(def, pawn);
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
            int basePriority = 3; // Default priority
            float personalityMultiplier = 1.0f;
            
            // Evaluate each condition for the specific pawn
            foreach (var condition in def.conditions)
            {
                try
                {
                    if (condition.type == ConditionType.personalityOffset)
                    {
                        // Handle personality-based multipliers
                        personalityMultiplier *= EvaluatePersonalityMultiplier(condition, pawn);
                    }
                    else
                    {
                        // Handle regular InfoGiver conditions
                        float infoValue = infoGiverManager.GetLastResult(condition.infoDefName, condition, pawn);
                        
                        // Find matching priority range
                        foreach (var range in def.priorityRanges)
                        {
                            if (range.Contains(infoValue))
                            {
                                basePriority = range.GetInterpolatedPriority(infoValue);
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating condition for PriorityGiver {def.defName} for pawn {pawn.Name}: {e.Message}");
                }
            }
            
            // Apply personality multiplier to base priority and return
            return Mathf.RoundToInt(basePriority * personalityMultiplier);
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

        /// <summary>
        /// Evaluates personality-based multipliers for a given condition and pawn
        /// </summary>
        private float EvaluatePersonalityMultiplier(PriorityCondition condition, Pawn pawn)
        {
            // Check if RimPsyche mod is available
            if (!ModsConfig.IsActive("maux36.rimpsyche"))
            {
                return 1.0f; // No multiplier if mod not available
            }

            try
            {
                // Use reflection to get the personality value safely
                var compPsyche = GetRimPsycheComponent(pawn);
                if (compPsyche == null)
                {
                    return 1.0f; // No personality component
                }

                // Get personality value using reflection
                float personalityValue = GetPersonalityValue(compPsyche, condition.personalityDefName);
                
                // Find matching multiplier range
                foreach (var multiplier in condition.personalityMultipliers)
                {
                    if (multiplier.personalityRange.Includes(personalityValue))
                    {
                        return multiplier.multiplier;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Autonomy] Failed to evaluate personality multiplier for {condition.personalityDefName} on pawn {pawn.Name}: {e.Message}");
            }

            return 1.0f; // Default multiplier if no range matches
        }

        /// <summary>
        /// Safely gets the RimPsyche component using reflection
        /// </summary>
        private object GetRimPsycheComponent(Pawn pawn)
        {
            try
            {
                // Get the extension method type
                var extensionType = GenTypes.GetTypeInAnyAssembly("Maux36.RimPsyche.PawnExtensions");
                if (extensionType == null) return null;

                // Get the compPsyche extension method
                var method = extensionType.GetMethod("compPsyche", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null) return null;

                // Call the extension method
                return method.Invoke(null, new object[] { pawn });
            }
            catch (Exception e)
            {
                Log.Warning($"[Autonomy] Exception getting RimPsyche component: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets personality value using reflection
        /// </summary>
        private float GetPersonalityValue(object compPsyche, string personalityDefName)
        {
            try
            {
                // Get the Personality property
                var personalityProperty = compPsyche.GetType().GetProperty("Personality");
                if (personalityProperty == null) return 0f;

                var personalityTracker = personalityProperty.GetValue(compPsyche);
                if (personalityTracker == null) return 0f;

                // Get the GetPersonality method
                var getPersonalityMethod = personalityTracker.GetType().GetMethod("GetPersonality", 
                    new Type[] { typeof(string) });
                if (getPersonalityMethod == null) return 0f;

                // Call GetPersonality with the defName
                var result = getPersonalityMethod.Invoke(personalityTracker, new object[] { personalityDefName });
                if (result is float personalityValue)
                {
                    return personalityValue;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Autonomy] Failed to get personality value for {personalityDefName}: {e.Message}");
            }

            return 0f;
        }

        #endregion
    }
}