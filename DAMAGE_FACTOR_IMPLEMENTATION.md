# Damage Factor Gene System - Implementation Summary

## Overview
Extended the gene-based InfoGiver system to support targeting genes by damage factors and returning the **sum** of damage multipliers. This enables dynamic priority adjustments based on genetic vulnerabilities (like tinderskin's 4x fire damage).

## New Features Added

### 1. Core System Enhancement

#### `AutonomyDefs.cs` (InfoGiverDef)
Added new field:
- `targetDamageFactor` (string) - Target damage type (e.g., "Flame", "Blunt", "Sharp", "Heat")
  - When set, returns the **sum** of damage factors instead of gene count
  - Example: Tinderskin has Flame damage factor of 4.0, so it returns 4.0

#### Updated Validation
- Added `targetDamageFactor` to gene targeting validation criteria
- Ensures at least one targeting method is specified

### 2. InfoGiverManager Implementation

#### New Methods
1. **`GetPawnGeneDamageFactor()`**
   - Sums damage factors for specified damage type
   - Filters genes by additional criteria (if specified)
   - Returns total damage multiplier for the pawn

2. **`GeneMatchesOtherFilters()`**
   - Helper method for filtering genes when summing damage factors
   - Checks all non-damage-factor filters (defName, class, mental break, biostats)
   - Allows combining damage factor targeting with other filters

#### Updated Methods
1. **`GetPawnGeneCount()`**
   - Now branches based on whether `targetDamageFactor` is set
   - Returns damage factor sum when targeting damage types
   - Returns gene count for normal gene targeting

2. **`GeneMatchesFilter()`**
   - Added damage factor matching logic
   - Checks if gene has damageFactors for the specified damage type
   - Integrated with existing filter logic

## Implementation Details

### How Damage Factor Targeting Works

1. **InfoGiver checks `targetDamageFactor` field**
   - If set, switches to damage factor sum mode
   - If not set, uses normal gene counting

2. **For each pawn's genes:**
   - Check if gene has damageFactors defined
   - Look for matching DamageDef
   - Apply additional filters (if any)
   - Sum the factor values

3. **Return total sum per pawn**
   - Individual data tracks sum per pawn
   - Can compare against colony average
   - Enables scaled priority adjustments

### Supported Damage Types
Common damage types that can be targeted:
- `Flame` - Fire damage (tinderskin = 4.0x)
- `Blunt` - Blunt damage
- `Sharp` - Sharp/cutting damage
- `Heat` - Heat damage
- `Cold` - Cold damage
- `Electric` - Electrical damage

### Combining with Other Filters

You can combine damage factor targeting with other gene filters:

```xml
<targetDamageFactor>Flame</targetDamageFactor>
<filterBiostatMet>>=2</filterBiostatMet> <!-- Only expensive genes -->
<targetGeneClasses>                       <!-- Or specific classes -->
    <li>Gene_Custom</li>
</targetGeneClasses>
```

## Test Implementation

### FireWeaknessGeneInfoGiver
- Targets genes with Flame damage factors
- Returns sum of factors (e.g., 4.0 for tinderskin)
- Individualizable for per-pawn tracking
- Urgent evaluation for fire emergencies

### FirefightingFireWeaknessPriority
- Scaled priority reduction based on damage factor
- Priority ranges:
  - 0.0: No effect (no fire weakness)
  - 0.1-1.5: -5 priority (slightly vulnerable)
  - 1.51-2.5: -10 priority (vulnerable)
  - 2.51-3.5: -20 priority (very vulnerable)
  - 3.51+: -30 priority (extremely vulnerable, like tinderskin)

### Personality Integration
- **Bravery**: Brave pawns reduce avoidance (0.75x), cowards increase (1.5x)
- **Tension**: High tension increases avoidance
- **Deliberation**: Deliberate pawns carefully assess risk (up to 2x multiplier)
- **Stability**: Stable pawns less affected by vulnerability

## Key Differences: Count vs Sum

### Normal Gene Count (`targetGenes`, `targetGeneClasses`, etc.)
- Returns **count** of matching genes
- Example: Pawn with 2 archite genes = result of 2

### Damage Factor Sum (`targetDamageFactor`)
- Returns **sum** of damage factor values
- Example: Pawn with tinderskin = result of 4.0 (the damage multiplier)
- Enables proportional priority adjustments

## Use Cases

### 1. **Fire Vulnerability Management**
- Tinderskin carriers avoid firefighting
- Proportional to vulnerability level
- Modified by personality traits

### 2. **Combat Role Assignment**
- High Sharp damage resistance → frontline
- High Blunt damage resistance → melee
- Low resistance → ranged support

### 3. **Environmental Work**
- Cold/Heat resistance affects outdoor work
- Electric resistance for power work
- Toxic resistance for hazardous cleanup

### 4. **Medical Prioritization**
- High vulnerability = higher medical priority
- Damage factors affect treatment urgency
- Risk-based work assignment

## Documentation Updates

### Wiki (`InfoGiver-GeneCount.md`)
- Added section on damage factor targeting
- Explained sum vs count behavior
- Complete tinderskin example with personality integration
- Added personality multiplier explanation

### XML Reference (`XML_REFERENCE.md`)
- Added `targetDamageFactor` field to InfoGiverDef table
- Added fire weakness example
- Included damage type reference

## Technical Notes

### Performance
- Damage factor lookup is efficient (uses LINQ FirstOrDefault)
- No additional memory overhead
- Same evaluation frequency as normal gene counting

### Compatibility
- Works with all vanilla damage types
- Compatible with modded damage types via DefDatabase
- Gracefully handles missing damage definitions

### Edge Cases Handled
- Genes without damageFactors return 0
- Unknown damage types log warning and return 0
- Multiple genes with same damage type sum correctly
- Works with additional filters simultaneously

## Testing Recommendations

1. **Basic Functionality**
   - Test with tinderskin gene (should return 4.0)
   - Test without fire weakness (should return 0.0)
   - Verify sum for multiple vulnerability genes

2. **Priority Scaling**
   - Start fire near tinderskin pawn
   - Verify they avoid firefighting (-30 priority)
   - Test personality modifier effects

3. **Edge Cases**
   - Pawns without genes system (animals)
   - Custom/modded damage types
   - Combination with other gene filters

4. **Performance**
   - Multiple pawns with various genes
   - Large fires with many candidates
   - Urgent evaluation frequency

## Future Enhancements

1. **Resistance Tracking**
   - Track damage resistance (factors < 1.0)
   - Prioritize resistant pawns for dangerous work

2. **Damage Type Combinations**
   - Target multiple damage types
   - Return max/min/average across types

3. **Threshold Filtering**
   - Only return factors above threshold
   - Filter out minor vulnerabilities

4. **Armor Integration**
   - Combine gene factors with equipped armor
   - Total vulnerability calculation

## Build Status
✅ Compiled successfully with no errors  
✅ All documentation updated  
✅ Test implementation ready
