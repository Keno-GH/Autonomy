# Autonomy - RimWorld Mod

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-green.svg)](https://rimworldgame.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Development Status](https://img.shields.io/badge/Status-In%20Development-yellow.svg)](https://github.com/Keno-GH/Autonomy/projects)

A revolutionary RimWorld mod that allows colonists to autonomously manage their work priorities based on colony needs, personal traits, and current conditions. Built with a powerful XML-driven system that's highly modular and extensible.

## 🎯 **Vision**

Autonomy transforms RimWorld's work management from micromanagement to intelligent automation. Colonists analyze their surroundings, assess colony needs, and adjust their work priorities accordingly - all while maintaining their unique personalities and preferences.

## ✨ **Key Features**

### 🧠 **Intelligent Decision Making**
- **Dynamic Priority Calculation**: Colonists evaluate colony conditions and adjust priorities in real-time
- **Personality Integration**: Traits influence how colonists prioritize different types of work
- **Contextual Awareness**: Weather, threats, resources, and colony needs all factor into decisions

### 🔧 **Modular XML System**
- **PriorityGiverDefs**: Define how colony conditions translate to work priority adjustments
- **InfoGiverDefs**: Modular data collection from various game systems
- **Easy Customization**: All logic defined through XML for easy modification and mod compatibility

### 🌐 **Mod Compatibility**
- **ThingCategoryDef Support**: Automatic compatibility with modded items
- **Dynamic Weather Analysis**: Works with any weather mod without patches
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

## 📖 **Documentation**

### **For Players**
- **[Installation Guide](../../wiki/Installation)** - How to install and configure
- **[User Guide](../../wiki/User-Guide)** - Understanding how the mod works
- **[FAQ](../../wiki/FAQ)** - Common questions and troubleshooting

### **For Modders**
- **[XML Reference Guide](XML_REFERENCE.md)** - Complete XML definition reference
- **[Architecture Documentation](ARCHITECTURE.md)** - System design and concepts
- **[API Documentation](../../wiki/API)** - For advanced modders and plugin developers

### **For Developers**
- **[Development Setup](../../wiki/Development-Setup)** - Setting up the development environment
- **[Contributing Guidelines](CONTRIBUTING.md)** - How to contribute to the project
- **[Code Style Guide](../../wiki/Code-Style)** - Coding standards and practices

## 🚀 **Quick Start**

### **For Players**
1. Install the mod from the Steam Workshop (coming soon)
2. Ensure you have Harmony installed
3. Add to your mod list and start a new game or load existing save
4. Watch as your colonists begin making smarter work decisions!

### **For Modders**
1. Check out the [XML Reference Guide](XML_REFERENCE.md)
2. Look at the [example definitions](1.6/Defs/)
3. Create your own PriorityGiverDefs and InfoGiverDefs
4. Share your creations with the community!

### **For Developers**
1. Clone this repository
2. Follow the [Development Setup Guide](../../wiki/Development-Setup)
3. Check out the [current TODO](TODO.md) for contribution opportunities
4. Read the [Contributing Guidelines](CONTRIBUTING.md)

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
- **[Discord Community](https://discord.gg/TODO)** (Coming Soon)
- **[Bug Reports](https://github.com/Keno-GH/Autonomy/issues)**

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👏 **Acknowledgments**

- **RimWorld Community** for inspiration and feedback
- **Harmony Library** for making advanced modding possible
- **Template Mod Contributors** for the initial framework
- **Beta Testers** who help make this mod stable and balanced

---

**Made with ❤️ for the RimWorld community**

*For support, questions, or just to chat about the mod, join our [Discord](https://discord.gg/TODO) or check out the [discussions](https://github.com/Keno-GH/Autonomy/discussions)!*
