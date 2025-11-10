# Filter Condition Implementation

## Overview

This document describes the implementation of the `filter` condition type for PriorityGivers, which provides an outer layer filtering mechanism for pawns.

## Purpose

The filter condition type allows PriorityGivers to pre-filter pawns before evaluating other conditions. If a pawn fails the filter, the PriorityGiver returns 0 priority immediately without evaluating InfoGivers or calculating offsets. This is particularly useful for:

1. Creating specialized priority givers for specific pawn types
2. Avoiding unnecessary calculations for pawns that shouldn't be affected
3. Implementing setting-based priority paths (e.g., self-tend enabled/disabled)

## Implementation Details

### Core Components

1. **New ConditionType**: `filter`
   - Added to `ConditionType` enum in `AutonomySupportingClasses.cs`
   - Acts as an outer layer filter, evaluated before all other conditions

2. **New PriorityCondition Field**: `filters`
   - Type: `InfoFilters`
   - Contains the same filter structure used by InfoGivers
   - Supports include/exclude lists

3. **New Filter Options**:
   - `selfTendEnabled` - Only includes pawns with self-tend enabled (player colonists and slaves)
   - `selfTendDisabled` - Only includes pawns with self-tend disabled (player colonists and slaves)

### Evaluation Order

The filter condition is evaluated in a special two-pass system:

**Pass 1: Filter Evaluation (Early Exit)**
```
For each condition in PriorityGiver.conditions:
    If condition.type == filter:
        If pawn fails filter:
            Return (0, "Does not match filter criteria")
```

**Pass 2: Normal Condition Evaluation**
```
For each condition in PriorityGiver.conditions:
    If condition.type == personalityOffset:
        Apply personality multiplier
    Else if condition.type == infoGiver:
        Evaluate InfoGiver and match priority range
    (filter type is skipped here, already evaluated)
```

This ensures:
- Filters are evaluated FIRST
- Failed filters exit immediately (performance optimization)
- Passed filters allow normal priority calculation to proceed

### Code Changes

#### 1. AutonomySupportingClasses.cs
- Added `filter` to `ConditionType` enum
- Updated `InfoFilters` documentation to include new filter options

#### 2. AutonomyDefs.cs
- Added `filters` field to `PriorityCondition` class
- Added validation in `ResolveReferences()` to ensure filter conditions have filters defined

#### 3. PriorityGiverManager.cs
- Modified `EvaluatePriorityGiverForPawnWithDescription()` to evaluate filters first
- Added `EvaluateFilterCondition()` method that:
  - Checks inclusion filters (pawn must match at least one)
  - Checks exclusion filters (pawn must not match any)
  - Handles selfTendEnabled/selfTendDisabled special cases
  - Returns true if pawn passes all filters, false otherwise

#### 4. PatientGivers.xml
- Updated `NonBleedingInjuryPriorityGiverSelfTend` to use filter condition
- Changed parent from `SeverityInfectionChancePriority` to `NonBleedingInjuryPriorityGiver`
- Added filter condition with `selfTendEnabled` inclusion

## Usage Example

### Basic Filter Usage

```xml
<Autonomy.PriorityGiverDef>
    <defName>ExampleFilteredPriority</defName>
    <label>Example filtered priority</label>
    <description>Only applies to specific pawns</description>
    
    <conditions>
        <!-- Filter: Only apply to colonists with self-tend enabled -->
        <li>
            <type>filter</type>
            <filters>
                <include>
                    <li>selfTendEnabled</li>
                </include>
            </filters>
        </li>
        
        <!-- Regular InfoGiver condition -->
        <li>
            <type>infoGiver</type>
            <infoDefName>SomeInfoGiver</infoDefName>
            <requestIndividualData>true</requestIndividualData>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>0~10</validRange>
            <priority>5</priority>
            <description>Low priority</description>
        </li>
        <li>
            <validRange>11~100</validRange>
            <priority>10</priority>
            <description>High priority</description>
        </li>
    </priorityRanges>
    
    <targetWorkGivers>
        <li>SomeWorkGiver</li>
    </targetWorkGivers>
</Autonomy.PriorityGiverDef>
```

### Combined Filters

```xml
<conditions>
    <li>
        <type>filter</type>
        <filters>
            <include>
                <li>player</li>        <!-- Must be a player colonist -->
                <li>selfTendEnabled</li> <!-- Must have self-tend enabled -->
            </include>
            <exclude>
                <li>downed</li>        <!-- Must not be downed -->
            </exclude>
        </filters>
    </li>
</conditions>
```

## Performance Considerations

1. **Early Exit Optimization**: Filters are evaluated before expensive InfoGiver calculations
2. **Simple Boolean Logic**: Filter evaluation uses simple boolean checks on pawn properties
3. **No InfoGiver Lookups**: Filter conditions don't require InfoGiver data retrieval

## Self-Tend Filter Implementation

The `selfTendEnabled` and `selfTendDisabled` filters check:

1. **Pawn Type**: Only player colonists and slaves can have self-tend settings
2. **Setting Value**: Checks `pawn.playerSettings.selfTend` boolean
3. **Safety**: Returns false for pawns without playerSettings (animals, prisoners, etc.)

### Code Logic

```csharp
case "selftendenabled":
    if ((pawn.Faction == Faction.OfPlayer && pawn.IsColonist) || pawn.IsSlave)
    {
        if (pawn.playerSettings != null && pawn.playerSettings.selfTend)
            passedInclusion = true;
    }
    break;
    
case "selftenddisabled":
    if ((pawn.Faction == Faction.OfPlayer && pawn.IsColonist) || pawn.IsSlave)
    {
        if (pawn.playerSettings != null && !pawn.playerSettings.selfTend)
            passedInclusion = true;
    }
    break;
```

## Expected Behavior for NonBleedingInjuryPriorityGiverSelfTend

When a pawn evaluates this PriorityGiver:

1. **Filter Check**: Is self-tend enabled?
   - **No**: Return 0 priority immediately, skip all other conditions
   - **Yes**: Continue to next condition

2. **InfoGiver Check**: What is the pawn's non-bleeding injury severity?
   - Queries `TotalColonyNonBleedingInjuries` with individual data
   - Gets the pawn's specific injury severity value

3. **Priority Range Matching**: Map severity to priority
   - 0 = 0 priority (no injuries)
   - 0.01-10 = 5-10 priority (minor injuries)
   - 11-30 = 10-20 priority (moderate injuries)
   - 31-60 = 21-40 priority (serious injuries)
   - 61+ = 40 priority (very serious injuries)

4. **Personality Multipliers**: Apply personality-based adjustments
   - Expectation, SelfInterest, Tenacity, Bravery affect final priority

5. **Final Result**: Adjusted priority is assigned to self-tend work givers
   - `DoctorTendToSelf`
   - `DoctorTendToSelfEmergency`

## Future Extensions

The filter condition system can be extended to support:

1. **Additional Filter Types**:
   - Gene-based filters (e.g., `hasGene:Nearsighted`)
   - Trait-based filters (e.g., `hasTrait:Brawler`)
   - Capability-based filters (e.g., `canManipulate`)

2. **Complex Filter Logic**:
   - AND/OR combinations
   - Nested filter groups
   - Conditional filter chains

3. **Performance Optimizations**:
   - Filter result caching
   - Lazy evaluation
   - Filter pre-compilation

## Testing Recommendations

To test the filter condition implementation:

1. **Basic Functionality**:
   - Create pawns with self-tend enabled and disabled
   - Verify only self-tend enabled pawns get priority from the self-tend giver
   - Verify pawns without injuries get 0 priority even with self-tend enabled

2. **Edge Cases**:
   - Test with animals (should not match selfTendEnabled)
   - Test with prisoners (should not match selfTendEnabled)
   - Test with slaves (should match selfTendEnabled if setting is on)

3. **Performance**:
   - Verify filters are evaluated before InfoGivers (check logs)
   - Ensure no InfoGiver calculations for filtered-out pawns
   - Test with large colonies (20+ pawns)

4. **Integration**:
   - Verify parent def inheritance works correctly
   - Test interaction with personality multipliers
   - Ensure filter + InfoGiver conditions work together

## Conclusion

The filter condition type provides a clean, performant way to create specialized PriorityGivers that only apply to specific pawn types. The implementation follows the existing mod architecture and integrates seamlessly with the InfoGiver system.
