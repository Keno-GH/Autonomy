using System;
using System.Collections.Generic;
using System.Xml;
using Verse;
using RimWorld;

namespace Autonomy
{
    /// <summary>
    /// Defines how colony conditions translate to work priority adjustments
    /// </summary>
    public class PriorityGiverDef : Def
    {
        /// <summary>
        /// Whether this PriorityGiver should be evaluated more frequently (400 ticks instead of 2000)
        /// Use for urgent matters that require quick priority adjustments
        /// </summary>
        public bool isUrgent = false;
        
        /// <summary>
        /// Conditions that determine when and how this priority giver activates
        /// </summary>
        public List<PriorityCondition> conditions = new List<PriorityCondition>();
        
        /// <summary>
        /// Maps condition results to actual priority values
        /// </summary>
        public List<PriorityRange> priorityRanges = new List<PriorityRange>();
        
        /// <summary>
        /// Specific WorkGiverDefs this priority affects (optional)
        /// </summary>
        public List<string> targetWorkGivers = new List<string>();
        
        /// <summary>
        /// Specific WorkTypeDefs this priority affects (required if targetWorkGivers is empty)
        /// </summary>
        public List<string> targetWorkTypes = new List<string>();

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            
            // Validate that we have either targetWorkTypes or targetWorkGivers
            if (targetWorkTypes.NullOrEmpty() && targetWorkGivers.NullOrEmpty())
            {
                Log.Error($"PriorityGiverDef {defName} must have either targetWorkTypes or targetWorkGivers defined");
            }
            
