# Calculation Condition - Quick Reference

## Overview
The calculation condition performs mathematical operations between two InfoGivers or static values.

## Basic Syntax
```xml
<li>
    <type>calculation</type>
    <infoDefName1>FirstInfoGiver</infoDefName1>
    <infoDefName2>SecondInfoGiver</infoDefName2>
    <operation>sub</operation>
    <requestIndividualData>true</requestIndividualData>
</li>
```

## Operations

| Operation | Formula | Example |
|-----------|---------|---------|
| `sub` | `a - b` | Immunity - Severity |
| `diff` | `abs(a - b)` | Temperature deviation |
| `sum` | `a + b` | Combined threats |
| `ratio` | `a / b` | Food per colonist |
| `max` | `max(a, b)` | Minimum threshold |
| `min` | `min(a, b)` | Maximum cap |
| `avg` | `(a + b) / 2` | Average skill |

## Using Static Values
```xml
<li>
    <type>calculation</type>
    <infoDefName1>PawnSpeed</infoDefName1>
    <value2>5.0</value2>  <!-- Static minimum -->
    <operation>max</operation>
</li>
```

## Complete Example
```xml
<Autonomy.PriorityGiverDef>
    <defName>DiseaseUrgency</defName>
    <label>Disease urgency</label>
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
            <priority>90</priority>
            <description>Disease winning!</description>
        </li>
        <li>
            <validRange>0.5~999</validRange>
            <priority>10</priority>
            <description>Immunity ahead</description>
        </li>
    </priorityRanges>
    <targetWorkGivers>
        <li>PatientGoToBedRecuperate</li>
    </targetWorkGivers>
</Autonomy.PriorityGiverDef>
```

## See Also
- Full documentation: `CALCULATION_CONDITION_IMPLEMENTATION.md`
- XML reference: `XML_REFERENCE.md`
- All InfoGivers: `InfoGivers_Table.csv`
