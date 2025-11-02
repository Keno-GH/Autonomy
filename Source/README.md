# Source Code Architecture

## Modular Design Philosophy

The Autonomy mod is structured around clear separation of concerns, with each component handling a specific part of the autonomous work priority pipeline.

## Directory Structure

### `/Components/`
Core map components that handle data collection and evaluation:

- **`InfoGiverManager.cs`** - Collects colony/pawn/location data
  - Evaluates InfoGiverDefs to produce numerical data about game state
  - Handles individual, localized, and global data contexts
  - Provides data to other systems via GetLastResult() methods

- **`PriorityGiverManager.cs`** - Converts conditions into priority values  
  - Evaluates PriorityGiverDefs for individual pawns
  - Transforms InfoGiver data into work priority adjustments
  - Per-pawn evaluation ensures individual autonomous decision-making

### `/Systems/`
Higher-level systems that orchestrate the autonomy pipeline:

- **`WorkPriorityAssignmentSystem.cs`** - (Future) Final work priority assignment
  - Will aggregate priority results per WorkType/WorkGiver
  - Will rank and assign final work priorities (1-4 scale)
  - Will complete the autonomy pipeline

### `/Defs/`
Definition classes for XML configuration:

- **`AutonomyDefs.cs`** - Core definition classes (InfoGiverDef, PriorityGiverDef, etc.)
- **`AutonomySupportingClasses.cs`** - Supporting structures and enums

### `/UI/`
User interface components for monitoring and debugging:

- **`MainTabWindow_Autonomy.cs`** - Main autonomy monitoring tab
- **`InfoGiverWindow.cs`** - Detailed InfoGiver inspection window
- **`MainTabWindow_History_Patch.cs`** - Integration with game's History tab

## Data Flow Pipeline

```
1. InfoGivers → Collect game state data (per pawn/location/global)
2. PriorityGivers → Convert data to priority values (per pawn)
3. WorkPriorityAssignment → Aggregate and rank priorities (per pawn)
4. Autonomous Work Selection → Apply priorities to work settings
```

## Key Design Principles

1. **Per-Pawn Evaluation**: Everything is evaluated individually for each pawn
2. **Modular Components**: Each system has a single, clear responsibility  
3. **Data-Driven Configuration**: Behavior defined through XML definitions
4. **Extensible Architecture**: Easy to add new InfoGivers and PriorityGivers
5. **Clear Interfaces**: Well-defined methods for inter-component communication