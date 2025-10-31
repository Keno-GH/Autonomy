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
        /// Conditions that determine when and how this priority giver activates
        /// </summary>
        public List<PriorityCondition> conditions = new List<PriorityCondition>();
        
        /// <summary>
        /// Personality trait-based modifications to condition results
        /// </summary>
        public List<PersonalityOffset> personalityOffsets = new List<PersonalityOffset>();
        
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
        public CalculationType calculation = CalculationType.sum;
        
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
                    if (targetItems.NullOrEmpty() && targetCategories.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType itemCount requires either targetItems or targetCategories");
                    break;
                    
                case InfoSourceType.constructionCount:
                    if (targetItems.NullOrEmpty() && targetCategories.NullOrEmpty())
                        Log.Error($"InfoGiverDef {defName} with sourceType constructionCount requires either targetItems or targetCategories");
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
        public CalculationType calculation = CalculationType.flat;
        
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

        public void ResolveReferences()
        {
            // Validate InfoGiver reference if used
            if (!infoDefName.NullOrEmpty())
            {
                var infoDef = DefDatabase<InfoGiverDef>.GetNamedSilentFail(infoDefName);
                if (infoDef == null)
                {
                    Log.Error($"PriorityCondition references unknown InfoGiverDef: {infoDefName}");
                }
            }
        }
    }

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
    }

    // Supporting classes and enums are now in AutonomySupportingClasses.cs
}