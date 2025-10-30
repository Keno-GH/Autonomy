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

### InfoGiver Source Types

| Type | Description | Required Fields |
|------|-------------|-----------------|
| `itemCount` | Count items | `targetItems` OR `targetCategories` |
| `pawnCount` | Count pawns | `filters` |
| `pawnStat` | Pawn statistics | `targetStat` |
| `pawnNeed` | Pawn needs | `targetNeed` |
| `constructionCount` | Construction projects | `targetMaterials` |
| `mapCondition` | Map conditions | `conditions` |
| `weather` | Weather assessment | `weatherProperty` |

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
            <hediffClass>HediffWithComps</hediffClass>
            <hasComps>HediffCompProperties_Immunizable</hasComps>
            <deltaImmunitySeverity>&lt;0</deltaImmunitySeverity>
        </li>
    </hediffs>
</filters>
```

#### Hediff Filter Comparators
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

## Tips and Best Practices

1. **Start Simple**: Begin with flat value conditions before using InfoGivers
2. **Test Thoroughly**: Use debug mode to verify calculations
3. **Use Descriptive Names**: Make defNames and descriptions clear
4. **Consider Balance**: Avoid making priorities too aggressive
5. **Plan for Edge Cases**: Consider what happens with extreme values
6. **Use Personality Wisely**: Not every priority needs personality offsets
7. **Optimize Performance**: Prefer simple conditions when possible

## Common Mistakes

- **Missing Target**: Must specify either `targetWorkTypes` or `targetWorkGivers`
- **Invalid Ranges**: Ensure min ≤ max in range specifications
- **Circular References**: Don't create InfoGivers that depend on themselves
- **Performance Issues**: Avoid too many complex InfoGivers per priority
- **Balance Problems**: Test with various colony sizes and situations