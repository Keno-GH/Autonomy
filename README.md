# Autonomy - RimWorld Mod

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-green.svg)](https://rimworldgame.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

This RimWorld mod introduces a system where colonists manage their work priorities based on their own free will. The mod includes complex systems for evaluating colony needs and conditions, but its core focus is on how each colonist interprets this information. Their personalities, interests, and individual perspectives shape their decisions, resulting in varied and personal responses.

## 🎯 **Vision**

Autonomy is designed to make colonists feel more alive. Colonists respond to a changing world by weighing their surroundings through their own personalities and interests. They adjust their work priorities not only to meet the needs of the colony, but also according to what they personally consider important. Leading to a more dynamic and responsive colony experience.

## 📦 **Requirements**

### **Required Dependencies**
- **[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)** - Core modding framework
- **[RimPsyche - Personality Core](https://steamcommunity.com/sharedfiles/filedetails/?id=3535112473)** - Personality system integration
- **[XML Extensions](https://steamcommunity.com/sharedfiles/filedetails/?id=2574315206)** - Settings menu and advanced XML features

### **Supported Game Versions**
- RimWorld 1.6

### **Optional DLC Support**
- **Ideology** - Precept-based work preferences and ideological priorities
- **Biotech** - Gene-based priority modifiers (future enhancement)

## ✨ **Key Features**

### 🧠 **Intelligent Decision Making**
- **Dynamic Priority Calculation**: Colonists evaluate colony conditions and adjust priorities in real-time with 100+ predefined priority rules
- **Personality Integration**: Personality from [RimPsyche](https://github.com/jagerguy36/Rimpsyche) PersonalityDefs influences work prioritization through multipliers
- **Passion-Based Priorities**: Skills and passions (including VSE and Alpha Skills) affect work preferences with proper mod hierarchy support
- **Contextual Awareness**: Weather, threats, resources, and colony needs all factor into decisions through 26 InfoGivers
- **Ideology Support**: Precepts and ideological beliefs influence work preferences (requires Ideology DLC)

### 🔧 **Modular XML System**
- **PriorityGiverDefs**: Define how colony conditions translate to work priority adjustments - 100+ definitions covering all work types
- **InfoGiverDefs**: Modular data collection from various game systems - 26 definitions for comprehensive colony monitoring
- **Easy Customization**: All logic defined through XML for easy modification and mod compatibility
- **Settings Integration**: Configure optional work types through in-game settings menu (XML Extensions)

### 🌐 **Mod Compatibility**
- **ThingCategoryDef Support**: Automatic compatibility with modded items by using ThingCategoryDefs instead of ThingDefs where possible.
- **Dynamic Weather Analysis**: Uses weather properties to ensure compatibility with any weather mod
- **Extensible Framework**: Other mods can add their own priority logic

### 🎮 **Player Control & Interface**
- **Pause Autonomy**: Per-pawn control to temporarily or permanently pause autonomous priority adjustments
  - Quick access column in the work tab
  - Multiple duration options (3 hours, 8 hours, 24 hours, 3 days, forever)
  - Visual indicators for paused state
- **Autonomy Monitor Tab**: In-game window to inspect InfoGiver values and system state
- **InfoGiver Inspector**: Detailed breakdown of data collection and calculations
- **Work Priority Tooltips**: Enhanced tooltips showing why priorities were assigned
- **Settings Menu**: Customize which work types use autonomous priorities (via XML Extensions)

## 📋 **Current Development Status**

This mod has **substantially completed Phase 1** with core systems fully implemented and functional. Many Phase 2 features are also complete. See our [Development Roadmap](TODO.md) for detailed progress (note: TODO.md may not reflect latest progress).

### ✅ **Completed Features**

#### **Phase 0 - Foundation**
- [x] Complete architecture design and documentation
- [x] XML definition system with comprehensive templates
- [x] C# framework with Def classes and supporting structures
- [x] Advanced filtering system for complex condition evaluation
- [x] Mod compatibility framework

#### **Phase 1 - Core Implementation**
- [x] **InfoGiver Manager** - Fully functional data collection system with 26 InfoGivers
  - Item counts (meals, medicine, resources)
  - Map conditions (threats, weather, temperature)
  - Medical tracking (injuries, diseases, immunity races)
  - Animal management (bonded, trainable, hungry)
  - Social and needs monitoring
  - Pawn statistics and capabilities
- [x] **PriorityGiver Manager** - Complete priority calculation system with 100+ PriorityGivers
  - All 18 vanilla work types covered
  - Personality-based priority adjustments (RimPsyche integration)
  - Passion and skill-based priorities
  - Precept and ideology integration (Ideology DLC)
- [x] **WorkPriorityAssignment System** - Automated work priority application
  - Dynamic priority ranking (1-4 scale)
  - Per-pawn autonomous decision making
  - Intelligent caching and performance optimization
- [x] **Harmony Integration** - Priority application and UI patches
  - Automatic priority updates
  - Work settings integration
  - Tooltip enhancements

#### **Phase 2 - UI and Player Control**
- [x] **Autonomy Tab** - In-game monitoring window for InfoGivers and priorities
- [x] **InfoGiver Inspector** - Detailed view of data collection and evaluation
- [x] **Pause Autonomy Feature** - Per-pawn control system
  - UI column in work tab for easy access
  - Temporary pause options (3 hours, 8 hours, 24 hours, 3 days)
  - Permanent pause option
  - Visual indicators for paused state
- [x] **Settings Menu** - Customizable work type toggles via XML Extensions
  - Optional work types (butchering, brewing, training, slaughtering, etc.)
  - Per-feature control for player preference

### 🚧 **Current Development Focus**
- **Rebalancing Priority Givers**: Adjusting priority ranges to prevent some givers from dominating calculations
- **Mod Compatibility Expansion**: Adding support for popular mods that extend RimWorld's work systems
- **Code Cleanup**: Removing unused InfoGiver types and fields, streamlining codebase
- **Performance Testing**: Evaluating mod behavior with complex modlists and various colony sizes
- **Wiki Rewrite**: Updating documentation to accurately reflect current features and examples

### 📅 **Planned Mod Compatibility**

#### **High Priority**
- **[Meditate As WorkType (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3273540559)**
  - InfoGivers for pawn psyfocus levels and animagrass counts
  - PriorityGivers for meditation based on psyfocus needs and personality
  - Fallback meditation priority for psycasters when idle
  - Animagrass meditation for tribal/animist pawns
  
- **[Recreation Work Type](https://steamcommunity.com/sharedfiles/filedetails/?id=3535871111)**
  - InfoGivers for pawn recreation levels
  - PriorityGivers for recreation based on mood and recreation needs
  - Fallback recreation priority for pawns when idle
  
- **[Colony Manager Redux](https://steamcommunity.com/sharedfiles/filedetails/?id=3310027356)**
  - Integration with food security priorities
  - Colony hunger-based management prioritization

#### **Under Consideration**
- **[Gastronomy (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3509488152)** - Restaurant and server work types
- **[Hospitality (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3509486825)** - Guest management
- **[Hospitality: Storefront](https://steamcommunity.com/sharedfiles/filedetails/?id=2952321484)** - Trade management
- **[Therapy (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3509488682)** - Mental health care
- Various fishing mods and education mods

### 🔮 **Future Enhancements**
- Advanced player feedback and notification systems
- Community-contributed priority configurations
- Additional priority levels for finer-grained control
- Video tutorials and enhanced documentation

## 🏗️ **Architecture**

### **System Flow**
```
InfoGiver Manager → PriorityGiver Manager → WorkPriority Assignment → Autonomous Work Selection
     ↓                      ↓                         ↓                           ↓
 Collect Data      Calculate Priorities      Rank & Assign          Apply to Pawn Settings
(Colony Stats)    (Per Pawn + Per Work)    (Priority 1-4)         (Unless Paused)
```

1. **InfoGiver Manager**: Gathers colony statistics, resource counts, threats, weather, pawn states
   - 26 InfoGiver definitions
   - Optimized with intelligent caching and staggered updates
   - Supports individualized (per-pawn) and localized (per-location) data
2. **PriorityGiver Manager**: Evaluates individual pawn contexts against colony conditions
   - 100+ PriorityGiver definitions covering all work types
   - Personality, skill, and passion modifiers
   - Ideology and precept integration
3. **WorkPriority Assignment**: Aggregates priorities and ranks work types
   - Calculates final priorities (1-4 scale or enabled/disabled)
   - Deduplication to prevent double-counting
   - Per-pawn autonomous decision making
4. **Harmony Patches**: Apply priorities while respecting player control
   - Skips paused pawns
   - Integrates with vanilla work tab
   - Enhanced tooltips

### **Key Components**
- **[InfoGiverDefs](1.6/Defs/InfoGivers/)**: Data collection definitions (item counts, map conditions, medical, animals, social, stats)
- **[PriorityGiverDefs](1.6/Defs/PriorityGivers/)**: Priority calculation logic (work types, passions, skills, precepts)
- **[Filtering System](XML_REFERENCE.md#advanced-filtering-system)**: Advanced condition evaluation for complex scenarios
- **[Pause System](Source/Core/PawnAutonomyPauseTracker.cs)**: Player control for per-pawn autonomy management

## 📖 **Documentation for Modders**

### Core Documentation
- **[XML Reference Guide](XML_REFERENCE.md)** – Complete reference for all XML definitions and advanced features
- **[Architecture Documentation](ARCHITECTURE.md)** – System design and implementation concepts
- **[Source Code README](Source/README.md)** – Component structure and data flow pipeline

### Feature-Specific Guides
- **[Calculation Conditions](CALCULATION_CONDITION_IMPLEMENTATION.md)** – Perform math operations between InfoGivers
  - [Quick Reference](CALCULATION_CONDITION_QUICK_REFERENCE.md)
- **[Filter Conditions](FILTER_CONDITION_IMPLEMENTATION.md)** – Advanced pawn filtering system
  - [Implementation Summary](FILTER_IMPLEMENTATION_SUMMARY.md)
  - [Quick Reference](FILTER_QUICK_REFERENCE.md)
  - [Pawn Filter System](PAWN_FILTER_SYSTEM.md)
- **[Hediff System](HEDIFF_SYSTEM_IMPLEMENTATION.md)** – Complex health condition filtering
  - [Effective Properties](EFFECTIVE_HEDIFF_PROPERTIES.md)
- **[Gene System](GENE_SYSTEM_IMPLEMENTATION.md)** – Genetic trait integration (Biotech DLC)
- **[Damage Factors](DAMAGE_FACTOR_IMPLEMENTATION.md)** – Damage calculation and assessment
- **[Personality System](PERSONALITY_SYSTEM.md)** – RimPsyche integration and trait-based priorities

### Development Resources
- **[Contributing Guidelines](CONTRIBUTING.md)** – How to contribute to the project
- **[Improvements Summary](IMPROVEMENTS_SUMMARY.md)** – Recent enhancements and system improvements
- **[Development Roadmap](TODO.md)** – Future plans and progress tracking

## 🤝 **Contributing**

We welcome contributions from the community! Whether you're:
- **Reporting bugs** or suggesting features
- **Creating XML definitions** for new scenarios
- **Improving documentation** or examples
- **Contributing code** to the core system

See our [Contributing Guidelines](CONTRIBUTING.md) for details on how to get involved.

## 📊 **Examples**

The mod includes **100+ PriorityGiver definitions** and **26 InfoGiver definitions** covering all aspects of colony management. Here are a few examples:

### **Emergency Medical Response**
```xml
<Autonomy.PriorityGiverDef>
    <defName>EmergencyMedical</defName>
    <label>Emergency medical response</label>
    <description>High priority medical when colonists have severe injuries</description>
    
    <conditions>
        <li>
            <type>infoGiver</type>
            <infoDefName>SevereInjuryCount</infoDefName>
        </li>
    </conditions>
    
    <priorityRanges>
        <li>
            <validRange>2~20</validRange>
            <priority>4</priority>
            <description>Multiple severe injuries - medical emergency</description>
        </li>
    </priorityRanges>
    
    <targetWorkTypes>
        <li>Doctor</li>
    </targetWorkTypes>
</Autonomy.PriorityGiverDef>
```

### **Passion-Based Work Priority**
Pawns with major passion for cooking automatically get higher cooking priority:
```xml
<Autonomy.PriorityGiverDef>
    <defName>CookingMajorPassion</defName>
    <label>Cooking major passion</label>
    <description>Pawns with major passion for cooking prioritize it highly</description>
    
    <filters>
        <passionForSkill>
            <skill>Cooking</skill>
            <passion>Major</passion>
        </passionForSkill>
    </filters>
    
    <targetWorkTypes>
        <li>Cooking</li>
    </targetWorkTypes>
    
    <priorityRanges>
        <li>
            <priority>4</priority>
        </li>
    </priorityRanges>
</Autonomy.PriorityGiverDef>
```

### **Comprehensive Coverage**
Real implementations in the mod cover:
- **All 18 vanilla work types** with situation-aware priorities
- **Medical emergencies** (bleeding, disease, immunity races)
- **Resource management** (food shortage, medicine shortage, material needs)
- **Animal care** (bonded animals, hungry animals, trainable animals)
- **Environmental response** (fires, infestations, raids)
- **Passion and skill-based** preferences for all colonists
- **Personality-gated** priorities (RimPsyche integration)
- **Ideology integration** (precept-based work preferences)

See the [1.6/Defs/PriorityGivers/](1.6/Defs/PriorityGivers/) and [1.6/Defs/InfoGivers/](1.6/Defs/InfoGivers/) directories for all definitions.

More examples available in our [XML Reference Guide](XML_REFERENCE.md).

## 🔗 **Links**

- **[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=TODO)** (Coming Soon)
- **[GitHub Repository](https://github.com/Keno-GH/Autonomy)**
- **[Wiki Documentation](../../wiki)**
- **[Bug Reports](https://github.com/Keno-GH/Autonomy/issues)**

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
Basically, do whatever you want with it, let other's do the same.

## 👏 **Acknowledgments**

- **RimWorld Community** for inspiration and feedback
- **Harmony Library** for making advanced modding possible
- **Template Mod Contributors** for the initial framework
- **Beta Testers** who help make this mod stable and balanced

---

**Made with coffee for the RimWorld community**
