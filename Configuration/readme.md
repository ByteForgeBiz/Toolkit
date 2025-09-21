# Configuration Module

## ByteForge.Toolkit Configuration Management

A sophisticated, INI-based configuration system designed for enterprise applications with comprehensive support for strongly-typed sections, arrays, dictionaries, and advanced configuration patterns. Built on Microsoft.Extensions.Configuration with enhanced features for complex configuration scenarios.

### 🚀 Key Features

- **Strongly-Typed Sections**: Generic `ConfigSection<T>` with automatic type conversion
- **Advanced Collection Support**: Arrays, lists, and dictionaries with flexible naming
- **Thread-Safe Singleton**: Lazy initialization with concurrent section management
- **Enhanced Naming Conventions**: New `section#{name}{suffix}` convention with backward compatibility
- **Comment & Structure Preservation**: Maintains INI file formatting and comments
- **Flexible Property Mapping**: Custom naming and property control attributes
- **Globalization Support**: Culture-aware formatting for dates, numbers, and currency
- **Custom Default Providers**: Extensible default value resolution system
- **Dynamic Configuration Access**: Runtime configuration modification with save support
- **Parser Integration**: Seamless integration with ByteForge Parser for type conversion

### 🧱 Core Architecture

#### Configuration Manager
- **Singleton Pattern**: Thread-safe lazy initialization with static and instance access
- **IConfigurationManager Interface**: Clean API with explicit interface implementation
- **Dynamic Root Access**: Direct access to underlying `IConfigurationRoot`
- **Section Caching**: Concurrent dictionary for efficient section management
- **Save/Load Cycle**: Complete round-trip configuration persistence

#### ConfigSection<T>
- **Generic Type Safety**: Compile-time type checking with runtime validation
- **Property Reflection**: Automatic discovery and mapping of configuration properties
- **Collection Management**: Sophisticated array and dictionary handling
- **Attribute Processing**: Comprehensive attribute-based configuration control
- **Save Integration**: Intelligent section serialization with structure preservation

#### Enhanced Naming System
- **Default Convention**: `section#{PropertyName}{Suffix}` for arrays (`Array`) and dictionaries (`Dict`)
- **Custom Override**: Explicit section naming via attribute parameters
- **Backward Compatibility**: Supports legacy naming conventions seamlessly
- **Reference Management**: Automatic section reference creation and maintenance

### 🔧 Comprehensive Attribute System

#### Core Property Attributes
- **`[ConfigName("CustomKey")]`**: Maps properties to custom INI key names
- **`[DefaultValue(value)]`**: Specifies default values for properties
- **`[DefaultValueProvider(Type, "MethodName")]`**: Custom default value resolution methods
- **`[DoNotPersist]`**: Loads from INI but excludes from save operations (runtime-only data)
- **`[Ignore]`**: Completely bypasses property during load/save (preserves existing INI entries)

#### Collection Attributes
- **`[Array]`**: Enables array/collection support with default naming (`{PropertyName}Array`)
- **`[Array("CustomSection")]`**: Uses custom section name for array storage
- **`[Dictionary]`**: Enables dictionary support with default naming (`{PropertyName}Dict`)
- **`[Dictionary("CustomSection")]`**: Uses custom section name for dictionary storage

### 📋 Supported Collection Types

#### Array/List Collections
- **Basic Arrays**: `T[]` (all primitive and supported types)
- **Generic Lists**: `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`
- **Non-Generic**: `IList`, `ICollection`, `IEnumerable` (with string conversion)
- **Read-Only**: `IReadOnlyList<T>`, `IReadOnlyCollection<T>`

#### Dictionary Collections
- **Core Dictionaries**: `Dictionary<string, string>`, `IDictionary<string, string>`
- **Read-Only**: `IReadOnlyDictionary<string, string>`
- **KeyValuePair Collections**: `ICollection<KeyValuePair<string, string>>`, `IEnumerable<KeyValuePair<string, string>>`
- **Read-Only Collections**: `IReadOnlyCollection<KeyValuePair<string, string>>`

