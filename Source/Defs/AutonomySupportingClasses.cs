using System;
using System.Collections.Generic;
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
        PawnStat,           // Individual pawn statistics
        PawnNeed,           // Individual pawn needs (hunger, joy, etc.)
        PawnCount,          // Count of pawns meeting criteria
        ItemCount,          // Count of items/resources
        ConstructionCount,  // Count of construction projects
        MapCondition,       // Map-level conditions and threats
        Weather            // Weather conditions
    }

    /// <summary>
    /// How to calculate results from collected data
    /// </summary>
    public enum CalculationType
    {
        Sum,        // Add all values
        Average,    // Average of all values
        Maximum,    // Highest value
        Minimum,    // Lowest value
        Count,      // Count of items
        Flat,       // Use flat comparison value
        BestOf,     // Best result from multiple sources
        WorstOf,    // Worst result from multiple sources
        Weighted    // Weighted calculation
    }

    /// <summary>
    /// Types of conditions that can be evaluated
    /// </summary>
    public enum ConditionType
    {
        Stat,       // Check pawn statistics
        InfoGiver,  // Use InfoGiverDef results
        Flat,       // Compare against fixed values
        MapStat     // Check map-level statistics
    }

    /// <summary>
    /// Mathematical operations for personality offsets
    /// </summary>
    public enum OffsetOperator
    {
        Add,        // +
        Subtract,   // -
        Multiply    // x
    }

    /// <summary>
    /// Filters for InfoGiver data collection - modular and extensible
    /// </summary>
    public class InfoFilters
    {
        // Pawn inclusion/exclusion (replaces hardcoded booleans)
        public List<string> include = new List<string>();     // "player", "guests", "hostiles", "traders"
        public List<string> exclude = new List<string>();     // Same options as include

        // Advanced hediff filtering
        public List<HediffFilter> hediffs = new List<HediffFilter>();

        // Item filtering
        public bool stockpileOnly = false;
        public bool excludeForbidden = true;

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
        public bool? hediffTended;                          // Whether hediff has been tended
        public string hasComps;                             // Specific comp class name (HediffCompProperties_Immunizable)
        
        // Dynamic property filtering with comparators
        public string severity;                             // e.g., ">0.5", "<=0.8", "=1.0"
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
}