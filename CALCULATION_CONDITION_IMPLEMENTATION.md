# Calculation Condition Implementation

## Overview

The **calculation condition** is a new condition type for PriorityGivers that performs mathematical operations between two values. These values can come from InfoGivers (dynamic game data) or be static float values. This allows for sophisticated priority calculations based on relationships between different game metrics.

## Purpose

The calculation condition was created to enable more complex priority logic by:
- Comparing two InfoGiver values (e.g., immunity vs disease severity)
- Applying mathematical operations to combine data sources
- Using static values as thresholds or constants in calculations
- Supporting all the same data request flags as regular InfoGiver conditions (localized, individualized, distance-based)

## Syntax

```xml
<li>
    <type>calculation</type>
    
    <!-- First operand: either InfoGiver or static value -->
    <infoDefName1>InfoGiverDefName</infoDefName1>  <!-- OR -->
    <value1>5.0</value1>
    
    <!-- Second operand: either InfoGiver or static value -->
    <infoDefName2>AnotherInfoGiverDefName</infoDefName2>  <!-- OR -->
    <value2>10.0</value2>
    
    <!-- Mathematical operation to perform -->
    <operation>sub</operation>
    
    <!-- Optional: Data request flags (same as infoGiver conditions) -->
    <requestLocalizedData>false</requestLocalizedData>
    <requestIndividualData>true</requestIndividualData>
    <requestDistanceFromGlobal>false</requestDistanceFromGlobal>
    <requestNormalizedDistance>false</requestNormalizedDistance>
</li>
```

## Operations

The calculation condition supports seven mathematical operations:

### 1. `sub` - Subtraction
Returns: `value1 - value2`

**Use case**: Comparing two metrics to find which is greater
```xml
<operation>sub</operation>
<infoDefName1>ImmunizationPerDay</infoDefName1>
<infoDefName2>SeverityPerDay</infoDefName2>
```
Positive result: Immunity is outpacing disease  
Negative result: Disease is winning

### 2. `diff` - Absolute Difference
Returns: `abs(value1 - value2)`

**Use case**: Finding the magnitude of difference without caring about direction
```xml
<operation>diff</operation>
<infoDefName1>CurrentTemperature</infoDefName1>
<value2>21.0</value2>  <!-- Comfortable temperature -->
```
Result: How far from comfortable temperature, regardless of hot or cold

### 3. `sum` - Addition
Returns: `value1 + value2`

**Use case**: Combining multiple factors
```xml
<operation>sum</operation>
<infoDefName1>HostileRaidStrength</infoDefName1>
<infoDefName2>MechanoidThreatLevel</infoDefName2>
```
Result: Total threat level from all sources

### 4. `ratio` - Division
Returns: `value1 / value2`

**Use case**: Calculating proportions or rates
```xml
<operation>ratio</operation>
<infoDefName1>FoodAvailable</infoDefName1>
<infoDefName2>ColonistCount</infoDefName2>
```
Result: Food per colonist

**Note**: Division by zero is handled safely - returns 0 and logs a warning.

### 5. `max` - Maximum
Returns: `max(value1, value2)`

**Use case**: Ensuring a minimum value
```xml
<operation>max</operation>
<infoDefName1>PawnMoveSpeed</infoDefName1>
<value2>1.0</value2>  <!-- Minimum speed -->
```
Result: Use pawn speed, but never less than 1.0

### 6. `min` - Minimum
Returns: `min(value1, value2)`

**Use case**: Capping a value at a maximum
```xml
<operation>min</operation>
<infoDefName1>WorkPriority</infoDefName1>
<value2>100.0</value2>  <!-- Maximum priority -->
```
Result: Use work priority, but never more than 100

### 7. `avg` - Average
Returns: `(value1 + value2) / 2`

**Use case**: Finding the middle ground between two metrics
```xml
<operation>avg</operation>
<infoDefName1>CombatSkill</infoDefName1>
<infoDefName2>ShootingSkill</infoDefName2>
```
Result: Average combat capability

