# Autonomy Mod - XML Reference Guide

## Quick Start Templates

### Basic PriorityGiverDef Template
```xml
<Autonomy.PriorityGiverDef>
    <defName>YourPriorityGiver</defName>
    <label>Your Priority Name</label>
    <description>What this priority does</description>
    
    <conditions>
        <li>
            <type>flat</type>
            <flatValue>10</flatValue>
            <calculation>flat</calculation>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>0~10</validRange>
            <priority>2</priority>
            <description>Priority explanation</description>
        </li>
    </priorityRanges>
    
    <targetWorkTypes>
        <li>WorkTypeName</li>
    </targetWorkTypes>
</Autonomy.PriorityGiverDef>
```

### Basic InfoGiverDef Template
```xml
<Autonomy.InfoGiverDef>
    <defName>YourInfoGiver</defName>
    <label>Your Info Source</label>
    <description>What this info giver provides</description>
    
    <sourceType>itemCount</sourceType>
    <calculation>sum</calculation>
    <isUrgent>false</isUrgent> <!-- Set to true for urgent matters like hostiles/fires -->
    
    <!-- Use categories for better mod compatibility -->
    <targetCategories>
        <li>CategoryDefName</li>
    </targetCategories>
    
    <!-- Advanced filtering -->
    <filters>
        <include>
            <li>player</li>
        </include>
    </filters>
</Autonomy.InfoGiverDef>
```

## Field Reference

### PriorityGiverDef Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `defName` | string | ✓ | Unique identifier |
| `label` | string | ✓ | Display name |
| `description` | string | ✓ | Explanation text |
| `conditions` | List | ✓ | Conditions to evaluate |
| `personalityOffsets` | List | ✗ | Trait-based modifications |
| `priorityRanges` | List | ✓ | Result → priority mapping |
| `targetWorkTypes` | List | ✓* | Work types to affect |
| `targetWorkGivers` | List | ✓* | Work givers to affect |

*Either `targetWorkTypes` OR `targetWorkGivers` is required.

### Condition Types

| Type | Description | Required Fields |
|------|-------------|-----------------|
| `flat` | Compare to fixed value | `flatValue` |
| `stat` | Check pawn stat | `stat` |
| `infoGiver` | Use InfoGiver result | `infoDefName` |
| `mapStat` | Check map statistic | `name` |

### Calculation Types

| Type | Description |
|------|-------------|
| `flat` | Use flat comparison |
| `sum` | Add all values |
| `avg` | Average of values |
| `max` | Highest value |
| `min` | Lowest value |
| `bestOf` | Best result |
| `worstOf` | Worst result |

### InfoGiverDef Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `defName` | string | ✓ | Unique identifier |
| `label` | string | ✓ | Display name |
| `description` | string | ✓ | Explanation text |
| `sourceType` | enum | ✓ | Type of data to collect |
| `calculation` | enum | ✗ | How to calculate result (default: sum) |
| `isUrgent` | bool | ✗ | Update every 400 ticks instead of 2000 (default: false) |
| `targetStat` | string | * | Required for pawnStat sourceType |
| `targetNeed` | string | * | Required for pawnNeed sourceType |
| `weatherProperty` | string | * | Required for weather sourceType |
| `targetItems` | List | * | Items to target (alternative to categories) |
| `targetCategories` | List | * | Thing categories to target |
| `conditions` | List | * | Required for mapCondition sourceType |
| `targetGenes` | List | * | Gene defNames for geneCount sourceType |
| `targetGeneClasses` | List | * | Gene class names for geneCount sourceType |
| `targetMentalBreakDef` | string | * | Mental break def for gene filtering |
| `targetDamageFactor` | string | * | Damage type for gene damage factor sum (e.g., "Flame") |
| `filterBiostatMet` | string | * | Filter genes by biostatMet (e.g., ">=2") |
| `filterBiostatCpx` | string | * | Filter genes by biostatCpx (e.g., ">=3") |
| `filterBiostatArc` | string | * | Filter genes by biostatArc (e.g., ">=1") |
| `filters` | object | ✗ | Advanced filtering options |

### InfoGiver Source Types

| Type | Description | Required Fields | Suggested for Urgent |
|------|-------------|-----------------|---------------------|
| `itemCount` | Count items | `targetItems` OR `targetCategories` | Medical supplies, ammo |
| `pawnCount` | Count pawns | `filters` | Hostiles, downed colonists |
| `pawnStat` | Pawn statistics | `targetStat` | Combat readiness |
| `pawnNeed` | Pawn needs | `targetNeed` | Critical needs |
| `constructionCount` | Construction projects | `targetItems` OR `targetCategories` | Defenses, power |
| `mapCondition` | Map conditions | `conditions` | ✓ Toxic fallout, raids |
| `weather` | Weather assessment | `weatherProperty` | ✓ Extreme temperatures |
| `geneCount` | Count genes in pawns | Gene targeting fields* | Gene-dependent tasks |