#### Type Conversion Support
All collections support automatic type conversion via the integrated Parser system for:
- **Primitives**: `string`, `int`, `long`, `float`, `double`, `decimal`, `bool`
- **Date/Time**: `DateTime`, `TimeSpan` with culture-aware parsing
- **Identifiers**: `Guid` with validation
- **Enums**: All enum types with string-based parsing
- **Nullable Types**: All nullable variants of supported types

### 🧪 Advanced Implementation Examples

#### Complex Configuration Model
```csharp
public class EnterpriseConfiguration
{
    // Basic properties with custom naming
    [ConfigName("ConnectionString")]
    public string DatabaseConnection { get; set; }
    
    [DefaultValue(30)]
    public int CommandTimeout { get; set; }
    
    // Arrays with different naming strategies
    [Array] // Uses default: EnterpriseConfiguration#ServersArray
    public string[] Servers { get; set; }
    
    [Array("CustomServerList")] // Uses custom section name
    public List<string> BackupServers { get; set; }
    
    // Dictionaries with type-safe access
    [Dictionary] // Uses default: EnterpriseConfiguration#SettingsDict
    public Dictionary<string, string> Settings { get; set; }
    
    [Dictionary("AdvancedConfig")] // Custom section for advanced settings
    public IDictionary<string, string> AdvancedOptions { get; set; }
    
    // Read-only collections for immutable data
    [Dictionary("ReadOnlySettings")]
    public IReadOnlyDictionary<string, string> SystemSettings { get; set; }
    
    // Custom default value provider
    [DefaultValueProvider(typeof(ConfigDefaults), nameof(ConfigDefaults.GetDataPath))]
    public string DataDirectory { get; set; }
    
    // Runtime-only data (loads but doesn't save)
    [DoNotPersist]
    public DateTime LastConfigurationUpdate { get; set; }
    
    // Computed properties (completely ignored)
    [Ignore]
    public string FullConnectionString => $"{DatabaseConnection};Timeout={CommandTimeout}";
    
    // Private storage with public access pattern
    [Array("PrivateServerList")]
    private string[] _internalServers { get; set; }
    
    [Ignore]
    public IReadOnlyList<string> InternalServers => _internalServers?.AsReadOnly();
}

public static class ConfigDefaults
{
    public static string GetDataPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "EnterpriseApp", "Data");
}
```

#### Generated INI Structure with New Naming Convention
```ini
[EnterpriseConfiguration]
ConnectionString=Server=prod.db.com;Database=Enterprise;Integrated Security=true
CommandTimeout=45
Servers=EnterpriseConfiguration#ServersArray
BackupServers=EnterpriseConfiguration#CustomServerList
Settings=EnterpriseConfiguration#SettingsDict
AdvancedOptions=EnterpriseConfiguration#AdvancedConfig
SystemSettings=EnterpriseConfiguration#ReadOnlySettings
DataDirectory=C:\Users\AppData\EnterpriseApp\Data
LastConfigurationUpdate=2024-09-21T10:30:00

[EnterpriseConfiguration#ServersArray]
0=prod-db-01.company.com
1=prod-db-02.company.com
2=prod-db-03.company.com

[EnterpriseConfiguration#CustomServerList]
0=backup-db-01.company.com
1=backup-db-02.company.com

[EnterpriseConfiguration#SettingsDict]
MaxConnections=100
PoolTimeout=30
RetryAttempts=3
EnableLogging=true

[EnterpriseConfiguration#AdvancedConfig]
CacheSize=512MB
CompressionEnabled=true
EncryptionLevel=AES256

[EnterpriseConfiguration#ReadOnlySettings]
SystemVersion=2.1.0
LicenseType=Enterprise
SupportLevel=Premium
```