            // Resolve and validate condition references
            foreach (var condition in conditions)
            {
                condition.ResolveReferences();
            }
        }
    }

    /// <summary>
    /// Modular data collection definitions for various game systems
    /// </summary>
    public class InfoGiverDef : Def
    {
        /// <summary>
        /// What type of data source to query
        /// </summary>
        public InfoSourceType sourceType;
        
        /// <summary>
        /// How to calculate the final result from collected data
        /// </summary>
        public CalculationType calculation = CalculationType.Sum;
        
        /// <summary>
        /// Whether this InfoGiver should be evaluated more frequently (400 ticks instead of 2000)
        /// Use for urgent matters like hostiles, fires, or other time-critical conditions
        /// </summary>
        public bool isUrgent = false;
        
        /// <summary>
        /// Target stat for pawnStat sourceType
        /// </summary>
        public string targetStat;
        
        /// <summary>
        /// Target need for pawnNeed sourceType
        /// </summary>
        public string targetNeed;
        
        /// <summary>
        /// Target items for itemCount sourceType (alternative to categories)
        /// </summary>
        public List<string> targetItems = new List<string>();
        
        /// <summary>
        /// Target ThingCategoryDefs for itemCount sourceType (better mod compatibility)
        /// </summary>
        public List<string> targetCategories = new List<string>();
        
        /// <summary>
        /// Target ThingClass names for itemCount sourceType (e.g., "Filth" for all filth types)
        /// </summary>
        public List<string> targetThingClasses = new List<string>();
        
        /// <summary>
        /// Weather property for weather sourceType (moveSpeedMultiplier, accuracyMultiplier, etc.)
        /// </summary>
        public string weatherProperty;
        
        /// <summary>
        /// Map conditions for mapCondition sourceType
        /// </summary>
        public List<MapCondition> conditions = new List<MapCondition>();
        
        /// <summary>
        /// Filters to apply when collecting data
        /// </summary>
        public InfoFilters filters = new InfoFilters();
        
        /// <summary>
        /// Dynamic target amount calculation (replaces hardcoded targetAmount)
        /// </summary>
        public TargetAmountSource targetAmountSource;
        
        /// <summary>
        /// Whether to return result as percentage of target amount
        /// </summary>
        public bool returnAsPercentage;
        
        /// <summary>
        /// Whether to invert the final value (useful for needs where lower = higher priority)
        /// </summary>
        public bool invertValue;
        
        /// <summary>
        /// Whether this InfoGiver tracks data by location/room
        /// When true, the InfoGiver will store separate values for different rooms/areas
        /// and can be queried with a specific location context
        /// </summary>
        public bool isLocalizable = false;
        
        /// <summary>
        /// Whether this InfoGiver tracks data by individual pawn
        /// When true, the InfoGiver will store individual pawn values and distances from averages
        /// and can be queried with a specific pawn context
        /// </summary>
        public bool isIndividualizable = false;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            
            // Validate required fields based on source type
            switch (sourceType)
            {
                case InfoSourceType.pawnStat:
                    if (targetStat.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType pawnStat requires targetStat");
                    break;
                    
                case InfoSourceType.pawnNeed:
                    if (targetNeed.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType pawnNeed requires targetNeed");
                    break;
                    
                case InfoSourceType.itemCount:
                    if (targetItems.NullOrEmpty() && targetCategories.NullOrEmpty() && targetThingClasses.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType itemCount requires either targetItems, targetCategories, or targetThingClasses");
                    break;
                    
                case InfoSourceType.constructionCount:
                    if (targetItems.NullOrEmpty() && targetCategories.NullOrEmpty() && targetThingClasses.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType constructionCount requires either targetItems, targetCategories, or targetThingClasses");
                    break;
                    
                case InfoSourceType.weather:
                    if (weatherProperty.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType weather requires weatherProperty");
                    break;
            }
            
            // Validate targetAmountSource if used
            if (targetAmountSource != null && !targetAmountSource.infoDefName.NullOrEmpty())
            {
                var targetInfo = DefDatabase<InfoGiverDef>.GetNamedSilentFail(targetAmountSource.infoDefName);
                if (targetInfo == null)
                {
                    Log.Error($"InfoGiverDef {defName} targetAmountSource references unknown InfoGiverDef: {targetAmountSource.infoDefName}");
                }
            }
        }
    }

    /// <summary>
    /// Defines a condition that checks game state
    /// </summary>
    public class PriorityCondition
    {
        /// <summary>
        /// Type of condition to evaluate
        /// </summary>
        public ConditionType type;
        
        /// <summary>
        /// Name for special named conditions
        /// </summary>
        public string name;
        
        /// <summary>
        /// Stat name for stat-based conditions
        /// </summary>
        public string stat;
        
        /// <summary>
        /// How to calculate this condition
        /// </summary>
        public CalculationType calculation = CalculationType.Flat;
        
        /// <summary>
        /// Flat value for comparison
        /// </summary>
        public float flatValue;
        
        /// <summary>
        /// InfoGiverDef to use for data
        /// </summary>
        public string infoDefName;
        
        /// <summary>
        /// Range for infoGiver results
        /// </summary>
        public FloatRange infoRange;
        
        /// <summary>
        /// Request localized data from InfoGiver (if supported)
        /// When true, queries data specific to the pawn's current location/room
        /// </summary>
        public bool requestLocalizedData = false;
        
        /// <summary>
        /// Request individualized data from InfoGiver (if supported)
        /// When true, queries the individual pawn's value instead of the global result
        /// </summary>
        public bool requestIndividualData = false;
        
        /// <summary>
        /// Request distance from global value for individualized InfoGivers
        /// When true, returns how far the pawn's value is from the group average
        /// (positive = above average, negative = below average)
        /// </summary>
        public bool requestDistanceFromGlobal = false;

        /// <summary>
        /// Personality DefName for personalityOffset condition type
        /// Example: "Rimpsyche_Confidence"
        /// </summary>
        public string personalityDefName;
        
        /// <summary>
        /// Personality value ranges and their corresponding multipliers
        /// Each range maps a personality score range to a priority multiplier
        /// </summary>
        public List<PersonalityMultiplier> personalityMultipliers = new List<PersonalityMultiplier>();

        public void ResolveReferences()
        {
            // Validate InfoGiver reference if used
            if (!infoDefName.NullOrEmpty())
            {
                var infoDef = DefDatabase<InfoGiverDef>.GetNamedSilentFail(infoDefName);
                if (infoDef == null)
                {
                    Log.Error($"PriorityCondition references unknown InfoGiverDef: {infoDefName}");
                    return;
                }
                
                // Validate localized data request
                if (requestLocalizedData && !infoDef.isLocalizable)
                {
                    Log.Error($"PriorityCondition requests localized data from InfoGiver {infoDefName}, but it is not localizable");
                }
                
                // Validate individualized data requests
                if ((requestIndividualData || requestDistanceFromGlobal) && !infoDef.isIndividualizable)
                {
                    Log.Error($"PriorityCondition requests individualized data from InfoGiver {infoDefName}, but it is not individualizable");
                }
                
                // Validate that distance request implies individual data
                if (requestDistanceFromGlobal && !requestIndividualData)
                {
                    Log.Warning($"PriorityCondition requests distance from global but not individual data for {infoDefName}. Setting requestIndividualData to true.");
                    requestIndividualData = true;
                }
            }
            
            // Validate personalityOffset condition
            if (type == ConditionType.personalityOffset)
            {
                if (personalityDefName.NullOrEmpty())
                {
                    Log.Error($"PriorityCondition with personalityOffset type requires personalityDefName");
                }
                
                if (personalityMultipliers.NullOrEmpty())
                {
                    Log.Error($"PriorityCondition with personalityOffset type requires personalityMultipliers");
                }
            }
        }
    }

    // PersonalityOffset functionality removed as requested by user
    /*
    /// <summary>
    /// Personality trait offset for condition results
    /// </summary>
    public class PersonalityOffset
    {
        /// <summary>
        /// DefName of the personality trait
        /// </summary>
        public string name;
        
        /// <summary>
        /// Offset string in format: {operator}{value} (e.g., "+2", "-1.5", "x2.0")
        /// </summary>
        public string offset;

        /// <summary>
        /// Parsed operator from offset string
        /// </summary>
        public OffsetOperator OperatorType;
        
        /// <summary>
        /// Parsed value from offset string
        /// </summary>
        public float Value;

        public void PostLoad()
        {
            ParseOffset();
        }

        private void ParseOffset()
        {
            if (offset.NullOrEmpty())
            {
                Log.Error($"PersonalityOffset for {name} has empty offset");
                return;
            }

            char firstChar = offset[0];
            string valueStr;

            switch (firstChar)
            {
                case '+':
                    OperatorType = OffsetOperator.Add;
                    valueStr = offset.Substring(1);
                    break;
                case '-':
                    OperatorType = OffsetOperator.Subtract;
                    valueStr = offset.Substring(1);
                    break;
                case 'x':
                case 'X':
                    OperatorType = OffsetOperator.Multiply;
                    valueStr = offset.Substring(1);
                    break;
                default:
                    // Default to addition if no operator specified
                    OperatorType = OffsetOperator.Add;
                    valueStr = offset;
                    break;
            }

            if (!float.TryParse(valueStr, out Value))
            {
                Log.Error($"PersonalityOffset for {name} has invalid value: {valueStr}");
                Value = 0f;
            }
        }

        /// <summary>
        /// Apply this offset to a value
        /// </summary>
        public float Apply(float value)
        {
            switch (OperatorType)
            {
                case OffsetOperator.Add:
                    return value + Value;
                case OffsetOperator.Subtract:
                    return value - Value;
                case OffsetOperator.Multiply:
                    return value * Value;
                default:
                    return value;
            }
        }
    }
    */

    /// <summary>
    /// Maps condition results to priority values
    /// </summary>
    public class PriorityRange
    {
        /// <summary>
        /// Range of condition values this applies to (format: "min~max")
        /// </summary>
        public string validRange;
        
        /// <summary>
        /// Priority to assign (can be single int or range "min~max")
        /// </summary>
        public string priority;
        
        /// <summary>
        /// Description shown to player
        /// </summary>
        public string description;

        /// <summary>
        /// Parsed valid range
        /// </summary>
        public FloatRange ValidRangeParsed { get; private set; }
        
        /// <summary>
        /// Parsed priority range
        /// </summary>
        public IntRange PriorityRangeParsed { get; private set; }

        public void PostLoad()
        {
            ParseRanges();
        }

        private void ParseRanges()
        {
            // Parse validRange
            ValidRangeParsed = ParseFloatRange(validRange, "validRange");
            
            // Parse priority
            var priorityFloatRange = ParseFloatRange(priority, "priority");
            PriorityRangeParsed = new IntRange((int)priorityFloatRange.min, (int)priorityFloatRange.max);
        }

        private FloatRange ParseFloatRange(string rangeStr, string fieldName)
        {
            if (rangeStr.NullOrEmpty())
            {
                Log.Error($"PriorityRange has empty {fieldName}");
                return new FloatRange(0f, 0f);
            }

            if (rangeStr.Contains("~"))
            {
                string[] parts = rangeStr.Split('~');
                if (parts.Length == 2 && 
                    float.TryParse(parts[0], out float min) && 
                    float.TryParse(parts[1], out float max))
                {
                    return new FloatRange(min, max);
                }
            }
            else
            {
                if (float.TryParse(rangeStr, out float single))
                {
                    return new FloatRange(single, single);
                }
            }

            Log.Error($"PriorityRange has invalid {fieldName}: {rangeStr}");
            return new FloatRange(0f, 0f);
        }

        /// <summary>
        /// Check if a value falls within this range
        /// </summary>
        public bool Contains(float value)
        {
            return ValidRangeParsed.Includes(value);
        }

        /// <summary>
        /// Get a random priority value from this range
        /// </summary>
        public int GetRandomPriority()
        {
            return PriorityRangeParsed.RandomInRange;
        }
        
        /// <summary>
        /// Get interpolated priority based on where the value falls within the valid range
        /// For example, if validRange is 1~2 and priority is 10~20:
        /// - value 1.0 returns 10
        /// - value 1.5 returns 15  
        /// - value 2.0 returns 20
        /// </summary>
        public int GetInterpolatedPriority(float value)
        {
            // Clamp value to valid range
            value = UnityEngine.Mathf.Clamp(value, ValidRangeParsed.min, ValidRangeParsed.max);
            
            // If the valid range is a single point, return the single priority
            if (UnityEngine.Mathf.Approximately(ValidRangeParsed.min, ValidRangeParsed.max))
            {
                return PriorityRangeParsed.min;
            }
            
            // If the priority range is a single point, return that priority
            if (PriorityRangeParsed.min == PriorityRangeParsed.max)
            {
                return PriorityRangeParsed.min;
            }
            
            // Calculate interpolation factor (0.0 to 1.0)
            float t = (value - ValidRangeParsed.min) / (ValidRangeParsed.max - ValidRangeParsed.min);
            
            // Interpolate between min and max priority
            float interpolatedPriority = UnityEngine.Mathf.Lerp(PriorityRangeParsed.min, PriorityRangeParsed.max, t);
            
            return UnityEngine.Mathf.RoundToInt(interpolatedPriority);
        }
    }

    /// <summary>
    /// Defines passion-based priority adjustments for work types
    /// </summary>
    public class PassionGiverDef : Def
    {
        /// <summary>
        /// Name of the passion to match (e.g., "Minor", "Major")
        /// </summary>
        public string passionName;
        
        /// <summary>
        /// Whether this passion giver only applies when the passion is "active" 
        /// (relevant for mod passions that have special activation conditions)
        /// </summary>
        public bool onlyWhenActive = true;
        
        /// <summary>
        /// Conditions that determine personality-based multipliers for this passion giver
        /// </summary>
        public List<PriorityCondition> conditions = new List<PriorityCondition>();
        
        /// <summary>
        /// Priority result when this passion applies
        /// </summary>
        public PassionPriorityResult priorityResult = new PassionPriorityResult();

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            
            if (passionName.NullOrEmpty())
            {
                Log.Error($"PassionGiverDef {defName} requires passionName");
            }
            
            // Resolve and validate condition references
            foreach (var condition in conditions)
            {
                condition.ResolveReferences();
            }
        }
    }

    /// <summary>
    /// Result data for passion-based priority calculation
    /// </summary>
    public class PassionPriorityResult
    {
        public int priority = 0;
        public string description = "";
    }

    // Supporting classes and enums are now in AutonomySupportingClasses.cs
}