## Data Request Flags

Calculation conditions support all the same data request flags as regular infoGiver conditions. These flags are applied to **both** InfoGivers used in the calculation:

### `requestIndividualData`
When `true`, queries the individual pawn's value for each InfoGiver instead of the colony-wide result.

```xml
<requestIndividualData>true</requestIndividualData>
```

### `requestLocalizedData`
When `true`, queries data specific to the pawn's current location/room.

```xml
<requestLocalizedData>true</requestLocalizedData>
```

### `requestDistanceFromGlobal`
When `true`, returns how far the pawn's value is from the group average (positive = above average, negative = below average).

```xml
<requestDistanceFromGlobal>true</requestDistanceFromGlobal>
```

### `requestNormalizedDistance`
When `true`, returns a normalized value in range [-1, 1] where +1 = best in group, -1 = worst, 0 = average.

```xml
<requestNormalizedDistance>true</requestNormalizedDistance>
```

## Complete Examples

### Example 1: Disease Priority (Immunity vs Severity)

This example shows how to prioritize bed rest based on whether immunity is winning against disease:

```xml
<Autonomy.PriorityGiverDef>
    <defName>PatientGiverImmunizationVSeverity</defName>
    <label>Patient bed rest priority based on disease progress</label>
    <description>Assigns priority to bed rest based on the difference between severity gain and immunity gain for diseases.</description>
    <isIndividualizable>true</isIndividualizable>
    <isUrgent>true</isUrgent>

    <conditions>
        <li>
            <type>calculation</type>
            <infoDefName1>ImmunizationPerDay</infoDefName1>
            <infoDefName2>SeverityPerDay</infoDefName2>
            <operation>sub</operation>
            <requestIndividualData>true</requestIndividualData>
        </li>
    </conditions>

    <priorityRanges>
        <li>
            <validRange>-999~-0.5</validRange>
            <priority>80~95</priority>
            <description>Disease is winning - need urgent rest!</description>
        </li>
        <li>
            <validRange>-0.49~-0.1</validRange>
            <priority>40~79</priority>
            <description>Disease progression is concerning</description>
        </li>
        <li>
            <validRange>-0.09~0.1</validRange>
            <priority>10~39</priority>
            <description>Neck and neck with disease</description>
        </li>
        <li>
            <validRange>0.11~0.5</validRange>
            <priority>0~9</priority>
            <description>Immunity is ahead, but stay cautious</description>
        </li>
        <li>
            <validRange>0.51~999</validRange>
            <priority>0</priority>
            <description>Immunity is winning - no urgent rest needed</description>
        </li>
    </priorityRanges>

    <targetWorkGivers>
        <li>PatientGoToBedRecuperate</li>
    </targetWorkGivers>
</Autonomy.PriorityGiverDef>
```

### Example 2: Resource Per Colonist Ratio

Calculate food per colonist to prioritize hunting/gathering:

```xml
<conditions>
    <li>
        <type>calculation</type>
        <infoDefName1>TotalFood</infoDefName1>
        <infoDefName2>ColonistCount</infoDefName2>
        <operation>ratio</operation>
    </li>
</conditions>

<priorityRanges>
    <li>
        <validRange>0~5</validRange>
        <priority>90~100</priority>
        <description>Critical food shortage!</description>
    </li>
    <li>
        <validRange>5.1~15</validRange>
        <priority>50~89</priority>
        <description>Food running low</description>
    </li>
    <li>
        <validRange>15.1~30</validRange>
        <priority>10~49</priority>
        <description>Food adequate</description>
    </li>
    <li>
        <validRange>30.1~999</validRange>
        <priority>0~9</priority>
        <description>Food stockpile healthy</description>
    </li>
</priorityRanges>
```

### Example 3: Temperature Deviation with Static Value

Use a static value to measure deviation from ideal temperature:

```xml
<conditions>
    <li>
        <type>calculation</type>
        <infoDefName1>CurrentRoomTemp</infoDefName1>
        <value2>21.0</value2>  <!-- Ideal comfortable temperature -->
        <operation>diff</operation>
        <requestLocalizedData>true</requestLocalizedData>
    </li>
</conditions>

<priorityRanges>
    <li>
        <validRange>0~5</validRange>
        <priority>0~5</priority>
        <description>Temperature comfortable</description>
    </li>
    <li>
        <validRange>5.1~15</validRange>
        <priority>10~30</priority>
        <description>Temperature uncomfortable</description>
    </li>
    <li>
        <validRange>15.1~999</validRange>
        <priority>40~80</priority>
        <description>Temperature dangerous!</description>
    </li>
</priorityRanges>
```

### Example 4: Minimum Priority Using Max Operation

Ensure construction priority is never too low:

```xml
<conditions>
    <li>
        <type>calculation</type>
        <infoDefName1>ConstructionSkillLevel</infoDefName1>
        <value2>5.0</value2>  <!-- Minimum base priority -->
        <operation>max</operation>
        <requestIndividualData>true</requestIndividualData>
    </li>
</conditions>
```

### Example 5: Capped Priority Using Min Operation

Cap hauling priority to prevent it from overwhelming other tasks:

```xml
<conditions>
    <li>
        <type>calculation</type>
        <infoDefName1>ItemsNeedingHaul</infoDefName1>
        <value2>50.0</value2>  <!-- Maximum priority cap -->
        <operation>min</operation>
    </li>
</conditions>
```

### Example 6: Combined Skill Average

Average two skills to create a composite priority:

```xml
<conditions>
    <li>
        <type>calculation</type>
        <infoDefName1>MeleeSkill</infoDefName1>
        <infoDefName2>ShootingSkill</infoDefName2>
        <operation>avg</operation>
        <requestIndividualData>true</requestIndividualData>
    </li>
</conditions>
```

## Validation and Error Handling

The system includes comprehensive validation:

### At Definition Load Time (ResolveReferences)
- Validates that at least one input is provided for each operand
- Checks that referenced InfoGivers exist
- Validates that InfoGivers support requested data flags (localized/individualized)
- Warns about division by zero potential with ratio operation and value2=0

### At Runtime (EvaluateCalculationCondition)
- Safely handles division by zero (returns 0, logs warning)
- Handles missing or null InfoGiver values gracefully
- Logs errors for unknown operations

## Best Practices

1. **Choose the right operation**: Use `sub` when direction matters, `diff` when only magnitude matters
2. **Consider data flags**: Use `requestIndividualData` when calculating per-pawn priorities
3. **Validate ratio operations**: Ensure the second operand won't be zero, or use a static value
4. **Use static values wisely**: Good for thresholds, minimums, maximums, and ideal values
5. **Test priority ranges**: Ensure your validRange covers expected calculation results
6. **Combine with other conditions**: Calculation conditions work alongside personality offsets and filters

## Technical Implementation

### Enum: `CalculationOperation`
Defined in `AutonomySupportingClasses.cs`:
```csharp
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
```

### Fields in `PriorityCondition`
Defined in `AutonomyDefs.cs`:
```csharp
public string infoDefName1;         // First InfoGiver name
public string infoDefName2;         // Second InfoGiver name
public float value1 = 0f;           // First static value (if no InfoGiver)
public float value2 = 0f;           // Second static value (if no InfoGiver)
public CalculationOperation operation = CalculationOperation.sum;
```

### Evaluation
Handled in `PriorityGiverManager.cs` by the `EvaluateCalculationCondition` method, which:
1. Retrieves value1 (from InfoGiver or static field)
2. Retrieves value2 (from InfoGiver or static field)
3. Applies the specified operation
4. Returns the result for use in priority range matching

## Related Documentation

- See `FILTER_CONDITION_IMPLEMENTATION.md` for filter conditions
- See `PERSONALITY_SYSTEM.md` for personality-based multipliers
- See `XML_REFERENCE.md` for complete PriorityGiver XML reference
- See `InfoGivers_Table.csv` for available InfoGivers