*Gene targeting fields: `targetGenes`, `targetGeneClasses`, `targetMentalBreakDef`, `filterBiostatMet`, `filterBiostatCpx`, `filterBiostatArc`

### Update Frequency

The `isUrgent` field controls how often InfoGivers are evaluated:

- **Normal (isUrgent=false)**: Every 2000 ticks (~33 seconds) - suitable for resources, general statistics
- **Urgent (isUrgent=true)**: Every 400 ticks (~6.7 seconds) - suitable for threats, emergencies, time-critical conditions

Use urgent evaluation sparingly to avoid performance impact. Ideal for:
- Enemy pawn detection
- Fire/emergency response  
- Critical resource shortages
- Immediate threat assessment

### Advanced Filtering System

#### Pawn Filters
```xml
<filters>
    <include>
        <li>player</li>      <!-- Include only player pawns -->
        <li>guests</li>      <!-- Include guests -->
    </include>
    <exclude>
        <li>hostiles</li>    <!-- Exclude hostile pawns -->
        <li>traders</li>     <!-- Exclude trader pawns -->
    </exclude>
</filters>
```

#### Hediff Filters
```xml
<filters>
    <hediffs>
        <li>
            <hediffClass>Hediff_Injury</hediffClass>
            <tendable>true</tendable>
            <hediffTended>false</hediffTended>
            <severity>&gt;0.5</severity>  <!-- XML escaped > -->
        </li>
        <li>
            <hediffClass>Hediff_Injury</hediffClass>
            <severityRange>0.8~1.0</severityRange>  <!-- Range format -->
        </li>
        <li>
            <hediffClass>HediffWithComps</hediffClass>
            <hasComps>HediffCompProperties_Immunizable</hasComps>
            <deltaImmunitySeverity>&lt;0</deltaImmunitySeverity>
        </li>
    </hediffs>
</filters>
```

#### Hediff Filter Comparators
| Field | Format | Description |
|-------|--------|-------------|
| `severity` | `>0.5`, `<=0.8`, `=1.0` | Single value comparison |
| `severityRange` | `0.8~1.0` | Range comparison (min~max) |
| Operator | XML Escaped | Description |
|----------|-------------|-------------|
| `>` | `&gt;` | Greater than |
| `<` | `&lt;` | Less than |
| `>=` | `&gt;=` | Greater than or equal |
| `<=` | `&lt;=` | Less than or equal |
| `=` | `=` | Equal to |

### Dynamic Target Amount
```xml
<targetAmountSource>
    <infoDefName>ColonySize</infoDefName>
    <multiplier>50</multiplier>  <!-- 50 per colonist -->
    <offset>100</offset>         <!-- Plus base amount -->
</targetAmountSource>
<returnAsPercentage>true</returnAsPercentage>
```

### Personality Offset Format

| Format | Example | Description |
|--------|---------|-------------|
| `+X` | `+2` | Add X to result |
| `-X` | `-1.5` | Subtract X from result |
| `xX` | `x2.0` | Multiply result by X |
| `X` | `3` | Default: add X to result |

### Range Format

| Format | Example | Description |
|--------|---------|-------------|
| `X~Y` | `10~20` | Range from X to Y |
| `X` | `15` | Single value X |

## Common Work Types

- `Doctor` - Medical work
- `Cooking` - Food preparation
- `Mining` - Mining operations
- `Research` - Research tasks
- `Construction` - Building
- `Growing` - Farming
- `Crafting` - Item creation
- `Hauling` - Moving items
- `Cleaning` - Cleaning tasks
- `Hunting` - Hunting animals

## Example Scenarios

### Scenario 1: Emergency Medical with Advanced Filtering
Priority medical work when colonists have severe, untended injuries.

