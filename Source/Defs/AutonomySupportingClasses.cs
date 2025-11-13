using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    /// <summary>
    /// Supporting data structures and enums for Autonomy system
    /// </summary>
    
    /// <summary>
    /// Types of information sources that InfoGivers can query
    /// </summary>
    public enum InfoSourceType
    {
        pawnStat,           // Individual pawn statistics
        pawnNeed,           // Individual pawn needs (hunger, joy, etc.)
        pawnCount,          // Count of pawns meeting criteria
        itemCount,          // Count of items/resources
        constructionCount,  // Count of construction projects
        mapCondition,       // Map-level conditions and threats
        weather,            // Weather conditions
        geneCount,          // Count of genes in individual pawns (always individualizable)
        hediffCount         // Count/measure hediffs in individual pawns (always individualizable)
    }

    /// <summary>
    /// How to calculate results from collected data
    /// </summary>
    public enum CalculationType
    {
        Sum,        // Add all values
        Avg,        // Average of all values
        Max,        // Highest value
        Min,        // Lowest value
        Count,      // Count of items
        Flat,       // Use flat comparison value
        BestOf,     // Best result from multiple sources
        WorstOf,    // Worst result from multiple sources
        Weighted    // Weighted calculation
    }

    /// <summary>
    /// How to calculate skill-based priority for SkillGivers
    /// </summary>
    public enum SkillCalculationType
    {
        none,       // Use skill level directly with priority ranges (default behavior)
        order       // Compare pawn's skill against colony to determine rank-based priority
    }

    /// <summary>
    /// Types of conditions that can be evaluated
    /// </summary>
    public enum ConditionType
    {
        stat,           // Check pawn statistics
        infoGiver,      // Use InfoGiverDef results
        flat,           // Compare against fixed values
        mapStat,        // Check map-level statistics
        personalityOffset, // Apply personality-based multipliers to priority
        filter,         // Filter pawns based on specific criteria (outer layer filter)
        calculation     // Perform calculations between two InfoGivers or floats
    }

    /// <summary>
    /// Mathematical operations for calculation conditions
    /// </summary>
    public enum CalculationOperation
    {
        sub,    // value1 - value2
        diff,   // abs(value1 - value2)
        sum,    // value1 + value2
        ratio,  // value1 / value2
        max,    // max(value1, value2)
        min,    // min(value1, value2)
        avg     // (value1 + value2) / 2
    }

    // OffsetOperator enum commented out with PersonalityOffset functionality
    /*
    /// <summary>
    /// Mathematical operations for personality offsets
    /// </summary>
    public enum OffsetOperator
    {
        Add,        // +
        Subtract,   // -
        Multiply    // x
    }
    */

    /// <summary>
    /// Enhanced pawn filter with race, faction, and status conditions
    /// Supports chained conditions within a single filter (AND logic)
    /// Multiple PawnFilters in a list work as OR logic
    /// </summary>
    public class PawnFilter
    {
        /// <summary>
        /// Race filter: "Humanlike", "AnimalAny", "Any", or specific race def name
        /// Default: "Any"
        /// </summary>
        public string race = "Any";
        
        /// <summary>
        /// Faction filter: "player", "non_player", "hostile", "Any"
        /// Default: "Any"
        /// </summary>
        public string faction = "Any";
        
        /// <summary>
        /// Status filters (AND logic within this list)
        /// Examples: "prisoner", "guest", "dead", "downed", "roaming", "unenclosed", "animal", "colonist", "slave"
        /// </summary>
        public List<string> status = new List<string>();
    }

    /// <summary>
    /// Filters for InfoGiver data collection - modular and extensible
    /// </summary>
    public class InfoFilters
    {
        // NEW: Enhanced pawn filters with race, faction, and status
        public List<PawnFilter> pawnFilters = new List<PawnFilter>();
        public List<PawnFilter> excludePawnFilters = new List<PawnFilter>();
        
        // LEGACY: Simple string inclusion/exclusion (kept for backward compatibility)
        // "player", "guests", "hostiles", "traders", "selfTendEnabled", "selfTendDisabled", "animal", "prisoner"
        public List<string> include = new List<string>();
        public List<string> exclude = new List<string>();
        
        // Pawn capability checks (AND list). Each entry is a SkillDef name (e.g. "Construction").
        // A pawn must be capable (not TotallyDisabled) of ALL listed skills to pass this filter.
        public List<string> capableOf = new List<string>();

        // Advanced hediff filtering
        public List<HediffFilter> hediffs = new List<HediffFilter>();

        // Item filtering
        public bool stockpileOnly = false;
        public bool excludeForbidden = true;
        public bool homeAreaOnly = false;

        // Construction filtering
        public bool onlyDesignated = true;
        public bool excludeStarted = false;
    }

    /// <summary>
    /// Advanced hediff filtering with dynamic property checking
    /// </summary>
    public class HediffFilter
    {
        public string hediffClass;                          // Class name (Hediff_Injury, HediffWithComps, etc.)
        public bool? tendable;                              // Whether hediff is tendable
        public bool? tended;                                // Whether hediff has been tended (changed from hediffTended)
        public bool? hasInfectionChance;                    // Whether hediff has infection chance
        public bool? hasBleedRate;                          // Whether hediff has bleed rate
        public bool? isImmunizable;                         // Whether hediff is immunizable
        public string hasComps;                             // Specific comp class name (HediffCompProperties_Immunizable)
        
        // Dynamic property filtering with comparators
        public string severity;                             // e.g., ">0.5", "<=0.8", "=1.0"
        public string severityRange;                        // e.g., "0.8~1.0" for range filtering
        public string deltaImmunitySeverity;               // Special: immunity vs severity comparison
        
        // Add more dynamic property filters as needed
        public Dictionary<string, string> propertyFilters = new Dictionary<string, string>();

        public void PostLoad()
        {
            // Parse severity and other numeric comparisons
            // This will be implemented in the evaluation engine
        }
    }

    /// <summary>
    /// Maps personality value ranges to priority multipliers
    /// </summary>
    public class PersonalityMultiplier
    {
        /// <summary>
        /// Range of personality values (-1 to 1)
        /// </summary>
        public FloatRange personalityRange;
        
        /// <summary>
        /// Multiplier to apply to final priority when personality is in this range
        /// </summary>
        public float multiplier = 1.0f;
        
        /// <summary>
        /// Flat offset to add after multiplier is applied
        /// Applied as: (basePriority * multiplier) + flat
        /// </summary>
        public float flat = 0f;
    }

    /// <summary>
    /// Dynamic target amount calculation
    /// </summary>
    public class TargetAmountSource
    {
        public string infoDefName;                          // InfoGiver to base calculation on
        public float multiplier = 1.0f;                    // Multiplier for the result
        public float offset = 0f;                           // Offset to add after multiplication
    }

    /// <summary>
    /// Weather condition definition for InfoGivers - now uses dynamic properties
    /// </summary>
    public class WeatherPropertyEvaluator
    {
        public string weatherProperty;                      // Property name (moveSpeedMultiplier, accuracyMultiplier)
        public bool invertValue = false;                    // Whether to invert the result
        public float multiplier = 1.0f;                    // Multiplier for the property value
    }

    /// <summary>
    /// Map condition definition for InfoGivers
    /// </summary>
    public class MapCondition
    {
        public string type;         // Type of condition to check
        public float range;         // Range for proximity checks
        public float weight;        // Weight for calculation
    }

    /// <summary>
    /// Range struct for integer values
    /// </summary>
    public struct IntRange
    {
        public int min;
        public int max;

        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int RandomInRange
        {
            get { return UnityEngine.Random.Range(min, max + 1); }
        }

        public bool Includes(int value)
        {
            return value >= min && value <= max;
        }
    }

    /// <summary>
    /// Results from priority calculation for debugging and display
    /// </summary>
    public class PriorityCalculationResult
    {
        public PriorityGiverDef priorityGiver;
        public float conditionResult;
        public float finalResult;
        public int assignedPriority;
        public string explanation;
        public List<string> workTypesAffected = new List<string>();
        public List<string> workGiversAffected = new List<string>();
    }

    /// <summary>
    /// Caches calculation results for performance
    /// </summary>
    public class AutonomyCache
    {
        private static Dictionary<string, object> cache = new Dictionary<string, object>();
        private static Dictionary<string, int> cacheTimestamps = new Dictionary<string, int>();
        
        private const int CACHE_DURATION_TICKS = 60000; // ~1 day in ticks

        public static T GetCached<T>(string key, Func<T> calculator) where T : class
        {
            int currentTick = Find.TickManager.TicksGame;
            
            if (cache.ContainsKey(key) && 
                cacheTimestamps.ContainsKey(key) &&
                currentTick - cacheTimestamps[key] < CACHE_DURATION_TICKS)
            {
                return cache[key] as T;
            }

            T result = calculator();
            cache[key] = result;
            cacheTimestamps[key] = currentTick;
            
            return result;
        }

        public static void ClearCache()
        {
            cache.Clear();
            cacheTimestamps.Clear();
        }

        public static void ClearExpired()
        {
            int currentTick = Find.TickManager.TicksGame;
            var expiredKeys = new List<string>();
            
            foreach (var kvp in cacheTimestamps)
            {
                if (currentTick - kvp.Value >= CACHE_DURATION_TICKS)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (string key in expiredKeys)
            {
                cache.Remove(key);
                cacheTimestamps.Remove(key);
            }
        }
    }

    /// <summary>
    /// Settings for the Autonomy mod
    /// </summary>
    public class AutonomySettings : ModSettings
    {
        public bool enableAutonomy = true;
        public bool enableDebugMode = false;
        public int calculationFrequencyHours = 24;
        public bool showPriorityTooltips = true;
        public bool logPriorityChanges = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableAutonomy, "enableAutonomy", true);
            Scribe_Values.Look(ref enableDebugMode, "enableDebugMode", false);
            Scribe_Values.Look(ref calculationFrequencyHours, "calculationFrequencyHours", 24);
            Scribe_Values.Look(ref showPriorityTooltips, "showPriorityTooltips", true);
            Scribe_Values.Look(ref logPriorityChanges, "logPriorityChanges", false);
            base.ExposeData();
        }
    }

    /// <summary>
    /// Tracks location-specific data for localizable InfoGivers
    /// </summary>
    public class LocationalData
    {
        /// <summary>
        /// Room ID to data mapping (room index or null for outside)
        /// </summary>
        public Dictionary<int, float> roomData = new Dictionary<int, float>();
        
        /// <summary>
        /// Cell position to data mapping for fine-grained location tracking
        /// </summary>
        public Dictionary<IntVec3, float> cellData = new Dictionary<IntVec3, float>();
        
        /// <summary>
        /// The overall/global value for this InfoGiver
        /// </summary>
        public float globalValue = 0f;
        
        /// <summary>
        /// Get data for a specific room
        /// </summary>
        public float GetRoomValue(Room room)
        {
            if (room == null) return GetOutsideValue();
            return roomData.TryGetValue(room.ID, out float value) ? value : 0f;
        }
        
        /// <summary>
        /// Get data for outside areas (null room)
        /// </summary>
        public float GetOutsideValue()
        {
            return roomData.TryGetValue(-1, out float value) ? value : 0f;
        }
        
        /// <summary>
        /// Set data for a specific room
        /// </summary>
        public void SetRoomValue(Room room, float value)
        {
            int roomId = room?.ID ?? -1;
            roomData[roomId] = value;
        }
        
        /// <summary>
        /// Get data for a specific cell
        /// </summary>
        public float GetCellValue(IntVec3 cell)
        {
            return cellData.TryGetValue(cell, out float value) ? value : 0f;
        }
        
        /// <summary>
        /// Set data for a specific cell
        /// </summary>
        public void SetCellValue(IntVec3 cell, float value)
        {
            cellData[cell] = value;
        }
    }

    /// <summary>
    /// Tracks individual pawn data for individualizable InfoGivers
    /// </summary>
    public class IndividualData
    {
        /// <summary>
        /// Pawn ID to individual value mapping
        /// </summary>
        public Dictionary<int, float> pawnValues = new Dictionary<int, float>();
        
        /// <summary>
        /// The global/average value for this InfoGiver
        /// </summary>
        public float globalValue = 0f;
        
        /// <summary>
        /// Calculation type used to determine the global value
        /// </summary>
        public CalculationType calculationType = CalculationType.Avg;
        
        /// <summary>
        /// Get individual value for a specific pawn
        /// </summary>
        public float GetPawnValue(Pawn pawn)
        {
            if (pawn?.thingIDNumber == null) return globalValue;
            return pawnValues.TryGetValue(pawn.thingIDNumber, out float value) ? value : globalValue;
        }
        
        /// <summary>
        /// Set individual value for a specific pawn
        /// </summary>
        public void SetPawnValue(Pawn pawn, float value)
        {
            if (pawn?.thingIDNumber != null)
            {
                pawnValues[pawn.thingIDNumber] = value;
            }
        }
        
        /// <summary>
        /// Get distance from global value for a specific pawn
        /// </summary>
        public float GetDistanceFromGlobal(Pawn pawn)
        {
            float pawnValue = GetPawnValue(pawn);
            return Math.Abs(pawnValue - globalValue);
        }
        
        /// <summary>
        /// Get signed distance from global value for a specific pawn (positive = above global, negative = below)
        /// </summary>
        public float GetSignedDistanceFromGlobal(Pawn pawn)
        {
            float pawnValue = GetPawnValue(pawn);
            return pawnValue - globalValue;
        }
        
        /// <summary>
        /// Get normalized distance from global value for a specific pawn
        /// Returns a value in range [-1, 1] where:
        /// - +1 = best pawn (highest value)
        /// - -1 = worst pawn (lowest value)
        /// - 0 = at global average
        /// Values are normalized based on the range of all pawn values
        /// </summary>
        public float GetNormalizedDistanceFromGlobal(Pawn pawn)
        {
            if (pawnValues.Count == 0) return 0f;
            if (pawnValues.Count == 1) return 0f; // Single pawn is both best and worst, so return 0
            
            float pawnValue = GetPawnValue(pawn);
            
            // Get min and max values from all pawns
            float minValue = pawnValues.Values.Min();
            float maxValue = pawnValues.Values.Max();
            
            // If all pawns have same value, return 0
            if (Math.Abs(maxValue - minValue) < 0.0001f)
            {
                return 0f;
            }
            
            // Normalize: convert pawnValue from [minValue, maxValue] to [-1, 1]
            // where minValue maps to -1, globalValue maps to ~0, and maxValue maps to +1
            
            // Calculate range
            float range = maxValue - minValue;
            
            // First normalize to [0, 1] range
            float normalized01 = (pawnValue - minValue) / range;
            
            // Then scale to [-1, 1] range
            float normalized = (normalized01 * 2f) - 1f;
            
            // Clamp to ensure we're in valid range
            if (normalized < -1f) normalized = -1f;
            if (normalized > 1f) normalized = 1f;
            
            return normalized;
        }
        
        /// <summary>
        /// Calculate global value from individual pawn values
        /// </summary>
        public void RecalculateGlobalValue()
        {
            if (pawnValues.Count == 0)
            {
                globalValue = 0f;
                return;
            }
            
            var values = pawnValues.Values.ToList();
            
            switch (calculationType)
            {
                case CalculationType.Sum:
                    globalValue = values.Sum();
                    break;
                case CalculationType.Avg:
                    globalValue = values.Average();
                    break;
                case CalculationType.Max:
                    globalValue = values.Max();
                    break;
                case CalculationType.Min:
                    globalValue = values.Min();
                    break;
                case CalculationType.Count:
                    globalValue = values.Count;
                    break;
                default:
                    globalValue = values.Average();
                    break;
            }
        }
    }

    /// <summary>
    /// Context for InfoGiver queries
    /// </summary>
    public class InfoGiverQueryContext
    {
        /// <summary>
        /// The requesting pawn (for individualized queries)
        /// </summary>
        public Pawn requestingPawn = null;
        
        /// <summary>
        /// The location context (for localized queries)
        /// </summary>
        public IntVec3? location = null;
        
        /// <summary>
        /// Whether to request individual pawn data
        /// </summary>
        public bool requestIndividualData = false;
        
        /// <summary>
        /// Whether to request distance from global value
        /// </summary>
        public bool requestDistanceFromGlobal = false;
        
        /// <summary>
        /// Whether to request normalized distance from global value (range [-1, 1])
        /// </summary>
        public bool requestNormalizedDistance = false;
        
        /// <summary>
        /// Whether to request localized data
        /// </summary>
        public bool requestLocalizedData = false;
    }
}