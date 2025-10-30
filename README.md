# Autonomy - RimWorld Mod

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-green.svg)](https://rimworldgame.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

This RimWorld mod introduces a system where colonists manage their work priorities based on their own free will. The mod includes complex systems for evaluating colony needs and conditions, but its core focus is on how each colonist interprets this information. Their personalities, interests, and individual perspectives shape their decisions, resulting in varied and personal responses.

## 🎯 **Vision**

Autonomy is designed to make colonists feel more alive. Colonists respond to a changing world by weighing their surroundings through their own personalities and interests. They adjust their work priorities not only to meet the needs of the colony, but also according to what they personally consider important. Leading to a more dynamic and responsive colony experience.

## ✨ **Key Features**

### 🧠 **Intelligent Decision Making**
- **Dynamic Priority Calculation**: Colonists evaluate colony conditions and adjust priorities in real-time
- **Personality Integration**: Personality from Rimpsyches PersonalityDefs and InterestDomainDefs influences work prioritization
- **Contextual Awareness**: Weather, threats, resources, and colony needs all factor into decisions

### 🔧 **Modular XML System**
- **PriorityGiverDefs**: Define how colony conditions translate to work priority adjustments
- **InfoGiverDefs**: Modular data collection from various game systems, meant to be expandable and patchable.
- **Easy Customization**: All logic defined through XML for easy modification and mod compatibility

### 🌐 **Mod Compatibility**
- **ThingCategoryDef Support**: Automatic compatibility with modded items by using ThingCategoryDefs instead of ThingDefs where possible.
- **Dynamic Weather Analysis**: Uses weather properties to ensure compatibility with any weather mod
- **Extensible Framework**: Other mods can add their own priority logic

## 📋 **Current Development Status**

This mod is currently in **Phase 1 Development**. See our [Development Roadmap](TODO.md) for detailed progress.

### ✅ **Completed (Phase 0)**
- [x] Complete architecture design and documentation
- [x] XML definition system with comprehensive templates
- [x] C# framework with Def classes and supporting structures
- [x] Advanced filtering system for complex condition evaluation
- [x] Mod compatibility framework

### 🚧 **In Progress (Phase 1)**
- [ ] Core InfoGiver evaluation engine
- [ ] Basic priority calculation system
- [ ] Simple condition evaluation
- [ ] Harmony integration for priority application

### 📅 **Planned Features**
- Advanced personality trait integration
- Performance optimization and caching
- Debug tools and player transparency features
- Extensive mod compatibility patches

## 🏗️ **Architecture**

### **System Flow**
```
Map Analysis → Pawn Analysis → Priority Calculation → Work Assignment
```

1. **Map Analysis**: Gather colony statistics, resource counts, threats, weather
2. **Pawn Analysis**: Evaluate individual stats, traits, needs, health
3. **Priority Calculation**: Process PriorityGiverDefs to determine adjustments
4. **Work Assignment**: Apply calculated priorities to work types

### **Key Components**
- **[PriorityGiverDef](1.6/Defs/PriorityGiverDef_Examples.xml)**: Defines priority calculation logic
- **[InfoGiverDef](1.6/Defs/InfoGiverDef_Examples.xml)**: Modular data collection definitions
- **[Filtering System](XML_REFERENCE.md#advanced-filtering-system)**: Advanced condition evaluation

## 📖 **Documentation for Modders**

- **[XML Reference Guide](XML_REFERENCE.md)** – Complete reference for all XML definitions
- **[Architecture Documentation](ARCHITECTURE.md)** – System design and concepts
- **[API Documentation](../../wiki/API)** – For advanced modders and plugin developers

For development and contributions:
- **[Development Setup](../../wiki/Development-Setup)** – Setting up the development environment
- **[Contributing Guidelines](CONTRIBUTING.md)** – How to contribute to the project
- **[Code Style Guide](../../wiki/Code-Style)** – Coding standards and practices

## 🤝 **Contributing**

We welcome contributions from the community! Whether you're:
- **Reporting bugs** or suggesting features
- **Creating XML definitions** for new scenarios
- **Improving documentation** or examples
- **Contributing code** to the core system

See our [Contributing Guidelines](CONTRIBUTING.md) for details on how to get involved.

## 📊 **Examples**

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