#### Real-World GHM Export Configuration
```csharp
public class GHMExportConfiguration
{
    public string WiredFolder { get; set; }
    public string WirelessFolder { get; set; }
    
    [DefaultValue(30)]
    public int RetainFor { get; set; }
    
    // Dictionary for format configurations with custom section
    [Dictionary("GHMFormats")]
    public Dictionary<string, string> FormatConfigurations { get; set; }
    
    // Dictionary for output paths
    [Dictionary("OutputPaths")]
    public Dictionary<string, string> ExportPaths { get; set; }
    
    // Array for processing schedules
    [Array("ProcessingSchedule")]
    public List<string> ScheduleTimes { get; set; }
    
    // Runtime tracking (doesn't save back to INI)
    [DoNotPersist]
    public DateTime LastExportTime { get; set; }
    
    [DoNotPersist]
    public int ExportCount { get; set; }
    
    // Computed property for validation
    [Ignore]
    public bool IsConfigurationValid => !string.IsNullOrEmpty(WiredFolder) && 
                                      !string.IsNullOrEmpty(WirelessFolder) &&
                                      FormatConfigurations?.Count > 0;
}
```

### 🧭 Usage Patterns

#### Basic Configuration Setup
```csharp
// Initialize configuration system
Configuration.Initialize("app.ini");

// Get strongly-typed section
var dbConfig = Configuration.GetSection<DatabaseConfiguration>("Database");

// Access properties with type safety
string connectionString = dbConfig.ConnectionString;
int timeout = dbConfig.Timeout; // Uses default value if not in INI

// Modify and save
dbConfig.ConnectionString = "Server=new.db.com;Database=App";
Configuration.Save();
```

#### Advanced Multi-Section Configuration
```csharp
// Initialize with custom configuration file
var configManager = new Configuration();
configManager.Initialize(@"C:\Config\enterprise.ini");

// Get multiple sections
var dbSettings = configManager.GetSection<DatabaseSettings>("Production");
var apiSettings = configManager.GetSection<ApiConfiguration>("WebAPI");
var cacheSettings = configManager.GetSection<CacheConfiguration>("Redis");

// Work with arrays and dictionaries
dbSettings.ConnectionStrings = new[] { 
    "Server=db1;Database=App", 
    "Server=db2;Database=App" 
};

apiSettings.EndpointSettings["Timeout"] = "30";
apiSettings.EndpointSettings["MaxRetries"] = "3";

// Save all changes atomically
configManager.Save();
```

#### Dynamic Configuration with Globalization
```csharp
// Access globalization settings
var globalization = Configuration.Globalization;
string culture = globalization.CultureInfo.Name; // e.g., "en-US"

// Format values using configuration culture
decimal amount = 1234.56m;
string formatted = globalization.FormatCurrency(amount); // "$1,234.56"

// Parse values with culture awareness
DateTime parsedDate = globalization.ParseDateTime("12/25/2024");
```

#### Thread-Safe Concurrent Access
```csharp
// Configuration is thread-safe for concurrent access
await Task.Run(() =>
{
    var section1 = Configuration.GetSection<AppSettings>("App");
    section1.MaxUsers = 100;
});

await Task.Run(() =>
{
    var section2 = Configuration.GetSection<DatabaseSettings>("Database");
    section2.Timeout = 60;
});

// Save is thread-safe
Configuration.Save();
```

### 🔍 Array and Dictionary Deep Dive

#### Enhanced Section Naming Convention
The new naming convention uses the format: `{SectionName}#{PropertyName}{Suffix}`

```csharp
public class ConfigExample
{
    [Array] // Creates section: ConfigExample#ItemsArray
    public string[] Items { get; set; }
    
    [Dictionary] // Creates section: ConfigExample#SettingsDict
    public Dictionary<string, string> Settings { get; set; }
    
    [Array("CustomItems")] // Creates section: ConfigExample#CustomItems
    public List<string> CustomList { get; set; }
}
```

