using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            // Use FreeColonistsSpawned to match the comparison in CalculateOrderBasedPriority
            // We can't assign work priorities to unspawned pawns anyway
            // Create a copy with ToList() to prevent "Collection was modified" errors
            // IMPORTANT: This snapshot is passed down to ensure consistent pawn lists throughout calculation
            var pawns = map.mapPawns.FreeColonistsSpawned.ToList();
            
            // Get pause tracker to skip paused pawns
            var pauseTracker = Current.Game?.GetComponent<PawnAutonomyPauseTracker>();
            
            foreach (var pawn in pawns)
            {
                // Skip paused pawns - they keep their manual priorities
                if (pauseTracker?.IsPaused(pawn) == true)
                {
                    continue;
                }
                
                try
                {
                    CalculateAndApplyPriorityForPawn(pawn, pawns);
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error calculating work priorities for {pawn.Name}: {e.Message}");
                }
            }
        }

        private void CalculateAndApplyPriorityForPawn(Pawn pawn, List<Pawn> allPawnsSnapshot)
        {
            var workTypePriorities = new Dictionary<WorkTypeDef, WorkTypePriorityResult>();
            
            // Step 1-3: Calculate priority for ALL WorkTypes (excluding disabled ones)
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (workType.workGiversByPriority.Any() && !pawn.WorkTypeIsDisabled(workType))
                {
                    var result = CalculateWorkTypePriority(pawn, workType, allPawnsSnapshot);
                    // Include ALL work types, even those with 0 priority
                    workTypePriorities[workType] = result;
                }
            }
            
            // Step 4: Rank WorkTypes and assign priority levels
            AssignPriorityLevels(pawn, workTypePriorities);
            
            // Cache results for tooltip display
            pawnWorkTypePriorities[pawn] = workTypePriorities;
        }

        private WorkTypePriorityResult CalculateWorkTypePriority(Pawn pawn, WorkTypeDef workType, List<Pawn> allPawnsSnapshot)
        {
            var result = new WorkTypePriorityResult
            {
                WorkType = workType,
                WorkGiverResults = new Dictionary<WorkGiverDef, WorkGiverPriorityResult>()
            };

            // Track PriorityGivers used within THIS WorkType only to prevent double-counting within the same WorkType
            // But allow the same PriorityGiver to apply to different WorkTypes
            var usedPriorityGivers = new HashSet<string>();

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
                        var (priority, description) = priorityGiverManager.EvaluatePriorityGiverForPawnWithDescription(priorityGiver, pawn);
                        var priorityResult = new PriorityGiverResult
                        {
                            PriorityGiver = priorityGiver,
                            Priority = priority,
                            Description = description,
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
            
            // Step 4: Add passion-based priority at WorkType level
            var passionResults = EvaluatePassionPriorityForWorkType(pawn, workType, usedPriorityGivers);
            result.PriorityGiverResults.AddRange(passionResults);

            // Step 5: Add skill-based priority at WorkType level
            var skillResults = EvaluateSkillPriorityForWorkType(pawn, workType, usedPriorityGivers, allPawnsSnapshot);
            result.PriorityGiverResults.AddRange(skillResults);

            // Note: Precept-based priorities are evaluated at WorkGiver level only to avoid duplication

            result.TotalPriority = result.WorkGiverSum + result.PriorityGiverResults.Sum(p => p.Priority);
            
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
                    var (priority, description) = priorityGiverManager.EvaluatePriorityGiverForPawnWithDescription(priorityGiver, pawn);
                    bool isDeduplication = usedPriorityGivers.Contains(priorityGiver.defName);
                    
                    var priorityResult = new PriorityGiverResult
                    {
                        PriorityGiver = priorityGiver,
                        Priority = priority,
                        Description = description,
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

            // Add precept-based priority at WorkGiver level
            var preceptResults = EvaluatePreceptPriorityForWorkGiver(pawn, workGiver, usedPriorityGivers);
            result.PriorityGiverResults.AddRange(preceptResults);

            result.TotalPriority = result.PriorityGiverResults.Where(p => !p.IsDeduplication).Sum(p => p.Priority);
            
            return result;
        }
        
        /// <summary>
        /// Evaluate passion-based priority for a WorkType
        /// </summary>
        private List<PriorityGiverResult> EvaluatePassionPriorityForWorkType(Pawn pawn, WorkTypeDef workType, HashSet<string> usedPriorityGivers)
        {
            var results = new List<PriorityGiverResult>();
            
            // Check if this WorkType has relevant skills
            if (workType.relevantSkills == null || !workType.relevantSkills.Any()) return results;
            
            // Get the highest passion among relevant skills
            int highestPassion = 0;
            string passionName = "None";
            SkillDef bestSkill = null;
            
            foreach (var skillDef in workType.relevantSkills)
            {
                var skillRecord = pawn.skills.GetSkill(skillDef);
                if (skillRecord != null)
                {
                    string currentPassionName = GetPassionName(skillRecord);
                    int currentPassionValue = GetPassionPriorityValue(currentPassionName);
                    
                    if (Math.Abs(currentPassionValue) > Math.Abs(highestPassion))
                    {
                        highestPassion = currentPassionValue;
                        passionName = currentPassionName;
                        bestSkill = skillDef;
                    }
                }
            }
            
            if (passionName != "None" && bestSkill != null)
            {
                // Find matching PassionGiver
                var passionGiver = DefDatabase<PassionGiverDef>.AllDefs.FirstOrDefault(pg => pg.passionName == passionName);
                
                if (passionGiver != null)
                {
                    string passionKey = $"Passion_{passionName}_{workType.defName}";
                    bool isDeduplication = usedPriorityGivers.Contains(passionKey);
                    
                    // Calculate base priority
                    int basePriority = passionGiver.priorityResult.priority;
                    
                    // Apply personality multiplier and flat offset
                    var (personalityMultiplier, personalityFlatOffset) = EvaluatePassionPersonalityMultiplier(passionGiver, pawn);
                    int adjustedPriority = (int)(basePriority * personalityMultiplier + personalityFlatOffset + 0.5f);
                    
                    var priorityResult = new PriorityGiverResult
                    {
                        PriorityGiver = null, // No direct PriorityGiver, this is passion-based
                        Priority = adjustedPriority,
                        Description = passionGiver.priorityResult.description,
                        IsDeduplication = isDeduplication
                    };
                    
                    results.Add(priorityResult);
                    
                    if (!isDeduplication)
                    {
                        usedPriorityGivers.Add(passionKey);
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Evaluate skill-based priority for a WorkType
        /// </summary>
        private List<PriorityGiverResult> EvaluateSkillPriorityForWorkType(Pawn pawn, WorkTypeDef workType, HashSet<string> usedPriorityGivers, List<Pawn> allPawnsSnapshot)
        {
            var results = new List<PriorityGiverResult>();
            
            // Check if this WorkType has relevant skills
            if (workType.relevantSkills == null || !workType.relevantSkills.Any())
            {
                return results;
            }
            
            // Get all SkillGivers
            var allSkillGivers = DefDatabase<SkillGiverDef>.AllDefs;
            
            foreach (var skillGiver in allSkillGivers)
            {
                // Check if this SkillGiver targets this WorkType
                bool targetsThisWorkType = skillGiver.targetWorkTypes.Contains("All") || 
                                          skillGiver.targetWorkTypes.Contains(workType.defName);
                
                if (!targetsThisWorkType)
                {
                    continue;
                }
                
                // Check if any of the workType's skills match the skillGiver's target skills
                bool hasMatchingSkill = false;
                SkillDef matchedSkill = null;
                int matchedSkillLevel = 0;
                
                foreach (var skillDef in workType.relevantSkills)
                {
                    // Check if this skill is targeted by the SkillGiver
                    bool targetsThisSkill = skillGiver.targetSkills.Contains("All") || 
                                           skillGiver.targetSkills.Contains(skillDef.defName);
                    
                    if (targetsThisSkill)
                    {
                        hasMatchingSkill = true;
                        matchedSkill = skillDef;
                        var skillRecord = pawn.skills.GetSkill(skillDef);
                        if (skillRecord != null)
                        {
                            matchedSkillLevel = skillRecord.Level;
                        }
                        break; // Only apply once per worktype
                    }
                }
                
                if (!hasMatchingSkill)
                {
                    continue;
                }
                
                // Create unique key for deduplication
                string skillKey = $"Skill_{skillGiver.defName}_{workType.defName}";
                bool isDeduplication = usedPriorityGivers.Contains(skillKey);
                
                // Calculate priority based on calculation type
                int basePriority = 0;
                string description = "";
                
                if (skillGiver.calculation == SkillCalculationType.order)
                {
                    // Order-based calculation: rank-based priority using priority ranges
                    var orderResult = CalculateOrderBasedPriority(pawn, workType, matchedSkill, matchedSkillLevel, skillGiver, allPawnsSnapshot);
                    basePriority = orderResult.priority;
                    description = orderResult.description;
                }
                else // SkillCalculationType.none (default)
                {
                    // Standard calculation: use skill level with priority ranges
                    foreach (var range in skillGiver.priorityRanges)
                    {
                        if (range.Contains(matchedSkillLevel))
                        {
                            basePriority = range.GetInterpolatedPriority(matchedSkillLevel);
                            description = range.description;
                            break;
                        }
                    }
                }
                
                // Apply personality multiplier and flat offset
                var (personalityMultiplier, personalityFlatOffset) = EvaluateSkillPersonalityMultiplier(skillGiver, pawn);
                int adjustedPriority = (int)(basePriority * personalityMultiplier + personalityFlatOffset + 0.5f);
                
                var priorityResult = new PriorityGiverResult
                {
                    PriorityGiver = null, // No direct PriorityGiver, this is skill-based
                    Priority = adjustedPriority,
                    Description = description,
                    IsDeduplication = isDeduplication
                };
                
                results.Add(priorityResult);
                
                if (!isDeduplication)
                {
                    usedPriorityGivers.Add(skillKey);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Evaluates personality-based multipliers for a skill giver
        /// Returns (combinedMultiplier, combinedFlatOffset) tuple
        /// </summary>
        private (float multiplier, float flatOffset) EvaluateSkillPersonalityMultiplier(SkillGiverDef skillGiver, Pawn pawn)
        {
            float personalityMultiplier = 1.0f;
            float personalityFlatOffset = 0f;
            
            if (skillGiver.conditions.NullOrEmpty())
            {
                return (personalityMultiplier, personalityFlatOffset); // No conditions to evaluate
            }

            foreach (var condition in skillGiver.conditions)
            {
                if (condition.type == ConditionType.personalityOffset)
                {
                    // Apply personality multiplier and flat offset
                    var (conditionMultiplier, conditionFlat) = EvaluatePersonalityMultiplier(condition, pawn);
                    personalityMultiplier *= conditionMultiplier;
                    personalityFlatOffset += conditionFlat;
                }
            }

            return (personalityMultiplier, personalityFlatOffset);
        }
        
        /// <summary>
        /// Calculate order-based priority by comparing pawn's skill rank against colony
        /// Uses the SkillGiver's priority ranges based on the pawn's rank position
        /// </summary>
        private (int priority, string description) CalculateOrderBasedPriority(Pawn pawn, WorkTypeDef workType, SkillDef skill, int pawnSkillLevel, SkillGiverDef skillGiver, List<Pawn> allPawnsSnapshot)
        {
            // Get all capable pawns for comparison
            var capablePawns = new List<(Pawn p, int level)>();
            
            // Always include the calling pawn first (so we never lose them)
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                var callingPawnSkillRecord = pawn.skills.GetSkill(skill);
                if (callingPawnSkillRecord != null)
                {
                    capablePawns.Add((pawn, callingPawnSkillRecord.Level));
                }
            }
            
            // Use the consistent snapshot of pawns passed from the outer loop
            // This ensures we're comparing against the same set of pawns that are being processed
            foreach (var colonist in allPawnsSnapshot)
            {
                // Skip if already added (the calling pawn)
                if (colonist == pawn) continue;
                
                // Skip downed pawns
                if (colonist.Downed) continue;
                
                // If urgent, also skip resting pawns
                if (skillGiver.isUrgent && colonist.jobs?.curJob?.def == RimWorld.JobDefOf.LayDown)
                {
                    continue;
                }
                
                // Check if pawn is capable of this work type
                if (colonist.WorkTypeIsDisabled(workType)) continue;
                
                // Get skill level
                var skillRecord = colonist.skills.GetSkill(skill);
                if (skillRecord != null)
                {
                    capablePawns.Add((colonist, skillRecord.Level));
                }
            }
            
            // If no capable pawns (calling pawn was filtered out), return neutral
            if (capablePawns.Count == 0)
            {
                return (0, "Not capable of this work");
            }
            
            // If only this pawn is capable, use neutral/default priority from ranges
            if (capablePawns.Count == 1)
            {
                // Use middle of priority ranges
                if (skillGiver.priorityRanges.Any())
                {
                    var midRange = skillGiver.priorityRanges[skillGiver.priorityRanges.Count / 2];
                    int midPriority = (midRange.PriorityRangeParsed.min + midRange.PriorityRangeParsed.max) / 2;
                    return (midPriority, "Only capable pawn");
                }
                return (0, "Only capable pawn");
            }
            
            // Sort by skill level (descending)
            capablePawns.Sort((a, b) => b.level.CompareTo(a.level));
            
            // Find this pawn's rank (0-based index)
            int rank = capablePawns.FindIndex(p => p.p == pawn);
            
            if (rank < 0)
            {
                // Pawn not in list (shouldn't happen, but handle gracefully)
                return (0, "Not ranked");
            }
            
            int totalPawns = capablePawns.Count;
            
            // Map rank to skill level range index
            // Best (rank 0) → last range (highest skills)
            // Worst (rank n-1) → first range (lowest skills)
            float rankPercentile = totalPawns > 1 ? (float)rank / (float)(totalPawns - 1) : 0f;
            int rangeIndex = (int)((1f - rankPercentile) * (skillGiver.priorityRanges.Count - 1));
            rangeIndex = UnityEngine.Mathf.Clamp(rangeIndex, 0, skillGiver.priorityRanges.Count - 1);
            
            var selectedRange = skillGiver.priorityRanges[rangeIndex];
            int priority = selectedRange.GetInterpolatedPriority(pawnSkillLevel);
            string description = selectedRange.description;
            
            return (priority, description);
        }
        
        /// <summary>
        /// Evaluate precept-based priority for a WorkGiver
        /// </summary>
        private List<PriorityGiverResult> EvaluatePreceptPriorityForWorkGiver(Pawn pawn, WorkGiverDef workGiver, HashSet<string> usedPriorityGivers)
        {
            var results = new List<PriorityGiverResult>();
            
            // Check if Ideology DLC is active
            if (!ModsConfig.IdeologyActive)
            {
                return results;
            }
            
            // Check if pawn has an ideo
            if (pawn.Ideo == null)
            {
                return results;
            }
            
            // Get all PreceptGivers
            var allPreceptGivers = DefDatabase<PreceptGiverDef>.AllDefs;
            
            foreach (var preceptGiver in allPreceptGivers)
            {
                // Check if this workGiver is targeted by this PreceptGiver
                if (!preceptGiver.targetWorkGivers.Contains(workGiver.defName))
                {
                    continue;
                }
                
                // Check if pawn has any of the precepts in this PreceptGiver
                foreach (var preceptResult in preceptGiver.priorityResults)
                {
                    // Check if pawn's ideo has this precept
                    var matchingPrecept = pawn.Ideo.PreceptsListForReading.FirstOrDefault(p => p.def.defName == preceptResult.preceptDef);
                    
                    if (matchingPrecept != null)
                    {
                        // Create unique key for deduplication - limit to once per WorkType to avoid stacking across WorkGivers
                        string workTypeKey = workGiver.workType?.defName ?? workGiver.defName;
                        string preceptKey = $"Precept_{preceptGiver.defName}_{workTypeKey}";
                        bool isDeduplication = usedPriorityGivers.Contains(preceptKey);
                        
                        var priorityGiverResult = new PriorityGiverResult
                        {
                            PriorityGiver = null, // No direct PriorityGiver, this is precept-based
                            Priority = preceptResult.priority,
                            Description = preceptResult.description,
                            IsDeduplication = isDeduplication
                        };
                        
                        results.Add(priorityGiverResult);
                        
                        if (!isDeduplication)
                        {
                            usedPriorityGivers.Add(preceptKey);
                        }
                        
                        // Only apply the first matching precept from this giver
                        break;
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Get passion name from skill record (supports vanilla, VSE, and Alpha Skills)
        /// </summary>
        private string GetPassionName(SkillRecord skillRecord)
        {
            // If VSE is active, try VSE first
            if (ModsConfig.IsActive("vanillaexpanded.skills"))
            {
                string vseResult = GetVSEPassionName(skillRecord);
                // If VSE recognized the passion, use it
                if (vseResult != null)
                {
                    return vseResult;
                }
            }
            
            // If Alpha Skills is active and VSE didn't recognize it, try Alpha Skills
            if (ModsConfig.IsActive("sarg.alphaskills"))
            {
                string alphaResult = GetAlphaSkillsPassionName(skillRecord);
                // If Alpha Skills recognized the passion, use it
                if (alphaResult != null)
                {
                    return alphaResult;
                }
            }
            
            // Fall back to vanilla passion handling
            return GetVanillaPassionName((int)skillRecord.passion);
        }
        
        /// <summary>
        /// Get Alpha Skills passion name using passion labels
        /// </summary>
        private string GetAlphaSkillsPassionName(SkillRecord skillRecord)
        {
            try
            {
                // Alpha Skills also uses SkillUI.GetLabel but with more complex label patterns
                string passionLabel = RimWorld.SkillUI.GetLabel(skillRecord.passion).ToLower();
                
                // Map Alpha Skills passion labels to our defNames
                switch (passionLabel)
                {
                    // Vanilla passions (Alpha Skills preserves these)
                    case "none":
                        return "None";
                    case "interested":
                        return "Minor";
                    case "burning":
                        return "Major";
                    
                    // Alpha Skills specific passions
                    case "drunken":
                        return "AS_DrunkenPassion";
                    case "drunken (active)":
                        return "AS_DrunkenPassion_Active";
                    case "frozen":
                        return "AS_FrozenPassion";
                    case "synergistic":
                        return "AS_SynergisticPassion";
                    case "night":
                        return "AS_NightPassion";
                    case "night (active)":
                        return "AS_NightPassion_Active";
                    case "youth":
                        return "AS_YouthPassion";
                    case "dedicated":
                        return "AS_DedicatedPassion";
                    case "obsessive":
                        return "AS_ObsessivePassion";
                    case "vengeful":
                        return "AS_VengefulPassion";
                    case "vengeful (active)":
                        return "AS_VengefulPassion_Active";
                    case "forbidden":
                        return "AS_ForbiddenPassion";
                    case "nomadic":
                        return "AS_NomadicPassion";
                    case "nomadic (active)":
                        return "AS_NomadicPassion_Active";
                    
                    default:
                        // Return null to indicate Alpha Skills doesn't recognize this passion
                        return null;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Autonomy] Failed to get Alpha Skills passion via label: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get VSE passion name using passion labels (also handles Alpha Skills extensions)
        /// </summary>
        private string GetVSEPassionName(SkillRecord skillRecord)
        {
            try
            {
                // Use SkillUI.GetLabel to get the passion label, which works for both vanilla and VSE
                string passionLabel = RimWorld.SkillUI.GetLabel(skillRecord.passion).ToLower();
                
                // Map VSE passion labels to our defNames
                switch (passionLabel)
                {
                    case "none":
                        return "None";
                    case "interested":
                        return "Minor";
                    case "burning":
                        return "Major";
                    case "apathy":
                        return "VSE_Apathy";
                    case "natural":
                        return "VSE_Natural";
                    case "critical":
                        return "VSE_Critical";
                    default:
                        // Return null to indicate VSE doesn't recognize this passion
                        return null;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Autonomy] Failed to get VSE passion via label: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Convert vanilla passion enum value to name
        /// </summary>
        private string GetVanillaPassionName(int passionValue)
        {
            switch (passionValue)
            {
                case 1: return "Minor";
                case 2: return "Major";
                default: return "None";
            }
        }
        
        /// <summary>
        /// Evaluates personality-based multipliers for a passion giver
        /// Returns (combinedMultiplier, combinedFlatOffset) tuple
        /// </summary>
        private (float multiplier, float flatOffset) EvaluatePassionPersonalityMultiplier(PassionGiverDef passionGiver, Pawn pawn)
        {
            float personalityMultiplier = 1.0f;
            float personalityFlatOffset = 0f;
            
            if (passionGiver.conditions.NullOrEmpty())
            {
                return (personalityMultiplier, personalityFlatOffset); // No conditions to evaluate
            }

            foreach (var condition in passionGiver.conditions)
            {
                if (condition.type == ConditionType.personalityOffset)
                {
                    // Apply personality multiplier and flat offset
                    var (conditionMultiplier, conditionFlat) = EvaluatePersonalityMultiplier(condition, pawn);
                    personalityMultiplier *= conditionMultiplier;
                    personalityFlatOffset += conditionFlat;
                }
            }

            return (personalityMultiplier, personalityFlatOffset);
        }
        
        /// <summary>
        /// Evaluates personality-based multipliers for a given condition and pawn
        /// Returns (multiplier, flatOffset) tuple
        /// </summary>
        private (float multiplier, float flatOffset) EvaluatePersonalityMultiplier(PriorityCondition condition, Pawn pawn)
        {
            // Check if RimPsyche mod is available
            if (!ModsConfig.IsActive("maux36.rimpsyche"))
            {
                return (1.0f, 0f); // No multiplier or offset if mod not available
            }

            try
            {
                // Use reflection to get the personality value safely
                var compPsyche = GetRimPsycheComponent(pawn);
                if (compPsyche == null)
                {
                    return (1.0f, 0f); // No personality component
                }

                // Get personality value using reflection
                float personalityValue = GetPersonalityValue(compPsyche, condition.personalityDefName);

                // Find matching multiplier range
                foreach (var multiplier in condition.personalityMultipliers)
                {
                    if (multiplier.personalityRange.Includes(personalityValue))
                    {
                        return (multiplier.multiplier, multiplier.flat);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Autonomy] Failed to evaluate personality multiplier for {condition.personalityDefName} on pawn {pawn.Name}: {e.Message}");
            }

            return (1.0f, 0f); // Default multiplier and no offset if no range matches
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
                if (extensionType == null) 
                {
                    return null;
                }

                // Get the compPsyche extension method
                var compPsycheMethod = extensionType.GetMethod("compPsyche", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (compPsycheMethod == null) 
                {
                    return null;
                }

                // Call the extension method
                var result = compPsycheMethod.Invoke(null, new object[] { pawn });
                return result;
            }
            catch (Exception e)
            {
                Log.Warning($"[Autonomy] Exception getting RimPsyche component: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a personality value using reflection
        /// </summary>
        private float GetPersonalityValue(object compPsyche, string personalityDefName)
        {
            try
            {
                // Get the Personality property
                var personalityProperty = compPsyche.GetType().GetProperty("Personality");
                if (personalityProperty == null)
                {
                    return 0f;
                }

                var personalityTracker = personalityProperty.GetValue(compPsyche);
                if (personalityTracker == null)
                {
                    return 0f;
                }

                // Get the GetPersonality method
                var getPersonalityMethod = personalityTracker.GetType().GetMethod("GetPersonality", 
                    new Type[] { typeof(string) });
                if (getPersonalityMethod == null)
                {
                    return 0f;
                }

                // Call GetPersonality with the defName
                var result = getPersonalityMethod.Invoke(personalityTracker, new object[] { personalityDefName });
                if (result is float personalityValue)
                {
                    return personalityValue;
                }
                
                // Non-float result
            }
            catch (Exception e)
            {
                Log.Warning($"[Autonomy] Failed to get personality value for {personalityDefName}: {e.Message}");
            }

            return 0f;
        }
        
        /// <summary>
        /// Get priority value for passion comparison (higher absolute value = higher priority)
        /// </summary>
        private int GetPassionPriorityValue(string passionName)
        {
            var passionGiver = DefDatabase<PassionGiverDef>.AllDefs.FirstOrDefault(pg => pg.passionName == passionName);
            return passionGiver?.priorityResult.priority ?? 0;
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
        public Autonomy.PriorityGiverDef PriorityGiver;
        public int Priority;
        public string Description;
        public bool IsDeduplication; // True if this priority was already counted for another WorkGiver
    }

    #endregion
}