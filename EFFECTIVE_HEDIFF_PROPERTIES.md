# Effective Hediff Properties Implementation

## Overview

Added two new hediff properties that calculate the **actual** (effective) immunity and severity rates for diseases, considering all game factors like bed quality, food, pawn stats, tending quality, and random modifiers.

## Problem

The previous `immunityPerDay` and `severityPerDay` properties only returned **base rates** from the hediff definitions. They did NOT consider:
- ImmunityGainSpeed stat (modified by bed quality, food quality, medical conditions)
- Bed rest quality
- Food quality and nutrition
- Medicine quality used in tending
- Random factors (0.8x to 1.2x variation)
- Severity modifiers from other hediffs
- Room cleanliness
- Any other game factors

This meant bed rest priorities were calculated using theoretical base rates instead of actual disease progression rates, leading to incorrect prioritization decisions.

## Solution

Implemented two new hediff properties that tap into RimWorld's actual immunity and severity calculation systems:

### 1. `effectiveImmunityPerDay`
- **What it does:** Returns the ACTUAL immunity gain per day for each disease
- **Implementation:** Uses `ImmunityRecord.ImmunityChangePerTick()` × 60000
- **Considers:**
  - Base `immunityPerDaySick` from hediff definition
  - Pawn's `ImmunityGainSpeed` stat (affected by bed quality, food, medical care)
  - Random factor (0.8x to 1.2x) unique to each disease instance
  - All other game modifiers

### 2. `effectiveSeverityPerDay`
- **What it does:** Returns the ACTUAL severity change per day for each disease
- **Implementation:** Uses `HediffComp_Immunizable.SeverityChangePerDay()`
- **Considers:**
  - Base severity rate (NotImmune or Immune variant)
  - Random severity factor unique to the disease instance
  - Severity modifiers from other hediffs
  - All other game factors

## Changes Made

### Code Changes (`Source/Components/InfoGiverManager.cs`)

Added two new case blocks in the `GetPawnHediffValue()` method:

1. **effectiveimmunityperday**: Gets immunity record and calls `ImmunityChangePerTick()`, multiplies by 60000 to convert from per-tick to per-day
2. **effectiveseverityperday**: Gets immunizable comp and calls `SeverityChangePerDay()` which already returns effective daily rate

### XML Definition Updates (`1.6/Defs/BedRestPriorityGivers.xml`)

Updated the bed rest priority system to use effective rates:

**Old (INCORRECT):**
```xml
<defName>SeverityPerDay</defName>
<hediffProperty>severityPerDay</hediffProperty>

<defName>ImmunizationPerDay</defName>
<hediffProperty>immunityPerDay</hediffProperty>
```

**New (CORRECT):**
```xml
<defName>EffectiveSeverityPerDay</defName>
<hediffProperty>effectiveSeverityPerDay</hediffProperty>

<defName>EffectiveImmunizationPerDay</defName>
<hediffProperty>effectiveImmunityPerDay</hediffProperty>
```

### Documentation Updates

1. **`wiki/InfoGiver-HediffCount.md`**: Added detailed explanation of base vs effective properties with examples
2. **`XML_REFERENCE.md`**: Updated hediffProperty list with recommendations

## Usage Recommendations

### DO USE (Recommended):
- `effectiveImmunityPerDay` - For accurate disease management
- `effectiveSeverityPerDay` - For accurate disease progression tracking

### DON'T USE (Not Recommended):
- `immunityPerDay` - Only gives base rate, ignores pawn conditions
- `severityPerDay` - Only gives base rate, ignores modifiers

## Example Comparison

**Scenario:** Colonist with flu in a poor bed vs hospital bed

**Base Properties (WRONG):**
- `immunityPerDay`: 0.15 (same for both beds)
- `severityPerDay`: 0.5 (same for both beds)
- Result: No difference in priority

**Effective Properties (CORRECT):**
- Poor bed:
  - `effectiveImmunityPerDay`: ~0.12 (0.15 × 0.8 ImmunityGainSpeed × random)
  - `effectiveSeverityPerDay`: ~0.55 (0.5 × random × modifiers)
  - Net: -0.43/day (disease winning)
  
- Hospital bed:
  - `effectiveImmunityPerDay`: ~0.195 (0.15 × 1.3 ImmunityGainSpeed × random)
  - `effectiveSeverityPerDay`: ~0.55 (same)
  - Net: -0.355/day (still losing but better)

Result: Correct prioritization based on actual bed quality

## Technical Details

### Immunity Calculation Flow
1. `effectiveImmunityPerDay` property requested
2. Get `ImmunityRecord` for the disease from pawn's immunity handler
3. Call `ImmunityRecord.ImmunityChangePerTick(pawn, sick: true, hediff)`
4. This internally:
   - Gets base `immunityPerDaySick` from `HediffCompProperties_Immunizable`
   - Multiplies by pawn's `ImmunityGainSpeed` stat
   - Applies random factor (0.8-1.2) based on disease instance seed
   - Returns per-tick rate
5. Multiply by 60000 to convert to per-day

### Severity Calculation Flow
1. `effectiveSeverityPerDay` property requested
2. Get `HediffComp_Immunizable` component from the disease hediff
3. Call `immunizableComp.SeverityChangePerDay()`
4. This internally:
   - Checks if fully immune
   - Gets appropriate base rate (`severityPerDayImmune` or `severityPerDayNotImmune`)
   - Multiplies by random severity factor (stored per disease instance)
   - Multiplies by severity modifiers from other hediffs
   - Returns effective daily rate

## Benefits

1. **Accurate Priorities**: Bed rest priorities now reflect actual disease progression
2. **Responsive to Quality**: Better beds/food/care result in appropriate priority changes
3. **Individual Variance**: Each disease instance has unique progression due to random factors
4. **Game-Accurate**: Uses same calculation methods as RimWorld's core disease system
5. **Future-Proof**: Will automatically benefit from any game balance changes to immunity/severity systems

## Backward Compatibility

- Old `immunityPerDay` and `severityPerDay` properties still work (for base rates)
- Existing definitions using old properties will continue to function
- Recommended to update to effective properties for better accuracy