#### Array Key Ordering and Best Practices
Arrays use alphabetical key sorting (Microsoft.Extensions.Configuration behavior):

```ini
# ⚠️ File order ≠ Array order due to alphabetical key sorting
[Items]
z=Last     # Becomes Items[0] - alphabetically first
a=First    # Becomes Items[1] - alphabetically second  
m=Middle   # Becomes Items[2] - alphabetically third

# Result array: ["Last", "First", "Middle"] - NOT file order!

# ✅ Use zero-padded numeric keys for predictable ordering:
[Items]
00=First   # Items[0]
01=Second  # Items[1]
02=Third   # Items[2]
10=Tenth   # Items[3]
99=Last    # Items[4]
```

#### Dictionary Integration Examples
```csharp
public class ApiConfiguration
{
    [Dictionary("EndpointSettings")]
    public Dictionary<string, string> Endpoints { get; set; }
    
    [Dictionary("AuthenticationConfig")]
    public IReadOnlyDictionary<string, string> AuthSettings { get; set; }
    
    [Dictionary("FeatureFlags")]
    public ICollection<KeyValuePair<string, string>> Features { get; set; }
}

// Usage in code
var config = Configuration.GetSection<ApiConfiguration>("API");

// Add/modify dictionary entries
config.Endpoints["BaseUrl"] = "https://api.company.com";
config.Endpoints["Version"] = "v2";
config.Endpoints["Timeout"] = "30";

// Access read-only configurations
string authMode = config.AuthSettings["Mode"]; // e.g., "OAuth2"

// Work with feature flags
foreach (var feature in config.Features)
{
    Console.WriteLine($"Feature {feature.Key}: {feature.Value}");
}

Configuration.Save();
```

### 🎯 Advanced Features and Integration

#### Parser Integration for Type Conversion
The configuration system seamlessly integrates with ByteForge.Toolkit.Parser:

```csharp
public class TypedConfiguration
{
    public DateTime StartDate { get; set; }
    public TimeSpan ProcessingInterval { get; set; }
    public LogLevel MinimumLevel { get; set; }
    public Guid ApplicationId { get; set; }
    
    [Array]
    public List<LogLevel> EnabledLevels { get; set; }
    
    [Dictionary("TimeoutSettings")]
    public Dictionary<string, string> Timeouts { get; set; } // Parsed as TimeSpan when accessed
}

// Custom type registration with Parser
Parser.RegisterType(typeof(TimeSpan), 
    value => TimeSpan.ParseExact(value, @"hh\:mm\:ss", null),
    value => ((TimeSpan)value).ToString(@"hh\:mm\:ss"));

// Configuration automatically uses registered parsers
var config = Configuration.GetSection<TypedConfiguration>("Processing");
config.ProcessingInterval = new TimeSpan(2, 30, 0); // Saved as "02:30:00"
```

#### Custom Default Value Providers
```csharp
public class AdvancedConfiguration
{
    [DefaultValueProvider(typeof(EnvironmentDefaults), nameof(EnvironmentDefaults.GetTempPath))]
    public string TempDirectory { get; set; }
    
    [DefaultValueProvider(typeof(EnvironmentDefaults), nameof(EnvironmentDefaults.GetMaxThreads))]
    public int MaxWorkerThreads { get; set; }
    
    [DefaultValueProvider(typeof(EnvironmentDefaults), nameof(EnvironmentDefaults.GetServerList))]
    [Array("DefaultServers")]
    public string[] DefaultServerList { get; set; }
}

public static class EnvironmentDefaults
{
    public static string GetTempPath() => Path.GetTempPath();
    
    public static int GetMaxThreads() => Environment.ProcessorCount * 2;
    
    public static string[] GetServerList() => new[] { 
        Environment.MachineName + ".local",
        "localhost",
        "127.0.0.1"
    };
}
```

