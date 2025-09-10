# ByteForge Configuration Management

A flexible and robust configuration management system for .NET applications that supports INI file handling with section management, dynamic configuration access, and strong typing.

## Features

- INI file configuration management with section support
- Dynamic configuration access through a singleton pattern
- Strongly-typed configuration sections
- Comment preservation during file updates
- Automatic type conversion for various .NET types
- Globalization support
- Customizable property persistence
- Thread-safe operations

## Key Components

### Configuration Class

The main entry point for configuration management. It provides:

- Singleton access through `Configuration.Obj`
- Static initialization methods
- Dynamic member access
- Section management
- Automatic initialization when needed
- Thread-safe operations

Usage example:
```csharp
// Initialize configuration with specific file
Configuration.Initialize(@"C:\config\settings.ini");

// Access configuration dynamically
var setting = Configuration.Obj.SomeSetting;

// Get typed section
var globalization = Configuration.Globalization;
```

### ConfigurationSection<T>

Handles strongly-typed configuration sections with features like:

- Automatic property mapping
- Type conversion for various .NET types
- Support for custom property names through attributes
- Thread-safe operations
- Dictionary conversion capability

Example of creating a custom section:
```csharp
public class DatabaseSettings
{
    public string ConnectionString { get; set; }
    public int Timeout { get; set; }
    
    [PropertyName("MaxConn")]
    public int MaxConnections { get; set; }
    
    [DoNotPersist]
    public string TempSetting { get; set; }
}

// Get or create the section
var dbSettings = Configuration.GetSection<DatabaseSettings>();
```

### GlobalizationInfo

Provides culture and formatting settings:

- Culture information management
- Date and time format specifications
- Combined date-time format support

Example usage:
```csharp
var globalization = Configuration.Globalization;
var culture = globalization.CultureInfo;
var dateFormat = globalization.DateFormat;
```

## Attributes

### DoNotPersistAttribute

Use this attribute to mark properties that should not be saved to the configuration file:

```csharp
public class MySection
{
    public string SaveThis { get; set; }
    
    [DoNotPersist]
    public string DontSaveThis { get; set; }
}
```

## Thread Safety

The system implements thread-safe operations through:
- Lock-based synchronization for critical sections
- Thread-safe collections (ConcurrentDictionary)
- Lazy initialization patterns

## Best Practices

1. **Initialization**: Always initialize the configuration system before using it:
   ```csharp
   Configuration.Initialize("settings.ini");
   ```

2. **Typed Sections**: Use strongly-typed sections for related settings:
   ```csharp
   public class LoggingSettings
   {
       public string LogPath { get; set; }
       public string LogLevel { get; set; }
   }
   
   var logging = Configuration.GetSection<LoggingSettings>();
   ```

3. **Property Persistence**: Use `[DoNotPersist]` for temporary or runtime-only properties.

4. **Error Handling**: The system throws appropriate exceptions for invalid operations:
   - `ArgumentNullException` for null parameters
   - `InvalidOperationException` for invalid operations
   - `DirectoryNotFoundException` for missing directories
   - `FileNotFoundException` for missing files

## Requirements

- .NET Framework 4.8
- Microsoft.Extensions.Configuration
- System.Web (for web applications)

## Notes

- Comments in the INI file are preserved during save operations
- The system supports dynamic member access for simple key-value pairs
- Section names default to the class name if not specified
- All file operations are thread-safe
- The configuration is lazily initialized when first accessed
