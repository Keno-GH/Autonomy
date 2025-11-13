# Enhanced Pawn Filter System Implementation

## Overview
This document describes the enhanced pawn filter system for InfoGivers that supports chained conditions with race, faction, and status filters.

## Purpose
The enhanced filter system allows InfoGivers to target specific pawns using complex, chained conditions. This is particularly useful for animal-related filters where we need to check multiple attributes like:
- Race (Humanlike, AnimalAny, specific race)
- Faction (player, non_player, hostile)
- Status (roaming, unenclosed, prisoner, dead, downed, etc.)

## Architecture

### PawnFilter Class
Located in `Source/Defs/AutonomySupportingClasses.cs`

```csharp
public class PawnFilter
{
    public string race = "Any";           // "Humanlike", "AnimalAny", "Any", or specific race def
    public string faction = "Any";        // "player", "non_player", "hostile", "Any"
    public List<string> status = new List<string>();  // AND logic for status checks
}
```

### InfoFilters Update
The `InfoFilters` class now supports both:
1. **NEW**: `List<PawnFilter> pawnFilters` and `List<PawnFilter> excludePawnFilters`
2. **LEGACY**: `List<string> include` and `List<string> exclude` (for backward compatibility)

### Logic Flow
- Multiple `PawnFilter` entries in `pawnFilters` use **OR** logic (pawn matches ANY filter)
- Multiple status entries within a single `PawnFilter` use **AND** logic (ALL must match)
- `excludePawnFilters` work the same way but exclude matching pawns

## Supported Status Filters

### General Status
- `prisoner` - Pawn is a prisoner
- `guest` - Pawn is a guest
- `dead` - Pawn is dead
- `downed` - Pawn is downed
- `colonist` - Pawn is a colonist
- `slave` - Pawn is a slave
- `animal` - Pawn is an animal

### Animal-Specific Status
- `roaming` - Animal has RoamMtbDays (is a roamer)
- `unenclosed` - Animal is not in an enclosed pen
- `unpenned` - Animal is a roamer AND not in an enclosed pen (combined check)

### Self-Tend Status
- `selftendenabled` - Pawn has self-tend enabled
- `selftenddisabled` - Pawn has self-tend disabled

## XML Examples

### Example 1: Roaming Unenclosed Animals (Using Combined Status)
```xml
<filters>
    <pawnFilters>
        <li>
            <race>AnimalAny</race>
            <faction>player</faction>
            <status>
                <li>unpenned</li>
            </status>
        </li>
    </pawnFilters>
    <excludePawnFilters>
        <li>
            <race>Any</race>
            <faction>Any</faction>
            <status>
                <li>dead</li>
            </status>
        </li>
    </excludePawnFilters>
</filters>
```
This filter:
1. Includes player faction animals that are unpenned (roamers AND not in enclosed pens)
2. Excludes any dead pawns

Note: You can also use separate `roaming` and `unenclosed` status checks if you need them independently.

### Example 2: Legacy Format (Still Supported)
```xml
<filters>
    <include>
        <li>player</li>
        <li>animal</li>
    </include>
    <exclude>
        <li>dead</li>
        <li>downed</li>
    </exclude>
</filters>
```

### Example 3: Complex Multi-Condition
```xml
<filters>
    <pawnFilters>
        <!-- Humanlike player colonists -->
        <li>
            <race>Humanlike</race>
            <faction>player</faction>
            <status>
                <li>colonist</li>
            </status>
        </li>
        <!-- OR hostile humanlike prisoners -->
        <li>
            <race>Humanlike</race>
            <faction>non_player</faction>
            <status>
                <li>prisoner</li>
            </status>
        </li>
        <!-- OR player faction animals -->
        <li>
            <race>AnimalAny</race>
            <faction>player</faction>
        </li>
    </pawnFilters>
</filters>
```

## Implementation Details

### InfoGiverManager.cs Changes

#### ApplyPawnTypeFilters Method
- Checks for new `pawnFilters` first, falls back to legacy `include` if not present
- Uses `PawnMatchesPawnFilter` method to evaluate each filter
- Maintains complete backward compatibility

#### PawnMatchesPawnFilter Method
Evaluates a single PawnFilter:
1. Checks race (if specified)
2. Checks faction (if specified)
3. Checks all status conditions (AND logic)

#### PawnMatchesStatus Method
Evaluates individual status strings:
- General statuses (prisoner, guest, dead, downed, etc.)
- Animal-specific statuses (roaming, unenclosed)
- Self-tend statuses

#### IsAnimalUnenclosed Method
Complex logic to determine if an animal is not in an enclosed pen:
1. Gets the district the animal is in
2. Iterates through all pen markers
3. Checks if any enclosed pen contains the animal's district
4. Returns true if not in any enclosed pen

## Use Cases

### Primary Use Case: Roaming Animals Handler Priority
**InfoGiver**: `RoamingUnenclosedAnimals`
- Counts player faction animals that are roamers and not enclosed
- Used by `RoamingAnimalsPriority` PriorityGiver
- Increases handler work priority when animals need penning

**Files**:
- InfoGiver: `1.6/Defs/InfoGivers/AnimalInfoGivers.xml`
- PriorityGiver: `1.6/Defs/PriorityGivers/WorkTypes/HandlePriorityGivers.xml`

## Testing Checklist

1. ✅ Code compiles without errors
2. ⏳ Legacy filters still work (existing InfoGivers)
3. ⏳ New pawnFilters work correctly
4. ⏳ Roaming status detection works
5. ⏳ Unenclosed status detection works
6. ⏳ Combined roaming + unenclosed status works
7. ⏳ Priority increases when roaming animals detected
8. ⏳ Priority works with RopeToPen and RopeRoamerToUnenclosedPen work givers

## Future Enhancements

### Possible Additional Status Filters
- `hasMasterBond` - Animal has a master bond
- `pregnant` - Female animal is pregnant
- `hasMilk` - Animal can be milked
- `hasWool` - Animal can be sheared
- `trainable` - Animal can be trained
- `wildMan` - Pawn is a wild man
- `mechanoid` - Pawn is a mechanoid

### Possible Additional Race Filters
- `Insectoid` - Insect-like creatures
- `Mechanoid` - Mechanical entities
- Specific animal categories (e.g., `Herbivore`, `Carnivore`)

### Possible Additional Faction Filters
- `allied` - Allied factions
- `neutral` - Neutral factions
- `empire` - Empire faction (Royalty DLC)
- `pirate` - Pirate factions

## Related Documentation
- See `FILTER_CONDITION_IMPLEMENTATION.md` for filter condition system
- See `HEDIFF_SYSTEM_IMPLEMENTATION.md` for hediff filtering
- See `XML_REFERENCE.md` for general XML structure
