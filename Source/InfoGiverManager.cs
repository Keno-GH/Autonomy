using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    /// <summary>
    /// MapComponent that evaluates InfoGivers and manages the information gathering system
    /// </summary>
    public class InfoGiverManager : MapComponent
    {
        private Dictionary<string, float> lastResults = new Dictionary<string, float>();
        private Dictionary<string, LocationalData> locationalData = new Dictionary<string, LocationalData>();
        private Dictionary<string, IndividualData> individualData = new Dictionary<string, IndividualData>();
        private int ticksSinceLastUpdate = 0;
        private int ticksSinceLastUrgentUpdate = 0;
        private const int UPDATE_INTERVAL = 2000; // Update every 2000 ticks (~33 seconds)
        private const int URGENT_UPDATE_INTERVAL = 400; // Urgent updates every 400 ticks (~6.7 seconds)

        public InfoGiverManager(Map map) : base(map)
        {
        }

        /// <summary>
        /// Get the latest result for an InfoGiver
        /// </summary>
        public float GetLastResult(string infoGiverDefName)
        {
            return lastResults.TryGetValue(infoGiverDefName, out float value) ? value : 0f;
        }

        /// <summary>
        /// Get InfoGiver result with context (localized/individualized)
        /// </summary>
        public float GetLastResult(string infoGiverDefName, InfoGiverQueryContext context)
        {
            var infoDef = DefDatabase<InfoGiverDef>.GetNamedSilentFail(infoGiverDefName);
            if (infoDef == null) return GetLastResult(infoGiverDefName);
            
            // Handle individualized InfoGivers
            if (infoDef.isIndividualizable && context.requestingPawn != null)
            {
                // Ensure individualized data exists
                if (!individualData.ContainsKey(infoGiverDefName) && CanBeIndividualized(infoDef.sourceType))
                {
                    CollectIndividualizedData(infoDef);
                }
                
                if (individualData.TryGetValue(infoGiverDefName, out IndividualData indData))
                {
                    if (context.requestDistanceFromGlobal)
                    {
                        return indData.GetSignedDistanceFromGlobal(context.requestingPawn);
                    }
                    else if (context.requestIndividualData)
                    {
                        return indData.GetPawnValue(context.requestingPawn);
                    }
                }
            }
            
            // Handle localized InfoGivers
            if (infoDef.isLocalizable && context.location.HasValue)
            {
                // Ensure localized data exists
                if (!locationalData.ContainsKey(infoGiverDefName) && CanBeLocalized(infoDef.sourceType))
                {
                    CollectLocalizedData(infoDef);
                }
                
                if (locationalData.TryGetValue(infoGiverDefName, out LocationalData locData))
                {
                    if (context.requestLocalizedData)
                    {
                        Room room = map.regionGrid.GetValidRegionAt(context.location.Value)?.Room;
                        // Treat "None" rooms as outside areas
                        if (ShouldIgnoreRoom(room))
                        {
                            room = null;
                        }
                        return locData.GetRoomValue(room);
                    }
                }
            }
            
            // Fall back to global result
            return GetLastResult(infoGiverDefName);
        }

        /// <summary>
        /// Get InfoGiver result based on PriorityCondition request flags
        /// </summary>
        public float GetLastResult(string infoGiverDefName, PriorityCondition condition, Pawn requestingPawn = null)
        {
            // Build query context from condition flags
            var context = new InfoGiverQueryContext
            {
                requestingPawn = requestingPawn,
                location = requestingPawn?.Position,
                requestIndividualData = condition.requestIndividualData,
                requestDistanceFromGlobal = condition.requestDistanceFromGlobal,
                requestLocalizedData = condition.requestLocalizedData
            };
            
            return GetLastResult(infoGiverDefName, context);
        }

        /// <summary>
        /// Get all InfoGiver results for UI display
        /// </summary>
        public Dictionary<string, float> GetAllResults()
        {
            return new Dictionary<string, float>(lastResults);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            ticksSinceLastUpdate++;
            ticksSinceLastUrgentUpdate++;
            
            // Check urgent InfoGivers every 400 ticks
            if (ticksSinceLastUrgentUpdate >= URGENT_UPDATE_INTERVAL)
            {
                ticksSinceLastUrgentUpdate = 0;
                EvaluateUrgentInfoGivers();
            }
            
            // Check all InfoGivers every 2000 ticks
            if (ticksSinceLastUpdate >= UPDATE_INTERVAL)
            {
                ticksSinceLastUpdate = 0;
                EvaluateAllInfoGivers();
            }
        }

        private void EvaluateUrgentInfoGivers()
        {
            var infoGivers = DefDatabase<InfoGiverDef>.AllDefs.Where(ig => ig.isUrgent);
            
            foreach (var infoGiver in infoGivers)
            {
                try
                {
                    float result = EvaluateInfoGiver(infoGiver);
                    lastResults[infoGiver.defName] = result;
                    
                    // Log urgent results with different prefix
                    Log.Message($"[Autonomy-Urgent] {infoGiver.label}: {result:F2}");
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating urgent InfoGiver {infoGiver.defName}: {e.Message}");
                }
            }
        }

        private void EvaluateAllInfoGivers()
        {
            var infoGivers = DefDatabase<InfoGiverDef>.AllDefs;
            
            foreach (var infoGiver in infoGivers)
            {
                try
                {
                    float result = EvaluateInfoGiver(infoGiver);
                    lastResults[infoGiver.defName] = result;
                    
                    // Log the result with appropriate prefix
                    string prefix = infoGiver.isUrgent ? "[Autonomy-Urgent]" : "[Autonomy]";
                    Log.Message($"{prefix} {infoGiver.label}: {result:F2} ({infoGiver.description})");
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating InfoGiver {infoGiver.defName}: {e.Message}");
                }
            }
        }

        private float EvaluateInfoGiver(InfoGiverDef def)
        {
            float result = 0f;
            
            switch (def.sourceType)
            {
                case InfoSourceType.itemCount:
                    result = EvaluateItemCount(def);
                    break;
                
                case InfoSourceType.pawnCount:
                    result = EvaluatePawnCount(def);
                    break;
                    
                case InfoSourceType.pawnStat:
                    result = EvaluatePawnStat(def);
                    break;
                    
                case InfoSourceType.pawnNeed:
                    result = EvaluatePawnNeed(def);
                    break;
                    
                case InfoSourceType.constructionCount:
                    result = EvaluateConstructionCount(def);
                    break;
                    
                case InfoSourceType.mapCondition:
                    result = EvaluateMapCondition(def);
                    break;
                    
                case InfoSourceType.weather:
                    result = EvaluateWeather(def);
                    break;
                    
                default:
                    Log.Warning($"[Autonomy] Unknown sourceType {def.sourceType} for InfoGiver {def.defName}");
                    return 0f;
            }
            
            // Handle localized and individualized data collection for applicable source types
            if (def.isLocalizable && CanBeLocalized(def.sourceType))
            {
                CollectLocalizedData(def);
            }
            
            if (def.isIndividualizable && CanBeIndividualized(def.sourceType))
            {
                CollectIndividualizedData(def);
            }
            
            return result;
        }
        
        private bool CanBeLocalized(InfoSourceType sourceType)
        {
            // These source types can be tracked by location
            return sourceType == InfoSourceType.itemCount || 
                   sourceType == InfoSourceType.pawnCount ||
                   sourceType == InfoSourceType.constructionCount;
        }
        
        private bool CanBeIndividualized(InfoSourceType sourceType)
        {
            // These source types can be tracked by individual pawn
            return sourceType == InfoSourceType.pawnStat || 
                   sourceType == InfoSourceType.pawnNeed;
        }

        private float EvaluateItemCount(InfoGiverDef def)
        {
            var items = new List<Thing>();
            
            // Collect items based on targeting
            if (!def.targetItems.NullOrEmpty())
            {
                foreach (string itemDefName in def.targetItems)
                {
                    var thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(itemDefName);
                    if (thingDef != null)
                    {
                        items.AddRange(map.listerThings.ThingsOfDef(thingDef));
                    }
                }
            }
            
            if (!def.targetCategories.NullOrEmpty())
            {
                foreach (string categoryDefName in def.targetCategories)
                {
                    var categoryDef = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(categoryDefName);
                    if (categoryDef != null)
                    {
                        var thingDefs = DefDatabase<ThingDef>.AllDefs.Where(td => 
                            td.thingCategories != null && td.thingCategories.Contains(categoryDef));
                        
                        foreach (var thingDef in thingDefs)
                        {
                            items.AddRange(map.listerThings.ThingsOfDef(thingDef));
                        }
                    }
                }
            }

            // Apply filters
            items = ApplyItemFilters(items, def.filters);

            // Calculate result based on calculation type
            var values = items.Select(item => (float)item.stackCount).ToList();
            return CalculateResult(values, def.calculation);
        }

        private List<Thing> ApplyItemFilters(List<Thing> items, InfoFilters filters)
        {
            if (filters == null) return items;

            var filtered = items.AsEnumerable();

            // Filter by stockpile only
            if (filters.stockpileOnly)
            {
                filtered = filtered.Where(item => 
                {
                    var zone = map.zoneManager.ZoneAt(item.Position) as Zone_Stockpile;
                    return zone != null;
                });
            }

            // Filter out forbidden items
            if (filters.excludeForbidden)
            {
                filtered = filtered.Where(item => !item.IsForbidden(Faction.OfPlayer));
            }

            return filtered.ToList();
        }

        private float EvaluatePawnCount(InfoGiverDef def)
        {
            var pawns = new List<Pawn>();
            
            // Start with all pawns on the map
            var allPawns = map.mapPawns.AllPawns;
            
            // Apply basic pawn type filters
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            // Apply hediff filters if specified
            if (def.filters?.hediffs != null && def.filters.hediffs.Count > 0)
            {
                pawns = ApplyHediffFilters(pawns, def.filters.hediffs);
            }
            
            // Calculate result - for pawnCount, we typically just count
            var values = pawns.Select(p => 1f).ToList();
            return CalculateResult(values, def.calculation);
        }

        private List<Pawn> ApplyPawnTypeFilters(IEnumerable<Pawn> allPawns, InfoFilters filters)
        {
            if (filters == null) return allPawns.ToList();
            
            var filtered = allPawns.AsEnumerable();
            
            // Apply inclusion filters
            if (!filters.include.NullOrEmpty())
            {
                filtered = filtered.Where(pawn => 
                {
                    foreach (string filter in filters.include)
                    {
                        switch (filter.ToLower())
                        {
                            case "player":
                                if (pawn.Faction == Faction.OfPlayer && pawn.IsColonist) return true;
                                break;
                            case "prisoner":
                                if (pawn.IsPrisoner) return true;
                                break;
                            case "guest":
                                if (pawn.guest != null && pawn.guest.GuestStatus == GuestStatus.Guest) return true;
                                break;
                            case "animal":
                                if (pawn.AnimalOrWildMan() && pawn.Faction == Faction.OfPlayer) return true;
                                break;
                            case "hostile":
                                if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer)) return true;
                                break;
                        }
                    }
                    return false;
                });
            }
            
            // Apply exclusion filters
            if (!filters.exclude.NullOrEmpty())
            {
                filtered = filtered.Where(pawn => 
                {
                    foreach (string filter in filters.exclude)
                    {
                        switch (filter.ToLower())
                        {
                            case "dead":
                                if (pawn.Dead) return false;
                                break;
                            case "downed":
                                if (pawn.Downed) return false;
                                break;
                            case "guests":
                                if (pawn.guest != null && pawn.guest.GuestStatus == GuestStatus.Guest) return false;
                                break;
                        }
                    }
                    return true;
                });
            }
            
            return filtered.ToList();
        }

        private List<Pawn> ApplyHediffFilters(List<Pawn> pawns, List<HediffFilter> hediffFilters)
        {
            return pawns.Where(pawn => 
            {
                // Pawn must match at least one hediff filter to be included
                return hediffFilters.Any(filter => PawnMatchesHediffFilter(pawn, filter));
            }).ToList();
        }

        private bool PawnMatchesHediffFilter(Pawn pawn, HediffFilter filter)
        {
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            
            foreach (var hediff in hediffs)
            {
                // Check hediff class
                if (!filter.hediffClass.NullOrEmpty())
                {
                    bool classMatches = false;
                    switch (filter.hediffClass)
                    {
                        case "Hediff_Injury":
                            classMatches = hediff is Hediff_Injury;
                            break;
                        case "Hediff_MissingPart":
                            classMatches = hediff is Hediff_MissingPart;
                            break;
                        case "HediffWithComps":
                            classMatches = hediff is HediffWithComps;
                            break;
                        default:
                            // Try to match by type name
                            classMatches = hediff.GetType().Name == filter.hediffClass;
                            break;
                    }
                    if (!classMatches) continue;
                }
                
                // Check tendable
                if (filter.tendable.HasValue)
                {
                    if (hediff.TendableNow() != filter.tendable.Value) continue;
                }
                
                // Check if tended
                if (filter.hediffTended.HasValue)
                {
                    bool isTended = hediff.IsTended();
                    if (isTended != filter.hediffTended.Value) continue;
                }
                
                // Check severity if specified
                if (!filter.severity.NullOrEmpty())
                {
                    if (!EvaluateComparison(hediff.Severity, filter.severity)) continue;
                }
                
                // Check severity range if specified
                if (!filter.severityRange.NullOrEmpty())
                {
                    if (!EvaluateSeverityRange(hediff.Severity, filter.severityRange)) continue;
                }
                
                // If we get here, this hediff matches all criteria
                return true;
            }
            
            return false;
        }

        private bool EvaluateComparison(float value, string comparison)
        {
            if (comparison.NullOrEmpty()) return true;
            
            // Parse comparison operators like ">0.5", "<=0.8", "=1.0"
            if (comparison.StartsWith(">="))
            {
                if (float.TryParse(comparison.Substring(2), out float threshold))
                    return value >= threshold;
            }
            else if (comparison.StartsWith("<="))
            {
                if (float.TryParse(comparison.Substring(2), out float threshold))
                    return value <= threshold;
            }
            else if (comparison.StartsWith(">"))
            {
                if (float.TryParse(comparison.Substring(1), out float threshold))
                    return value > threshold;
            }
            else if (comparison.StartsWith("<"))
            {
                if (float.TryParse(comparison.Substring(1), out float threshold))
                    return value < threshold;
            }
            else if (comparison.StartsWith("="))
            {
                if (float.TryParse(comparison.Substring(1), out float threshold))
                    return Math.Abs(value - threshold) < 0.01f; // Close enough for floats
            }
            else
            {
                // Try direct parsing as equality
                if (float.TryParse(comparison, out float threshold))
                    return Math.Abs(value - threshold) < 0.01f;
            }
            
            return true; // Default to true if can't parse
        }

        private bool EvaluateSeverityRange(float value, string rangeStr)
        {
            if (rangeStr.NullOrEmpty()) return true;
            
            // Parse range format like "0.8~1.0"
            if (rangeStr.Contains("~"))
            {
                string[] parts = rangeStr.Split('~');
                if (parts.Length == 2 && 
                    float.TryParse(parts[0], out float min) && 
                    float.TryParse(parts[1], out float max))
                {
                    return value >= min && value <= max;
                }
            }
            else
            {
                // If no ~ found, treat as exact value
                if (float.TryParse(rangeStr, out float exactValue))
                {
                    return Math.Abs(value - exactValue) < 0.01f;
                }
            }
            
            return true; // Default to true if can't parse
        }

        private float EvaluatePawnStat(InfoGiverDef def)
        {
            if (def.targetStat.NullOrEmpty())
            {
                Log.Warning($"[Autonomy] PawnStat InfoGiver {def.defName} missing targetStat");
                return 0f;
            }

            var pawns = new List<Pawn>();
            
            // Start with all pawns on the map
            var allPawns = map.mapPawns.AllPawns;
            
            // Apply basic pawn type filters
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            // Get stat values from qualified pawns
            var values = new List<float>();
            
            foreach (var pawn in pawns)
            {
                float statValue = GetPawnStatValue(pawn, def.targetStat);
                if (statValue >= 0) // Only include valid stat values
                {
                    values.Add(statValue);
                }
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float GetPawnStatValue(Pawn pawn, string statName)
        {
            // Handle skills (special case)
            if (IsSkillStat(statName))
            {
                return GetPawnSkillLevel(pawn, statName);
            }
            
            // Handle general stats
            var statDef = DefDatabase<StatDef>.GetNamedSilentFail(statName);
            if (statDef != null)
            {
                return pawn.GetStatValue(statDef);
            }
            
            Log.Warning($"[Autonomy] Unknown stat: {statName}");
            return -1f;
        }

        private bool IsSkillStat(string statName)
        {
            // Common skill names
            var skillNames = new string[] 
            {
                "Shooting", "Melee", "Construction", "Mining", "Cooking", 
                "Plants", "Animals", "Crafting", "Artistic", "Medicine", 
                "Social", "Intellectual"
            };
            
            return skillNames.Contains(statName);
        }

        private float GetPawnSkillLevel(Pawn pawn, string skillName)
        {
            if (pawn.skills == null) return -1f;
            
            var skillDef = DefDatabase<SkillDef>.GetNamedSilentFail(skillName);
            if (skillDef == null) return -1f;
            
            var skill = pawn.skills.GetSkill(skillDef);
            if (skill == null) return -1f;
            
            // Return 0 if skill is disabled, otherwise return level
            return skill.TotallyDisabled ? 0f : skill.Level;
        }

        private float EvaluatePawnNeed(InfoGiverDef def)
        {
            if (def.targetNeed.NullOrEmpty())
            {
                Log.Warning($"[Autonomy] PawnNeed InfoGiver {def.defName} missing targetNeed");
                return 0f;
            }

            var pawns = new List<Pawn>();
            
            // Start with all pawns on the map
            var allPawns = map.mapPawns.AllPawns;
            
            // Apply basic pawn type filters
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            // Get need values from qualified pawns
            var values = new List<float>();
            
            foreach (var pawn in pawns)
            {
                float needValue = GetPawnNeedValue(pawn, def.targetNeed);
                if (needValue >= 0) // Only include valid need values
                {
                    values.Add(needValue);
                }
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float GetPawnNeedValue(Pawn pawn, string needName)
        {
            if (pawn.needs == null) return -1f;
            
            // Get the need def by name
            var needDef = DefDatabase<NeedDef>.GetNamedSilentFail(needName);
            if (needDef == null)
            {
                Log.Warning($"[Autonomy] Unknown need: {needName}");
                return -1f;
            }
            
            // Get the need from the pawn
            var need = pawn.needs.TryGetNeed(needDef);
            if (need == null) return -1f;
            
            // Return the need level (0.0 to 1.0)
            return need.CurLevel;
        }

        private float EvaluateConstructionCount(InfoGiverDef def)
        {
            var values = new List<float>();
            
            // Get all blueprints and frames on the map
            var allThings = map.listerThings.AllThings;
            var constructionItems = allThings
                .Where(t => t is Blueprint_Build || t is Frame)
                .ToList();
                
            if (!constructionItems.Any())
            {
                return CalculateResult(values, def.calculation); // Return 0 if no constructions
            }
            
            foreach (var item in constructionItems)
            {
                if (item is Blueprint_Build blueprint)
                {
                    // Count material requirements for this construction
                    float materialCount = CountConstructionMaterials(blueprint, def);
                    if (materialCount > 0)
                    {
                        values.Add(materialCount);
                    }
                }
                else if (item is Frame frame)
                {
                    // Count remaining materials needed for frame
                    float materialCount = CountFrameMaterials(frame, def);
                    if (materialCount > 0)
                    {
                        values.Add(materialCount);
                    }
                }
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float CountConstructionMaterials(Blueprint_Build blueprint, InfoGiverDef def)
        {
            if (blueprint.def.entityDefToBuild == null) return 0f;
            
            var buildingDef = blueprint.def.entityDefToBuild as ThingDef;
            if (buildingDef == null || buildingDef.costList == null) return 0f;
            
            float totalCount = 0f;
            
            foreach (var cost in buildingDef.costList)
            {
                if (IsTargetMaterial(cost.thingDef, def))
                {
                    totalCount += cost.count;
                }
            }
            
            return totalCount;
        }

        private float CountFrameMaterials(Frame frame, InfoGiverDef def)
        {
            if (frame.def.entityDefToBuild == null) return 0f;
            
            var buildingDef = frame.def.entityDefToBuild as ThingDef;
            if (buildingDef == null || buildingDef.costList == null) return 0f;
            
            float remainingCount = 0f;
            
            foreach (var cost in buildingDef.costList)
            {
                if (IsTargetMaterial(cost.thingDef, def))
                {
                    // Use work progress as a proxy for material completion
                    float workRemaining = frame.WorkToBuild - frame.workDone;
                    float totalWork = frame.WorkToBuild;
                    float completionRatio = totalWork > 0 ? workRemaining / totalWork : 0f;
                    
                    int remaining = (int)Math.Round(cost.count * completionRatio);
                    remainingCount += remaining;
                }
            }
            
            return remainingCount;
        }

        private bool IsTargetMaterial(ThingDef materialDef, InfoGiverDef def)
        {
            // Check if material matches target items
            if (def.targetItems != null && def.targetItems.Count > 0)
            {
                return def.targetItems.Contains(materialDef.defName);
            }
            
            // Check if material matches target categories
            if (def.targetCategories != null && def.targetCategories.Count > 0)
            {
                return def.targetCategories.Any(cat => materialDef.IsWithinCategory(ThingCategoryDef.Named(cat)));
            }
            
            // If no filters specified, include all materials
            return true;
        }

        private float EvaluateMapCondition(InfoGiverDef def)
        {
            if (def.conditions == null || def.conditions.Count == 0)
            {
                Log.Warning($"[Autonomy] MapCondition InfoGiver {def.defName} missing conditions");
                return 0f;
            }
            
            var gameConditionManager = map.gameConditionManager;
            if (gameConditionManager == null) return 0f;
            
            var values = new List<float>();
            
            foreach (var targetCondition in def.conditions)
            {
                float conditionValue = GetMapConditionValue(gameConditionManager, targetCondition);
                if (conditionValue > 0)
                {
                    values.Add(conditionValue);
                }
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float GetMapConditionValue(GameConditionManager conditionManager, MapCondition targetCondition)
        {
            if (targetCondition.type.NullOrEmpty())
            {
                return 0f;
            }
            
            // Get the game condition def
            var conditionDef = DefDatabase<GameConditionDef>.GetNamedSilentFail(targetCondition.type);
            if (conditionDef == null)
            {
                Log.Warning($"[Autonomy] Unknown map condition: {targetCondition.type}");
                return 0f;
            }
            
            // Check if the condition is currently active
            var activeCondition = conditionManager.GetActiveCondition(conditionDef);
            if (activeCondition == null)
            {
                return 0f; // Condition is not active
            }
            
            // Calculate condition intensity/value
            float baseValue = 1f; // Basic "condition is active" value
            
            // Apply weight modifier
            if (targetCondition.weight > 0)
            {
                baseValue *= targetCondition.weight;
            }
            
            // Some conditions have additional range/intensity considerations
            if (targetCondition.range > 0)
            {
                // For range-based conditions, we could add distance calculations
                // but for now, just use range as an additional multiplier
                baseValue *= targetCondition.range;
            }
            
            return baseValue;
        }

        private float EvaluateWeather(InfoGiverDef def)
        {
            if (def.weatherProperty.NullOrEmpty())
            {
                Log.Warning($"[Autonomy] Weather InfoGiver {def.defName} missing weatherProperty");
                return 0f;
            }
            
            var weatherManager = map.weatherManager;
            if (weatherManager == null) return 0f;
            
            // Get current weather value based on target weather property
            float weatherValue = GetWeatherValue(weatherManager, def.weatherProperty);
            
            // Weather evaluations typically return single values, not collections
            var values = new List<float> { weatherValue };
            
            return CalculateResult(values, def.calculation);
        }

        private float GetWeatherValue(WeatherManager weatherManager, string weatherTarget)
        {
            switch (weatherTarget.ToLower())
            {
                case "temperature":
                    return weatherManager.curWeather?.temperatureRange.Average ?? 0f;
                    
                case "windspeed":
                    return weatherManager.curWeather?.windSpeedFactor ?? 0f;
                    
                case "rainfallrate":
                    return weatherManager.RainRate;
                    
                case "snowfallrate":
                    return weatherManager.SnowRate;
                    
                case "visibility":
                    // Visibility is affected by weather conditions
                    var weather = weatherManager.curWeather;
                    if (weather == null) return 1f;
                    
                    // Estimate visibility based on weather effects
                    float visibility = 1f;
                    if (weather.rainRate > 0) visibility -= weather.rainRate * 0.3f;
                    if (weather.snowRate > 0) visibility -= weather.snowRate * 0.4f;
                    return Math.Max(0f, visibility);
                    
                case "weatherintensity":
                    // Overall weather intensity based on rain/snow rates
                    return Math.Max(weatherManager.RainRate, weatherManager.SnowRate);
                    
                default:
                    // Try to get weather def by name
                    var weatherDef = DefDatabase<WeatherDef>.GetNamedSilentFail(weatherTarget);
                    if (weatherDef != null)
                    {
                        // Return 1.0 if current weather matches, 0.0 otherwise
                        return weatherManager.curWeather == weatherDef ? 1f : 0f;
                    }
                    
                    Log.Warning($"[Autonomy] Unknown weather target: {weatherTarget}");
                    return 0f;
            }
        }

        private float CalculateResult(List<float> values, CalculationType calculation)
        {
            if (values.Count == 0) return 0f;

            switch (calculation)
            {
                case CalculationType.Sum:
                    return values.Sum();
                    
                case CalculationType.Avg:
                    return values.Average();
                    
                case CalculationType.Max:
                    return values.Max();
                    
                case CalculationType.Min:
                    return values.Min();
                    
                case CalculationType.Count:
                    return values.Count;
                    
                case CalculationType.Flat:
                    return values.FirstOrDefault();
                    
                default:
                    Log.Warning($"[Autonomy] Unsupported calculation type: {calculation}");
                    return values.Sum();
            }
        }

        /// <summary>
        /// Public method to get the last calculated result for an InfoGiver
        /// </summary>
        public float GetInfoGiverResult(string defName)
        {
            return lastResults.TryGetValue(defName, out float result) ? result : 0f;
        }

        /// <summary>
        /// Collect localized data for InfoGivers that support it
        /// </summary>
        private void CollectLocalizedData(InfoGiverDef def)
        {
            if (!locationalData.ContainsKey(def.defName))
            {
                locationalData[def.defName] = new LocationalData();
            }
            
            var locData = locationalData[def.defName];
            locData.roomData.Clear();
            locData.cellData.Clear();
            
            switch (def.sourceType)
            {
                case InfoSourceType.itemCount:
                    CollectLocalizedItemData(def, locData);
                    break;
                case InfoSourceType.pawnCount:
                    CollectLocalizedPawnData(def, locData);
                    break;
                case InfoSourceType.constructionCount:
                    CollectLocalizedConstructionData(def, locData);
                    break;
            }
            
            locData.globalValue = lastResults.TryGetValue(def.defName, out float globalVal) ? globalVal : 0f;
        }

        /// <summary>
        /// Collect individualized data for InfoGivers that support it
        /// </summary>
        private void CollectIndividualizedData(InfoGiverDef def)
        {
            if (!individualData.ContainsKey(def.defName))
            {
                individualData[def.defName] = new IndividualData();
            }
            
            var indData = individualData[def.defName];
            indData.pawnValues.Clear();
            indData.calculationType = def.calculation;
            
            switch (def.sourceType)
            {
                case InfoSourceType.pawnStat:
                    CollectIndividualPawnStatData(def, indData);
                    break;
                case InfoSourceType.pawnNeed:
                    CollectIndividualPawnNeedData(def, indData);
                    break;
            }
            
            indData.RecalculateGlobalValue();
        }

        private void CollectLocalizedItemData(InfoGiverDef def, LocationalData locData)
        {
            var items = new List<Thing>();
            
            // Collect items based on targeting
            if (!def.targetItems.NullOrEmpty())
            {
                foreach (string itemDefName in def.targetItems)
                {
                    var thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(itemDefName);
                    if (thingDef != null)
                    {
                        items.AddRange(map.listerThings.ThingsOfDef(thingDef));
                    }
                }
            }
            
            if (!def.targetCategories.NullOrEmpty())
            {
                foreach (string categoryDefName in def.targetCategories)
                {
                    var categoryDef = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(categoryDefName);
                    if (categoryDef != null)
                    {
                        var thingDefs = DefDatabase<ThingDef>.AllDefs.Where(td => 
                            td.thingCategories != null && td.thingCategories.Contains(categoryDef));
                        
                        foreach (var thingDef in thingDefs)
                        {
                            items.AddRange(map.listerThings.ThingsOfDef(thingDef));
                        }
                    }
                }
            }
            
            // Apply filters
            items = ApplyItemFilters(items, def.filters);
            
            // Group by room and calculate values, ignoring "None" rooms
            var roomGroups = items.GroupBy(item => 
            {
                Room room = map.regionGrid.GetValidRegionAt(item.Position)?.Room;
                if (ShouldIgnoreRoom(room))
                {
                    return -1; // Treat as outside if room should be ignored
                }
                return room?.ID ?? -1; // -1 for outside
            });
            
            foreach (var group in roomGroups)
            {
                var roomItems = group.ToList();
                var values = roomItems.Select(item => (float)item.stackCount).ToList();
                float roomValue = CalculateResult(values, def.calculation);
                locData.roomData[group.Key] = roomValue;
            }
        }

        private void CollectLocalizedPawnData(InfoGiverDef def, LocationalData locData)
        {
            var pawns = new List<Pawn>();
            var allPawns = map.mapPawns.AllPawns;
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            if (def.filters?.hediffs != null && def.filters.hediffs.Count > 0)
            {
                pawns = ApplyHediffFilters(pawns, def.filters.hediffs);
            }
            
            // Group by room and calculate values, ignoring "None" rooms
            var roomGroups = pawns.GroupBy(pawn => 
            {
                Room room = map.regionGrid.GetValidRegionAt(pawn.Position)?.Room;
                if (ShouldIgnoreRoom(room))
                {
                    return -1; // Treat as outside if room should be ignored
                }
                return room?.ID ?? -1; // -1 for outside
            });
            
            foreach (var group in roomGroups)
            {
                var roomPawns = group.ToList();
                var values = roomPawns.Select(p => 1f).ToList();
                float roomValue = CalculateResult(values, def.calculation);
                locData.roomData[group.Key] = roomValue;
            }
        }

        private void CollectLocalizedConstructionData(InfoGiverDef def, LocationalData locData)
        {
            var allThings = map.listerThings.AllThings;
            var constructionItems = allThings
                .Where(t => t is Blueprint_Build || t is Frame)
                .ToList();
                
            // Group by room and calculate values, ignoring "None" rooms
            var roomGroups = constructionItems.GroupBy(item => 
            {
                Room room = map.regionGrid.GetValidRegionAt(item.Position)?.Room;
                if (ShouldIgnoreRoom(room))
                {
                    return -1; // Treat as outside if room should be ignored
                }
                return room?.ID ?? -1; // -1 for outside
            });
            
            foreach (var group in roomGroups)
            {
                var roomItems = group.ToList();
                var values = new List<float>();
                
                foreach (var item in roomItems)
                {
                    if (item is Blueprint_Build blueprint)
                    {
                        float materialCount = CountConstructionMaterials(blueprint, def);
                        if (materialCount > 0) values.Add(materialCount);
                    }
                    else if (item is Frame frame)
                    {
                        float materialCount = CountFrameMaterials(frame, def);
                        if (materialCount > 0) values.Add(materialCount);
                    }
                }
                
                if (values.Count > 0)
                {
                    float roomValue = CalculateResult(values, def.calculation);
                    locData.roomData[group.Key] = roomValue;
                }
            }
        }

        private void CollectIndividualPawnStatData(InfoGiverDef def, IndividualData indData)
        {
            if (def.targetStat.NullOrEmpty())
            {
                Log.Warning($"[Autonomy] PawnStat InfoGiver {def.defName} missing targetStat");
                return;
            }

            var pawns = new List<Pawn>();
            var allPawns = map.mapPawns.AllPawns;
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            foreach (var pawn in pawns)
            {
                float statValue = GetPawnStatValue(pawn, def.targetStat);
                if (statValue >= 0) // Only include valid stat values
                {
                    indData.SetPawnValue(pawn, statValue);
                }
            }
        }

        private void CollectIndividualPawnNeedData(InfoGiverDef def, IndividualData indData)
        {
            if (def.targetNeed.NullOrEmpty())
            {
                Log.Warning($"[Autonomy] PawnNeed InfoGiver {def.defName} missing targetNeed");
                return;
            }

            var pawns = new List<Pawn>();
            var allPawns = map.mapPawns.AllPawns;
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            foreach (var pawn in pawns)
            {
                float needValue = GetPawnNeedValue(pawn, def.targetNeed);
                if (needValue >= 0) // Only include valid need values
                {
                    indData.SetPawnValue(pawn, needValue);
                }
            }
        }

        /// <summary>
        /// Check if a room should be ignored for localized data collection
        /// </summary>
        private bool ShouldIgnoreRoom(Room room)
        {
            // Ignore null rooms (outside areas are handled separately with room ID -1)
            if (room == null) return false;
            
            // Ignore rooms with "None" role or empty/meaningless spaces
            if (room.Role == null) return true;
            
            string roleLabel = room.Role.LabelCap;
            return roleLabel == "None" || roleLabel.ToLower() == "none";
        }
    }
}