```xml
<Autonomy.PriorityGiverDef>
    <defName>EmergencyMedical</defName>
    <label>Emergency medical response</label>
    <description>High priority medical when colonists have severe injuries</description>
    
    <conditions>
        <li>
            <type>infoGiver</type>
            <infoDefName>SevereInjuryCount</infoDefName>
            <calculation>flat</calculation>
            <flatValue>1</flatValue>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>2~20</validRange>
            <priority>4</priority>
            <description>Multiple severe injuries - medical emergency</description>
        </li>
        <li>
            <validRange>1~1</validRange>
            <priority>3</priority>
            <description>One severe injury - high medical priority</description>
        </li>
    </priorityRanges>
    
    <targetWorkTypes>
        <li>Doctor</li>
    </targetWorkTypes>
</Autonomy.PriorityGiverDef>

<!-- Supporting InfoGiver -->
<Autonomy.InfoGiverDef>
    <defName>SevereInjuryCount</defName>
    <label>Severe injury count</label>
    <description>Counts colonists with severe, untended injuries</description>
    
    <sourceType>pawnCount</sourceType>
    <calculation>count</calculation>
    
    <filters>
        <include>
            <li>player</li>
        </include>
        <hediffs>
            <li>
                <hediffClass>Hediff_Injury</hediffClass>
                <tendable>true</tendable>
                <hediffTended>false</hediffTended>
                <severity>&gt;0.3</severity>
            </li>
        </hediffs>
    </filters>
</Autonomy.InfoGiverDef>
```

### Scenario 2: Dynamic Cooking Based on Food Ratios
```xml
<Autonomy.PriorityGiverDef>
    <defName>CookingByFoodRatio</defName>
    <label>Cooking priority by food ratio</label>
    <description>Cooking priority based on meals vs raw food ratio</description>
    
    <conditions>
        <li>
            <type>infoGiver</type>
            <infoDefName>MealToRawFoodRatio</infoDefName>
            <calculation>flat</calculation>
            <flatValue>0.5</flatValue>
        </li>
    </conditions>
    
    <personalityOffsets>
        <li>
            <name>Gourmand</name>
            <offset>x1.5</offset>
        </li>
    </personalityOffsets>
    
    <priorityRanges>
        <li>
            <validRange>0~0.3</validRange>
            <priority>3~4</priority>
            <description>Very low meals - urgent cooking needed</description>
        </li>
        <li>
            <validRange>0.31~0.6</validRange>
            <priority>2</priority>
            <description>Low meals - cooking recommended</description>
        </li>
    </priorityRanges>
    
    <targetWorkTypes>
        <li>Cooking</li>
    </targetWorkTypes>
</Autonomy.PriorityGiverDef>
```

### Scenario 3: Weather-Based Outdoor Work
```xml
<Autonomy.PriorityGiverDef>
    <defName>WeatherOutdoorWork</defName>
    <label>Weather-adjusted outdoor priority</label>
    <description>Reduces outdoor work priority in bad weather</description>
    
    <conditions>
        <li>
            <type>infoGiver</type>
            <infoDefName>WeatherMovementImpact</infoDefName>
            <calculation>flat</calculation>
            <flatValue>0.8</flatValue>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>0.5~0.8</validRange>
            <priority>-1</priority>
            <description>Bad weather - reduce outdoor work</description>
        </li>
        <li>
            <validRange>0~0.49</validRange>
            <priority>-2</priority>
            <description>Severe weather - avoid outdoor work</description>
        </li>
    </priorityRanges>
    
    <targetWorkTypes>
        <li>Mining</li>
        <li>Growing</li>
        <li>Construction</li>
    </targetWorkTypes>
</Autonomy.PriorityGiverDef>
```

## Gene-Based Priority Example

### Pyrophobia Gene Detection
Reduce firefighting priority for pawns with pyrophobia (FireTerror mental break):

```xml
<!-- InfoGiver: Detect pyrophobia gene -->
<Autonomy.InfoGiverDef>
    <defName>PyrophobiaGeneInfoGiver</defName>
    <label>Pyrophobia gene</label>
    <description>Checks if pawn has genes with FireTerror mental break</description>
    <isLocalizable>false</isLocalizable>
    <isIndividualizable>true</isIndividualizable>
    <calculation>Count</calculation>
    <isUrgent>true</isUrgent>
    <sourceType>geneCount</sourceType>
    
    <targetMentalBreakDef>FireTerror</targetMentalBreakDef>
    
    <filters>
        <include>
            <li>player</li>
        </include>
        <exclude>
            <li>dead</li>
            <li>downed</li>
        </exclude>
    </filters>
</Autonomy.InfoGiverDef>

<!-- PriorityGiver: Reduce firefighting priority -->
<Autonomy.PriorityGiverDef>
    <defName>FirefightingPyrophobiaPriority</defName>
    <label>Firefighting pyrophobia avoidance</label>
    <description>Reduces firefighting priority for pawns with pyrophobia gene</description>
    <isUrgent>true</isUrgent>
    
    <conditions>
        <li>
            <type>infoGiver</type>
            <infoDefName>PyrophobiaGeneInfoGiver</infoDefName>
            <calculation>Flat</calculation>
            <infoRange>0~999</infoRange>
            <requestIndividualData>true</requestIndividualData>
        </li>
        <!-- Personality: brave pawns resist fear more -->
        <li>
            <type>personalityOffset</type>
            <personalityDefName>Rimpsyche_Bravery</personalityDefName>
            <personalityMultipliers>
                <li>
                    <personalityRange>-1.0~-0.5</personalityRange>
                    <multiplier>1.5</multiplier>
                </li>
                <li>
                    <personalityRange>0.51~1.0</personalityRange>
                    <multiplier>0.5</multiplier>
                </li>
            </personalityMultipliers>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>0</validRange>
            <priority>0</priority>
            <description>I don't have pyrophobia</description>
        </li>
        <li>
            <validRange>1~999</validRange>
            <priority>-20</priority>
            <description>Fires terrify me</description>
        </li>
    </priorityRanges>
    
    <targetWorkGivers>
        <li>FightFires</li>
    </targetWorkGivers>
</Autonomy.PriorityGiverDef>
```

