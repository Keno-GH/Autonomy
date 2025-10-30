# Autonomy Mod - Architecture Documentation

## Overview
The Autonomy mod allows colonists to autonomously decide their work priorities on a daily basis based on colony needs, personal traits, and current conditions. The system is designed to be highly modular and extensible through XML definitions.

## Core Philosophy
- **Modularity**: All logic is defined through XML to allow easy modification and mod compatibility
- **Extensibility**: Other mods can add their own priority givers and info sources
- **Performance**: Heavy calculations are optimized and cached appropriately
- **Transparency**: Players can see why pawns made certain priority decisions

## System Architecture

### 1. Priority Calculation Flow
```
Map Stats Collection → Pawn Analysis → Priority Giver Evaluation → Work Type Priority Assignment
```

#### 1.1 Map Stats Collection
- Gathers current colony statistics (resource counts, urgent needs, etc.)
- **Performance Critical**: Must be optimized and cached
- Sources: Stockpiles, construction projects, threat levels, weather, etc.

#### 1.2 Pawn Analysis
- Analyzes individual pawn: stats, personality, needs, health
- Considers: Skills, traits, current mood, injuries, equipment

#### 1.3 Priority Giver Evaluation
- Processes all PriorityGiverDefs that apply to the pawn
- Uses conditions to determine priority adjustments
- Applies personality offsets based on traits

#### 1.4 Work Type Priority Assignment
- Distributes calculated priorities to WorkTypeDefs
- Considers supported WorkGiverDefs per WorkType

## RimWorld Work System Hierarchy

```
WorkTypeDef (Mining, Cooking, etc.)
    ↓ contains multiple
WorkGiverDef (Mine designated areas, Cook meals, etc.)
    ↓ has a
GiverClass (WorkGiver_Miner, WorkGiver_Cook)
    ↓ provides
DriverClass (JobDriver_Mine, JobDriver_Cook)
    ↓ defined in
JobDef (Mine, CookMeal)
```

### Priority Targeting Strategy
- **Primary Target**: WorkTypeDef (what players see in work tab)
- **Calculation Source**: Individual WorkGiverDefs within the WorkType
- **Distribution**: Priority is divided among supported WorkGivers

## XML Definition System

### 2. PriorityGiverDef
Defines how colony conditions translate to work priority adjustments.

**Core Fields:**
- `defName`: Unique identifier
- `label`: Human-readable name
- `description`: Explanation of what this priority giver does
- `conditions`: Array of condition checks
- `personalityOffsets`: Trait-based modifications
- `priorityRanges`: Maps condition results to priority values
- `targetWorkGivers`: Specific WorkGiverDefs to affect
- `targetWorkTypes`: Specific WorkTypeDefs to affect

### 3. InfoGiverDef
Modular data collection definitions that can query various game systems.

**Core Fields:**
- `defName`: Unique identifier
- `label`: Human-readable name
- `description`: What information this giver provides
- `sourceType`: What system to query (hediffs, needs, stats, counts)
- `calculation`: How to process the data (sum, average, max, min, etc.)
- `filters`: Optional criteria to filter results

### 4. Condition System
Flexible condition evaluation that can check:
- Pawn stats (individual or colony-wide)
- Map statistics
- InfoGiver results
- Flat comparison values

**Condition Types:**
- `stat`: Check pawn or colony statistics
- `infoGiver`: Use InfoGiverDef results
- `flat`: Compare against fixed values
- `mapStat`: Check map-level information

### 5. Personality Integration
Personality traits modify condition results through mathematical operations:
- `+X`: Add offset
- `-X`: Subtract offset  
- `xX`: Multiply by factor
- `/X`: Divide by factor (not implemented yet, use `x0.5` instead of `/2`)

## Performance Considerations

### Optimization Strategies
1. **Caching**: Map stats cached per day, pawn analysis cached per calculation cycle
2. **Batching**: Process multiple pawns together when possible
3. **Early Exit**: Skip expensive calculations when conditions clearly won't match
4. **Lazy Loading**: Only calculate priorities when needed

### Performance Monitoring
- Track calculation times for optimization
- Log warnings for expensive operations
- Provide mod settings to adjust calculation frequency

## Extensibility

### For Mod Authors
- Add new PriorityGiverDefs via XML patches
- Create custom InfoGiverDefs for mod-specific data
- Extend personality system with custom traits
- Hook into calculation events for custom logic

### For Players
- XML files can be modified directly
- In-game settings for calculation frequency
- Debug overlays to understand priority decisions
- Tooltips explaining why pawns chose certain priorities

## Development Phases

### Phase 1: Core Framework
- Basic Def classes and XML parsing
- Simple condition evaluation
- Basic priority application

### Phase 2: Advanced Features
- InfoGiver system implementation
- Personality trait integration
- Performance optimization

### Phase 3: Polish & Extensibility
- Debug tools and player visibility
- Advanced mod compatibility
- Performance monitoring and tuning

## Error Handling

### Graceful Degradation
- Invalid XML definitions log errors but don't crash
- Missing InfoGivers fall back to default values
- Calculation errors revert to vanilla behavior

### Debug Support
- Verbose logging for troubleshooting
- In-game tools to inspect priority calculations
- XML validation and helpful error messages