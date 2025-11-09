# Filter Condition - Quick Reference Guide

## Basic Usage

### Minimal Filter Example
```xml
<conditions>
    <li>
        <type>filter</type>
        <filters>
            <include>
                <li>selfTendEnabled</li>
            </include>
        </filters>
    </li>
</conditions>
```

## Available Filter Options

### Basic Pawn Types
- `player` - Player faction colonists
- `prisoner` - Prisoners
- `guest` - Guests
- `animal` - Player faction animals
- `hostile` - Hostile faction pawns
- `slave` - Slaves

### Special States
- `dead` - Dead pawns (use in exclude)
- `downed` - Downed pawns (use in exclude)

### Self-Tend Filters (NEW)
- `selfTendEnabled` - Pawns with self-tend enabled (colonists/slaves only)
- `selfTendDisabled` - Pawns with self-tend disabled (colonists/slaves only)

## Common Patterns

### Pattern 1: Single Inclusion
Only apply to one pawn type:
```xml
<filters>
    <include>
        <li>selfTendEnabled</li>
    </include>
</filters>
```

### Pattern 2: Multiple Inclusions (OR logic)
Apply to any of several pawn types:
```xml
<filters>
    <include>
        <li>player</li>
        <li>slave</li>
    </include>
</filters>
```
→ Matches if pawn is player OR slave

### Pattern 3: Exclusion Only
Apply to all except specified types:
```xml
<filters>
    <exclude>
        <li>downed</li>
        <li>dead</li>
    </exclude>
</filters>
```
→ Matches any pawn that is NOT downed and NOT dead

### Pattern 4: Combined Include + Exclude
Apply to specific types, excluding certain states:
```xml
<filters>
    <include>
        <li>player</li>
    </include>
    <exclude>
        <li>downed</li>
    </exclude>
</filters>
```
→ Matches only player colonists who are not downed

## Filter Logic Rules

### Inclusion Rules
- If NO include filters: all pawns pass
- If ANY include filters: pawn must match AT LEAST ONE
- Pawn fails if it doesn't match any include filter

### Exclusion Rules
- If NO exclude filters: all pawns pass
- If ANY exclude filters: pawn must NOT match ANY
- Pawn fails if it matches any exclude filter

### Combined Rules
```
Pass = (No includes OR matches any include) 
       AND 
       (No excludes OR matches no exclude)
```

## Complete Example

```xml
<Autonomy.PriorityGiverDef>
    <defName>SelfTendInjuryPriority</defName>
    <label>Self-tend injury priority</label>
    <description>Increases priority for self-tending injuries</description>
    
    <conditions>
        <!-- Filter: only self-tend enabled pawns -->
        <li>
            <type>filter</type>
            <filters>
                <include>
                    <li>selfTendEnabled</li>
                </include>
            </filters>
        </li>
        
        <!-- InfoGiver: check injury severity -->
        <li>
            <type>infoGiver</type>
            <infoDefName>TotalColonyNonBleedingInjuries</infoDefName>
            <infoRange>0~999</infoRange>
            <requestIndividualData>true</requestIndividualData>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>0</validRange>
            <priority>0</priority>
            <description>No injuries</description>
        </li>
        <li>
            <validRange>0.01~10</validRange>
            <priority>5~10</priority>
            <description>Minor injuries</description>
        </li>
        <li>
            <validRange>11~30</validRange>
            <priority>11~20</priority>
            <description>Moderate injuries</description>
        </li>
    </priorityRanges>
    
    <targetWorkGivers>
        <li>DoctorTendToSelf</li>
    </targetWorkGivers>
</Autonomy.PriorityGiverDef>
```

## Key Points

1. **Evaluation Order**: Filters are evaluated FIRST, before InfoGivers
2. **Early Exit**: Failed filters return 0 priority immediately
3. **Performance**: Avoids InfoGiver calculations for filtered pawns
4. **Multiple Filters**: Can have multiple filter conditions in one PriorityGiver
5. **Optional**: Filter conditions are optional, existing PriorityGivers work unchanged

## Self-Tend Filter Details

### What Pawns Can Have Self-Tend?
- Player colonists: ✅
- Slaves: ✅
- Prisoners: ❌ (filter returns false)
- Animals: ❌ (filter returns false)
- Guests: ❌ (filter returns false)

### Implementation Note
The filter checks:
1. Is pawn a colonist OR slave?
2. Does pawn have playerSettings?
3. Is playerSettings.selfTend true/false?

If any check fails, the pawn doesn't match the filter.

## Debugging Tips

### Filter Not Working?
1. Check pawn type is correct for filter
2. Verify XML syntax (correct tag names)
3. Ensure `<type>filter</type>` is specified
4. Check logs for filter evaluation errors

### Pawn Getting 0 Priority?
1. Filter might be excluding the pawn
2. Check both include and exclude lists
3. Verify pawn settings (e.g., self-tend)
4. Look for "Does not match filter criteria" in description

### All Pawns Getting 0 Priority?
1. Filter might be too restrictive
2. Check if filter options are valid
3. Verify filter logic (include OR, exclude AND)
4. Test without filter condition first

## Performance Notes

✅ **Good Performance:**
- Simple filter checks (boolean properties)
- Early exit for non-matching pawns
- No InfoGiver lookups for filtered pawns

⚠️ **Watch Out For:**
- Too many filter conditions (evaluate separately)
- Complex nested logic (use multiple PriorityGivers)
- Checking dynamic data (use InfoGivers instead)

## See Also

- `XML_REFERENCE.md` - Complete XML reference
- `FILTER_CONDITION_IMPLEMENTATION.md` - Technical documentation
- `FilterConditionExamples.xml` - More examples
- `PatientGivers.xml` - Real implementation example
