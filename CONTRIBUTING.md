# Contributing to Autonomy

Thank you for your interest in contributing to the Autonomy mod! This document provides guidelines and information for contributors.

## 🤝 Ways to Contribute

### 🐛 **Bug Reports**
- Use the [GitHub Issues](https://github.com/Keno-GH/Autonomy/issues) page
- Include your RimWorld version, mod list, and steps to reproduce
- Attach logs if possible (`Player.log` or `Unity Player.log`)
- Reporting bugs in the steam workshop is allowed but GitHub is preferred for tracking

### 💡 **Feature Suggestions**
- Open an issue with the `enhancement` label
- Describe the use case and expected behavior
- Consider how it fits with the mod's philosophy and vision

### 📝 **Documentation**
- Improve existing documentation
- Add examples and tutorials
- Translate documentation (when internationalization is added)
- Update the wiki with new information

### 🔧 **XML Definitions**
- Create new PriorityGiverDefs for common scenarios
- Design InfoGiverDefs for mod compatibility
- Submit example configurations for different playstyles

### 💻 **Code Contributions**
- Bug fixes and performance improvements
- New features from the roadmap
- Mod compatibility patches
- Testing and quality assurance

## 🚀 Development Setup

### Prerequisites
- **.NET Framework 4.8 Developer Pack**
- **Visual Studio Code** with C# extension
- **Git** for version control
- **RimWorld 1.6** for testing

### Setup Steps
1. **Fork and Clone**
   ```bash
   git clone https://github.com/YOUR_USERNAME/Autonomy.git
   cd Autonomy
   ```

2. **Open in VS Code**
   ```bash
   code .
   ```

3. **Build and Test**
   - Press `F5` to build and launch RimWorld
   - Test your changes with various colony scenarios

### Development Workflow
1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
   - Follow the coding standards below
   - Add tests for new functionality
   - Update documentation as needed

3. **Test thoroughly**
   - Test with various mod combinations
   - Verify performance impact is minimal
   - Ensure no game-breaking bugs

4. **Submit a Pull Request**
   - Describe your changes clearly
   - Reference any related issues
   - Include screenshots/videos if relevant

## 📋 Coding Standards

### **C# Code Style**
- **Naming**: Use PascalCase for classes, methods, properties
- **Indentation**: 4 spaces (no tabs)
- **Braces**: Opening brace on same line
- **Comments**: XML documentation for public APIs
- **Error Handling**: Always handle potential null values

### **XML Definitions**
- **Indentation**: Tabs for consistency with RimWorld
- **Naming**: Use descriptive, clear defNames
- **Comments**: Explain complex logic and conditions
- **Validation**: Ensure all required fields are present

### **Performance Guidelines**
- **Caching**: Cache expensive calculations
- **Early Exit**: Return early when conditions aren't met
- **Batch Operations**: Process multiple items together when possible
- **Profiling**: Profile performance-critical code

## 🎯 Current Development Priorities

### **Phase 1 (In Progress)**
- [ ] InfoGiver evaluation engine
- [ ] Basic priority calculation
- [ ] Simple condition evaluation
- [ ] Harmony integration

### **Phase 2 (Planned)**
- [ ] Advanced filtering system
- [ ] Personality trait integration
- [ ] Performance optimization
- [ ] Mod compatibility patches

### **Community Contributions Needed**
- [ ] **XML Examples**: More PriorityGiverDef examples for common scenarios
- [ ] **Testing**: Compatibility testing with popular mods
- [ ] **Documentation**: User guides and tutorials
- [ ] **Localization**: Preparing for multi-language support

## 🧪 Testing Guidelines

### **Test Categories**
1. **Unit Tests**: Individual component functionality
2. **Integration Tests**: System interaction testing
3. **Performance Tests**: Memory and CPU impact
4. **Compatibility Tests**: Popular mod combinations

### **Test Scenarios**
- **Small Colony** (1-5 colonists): Basic functionality
- **Large Colony** (15+ colonists): Performance under load
- **Extreme Conditions**: Raids, diseases, disasters
- **Modded Environment**: Various mod combinations

### **Test Documentation**
- Document test cases and expected results
- Include savegame files for reproducible testing
- Report performance metrics for optimization

## 📖 Documentation Standards

### **Code Documentation**
```csharp
/// <summary>
/// Brief description of the class/method purpose
/// </summary>
/// <param name="parameter">Parameter description</param>
/// <returns>Return value description</returns>
public class ExampleClass
{
    // Implementation
}
```

### **XML Documentation**
```xml
<!-- Brief description of what this definition does -->
<Autonomy.PriorityGiverDef>
    <defName>ExamplePriority</defName>
    <!-- More XML here -->
</Autonomy.PriorityGiverDef>
```

### **Wiki Documentation**
- Use clear, concise language
- Include practical examples
- Add screenshots where helpful
- Link to related documentation

## 🔄 Release Process

### **Version Numbering**
- **Major.Minor.Patch** (e.g., 1.2.3)
- **Major**: Breaking changes or major features
- **Minor**: New features, backwards compatible
- **Patch**: Bug fixes and small improvements

### **Release Checklist**
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Version numbers updated
- [ ] Changelog prepared
- [ ] Steam Workshop description updated

## 🏷️ Issue Labels

- **`bug`**: Something isn't working correctly
- **`enhancement`**: New feature or improvement
- **`documentation`**: Documentation needs improvement
- **`good first issue`**: Good for newcomers
- **`help wanted`**: Extra attention needed
- **`performance`**: Performance-related issue
- **`compatibility`**: Mod compatibility issue

## 📞 Community

### **Communication Channels**
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General discussion and Q&A
- **Discord**: Real-time chat and community (coming soon)
- **Reddit**: r/RimWorld community discussions

### **Code of Conduct**
- Be respectful and inclusive
- Focus on constructive feedback
- Help newcomers learn and contribute
- Maintain a positive community environment

## 🙏 Recognition

Contributors will be recognized in:
- **CONTRIBUTORS.md** file
- **Release notes** for significant contributions
- **Steam Workshop description**
- **In-game credits** (planned feature)

## 📄 Legal

By contributing to this project, you agree that your contributions will be licensed under the same license as the project (MIT License).

---

Thank you for helping make Autonomy better for the entire RimWorld community! 🎉