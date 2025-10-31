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
        private int ticksSinceLastUpdate = 0;
        private const int UPDATE_INTERVAL = 2000; // Update every 2000 ticks (~33 seconds)

        public InfoGiverManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            ticksSinceLastUpdate++;
            if (ticksSinceLastUpdate >= UPDATE_INTERVAL)
            {
                ticksSinceLastUpdate = 0;
                EvaluateAllInfoGivers();
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
                    
                    // Log the result for now
                    Log.Message($"[Autonomy] {infoGiver.label}: {result:F2} ({infoGiver.description})");
                }
                catch (Exception e)
                {
                    Log.Error($"[Autonomy] Error evaluating InfoGiver {infoGiver.defName}: {e.Message}");
                }
            }
        }

        private float EvaluateInfoGiver(InfoGiverDef def)
        {
            switch (def.sourceType)
            {
                case InfoSourceType.itemCount:
                    return EvaluateItemCount(def);
                
                case InfoSourceType.pawnCount:
                    return EvaluatePawnCount(def);
                    
                case InfoSourceType.pawnStat:
                    return EvaluatePawnStat(def);
                    
                case InfoSourceType.pawnNeed:
                    return EvaluatePawnNeed(def);
                    
                case InfoSourceType.constructionCount:
                    return EvaluateConstructionCount(def);
                    
                case InfoSourceType.mapCondition:
                    return EvaluateMapCondition(def);
                    
                case InfoSourceType.weather:
                    return EvaluateWeather(def);
                    
                default:
                    Log.Warning($"[Autonomy] Unknown sourceType {def.sourceType} for InfoGiver {def.defName}");
                    return 0f;
            }
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
                case CalculationType.sum:
                    return values.Sum();
                    
                case CalculationType.avg:
                    return values.Average();
                    
                case CalculationType.max:
                    return values.Max();
                    
                case CalculationType.min:
                    return values.Min();
                    
                case CalculationType.count:
                    return values.Count;
                    
                case CalculationType.flat:
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
    }
}