#### Error Handling and Validation
```csharp
public class ValidatedConfiguration
{
    private string _connectionString;
    
    public string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Connection string cannot be empty");
            _connectionString = value;
        }
    }
    
    [DefaultValue(1)]
    public int MinConnections { get; set; }
    
    [DefaultValue(100)]
    public int MaxConnections { get; set; }
    
    // Validation in computed property
    [Ignore]
    public bool IsValid => !string.IsNullOrEmpty(ConnectionString) && 
                          MinConnections > 0 && 
                          MaxConnections >= MinConnections;
}

// Usage with error handling
try
{
    var config = Configuration.GetSection<ValidatedConfiguration>("Database");
    if (!config.IsValid)
    {
        throw new InvalidOperationException("Database configuration is invalid");
    }
}
catch (Exception ex)
{
    Log.Error($"Configuration error: {ex.Message}");
    // Handle configuration errors appropriately
}
```

### ⚠️ Important Considerations and Limitations

#### Array Key Ordering
**Critical**: Arrays are sorted alphabetically by key due to Microsoft.Extensions.Configuration.Ini provider:
- File order ≠ Array order
- Use zero-padded numeric keys (`00`, `01`, `02`) for predictable ordering
- Test array ordering if using non-numeric or inconsistent key formats

#### Section Reference Management
- Each array/dictionary property creates a section reference in the main section
- Shared section names between properties create conflicts (last save wins)
- Use unique section names or rely on default naming conventions

#### Save Operation Behavior
- `Save()` writes the entire configuration state
- Comments and manual INI formatting may be lost during save
- Use `[DoNotPersist]` for data that should load but not save back
- Use `[Ignore]` for computed properties that should be completely bypassed

#### Thread Safety
- Configuration class is thread-safe for concurrent read operations
- Section creation is thread-safe with internal locking
- Save operations are protected with write locks
- Individual property modifications within sections are not automatically thread-safe

### ✅ Best Practices and Recommendations

#### Configuration Design
- **Early Initialization**: Initialize configuration at application startup
- **Section Organization**: Use logical section grouping by functional domain
- **Property Naming**: Use clear, descriptive property names that match INI conventions
- **Default Values**: Provide sensible defaults for all optional properties

#### Array and Dictionary Usage
- **Predictable Ordering**: Use zero-padded numeric keys for arrays when order matters
- **Unique Naming**: Use default naming conventions or ensure unique custom section names
- **Type Safety**: Leverage strongly-typed properties over dynamic access
- **Validation**: Implement property validation for critical configuration values

#### Performance Optimization
- **Section Caching**: Configuration sections are cached for efficient repeated access
- **Lazy Loading**: Sections are created on-demand to minimize memory usage
- **Batch Operations**: Group related configuration changes before calling Save()
- **Parser Registration**: Register custom types once at application startup

#### Error Handling
- **Validation**: Validate configuration files and required sections during initialization
- **Exception Handling**: Wrap configuration access in appropriate try-catch blocks
- **Fallback Strategies**: Implement fallback configurations for critical applications
- **Logging Integration**: Use ByteForge.Toolkit.Logging for configuration-related diagnostics

#### Attribute Strategy
- **Runtime Data**: Use `[DoNotPersist]` for timestamps, counters, and calculated values
- **Computed Properties**: Use `[Ignore]` for properties that should never touch INI files
- **Custom Naming**: Use `[ConfigName]` when INI keys must differ from property names
- **Collection Control**: Use `[Array]` and `[Dictionary]` attributes for precise section control

#### Testing and Maintenance
- **Configuration Validation**: Test configuration loading with various INI file states
- **Array Ordering**: Verify array ordering behavior with your specific key formats
- **Round-Trip Testing**: Test save/load cycles to ensure data integrity
- **Thread Safety**: Test concurrent access patterns if using multi-threaded applications

---

## 📚 Related Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [CLI](../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |