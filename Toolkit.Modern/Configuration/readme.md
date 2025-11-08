# Configuration

This folder contains the configuration management system for ByteForge.Toolkit, focused on INI-based configuration with strong typing, section support, arrays, and dictionaries. It leverages Microsoft.Extensions.Configuration for robust parsing and thread-safe access.

## Key Classes & Interfaces

- **Configuration**: The main configuration manager. Loads, saves, and provides access to configuration sections. Supports static and instance usage, thread safety, and globalization settings.
- **ConfigSection<T>**: Represents a strongly-typed configuration section, supporting property mapping, arrays, dictionaries, default values, and attribute-based customization.
- **IConfigurationManager**: Interface for configuration manager functionality.
- **IConfigSection / IConfigSection<T>**: Interfaces for configuration section abstraction and strong typing.
- **GlobalizationInfo**: Holds culture, date/time, and numeric formatting settings for the application.
- **AssemblyException**: Exception for errors during configuration loading related to assembly resolution.

## Attributes
- **ArrayAttribute**: Marks a property as an array, optionally specifying a custom section name.
- **DictionaryAttribute**: Marks a property as a dictionary, optionally specifying a custom section name.
- **ConfigNameAttribute**: Maps a property to a custom configuration key name.
- **DefaultValueProviderAttribute**: Specifies a static method to provide a property's default value.
- **DoNotPersistAttribute**: Prevents a property from being persisted to configuration.
- **IgnoreAttribute**: Prevents a property from being loaded or saved (extends DoNotPersist).

## Features
- INI file parsing and saving with preservation of comments and structure
- Strongly-typed configuration sections with attribute-based mapping
- Array and dictionary support via dedicated INI sections
- Default value and custom default provider support
- Globalization and formatting settings
- Thread-safe operations for concurrent access

## Usage Example
```csharp
// Initialize configuration
Configuration.Initialize("appsettings.ini");

// Access a strongly-typed section
var dbConfig = Configuration.GetSection<DatabaseConfig>("Database");
string connStr = dbConfig.ConnString;

// Save changes
Configuration.Save();
```

See also: `Models/ConfigSection.cs`, `Attributes/*.cs`, and test models in `Toolkit.Modern.Tests/Models/ConfigurationTestModels.cs` for advanced scenarios.

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                            | Description                |
|---------------------------------------------------|----------------------------|
| **[CLI](../CommandLine/readme.md)**               | Command-line parsing       |
| **[Core](../Core/readme.md)**                     | Core utilities             |
| **[Data](../Data/readme.md)**                     | Database & file processing |
| **[DataStructures](../DataStructures/readme.md)** | Collections & utilities    |
| **[JSON](../Json/readme.md)**                     | Delta serialization        |
| **[Logging](../Logging/readme.md)**               | Structured logging         |
| **[Mail](../Mail/readme.md)**                     | Email processing           |
| **[Net](../Net/readme.md)**                       | Network file transfers     |
| **[Security](../Security/readme.md)**             | Encryption & security      |
| **[Utils](../Utilities/readme.md)**               | General utilities          |
