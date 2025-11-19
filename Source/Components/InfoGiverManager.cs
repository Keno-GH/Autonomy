using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using System.Reflection;

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
        private const int UPDATE_INTERVAL = 100; // Update every 100 ticks (~1.7 seconds) - for testing
        private const int URGENT_UPDATE_INTERVAL = 10; // Urgent updates every 10 ticks (~0.17 seconds) - for testing
    private static readonly HashSet<string> loggedThingFilterWarnings = new HashSet<string>();

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
                    if (context.requestNormalizedDistance)
                    {
                        return indData.GetNormalizedDistanceFromGlobal(context.requestingPawn);
                    }
                    else if (context.requestDistanceFromGlobal)
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
                requestNormalizedDistance = condition.requestNormalizedDistance,
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
                    
                    // InfoGiver logging disabled - now logging PriorityGivers instead
                    // Log.Message($"[Autonomy-Urgent] {infoGiver.label}: {result:F2}");
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
                    
                    // InfoGiver logging disabled - now logging PriorityGivers instead
                    // string prefix = infoGiver.isUrgent ? "[Autonomy-Urgent]" : "[Autonomy]";
                    // Log.Message($"{prefix} {infoGiver.label}: {result:F2} ({infoGiver.description})");
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
                    
                case InfoSourceType.geneCount:
                    result = EvaluateGeneCount(def);
                    break;
                    
                case InfoSourceType.hediffCount:
                    result = EvaluateHediffCount(def);
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
                   sourceType == InfoSourceType.pawnNeed ||
                   sourceType == InfoSourceType.geneCount ||
                   sourceType == InfoSourceType.hediffCount;
        }

        private float EvaluateItemCount(InfoGiverDef def)
        {
            var items = new HashSet<Thing>();
            
            // Collect items based on targeting
            if (!def.targetItems.NullOrEmpty())
            {
                foreach (string itemDefName in def.targetItems)
                {
                    var thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(itemDefName);
                    if (thingDef != null)
                    {
                        CollectThingsOfDef(items, thingDef);
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
                        foreach (var thingDef in GetThingDefsForCategory(categoryDef))
                        {
                            CollectThingsOfDef(items, thingDef);
                        }
                    }
                }
            }
            
            if (!def.targetThingClasses.NullOrEmpty())
            {
                foreach (string thingClassName in def.targetThingClasses)
                {
                    // Find all ThingDefs whose thingClass matches the target class name
                    var thingDefs = DefDatabase<ThingDef>.AllDefs.Where(td => 
                        td.thingClass != null && 
                        (td.thingClass.Name == thingClassName || 
                         td.thingClass.Name.EndsWith(thingClassName) ||
                         IsSubclassOfType(td.thingClass, thingClassName)));
                    
                    foreach (var thingDef in thingDefs)
                    {
                        CollectThingsOfDef(items, thingDef);
                    }
                }
            }

            // Apply filters
            var filteredItems = ApplyItemFilters(items.ToList(), def.filters);

            // Calculate result based on calculation type
            var values = filteredItems.Select(item => (float)item.stackCount).ToList();
            return CalculateResult(values, def.calculation);
        }

        /// <summary>
        /// Safely checks if a type is a subclass of a named type
        /// </summary>
        private bool IsSubclassOfType(Type type, string typeName)
        {
            try
            {
                Type currentType = type.BaseType;
                while (currentType != null)
                {
                    if (currentType.Name == typeName)
                        return true;
                    currentType = currentType.BaseType;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private IEnumerable<ThingDef> GetThingDefsForCategory(ThingCategoryDef rootCategory)
        {
            if (rootCategory == null)
            {
                yield break;
            }

            var visitedCategories = new HashSet<ThingCategoryDef>();
            var pending = new List<ThingCategoryDef> { rootCategory };

            var yieldedDefs = new HashSet<ThingDef>();

            while (pending.Count > 0)
            {
                int lastIndex = pending.Count - 1;
                ThingCategoryDef current = pending[lastIndex];
                pending.RemoveAt(lastIndex);
                if (current == null || !visitedCategories.Add(current))
                {
                    continue;
                }

                if (!current.childThingDefs.NullOrEmpty())
                {
                    foreach (var thingDef in current.childThingDefs)
                    {
                        if (thingDef != null && yieldedDefs.Add(thingDef))
                        {
                            yield return thingDef;
                        }
                    }
                }

                if (!current.childCategories.NullOrEmpty())
                {
                    foreach (var child in current.childCategories)
                    {
                        if (child != null)
                        {
                            pending.Add(child);
                        }
                    }
                }
            }

            // Fallback: include ThingDefs that explicitly reference the category in thingCategories
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef?.thingCategories != null && thingDef.thingCategories.Contains(rootCategory) && yieldedDefs.Add(thingDef))
                {
                    yield return thingDef;
                }
            }
        }

        private void CollectThingsOfDef(HashSet<Thing> buffer, ThingDef thingDef)
        {
            if (buffer == null || thingDef == null || map?.listerThings == null)
            {
                return;
            }

            var thingsOnMap = map.listerThings.ThingsOfDef(thingDef);
            if (!thingsOnMap.NullOrEmpty())
            {
                buffer.UnionWith(thingsOnMap);
            }
        }

        private List<Thing> ApplyItemFilters(List<Thing> items, InfoFilters filters)
        {
            if (filters == null || items == null)
            {
                return items;
            }

            var filtered = items.AsEnumerable();
            bool usingThingFilterGroups = !filters.includeThingFilters.NullOrEmpty() || !filters.excludeThingFilters.NullOrEmpty();

            if (usingThingFilterGroups)
            {
                if (!filters.includeThingFilters.NullOrEmpty())
                {
                    filtered = filtered.Where(item => MatchesAnyThingFilterGroup(item, filters.includeThingFilters, defaultValueWhenNoGroups: true));
                }

                if (!filters.excludeThingFilters.NullOrEmpty())
                {
                    filtered = filtered.Where(item => !MatchesAnyThingFilterGroup(item, filters.excludeThingFilters, defaultValueWhenNoGroups: false));
                }
            }

            return filtered.ToList();
        }

        private bool MatchesAnyThingFilterGroup(Thing item, List<List<string>> groups, bool defaultValueWhenNoGroups)
        {
            if (groups.NullOrEmpty())
            {
                return defaultValueWhenNoGroups;
            }

            bool hasValidGroup = false;

            foreach (var group in groups)
            {
                if (group.NullOrEmpty())
                {
                    continue;
                }

                hasValidGroup = true;

                if (ThingMatchesFilterGroup(item, group))
                {
                    return true;
                }
            }

            return hasValidGroup ? false : defaultValueWhenNoGroups;
        }

        private bool ThingMatchesFilterGroup(Thing item, List<string> tags)
        {
            if (tags.NullOrEmpty())
            {
                return true;
            }

            foreach (string tag in tags)
            {
                if (tag.NullOrEmpty())
                {
                    continue;
                }

                if (!ThingMatchesFilterTag(item, tag))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ThingMatchesFilterTag(Thing item, string tag)
        {
            if (item == null || tag.NullOrEmpty())
            {
                return false;
            }

            string normalized = tag.Trim().ToLowerInvariant();

            switch (normalized)
            {
                case "instockpile":
                case "stockpile":
                case "stockpileonly":
                    return ItemInStockpile(item);

                case "homearea":
                case "inhomearea":
                    return ItemInHomeArea(item);

                case "nothomearea":
                    return item.Spawned && !ItemInHomeArea(item);

                case "outside":
                case "outsiderooms":
                case "outdoors":
                    return ItemIsOutside(item);

                case "inside":
                case "indoors":
                    return item.Spawned && !ItemIsOutside(item);

                case "deteriorable":
                case "candeteriorate":
                    return ItemIsDeteriorable(item);

                case "nondeteriorable":
                    return !ItemIsDeteriorable(item);

                case "forbidden":
                    return item.IsForbidden(Faction.OfPlayer);

                case "unforbidden":
                    return !item.IsForbidden(Faction.OfPlayer);

                case "incontainer":
                case "contained":
                case "instorage":
                    return ItemInContainer(item);

                case "notincontainer":
                case "notcontained":
                case "notinstorage":
                    return !ItemInContainer(item);

                default:
                    if (loggedThingFilterWarnings.Add(normalized))
                    {
                        Log.Warning($"[Autonomy] Unknown thing filter tag '{tag}' referenced in InfoGiver filters.");
                    }
                    return false;
            }
        }

        private bool ItemInStockpile(Thing item)
        {
            if (item == null || !item.Spawned)
            {
                return false;
            }

            return map.zoneManager.ZoneAt(item.Position) is Zone_Stockpile;
        }

        private bool ItemInHomeArea(Thing item)
        {
            if (item == null || !item.Spawned)
            {
                return false;
            }

            Area home = map.areaManager?.Home;
            return home != null && home[item.Position];
        }

        private bool ItemIsOutside(Thing item)
        {
            if (item == null || !item.Spawned)
            {
                return false;
            }

            Room room = item.Position.GetRoom(map);
            return room == null || room.PsychologicallyOutdoors || room.OpenRoofCount > 0;
        }

        private bool ItemInContainer(Thing item)
        {
            if (item == null)
            {
                return false;
            }

            if (!item.Spawned)
            {
                return true;
            }

            if (!(item.ParentHolder is Map))
            {
                return true;
            }

            return item.IsInAnyStorage();
        }

        private bool ItemIsDeteriorable(Thing item)
        {
            ThingDef thingDef = item?.def;
            return thingDef != null && thingDef.useHitPoints && thingDef.CanEverDeteriorate && thingDef.statBases.Any(sb => sb.stat == StatDefOf.DeteriorationRate);
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
            
            // NEW: Apply enhanced pawn filters (List<PawnFilter>)
            if (!filters.pawnFilters.NullOrEmpty())
            {
                filtered = filtered.Where(pawn => 
                {
                    // OR logic: pawn passes if it matches ANY PawnFilter
                    foreach (PawnFilter pawnFilter in filters.pawnFilters)
                    {
                        if (PawnMatchesPawnFilter(pawn, pawnFilter))
                            return true;
                    }
                    return false;
                });
            }
            // LEGACY: Apply old string-based inclusion filters (backward compatibility)
            else if (!filters.include.NullOrEmpty())
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
            
            // NEW: Apply enhanced pawn exclusion filters
            if (!filters.excludePawnFilters.NullOrEmpty())
            {
                filtered = filtered.Where(pawn => 
                {
                    // Exclude pawn if it matches ANY exclude filter
                    foreach (PawnFilter excludeFilter in filters.excludePawnFilters)
                    {
                        if (PawnMatchesPawnFilter(pawn, excludeFilter))
                            return false;
                    }
                    return true;
                });
            }
            // LEGACY: Apply old string-based exclusion filters (backward compatibility)
            else if (!filters.exclude.NullOrEmpty())
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

            // Apply capability filters (AND list) - each entry must be a SkillDef name
            if (!filters.capableOf.NullOrEmpty())
            {
                filtered = filtered.Where(pawn =>
                {
                    // Pawns without skill tracking fail the capability check
                    if (pawn.skills == null) return false;

                    foreach (string skillName in filters.capableOf)
                    {
                        var skillDef = DefDatabase<SkillDef>.GetNamedSilentFail(skillName);
                        if (skillDef == null)
                        {
                            // If the skill definition doesn't exist, treat as failure to be safe
                            return false;
                        }

                        var skill = pawn.skills.GetSkill(skillDef);
                        if (skill == null) return false;

                        // If the skill is completely disabled (pawn cannot use it), exclude the pawn
                        if (skill.TotallyDisabled) return false;
                    }

                    // Passed all capability checks
                    return true;
                });
            }
            
            return filtered.ToList();
        }

        /// <summary>
        /// Check if a pawn matches a PawnFilter (all conditions must pass - AND logic)
        /// </summary>
        private bool PawnMatchesPawnFilter(Pawn pawn, PawnFilter filter)
        {
            // Check race filter
            if (!filter.race.NullOrEmpty() && filter.race.ToLower() != "any")
            {
                string raceLower = filter.race.ToLower();
                if (raceLower == "humanlike")
                {
                    if (!pawn.RaceProps.Humanlike) return false;
                }
                else if (raceLower == "animalany")
                {
                    if (!pawn.RaceProps.Animal) return false;
                }
                else
                {
                    // Specific race def name
                    if (pawn.def.defName != filter.race) return false;
                }
            }
            
            // Check faction filter
            if (!filter.faction.NullOrEmpty() && filter.faction.ToLower() != "any")
            {
                string factionLower = filter.faction.ToLower();
                switch (factionLower)
                {
                    case "player":
                        if (pawn.Faction != Faction.OfPlayer) return false;
                        break;
                    case "non_player":
                        if (pawn.Faction == Faction.OfPlayer) return false;
                        break;
                    case "hostile":
                        if (pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer)) return false;
                        break;
                }
            }
            
            // Check status filters (AND logic - all must pass)
            if (!filter.status.NullOrEmpty())
            {
                foreach (string status in filter.status)
                {
                    if (!PawnMatchesStatus(pawn, status))
                        return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Check if a pawn matches a specific status string
        /// </summary>
        private bool PawnMatchesStatus(Pawn pawn, string status)
        {
            switch (status.ToLower())
            {
                case "prisoner":
                    return pawn.IsPrisoner;
                case "guest":
                    return pawn.guest != null && pawn.guest.GuestStatus == GuestStatus.Guest;
                case "dead":
                    return pawn.Dead;
                case "downed":
                    return pawn.Downed;
                case "animal":
                    return pawn.RaceProps.Animal;
                case "colonist":
                    return pawn.IsColonist;
                case "slave":
                    return pawn.IsSlave;
                case "roaming":
                    // Check if animal is a roamer (has RoamMtbDays)
                    return pawn.Roamer;
                case "unenclosed":
                    // Check if animal is not enclosed in a pen
                    return IsAnimalUnenclosed(pawn);
                case "unpenned":
                    // Check if animal needs to be penned (is a roamer AND not enclosed)
                    return pawn.Roamer && IsAnimalUnenclosed(pawn);
                case "selftendenabled":
                    if ((pawn.Faction == Faction.OfPlayer && pawn.IsColonist) || pawn.IsSlave)
                    {
                        return pawn.playerSettings != null && pawn.playerSettings.selfTend;
                    }
                    return false;
                case "selftenddisabled":
                    if ((pawn.Faction == Faction.OfPlayer && pawn.IsColonist) || pawn.IsSlave)
                    {
                        return pawn.playerSettings != null && !pawn.playerSettings.selfTend;
                    }
                    return false;
                default:
                    Log.Warning($"Unknown pawn status filter: {status}");
                    return false;
            }
        }

        /// <summary>
        /// Check if an animal is not enclosed in a pen
        /// Returns true if the animal is either:
        /// 1. Not in any pen district at all
        /// 2. In a pen district but the pen is unenclosed
        /// </summary>
        private bool IsAnimalUnenclosed(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null) return false;
            if (!pawn.RaceProps.Animal) return false;
            
            // Get the district the animal is in
            District district = pawn.Position.GetDistrict(pawn.Map);
            if (district == null) return true; // Not in any district means not enclosed
            
            // Check all pen markers to see if any of them cover this district and are enclosed
            foreach (Building penMarker in pawn.Map.listerBuildings.allBuildingsAnimalPenMarkers)
            {
                CompAnimalPenMarker comp = penMarker.TryGetComp<CompAnimalPenMarker>();
                if (comp != null && !comp.PenState.Unenclosed)
                {
                    // Check if this enclosed pen contains the pawn's district
                    if (comp.PenState.ConnectedRegions != null)
                    {
                        foreach (Region region in comp.PenState.ConnectedRegions)
                        {
                            if (region.District == district)
                            {
                                return false; // Animal is in an enclosed pen
                            }
                        }
                    }
                }
            }
            
            return true; // Not in any enclosed pen
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
                if (filter.tended.HasValue)
                {
                    bool isTended = hediff.IsTended();
                    if (isTended != filter.tended.Value) continue;
                }
                
                // Check infection chance
                if (filter.hasInfectionChance.HasValue)
                {
                    bool hasInfectionChance = false;
                    if (hediff is HediffWithComps hediffWithComps)
                    {
                        var infecterComp = hediffWithComps.TryGetComp<HediffComp_Infecter>();
                        hasInfectionChance = infecterComp != null && infecterComp.Props.infectionChance > 0f;
                    }
                    if (hasInfectionChance != filter.hasInfectionChance.Value) continue;
                }
                
                // Check bleed rate
                if (filter.hasBleedRate.HasValue)
                {
                    bool hasBleedRate = false;
                    // Prefer instance-level bleed rate when available (Hediff_Injury),
                    // otherwise fall back to the HediffDef's injuryProps (for
                    // Hediff_MissingPart and other non-injury hediff types).
                    if (hediff is Hediff_Injury injury)
                    {
                        hasBleedRate = injury.BleedRate > 0f;
                    }
                    else if (hediff.def?.injuryProps != null)
                    {
                        hasBleedRate = hediff.def.injuryProps.bleedRate > 0f;
                    }
                    if (hasBleedRate != filter.hasBleedRate.Value) continue;
                }
                
                // Check immunizable
                if (filter.isImmunizable.HasValue)
                {
                    bool isImmunizable = false;
                    if (hediff is HediffWithComps immunizableHediff)
                    {
                        var immunizableComp = immunizableHediff.TryGetComp<HediffComp_Immunizable>();
                        isImmunizable = immunizableComp != null;
                    }
                    if (isImmunizable != filter.isImmunizable.Value) continue;
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
                try
                {
                    // Some stats can be disabled for certain pawns (e.g. non-humanoids).
                    // Query the StatWorker first to avoid attempting to calculate a disabled stat,
                    // which would log an error inside the RimWorld stat system.
                    if (statDef.Worker != null && statDef.Worker.IsDisabledFor(pawn))
                    {
                        // Return -1 to indicate an invalid/unavailable stat for this pawn
                        return -1f;
                    }

                    return pawn.GetStatValue(statDef);
                }
                catch (Exception e)
                {
                    // Defensive: if something unexpected happens getting the stat, log it and skip the pawn
                    Log.Warning($"[Autonomy] Exception getting stat '{statName}' for pawn {pawn.LabelShort}: {e.Message}");
                    return -1f;
                }
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
            if (def.targetNeedValue.NullOrEmpty())
            {
                def.targetNeedValue = "curLevel"; // Default to current level if not specified
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
                float needValue = GetPawnNeedValue(pawn, def.targetNeed, def.targetNeedValue);
                if (needValue >= 0) // Only include valid need values
                {
                    values.Add(needValue);
                }
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float GetPawnNeedValue(Pawn pawn, string needName, string needValue = "curLevel")
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
            return GetNeedValueByProperty(need, needValue);
        }
        
        private float GetNeedValueByProperty(Need need, string property)
        {
            switch (property.ToLower())
            {
                case "curlevel":
                case "currentlevel":
                    return need.CurLevel;
                case "curlevelpercentage":
                    return need.CurLevelPercentage;
                case "maxlevel":
                case "maxlevelpercentage":
                    return need.MaxLevel;
                case "rate":
                case "currentrate":
                    if (need is Need_Food needFood)
                    {
                        return needFood.FoodFallPerTick * 60f * 2500f;
                    }
                    else if (need is Need_Rest needRest)
                    {
                        return needRest.RestFallPerTick * 60f * 2500f;
                    }
                    else if (need is Need_Joy needJoy)
                    {
                        return (float)(typeof(Need_Joy).GetProperty("FallPerInterval", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(needJoy) ?? 0f) * 60f * 2500f;
                    }
                    else if (need is Need_Mood)
                    {
                        return 0; // Mood does not have a direct rate property
                    }
                    else
                    {
                        float fallPerDay = need.def?.fallPerDay ?? 0f;
                        if (fallPerDay <= 0f)
                        {
                            return 0f; // No fall rate defined
                        }
                        return fallPerDay; // Generic fall rate per day, without modifiers
                    }
                default:
                    Log.Warning($"[Autonomy] Unknown need value property: {property}");
                    return -1f;
            }
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
            // New implementation: support targetValue, targetGameConditions, and targetGameConditionsWithProperties
            
            // Priority 1: Direct property access via targetValue
            if (!def.targetValue.NullOrEmpty())
            {
                return GetMapPropertyValue(def.targetValue);
            }
            
            // Priority 2: Check for specific game conditions (OR logic - any matching)
            if (!def.targetGameConditions.NullOrEmpty())
            {
                return CountActiveGameConditions(def.targetGameConditions);
            }
            
            // Priority 3: Check for game conditions with specific properties (OR logic - any condition with ALL properties)
            if (!def.targetGameConditionsWithProperties.NullOrEmpty())
            {
                return CountGameConditionsWithProperties(def.targetGameConditionsWithProperties);
            }
            
            // Fallback: Old conditions system for backwards compatibility
            if (def.conditions != null && def.conditions.Count > 0)
            {
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
            
            Log.Warning($"[Autonomy] MapCondition InfoGiver {def.defName} has no valid configuration");
            return 0f;
        }
        
        /// <summary>
        /// Get a property value from map, map.mapTemperature, map.weatherManager, etc.
        /// </summary>
        private float GetMapPropertyValue(string propertyPath)
        {
            if (map == null) return 0f;
            
            try
            {
                switch (propertyPath)
                {
                    // Map temperature properties
                    case "OutdoorTemp":
                        return map.mapTemperature?.OutdoorTemp ?? 0f;
                    case "SeasonalTemp":
                        return map.mapTemperature?.SeasonalTemp ?? 0f;
                    
                    // Weather manager properties
                    case "CurMoveSpeedMultiplier":
                        return map.weatherManager?.CurMoveSpeedMultiplier ?? 1f;
                    case "CurWeatherAccuracyMultiplier":
                        return map.weatherManager?.CurWeatherAccuracyMultiplier ?? 1f;
                    case "RainRate":
                        return map.weatherManager?.RainRate ?? 0f;
                    case "SnowRate":
                        return map.weatherManager?.SnowRate ?? 0f;
                    
                    default:
                        Log.Warning($"[Autonomy] Unknown map property: {propertyPath}");
                        return 0f;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Autonomy] Error accessing map property {propertyPath}: {ex.Message}");
                return 0f;
            }
        }
        
        /// <summary>
        /// Count how many of the specified game conditions are currently active (OR logic)
        /// </summary>
        private float CountActiveGameConditions(List<string> conditionDefNames)
        {
            if (map?.gameConditionManager == null) return 0f;
            
            int activeCount = 0;
            
            foreach (var defName in conditionDefNames)
            {
                var conditionDef = DefDatabase<GameConditionDef>.GetNamedSilentFail(defName);
                if (conditionDef != null)
                {
                    var activeCondition = map.gameConditionManager.GetActiveCondition(conditionDef);
                    if (activeCondition != null)
                    {
                        activeCount++;
                    }
                }
            }
            
            return activeCount;
        }
        
        /// <summary>
        /// Count game conditions that have ALL specified properties (OR logic on conditions, AND logic on properties)
        /// </summary>
        private float CountGameConditionsWithProperties(List<string> propertyNames)
        {
            if (map?.gameConditionManager == null) return 0f;
            
            var activeConditions = map.gameConditionManager.ActiveConditions;
            if (activeConditions.NullOrEmpty()) return 0f;
            
            int matchingCount = 0;
            
            foreach (var condition in activeConditions)
            {
                if (condition?.def == null) continue;
                
                bool hasAllProperties = true;
                
                // Check if this condition's def has ALL the required properties
                foreach (var propName in propertyNames)
                {
                    if (!GameConditionHasProperty(condition.def, propName))
                    {
                        hasAllProperties = false;
                        break;
                    }
                }
                
                if (hasAllProperties)
                {
                    matchingCount++;
                }
            }
            
            return matchingCount;
        }
        
        /// <summary>
        /// Check if a GameConditionDef has a specific property using reflection
        /// </summary>
        private bool GameConditionHasProperty(GameConditionDef def, string propertyName)
        {
            if (def == null || propertyName.NullOrEmpty()) return false;
            
            try
            {
                var field = def.GetType().GetField(propertyName);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)field.GetValue(def);
                }
                
                var property = def.GetType().GetProperty(propertyName);
                if (property != null && property.PropertyType == typeof(bool))
                {
                    return (bool)property.GetValue(def);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Autonomy] Error checking property {propertyName} on {def.defName}: {ex.Message}");
            }
            
            return false;
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

        /// <summary>
        /// DEPRECATED: Use mapCondition with targetValue instead
        /// </summary>
        private float EvaluateWeather(InfoGiverDef def)
        {
            Log.Warning($"[Autonomy] Weather InfoGiver {def.defName} uses deprecated sourceType 'weather'. Please use 'mapCondition' with 'targetValue' instead.");
            
            if (def.weatherProperty.NullOrEmpty())
            {
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

        /// <summary>
        /// DEPRECATED: Legacy weather value getter
        /// </summary>
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

        private float EvaluateGeneCount(InfoGiverDef def)
        {
            var pawns = new List<Pawn>();
            
            // Start with all pawns on the map
            var allPawns = map.mapPawns.AllPawns;
            
            // Apply basic pawn type filters
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            // Get gene counts from qualified pawns
            var values = new List<float>();
            
            foreach (var pawn in pawns)
            {
                float geneCount = GetPawnGeneCount(pawn, def);
                values.Add(geneCount);
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float GetPawnGeneCount(Pawn pawn, InfoGiverDef def)
        {
            // Check if pawn has genes
            if (pawn.genes == null) return 0f;
            
            var genes = pawn.genes.GenesListForReading;
            if (genes == null || genes.Count == 0) return 0f;
            
            // If targeting damage factors, return sum of damage factors
            if (!def.targetDamageFactor.NullOrEmpty())
            {
                return GetPawnGeneDamageFactor(pawn, def.targetDamageFactor, def);
            }
            
            // Otherwise, return count of matching genes
            int matchCount = 0;
            
            foreach (var gene in genes)
            {
                if (GeneMatchesFilter(gene, def))
                {
                    matchCount++;
                }
            }
            
            return matchCount;
        }

        private float GetPawnGeneDamageFactor(Pawn pawn, string damageType, InfoGiverDef def)
        {
            if (pawn.genes == null) return 0f;
            
            var genes = pawn.genes.GenesListForReading;
            if (genes == null || genes.Count == 0) return 0f;
            
            // Get the DamageDef for the target damage type
            var damageDef = DefDatabase<DamageDef>.GetNamedSilentFail(damageType);
            if (damageDef == null)
            {
                Log.Warning($"[Autonomy] Unknown damage type: {damageType}");
                return 0f;
            }
            
            float totalFactor = 0f;
            
            foreach (var gene in genes)
            {
                if (gene?.def?.damageFactors == null) continue;
                
                // Check if this gene matches other filters (if any)
                if (!GeneMatchesOtherFilters(gene, def)) continue;
                
                // Check if gene has damage factor for this damage type
                var damageFactor = gene.def.damageFactors.FirstOrDefault(df => df.damageDef == damageDef);
                if (damageFactor != null)
                {
                    // Add the factor value (e.g., 4.0 for tinderskin Flame damage)
                    totalFactor += damageFactor.factor;
                }
            }
            
            return totalFactor;
        }

        private bool GeneMatchesFilter(Gene gene, InfoGiverDef def)
        {
            if (gene?.def == null) return false;
            
            var geneDef = gene.def;
            bool matches = false;
            
            // Check if we have any targeting criteria
            bool hasCriteria = false;
            
            // Check damage factor targeting
            if (!def.targetDamageFactor.NullOrEmpty())
            {
                hasCriteria = true;
                if (geneDef.damageFactors != null)
                {
                    var damageDef = DefDatabase<DamageDef>.GetNamedSilentFail(def.targetDamageFactor);
                    if (damageDef != null)
                    {
                        if (geneDef.damageFactors.Any(df => df.damageDef == damageDef))
                        {
                            matches = true;
                        }
                    }
                }
            }
            
            // Check target genes by defName
            if (!def.targetGenes.NullOrEmpty())
            {
                hasCriteria = true;
                if (def.targetGenes.Contains(geneDef.defName))
                {
                    matches = true;
                }
            }
            
            // Check target gene classes
            if (!def.targetGeneClasses.NullOrEmpty())
            {
                hasCriteria = true;
                var geneClass = gene.GetType().Name;
                if (def.targetGeneClasses.Any(className => 
                    geneClass == className || 
                    geneClass.EndsWith(className) ||
                    IsSubclassOfTypeName(gene.GetType(), className)))
                {
                    matches = true;
                }
            }
            
            // Check mentalBreakDef
            if (!def.targetMentalBreakDef.NullOrEmpty())
            {
                hasCriteria = true;
                if (geneDef.mentalBreakDef != null && geneDef.mentalBreakDef.defName == def.targetMentalBreakDef)
                {
                    matches = true;
                }
            }
            
            // Check biostat filters
            if (!def.filterBiostatMet.NullOrEmpty())
            {
                hasCriteria = true;
                if (!EvaluateComparison(geneDef.biostatMet, def.filterBiostatMet))
                {
                    return false; // Failed filter
                }
            }
            
            if (!def.filterBiostatCpx.NullOrEmpty())
            {
                hasCriteria = true;
                if (!EvaluateComparison(geneDef.biostatCpx, def.filterBiostatCpx))
                {
                    return false; // Failed filter
                }
            }
            
            if (!def.filterBiostatArc.NullOrEmpty())
            {
                hasCriteria = true;
                if (!EvaluateComparison(geneDef.biostatArc, def.filterBiostatArc))
                {
                    return false; // Failed filter
                }
            }
            
            // If we had targeting criteria (genes/classes/mentalBreak/damageFactor), return matches result
            // Otherwise if we only had biostat filters, consider it a match if it passed all filters
            if (!def.targetGenes.NullOrEmpty() || !def.targetGeneClasses.NullOrEmpty() || 
                !def.targetMentalBreakDef.NullOrEmpty() || !def.targetDamageFactor.NullOrEmpty())
            {
                return matches;
            }
            else if (hasCriteria)
            {
                // Only biostat filters were specified, and we passed them all
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Helper method to check non-damage-factor filters for genes (used when summing damage factors)
        /// </summary>
        private bool GeneMatchesOtherFilters(Gene gene, InfoGiverDef def)
        {
            if (gene?.def == null) return false;
            
            var geneDef = gene.def;
            
            // Check target genes by defName
            if (!def.targetGenes.NullOrEmpty())
            {
                if (!def.targetGenes.Contains(geneDef.defName))
                {
                    return false;
                }
            }
            
            // Check target gene classes
            if (!def.targetGeneClasses.NullOrEmpty())
            {
                var geneClass = gene.GetType().Name;
                if (!def.targetGeneClasses.Any(className => 
                    geneClass == className || 
                    geneClass.EndsWith(className) ||
                    IsSubclassOfTypeName(gene.GetType(), className)))
                {
                    return false;
                }
            }
            
            // Check mentalBreakDef
            if (!def.targetMentalBreakDef.NullOrEmpty())
            {
                if (geneDef.mentalBreakDef == null || geneDef.mentalBreakDef.defName != def.targetMentalBreakDef)
                {
                    return false;
                }
            }
            
            // Check biostat filters
            if (!def.filterBiostatMet.NullOrEmpty())
            {
                if (!EvaluateComparison(geneDef.biostatMet, def.filterBiostatMet))
                {
                    return false;
                }
            }
            
            if (!def.filterBiostatCpx.NullOrEmpty())
            {
                if (!EvaluateComparison(geneDef.biostatCpx, def.filterBiostatCpx))
                {
                    return false;
                }
            }
            
            if (!def.filterBiostatArc.NullOrEmpty())
            {
                if (!EvaluateComparison(geneDef.biostatArc, def.filterBiostatArc))
                {
                    return false;
                }
            }
            
            // If no other filters were specified, return true (all genes match)
            // Otherwise, we passed all the filters
            return true;
        }

        private bool IsSubclassOfTypeName(Type type, string typeName)
        {
            try
            {
                Type currentType = type.BaseType;
                while (currentType != null)
                {
                    if (currentType.Name == typeName)
                        return true;
                    currentType = currentType.BaseType;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private float EvaluateHediffCount(InfoGiverDef def)
        {
            var pawns = new List<Pawn>();
            
            // Start with all pawns on the map
            var allPawns = map.mapPawns.AllPawns;
            
            // Apply basic pawn type filters
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            // Get hediff values from qualified pawns
            var values = new List<float>();
            
            foreach (var pawn in pawns)
            {
                float hediffValue = GetPawnHediffValue(pawn, def);
                values.Add(hediffValue);
            }
            
            return CalculateResult(values, def.calculation);
        }

        private float GetPawnHediffValue(Pawn pawn, InfoGiverDef def)
        {
            // Check if pawn has health/hediffs
            if (pawn.health?.hediffSet?.hediffs == null) return 0f;
            
            var hediffs = pawn.health.hediffSet.hediffs;
            if (hediffs.Count == 0) return 0f;
            
            // Check if we have any hediff filters
            if (def.filters?.hediffs == null || def.filters.hediffs.Count == 0) return 0f;
            
            // Property to extract (default is count)
            string property = def.hediffProperty.NullOrEmpty() ? "count" : def.hediffProperty.ToLower();
            
            float totalValue = 0f;
            int matchCount = 0;
            
            foreach (var hediff in hediffs)
            {
                // Check if this hediff matches ANY of the hediff filters
                bool matchesAnyFilter = false;
                foreach (var filter in def.filters.hediffs)
                {
                    if (HediffMatchesFilter(hediff, filter))
                    {
                        matchesAnyFilter = true;
                        break;
                    }
                }
                
                if (matchesAnyFilter)
                {
                    matchCount++;
                    
                    // Extract the requested property value
                    switch (property)
                    {
                        case "count":
                            totalValue += 1f;
                            break;
                            
                        case "severity":
                            totalValue += hediff.Severity;
                            break;
                            
                        // ===== BASE IMMUNITY RATE (Not Recommended) =====
                        // Returns the base immunity gain from hediff definition
                        // Does NOT consider: ImmunityGainSpeed stat, bed quality, food, needs, tending quality
                        // Use effectiveImmunityPerDay for accurate disease management
                        case "immunityperday":
                        case "immunity":
                            if (hediff is HediffWithComps hediffWithComps)
                            {
                                var immunizableComp = hediffWithComps.TryGetComp<HediffComp_Immunizable>();
                                if (immunizableComp != null)
                                {
                                    // Calculate immunity gain per day (BASE - does not consider stats/needs)
                                    float immunityGain = immunizableComp.Props.immunityPerDaySick;
                                    if (!hediffWithComps.FullyImmune())
                                    {
                                        totalValue += immunityGain;
                                    }
                                }
                            }
                            break;
                            
                        // ===== EFFECTIVE IMMUNITY RATE (Recommended) =====
                        // Returns the ACTUAL immunity gain per day considering all game factors
                        // Uses ImmunityRecord.ImmunityChangePerTick() which includes:
                        // - Base immunityPerDaySick, ImmunityGainSpeed stat (bed quality, food, tending), random factor
                        case "effectiveimmunityperday":
                            // Gets the ACTUAL immunity gain per day after considering pawn stats, needs, bed quality, etc.
                            if (hediff is HediffWithComps effectiveHediffWithComps)
                            {
                                var immunityRecord = pawn.health.immunity.GetImmunityRecord(hediff.def);
                                if (immunityRecord != null && !effectiveHediffWithComps.FullyImmune())
                                {
                                    // ImmunityChangePerTick returns per-tick, multiply by 60000 to get per day
                                    float effectiveImmunityPerDay = immunityRecord.ImmunityChangePerTick(pawn, sick: true, hediff) * 60000f;
                                    totalValue += effectiveImmunityPerDay;
                                }
                            }
                            break;
                            
                        // ===== BASE SEVERITY RATE (Not Recommended) =====
                        // Returns the base severity change from hediff definition
                        // Does NOT consider: random factor, severity modifiers from other hediffs
                        // Use effectiveSeverityPerDay for accurate disease management
                        case "severityperday":
                            if (hediff is HediffWithComps hediffComps)
                            {
                                var immunizableComp = hediffComps.TryGetComp<HediffComp_Immunizable>();
                                if (immunizableComp != null)
                                {
                                    // Get severity change per day (BASE - does not consider stats/needs)
                                    float severityPerDay = hediffComps.FullyImmune() 
                                        ? immunizableComp.Props.severityPerDayImmune 
                                        : immunizableComp.Props.severityPerDayNotImmune;
                                    totalValue += severityPerDay;
                                }
                            }
                            break;
                            
                        // ===== EFFECTIVE SEVERITY RATE (Recommended) =====
                        // Returns the ACTUAL severity change considering all game factors
                        // Uses HediffComp_Immunizable.SeverityChangePerDay() which includes:
                        // - Base severity rate, random factor, severity modifiers from other hediffs
                        case "effectiveseverityperday":
                            // Gets the ACTUAL severity change per day after considering pawn stats, needs, bed quality, etc.
                            if (hediff is HediffWithComps effectiveSeverityHediff)
                            {
                                var immunizableComp = effectiveSeverityHediff.TryGetComp<HediffComp_Immunizable>();
                                if (immunizableComp != null)
                                {
                                    // SeverityChangePerDay already returns the effective value considering all factors
                                    float effectiveSeverityPerDay = immunizableComp.SeverityChangePerDay();
                                    totalValue += effectiveSeverityPerDay;
                                }
                            }
                            break;
                            
                        case "bleedrate":
                            // Prefer the instance bleed rate for injuries, but fall back
                            // to the HediffDef.injuryProps bleedRate for non-injury
                            // hediffs such as MissingBodyPart.
                            if (hediff is Hediff_Injury injury)
                            {
                                totalValue += injury.BleedRate;
                            }
                            else if (hediff.def?.injuryProps != null)
                            {
                                totalValue += hediff.def.injuryProps.bleedRate;
                            }
                            break;
                            
                        case "infectionchance":
                            if (hediff is HediffWithComps infectionHediff)
                            {
                                var infecterComp = infectionHediff.TryGetComp<HediffComp_Infecter>();
                                if (infecterComp != null)
                                {
                                    totalValue += infecterComp.Props.infectionChance;
                                }
                            }
                            break;
                            
                        case "painoffset":
                            totalValue += hediff.PainOffset;
                            break;
                            
                        default:
                            Log.Warning($"[Autonomy] Unknown hediffProperty: {property}. Using count instead.");
                            totalValue += 1f;
                            break;
                    }
                }
            }
            
            return totalValue;
        }

        private bool HediffMatchesFilter(Hediff hediff, HediffFilter filter)
        {
            if (hediff == null || filter == null) return false;
            
            // Check hediff class filter
            if (!filter.hediffClass.NullOrEmpty())
            {
                bool classMatches = false;
                string className = filter.hediffClass;
                
                // Check exact type name
                if (hediff.GetType().Name == className)
                {
                    classMatches = true;
                }
                // Check if it's a subclass
                else if (IsSubclassOfTypeName(hediff.GetType(), className))
                {
                    classMatches = true;
                }
                // Check common base classes
                else if ((className == "Hediff_Injury" && hediff is Hediff_Injury) ||
                    (className == "Hediff_MissingPart" && hediff is Hediff_MissingPart) ||
                    (className == "HediffWithComps" && hediff is HediffWithComps))
                {
                    classMatches = true;
                }
                
                if (!classMatches)
                {
                    return false;
                }
            }
            
            // Filter by tendable
            if (filter.tendable.HasValue)
            {
                if (hediff.TendableNow() != filter.tendable.Value)
                {
                    return false;
                }
            }
            
            // Filter by tended status
            if (filter.tended.HasValue)
            {
                if (hediff.IsTended() != filter.tended.Value)
                {
                    return false;
                }
            }
            
            // Filter by infection chance
            if (filter.hasInfectionChance.HasValue)
            {
                bool hasInfectionChance = false;
                
                if (hediff is HediffWithComps hediffWithComps)
                {
                    var infecterComp = hediffWithComps.TryGetComp<HediffComp_Infecter>();
                    hasInfectionChance = infecterComp != null && infecterComp.Props.infectionChance > 0f;
                }
                
                if (hasInfectionChance != filter.hasInfectionChance.Value)
                {
                    return false;
                }
            }
            
            // Filter by bleed rate
            if (filter.hasBleedRate.HasValue)
            {
                bool hasBleedRate = false;
                // Some hediffs (e.g. Hediff_MissingPart) aren't Hediff_Injury but
                // can still define bleedRate in their HediffDef.injuryProps.
                if (hediff is Hediff_Injury injury)
                {
                    hasBleedRate = injury.BleedRate > 0f;
                }
                else if (hediff.def?.injuryProps != null)
                {
                    hasBleedRate = hediff.def.injuryProps.bleedRate > 0f;
                }

                if (hasBleedRate != filter.hasBleedRate.Value)
                {
                    return false;
                }
            }
            
            // Filter by immunizable
            if (filter.isImmunizable.HasValue)
            {
                bool isImmunizable = false;
                
                if (hediff is HediffWithComps immunizableHediff)
                {
                    var immunizableComp = immunizableHediff.TryGetComp<HediffComp_Immunizable>();
                    isImmunizable = immunizableComp != null;
                }
                
                if (isImmunizable != filter.isImmunizable.Value)
                {
                    return false;
                }
            }
            
            // Passed all filters
            return true;
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
                case InfoSourceType.geneCount:
                    CollectIndividualGeneCountData(def, indData);
                    break;
                case InfoSourceType.hediffCount:
                    CollectIndividualHediffCountData(def, indData);
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
            
            if (!def.targetThingClasses.NullOrEmpty())
            {
                foreach (string thingClassName in def.targetThingClasses)
                {
                    // Find all ThingDefs whose thingClass matches the target class name
                    var thingDefs = DefDatabase<ThingDef>.AllDefs.Where(td => 
                        td.thingClass != null && 
                        (td.thingClass.Name == thingClassName || 
                         td.thingClass.Name.EndsWith(thingClassName) ||
                         IsSubclassOfType(td.thingClass, thingClassName)));
                    
                    foreach (var thingDef in thingDefs)
                    {
                        items.AddRange(map.listerThings.ThingsOfDef(thingDef));
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

        private void CollectIndividualGeneCountData(InfoGiverDef def, IndividualData indData)
        {
            var pawns = new List<Pawn>();
            var allPawns = map.mapPawns.AllPawns;
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            foreach (var pawn in pawns)
            {
                float geneCount = GetPawnGeneCount(pawn, def);
                indData.SetPawnValue(pawn, geneCount);
            }
        }

        private void CollectIndividualHediffCountData(InfoGiverDef def, IndividualData indData)
        {
            var pawns = new List<Pawn>();
            var allPawns = map.mapPawns.AllPawns;
            pawns = ApplyPawnTypeFilters(allPawns, def.filters);
            
            foreach (var pawn in pawns)
            {
                float hediffValue = GetPawnHediffValue(pawn, def);
                indData.SetPawnValue(pawn, hediffValue);
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