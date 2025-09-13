# Configuration.md

## ByteForge Configuration Management

A powerful, INI-based configuration system with section support, strong typing, dynamic access, and array support for .NET Framework 4.8 applications.

### 🚀 Features
- Strongly-typed section mapping with automatic type conversion
- Array and collection support for strings and other types
- Singleton access with lazy initialization and thread-safety
- Comment and formatting preservation in INI files
- Flexible section naming and property mapping
- Globalization-aware formatting and parsing
- Custom default value providers and type converters

### 🧱 Key Components
- **Configuration**: Singleton manager with dynamic object access
- **ConfigSection<T>**: Generic typed section support with array handling
- **DefaultValueHelper**: Resolves default values from attributes or custom providers
- **GlobalizationInfo**: Comprehensive formatting for dates, numbers, and currency

### 🔧 Attributes
- **DoNotPersistAttribute**: Prevents persistence of runtime-only values (loads from INI, but doesn't save back)
- **IgnoreAttribute**: Completely ignores properties - not read, not saved, existing INI entries left untouched
- **ConfigNameAttribute**: Maps properties to custom INI key names
- **DefaultValueProviderAttribute**: Specifies custom default value methods
- **ArrayAttribute**: Enables array/collection support with flexible section naming

### 🧪 Basic Example
```csharp
Configuration.Initialize("settings.ini");
var dbSettings = Configuration.GetSection<DatabaseSettings>();
```

#### Custom Section with Arrays:
```csharp
public class DatabaseSettings
{
    public string ConnectionString { get; set; }
    
    [DefaultValue(30)]
    public int Timeout { get; set; }

    [Array] // Uses default section name "ConnectionStringsArray"
    private string[] _connectionStrings { get; set; }
    
    [Array("ServerList")] // Uses custom section name
    public List<string> Servers { get; set; }

    [DoNotPersist] // Loads from INI but doesn't save back
    public DateTime LastAccessed { get; set; }

    [Ignore] // Completely ignored during load/save
    public IReadOnlyList<string> ConnectionStrings => _connectionStrings?.AsReadOnly();
}
```

#### Corresponding INI Structure:
```ini
[DatabaseSettings]
ConnectionString=Server=main;Database=App
Timeout=45
ConnectionStrings=ConnectionStringsArray
Servers=ServerList

[ConnectionStringsArray]
0=Server=db1;Database=App
1=Server=db2;Database=App
2=Server=db3;Database=App

[ServerList]
0=db1.company.com
1=db2.company.com
2=db3.company.com
```

### 🏗️ Array Support

The configuration system supports arrays and collections through the `[Array]` attribute:

#### Supported Collection Types:
- `T[]` (arrays)
- `List<T>`
- `IList<T>`
- `ICollection<T>`
- `IEnumerable<T>`
- `IList`
- `ICollection`
- `IEnumerable`

#### Array Section Naming:
- **Default**: `{PropertyName}Array`
- **Custom**: `[Array("CustomSectionName")]`
- **Override**: Set value in INI file to use different section name

#### Flexible Array Key Format:
Array sections support any key format - the system reads values in file order, but normalizes to numeric indices when saved:

```ini
# All of these create the same array:

# Numeric indices:
[MyArray]
0=First
1=Second
2=Third

# Named keys:
[MyArray]
Primary=First
Secondary=Second
Backup=Third

# Mixed keys:
[MyArray]
99=First
abc=Second
xyz=Third

# After saving, all formats become:
[MyArray]
0=First
1=Second
2=Third
```

### 🧭 Globalization Example
```csharp
var culture = Configuration.Globalization.CultureInfo;
var formatted = Configuration.Globalization.FormatCurrency(1234.56m);
```

### 🎯 Advanced Features

#### Custom Default Values:
```csharp
public class AppSettings
{
    [DefaultValueProvider(typeof(ConfigDefaults), nameof(ConfigDefaults.GetDefaultPath))]
    public string DataPath { get; set; }
}

public static class ConfigDefaults
{
    public static string GetDefaultPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyApp");
}
```

#### Property Control Attributes:
```csharp
public class AdvancedConfig
{
    // Regular property - loads and saves
    public string NormalProperty { get; set; }
    
    // Loads from INI but doesn't save back (runtime-only)
    [DoNotPersist]
    public DateTime LastCalculated { get; set; }
    
    // Completely ignored - not read, not saved, existing INI entries preserved
    [Ignore] 
    public ComplexObject ComputedProperty => new ComplexObject(NormalProperty);
    
    // Private storage with public read-only access
    [Array]
    private string[] _items { get; set; }
    
    [Ignore]
    public IReadOnlyList<string> Items => _items?.AsReadOnly();
}
```

### ⚠️ Important Considerations

#### Array Section Sharing:
Be cautious when multiple sections reference the same array section name, as this creates a shared resource that the last-saved section will overwrite:

```ini
# This configuration has shared array sections:
[Section1]
MyArray=SharedArray

[Section2] 
MyArray=SharedArray  # Same section name!

[SharedArray]
# Last section to save wins!
```

Use unique section names or the `[Array]` attribute's default naming to avoid conflicts.

### ✅ Best Practices
- Initialize configuration early in application lifecycle
- Use `[Array]` for collections and let the system generate section names
- Use `[DoNotPersist]` for runtime data that should load but not save (timestamps, calculated values)
- Use `[Ignore]` for computed properties that should be completely bypassed (preserves any existing INI entries)
- Use `[ConfigName]` when INI key names must differ from property names
- Consider private storage properties with `[Array]` and public `[Ignore]` wrapper properties for complex scenarios
- Validate file paths and section names during initialization
- Consider using `DefaultValueProvider` for complex default value logic

---

## 📚 Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [CommandLine](../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |
