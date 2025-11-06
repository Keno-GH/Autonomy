# HediffCount InfoGiver Implementation Summary

## Overview
Successfully implemented a new `hediffCount` source type for InfoGivers that enables tracking and measuring hediffs (health conditions, injuries, diseases) in individual pawns. This allows for dynamic medical priority systems and emergency response automation.

## Architectural Refactor (Latest Update)
The hediffCount system was refactored to use the modular `filters.hediffs` array structure instead of standalone filter boolean fields. This change:
- **Improves modularity**: Hediff filters are now defined within the filters object alongside pawn type filters
- **Enhances consistency**: Uses the same HediffFilter class for both pawnCount and hediffCount source types
- **Enables multiple filters**: Can define multiple hediff filter criteria (OR logic between filters, AND logic within a filter)
- **Simplifies maintenance**: All filter logic is centralized in the HediffFilter class

**Before (old structure):**
```xml
<targetHediffClasses>
    <li>Hediff_Injury</li>
</targetHediffClasses>
<filterTendable>true</filterTendable>
<filterTended>false</filterTended>
```

**After (new structure):**
```xml
<filters>
    <hediffs>
        <li>
            <hediffClass>Hediff_Injury</hediffClass>
            <tendable>true</tendable>
            <tended>false</tended>
        </li>
    </hediffs>
</filters>
```

## Changes Made

### 1. Core System Changes

#### `AutonomySupportingClasses.cs`
- Added `hediffCount` to `InfoSourceType` enum

#### `AutonomySupportingClasses.cs` (HediffFilter)
Extended HediffFilter class with new fields:
- `tended` - Filter by whether hediff has been tended (renamed from hediffTended)
- `hasInfectionChance` - Filter by whether hediff has infection chance
- `hasBleedRate` - Filter by whether hediff has bleed rate
- `isImmunizable` - Filter by whether hediff is immunizable

#### `AutonomyDefs.cs` (InfoGiverDef)
Added hediff-targeting field:
- `hediffProperty` - Property to extract (severity, bleedRate, infectionChance, etc.)
- Removed standalone filter fields (now in filters.hediffs array)
- Updated validation in `ResolveReferences()` to check for filters.hediffs array

#### `InfoGiverManager.cs`
Added hediff evaluation functionality:
- Updated `CanBeIndividualized()` to include hediffCount
- Added `EvaluateHediffCount()` method
- Added `GetPawnHediffValue()` method with support for extracting:
  - count (default)
  - severity
  - bleedRate
  - infectionChance
  - immunityPerDay
  - severityPerDay
  - painOffset
- Refactored `HediffMatchesFilter()` method to accept HediffFilter parameter:
  - Hediff class matching (including common base classes)
  - Tendable/tended status filtering
  - Infection chance filtering
  - Bleed rate filtering
  - Immunizable status filtering
- Updated `PawnMatchesHediffFilter()` to support new filter fields (for pawnCount compatibility)
- Updated `CollectIndividualizedData()` to handle hediffCount
- Added `CollectIndividualHediffCountData()` method
- Added case for hediffCount in main `EvaluateInfoGiver()` switch

### 2. Example Definitions

#### `PatientGivers.xml`
Updated 5 comprehensive example InfoGivers to use new filters.hediffs structure:

1. **InjuredPawnNeedsTending**
   - Uses pawnCount with filters.hediffs array
   - Backward compatible implementation
   - Use case: Track pawns needing medical attention

2. **UntendedInfectionRiskSeverity**
   - Uses hediffCount with filters.hediffs array
   - Sums severity of untended injuries with infection chance
   - Individualizable, urgent
   - Use case: Prioritize medical care for serious infection risks

3. **BleedingInjuryCount**
   - Uses hediffCount with filters.hediffs array
   - Counts all bleeding injuries
   - Individualizable, urgent
   - Use case: Emergency medical response

4. **TotalColonyBleedRate**
   - Uses hediffCount with filters.hediffs array
   - Sums bleed rate across all pawns
   - Urgent
   - Use case: Assess medical emergency severity

5. **ImmunizableDiseaseCount**
   - Uses hediffCount with filters.hediffs array
   - Counts immunizable diseases (flu, plague, etc.)
   - Individualizable, urgent
   - Use case: Disease outbreak management

### 3. Documentation

#### New Wiki Page: `InfoGiver-HediffCount.md`
Comprehensive documentation including:
- Overview of hediff tracking system
- All targeting methods with examples
- Property extraction with descriptions
- Filter options with use cases
- Complete examples for various medical scenarios
- PriorityGiver integration example
- Technical notes and limitations
- Use case scenarios

#### Updated: `XML_REFERENCE.md`
- Added hediffCount to source types table
- Added hediff-related fields to InfoGiverDef fields table
- Added complete medical emergency example with InfoGiver + PriorityGiver
- Added list of available hediffProperty values
- Updated field descriptions

## Key Features

### Hediff Targeting
1. **By DefName**: Target specific hediffs (WoundInfection, Flu, etc.)
2. **By Class**: Target hediff types (Hediff_Injury, HediffWithComps, etc.)
3. **By Properties**: Filter by tendable, tended, infection chance, bleed rate, immunizable

### Property Extraction
- **count**: Number of matching hediffs
- **severity**: Severity values
- **bleedRate**: Bleeding rates (injuries only)
- **infectionChance**: Infection probabilities
- **immunityPerDay**: Immunity gain rates
- **severityPerDay**: Severity change rates
- **painOffset**: Pain values

### Calculation Support
All calculation types work with hediffCount:
- Count, Sum, Avg, Max, Min

### Individualization
Always individualizable - tracks per-pawn data:
- Individual hediff measurements
- Distance from colony average
- Enables personalized medical priorities

## Use Cases

### 1. Emergency Medical Response
- Track bleeding injuries for immediate attention
- Monitor infection risk for preventive care
- Identify critical severity cases

### 2. Disease Management
- Track epidemic spread across colony
- Monitor immunity development
- Prioritize treatment for severe cases

### 3. Individual Health Monitoring
- Compare pawn health against colony average
- Identify pawns with unusual medical conditions
- Personalize medical work priorities

### 4. Medical Resource Allocation
- Prioritize medical work based on severity
- Allocate doctors to most critical cases
- Balance treatment across multiple patients

## Technical Details

### Performance
- Efficient hediff iteration (only checks health.hediffSet.hediffs)
- Individual data cached per evaluation cycle
- Urgent InfoGivers: 10 ticks (0.17s)
- Normal InfoGivers: 100 ticks (1.7s)

### Compatibility
- Works with all vanilla hediffs
- Compatible with modded hediffs
- Safe with missing definitions (logs warning)
- Works with all DLC content

### Limitations
- Cannot directly filter by body part
- Cannot check hediff stage (use severity ranges)
- Property extraction requires appropriate hediff type
- Some properties return 0 if hediff doesn't support them

## Testing Recommendations

1. **Infection Risk Tracking**
   - Create injuries on colonists
   - Verify UntendedInfectionRiskSeverity returns correct severity sum
   - Test with various injury severities

2. **Bleeding Emergency**
   - Create bleeding injuries
   - Verify BleedingInjuryCount returns correct count
   - Verify TotalColonyBleedRate returns sum of bleed rates

3. **Disease Outbreak**
   - Infect colonists with flu/plague
   - Verify ImmunizableDiseaseCount tracks correctly
   - Test individualization (per-pawn values)

4. **Filter Combinations**
   - Test multiple filter combinations
   - Verify tendable + untended + infection filters work together
   - Test edge cases (no hediffs, all filtered out)

5. **Property Extraction**
   - Test each hediffProperty value
   - Verify correct values for different hediff types
   - Test unsupported properties (should return 0)

## Integration Notes

HediffCount InfoGivers can be used with:
- PriorityGivers (medical work prioritization)
- SkillGivers (doctor skill requirements)
- Personality system (brave doctors handle severe cases)
- Localization system (future: room-based medical care)

## Future Enhancements (Potential)

1. **Body Part Filtering**
   - Filter hediffs by affected body part
   - Example: Track only torso injuries

2. **Stage-Based Filtering**
   - Filter hediffs by current stage
   - Example: Only severe disease stages

3. **Tendency Tracking**
   - Track hediff progression over time
   - Example: Worsening vs improving conditions

4. **Comparative Analysis**
   - Compare hediff severity to immunity
   - Example: Critical race conditions

## Files Modified

- `Source/Defs/AutonomySupportingClasses.cs`
- `Source/Defs/AutonomyDefs.cs`
- `Source/Components/InfoGiverManager.cs`
- `1.6/Defs/PatientGivers.xml`
- `wiki/InfoGiver-HediffCount.md` (new)
- `XML_REFERENCE.md`

## Conclusion

The hediffCount InfoGiver source type provides a powerful and flexible system for tracking medical conditions and automating medical work priorities. It follows the same architecture pattern as geneCount, ensuring consistency with the mod's design philosophy while addressing a critical gameplay need: dynamic medical emergency response.