### Other Gene Targeting Examples

```xml
<!-- Target specific genes by defName -->
<targetGenes>
    <li>Hemogenic</li>
    <li>Deathless</li>
</targetGenes>

<!-- Target genes by class -->
<targetGeneClasses>
    <li>Gene_Hemogen</li>
    <li>Gene_Healing</li>
</targetGeneClasses>

<!-- Filter by biostat values -->
<filterBiostatArc>>=1</filterBiostatArc> <!-- Archite genes only -->
<filterBiostatCpx>>=3</filterBiostatCpx> <!-- High complexity -->
<filterBiostatMet>>=4</filterBiostatMet> <!-- High metabolic cost -->

<!-- Target damage factors (returns sum of factors) -->
<targetDamageFactor>Flame</targetDamageFactor> <!-- Fire weakness genes like tinderskin (4x) -->
```

### Fire Weakness (Damage Factor) Example

```xml
<!-- InfoGiver: Sum Flame damage factors -->
<Autonomy.InfoGiverDef>
    <defName>FireWeaknessGeneInfoGiver</defName>
    <label>Fire weakness gene damage factor</label>
    <sourceType>geneCount</sourceType>
    <calculation>Sum</calculation>
    <isIndividualizable>true</isIndividualizable>
    <isUrgent>true</isUrgent>
    
    <targetDamageFactor>Flame</targetDamageFactor>
    
    <filters>
        <include>
            <li>player</li>
        </include>
    </filters>
</Autonomy.InfoGiverDef>

<!-- PriorityGiver: Scaled avoidance based on vulnerability -->
<Autonomy.PriorityGiverDef>
    <defName>FirefightingFireWeaknessPriority</defName>
    <label>Fire weakness avoidance</label>
    <isUrgent>true</isUrgent>
    
    <conditions>
        <li>
            <type>infoGiver</type>
            <infoDefName>FireWeaknessGeneInfoGiver</infoDefName>
            <requestIndividualData>true</requestIndividualData>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>0</validRange>
            <priority>0</priority>
            <description>No fire weakness</description>
        </li>
        <li>
            <validRange>1.5~2.5</validRange>
            <priority>-10</priority>
            <description>Vulnerable to fire</description>
        </li>
        <li>
            <validRange>3.5~999</validRange>
            <priority>-30</priority>
            <description>Extremely vulnerable (tinderskin)</description>
        </li>
    </priorityRanges>
    
    <targetWorkGivers>
        <li>FightFires</li>
    </targetWorkGivers>
</Autonomy.PriorityGiverDef>
```

## Tips and Best Practices

1. **Start Simple**: Begin with flat value conditions before using InfoGivers
2. **Test Thoroughly**: Use debug mode to verify calculations
3. **Use Descriptive Names**: Make defNames and descriptions clear
4. **Consider Balance**: Avoid making priorities too aggressive
5. **Plan for Edge Cases**: Consider what happens with extreme values
6. **Use Personality Wisely**: Not every priority needs personality offsets
7. **Gene Targeting**: Use `targetMentalBreakDef` for behavior-related genes, biostat filters for balance
8. **Always Individualizable**: Remember that `geneCount` is always tracked per pawn
7. **Optimize Performance**: Prefer simple conditions when possible

## Common Mistakes

- **Missing Target**: Must specify either `targetWorkTypes` or `targetWorkGivers`
- **Invalid Ranges**: Ensure min ≤ max in range specifications
- **Circular References**: Don't create InfoGivers that depend on themselves
- **Performance Issues**: Avoid too many complex InfoGivers per priority
- **Balance Problems**: Test with various colony sizes and situations