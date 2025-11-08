# Attributes Module

## Overview

The **Attributes** module defines custom attributes used throughout the ByteForge Toolkit for declarative programming and metadata-driven configuration. These attributes enable annotation-based feature discovery and configuration without boilerplate code.

---

## Purpose

Custom attributes allow developers to:
1. **Mark intent** - Indicate how properties should be mapped
2. **Configure behavior** - Specify options without code changes
3. **Enable tooling** - Allow reflection-based discovery
4. **Reduce boilerplate** - Avoid manual mapping configuration
5. **Maintain DRY** - Don't repeat configuration logic

---

## Attributes by Category

### CSV Mapping Attributes

#### `CSVColumnAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Properties

**Purpose:** Maps a C# class property to a CSV column.

**Parameters:**
- `ColumnName` (string) - Name of CSV column header
- `ColumnIndex` (int, optional) - Zero-based column position
- `Format` (string, optional) - Format string for date/number columns
- `Optional` (bool) - Whether column can be missing

**Usage:**
```csharp
public class PersonRecord
{
    [CSVColumn("First Name")]
  public string FirstName { get; set; }
    
    [CSVColumn("Last Name")]
    public string LastName { get; set; }
    
 [CSVColumn("Birth Date", Format = "MM/dd/yyyy")]
    public DateTime BirthDate { get; set; }
    
    [CSVColumn("Email", Optional = true)]
    public string Email { get; set; }
}
```

**Usage in Code:**
```csharp
public void ProcessCSV(string filePath)
{
    var reader = new CSVReader();
    reader.RowHandler = (row, status, line) =>
    {
   if (status != CSVReader.CSVRowStatus.OK) return true;
        
        // Map to object
        var person = MapCSVRow<PersonRecord>(row);
        
      return true;  // Continue processing
    };
    reader.ReadFile(filePath);
}
```

---

### Database Mapping Attributes

#### `DBColumnAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Properties

**Purpose:** Maps a C# class property to a database column.

**Parameters:**
- `ColumnName` (string) - Database column name
- `DataType` (string, optional) - SQL data type
- `AllowNull` (bool) - Whether column allows NULL
- `IsPrimaryKey` (bool) - Indicates primary key
- `IsIdentity` (bool) - Auto-increment column
- `MaxLength` (int) - Maximum string length (-1 for unlimited)
- `Precision` (int) - Decimal precision
- `Scale` (int) - Decimal scale

**Usage:**
```csharp
public class UserEntity
{
    [DBColumn("UserId", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    
    [DBColumn("UserName", MaxLength = 50, AllowNull = false)]
    public string Username { get; set; }
    
    [DBColumn("EmailAddress", MaxLength = 100)]
    public string Email { get; set; }
    
    [DBColumn("CreationDate", DataType = "datetime")]
    public DateTime CreatedAt { get; set; }
    
    [DBColumn("LastLoginDate", AllowNull = true)]
    public DateTime? LastLogin { get; set; }
}
```

**Used By:** `BulkDbProcessor<T>`, `DBAccess` for automatic table mapping.

---

### Configuration Attributes

#### `ConfigNameAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Properties

**Purpose:** Maps a C# property to an INI configuration key.

**Parameters:**
- `ColumnName` (string) - INI key name
- `ColumnCaption` (string) - Friendly display name

**Usage:**
```csharp
public class DatabaseSettings
{
    [ConfigName("Server", "Database Server Name")]
    public string Server { get; set; }
    
    [ConfigName("Database", "Database Name")]
    public string DatabaseName { get; set; }
    
    [ConfigName("Port", "Server Port")]
    public int Port { get; set; }
}
```

**INI Mapping:**
```ini
[DatabaseSettings]
Server=prod.server.com
Database=MyApplication
Port=1433
```

---

#### `DefaultValueAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Properties

**Purpose:** Specifies default value when INI key is missing.

**Parameters:**
- `Value` (object) - Default value

**Usage:**
```csharp
public class AppSettings
{
    [ConfigName("Environment")]
    [DefaultValue("Development")]
    public string Environment { get; set; }
 
    [ConfigName("Port")]
    [DefaultValue(8080)]
    public int Port { get; set; }
    
    [ConfigName("Timeout")]
 [DefaultValue(30)]
    public int TimeoutSeconds { get; set; }
}
```

**Behavior:**
- Applied when key missing from INI
- Type-coerced to property type
- Improves graceful degradation

---

#### `DefaultValueProviderAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Properties

**Purpose:** Uses a provider class to generate default values for complex types.

**Parameters:**
- `ProviderType` (Type) - Class implementing `IDefaultValueProvider`

**Usage:**
```csharp
public class ConnectionSettings
{
    [ConfigName("ConnectionString")]
    [DefaultValueProvider(typeof(DefaultConnectionStringProvider))]
    public string ConnectionString { get; set; }
}

public class DefaultConnectionStringProvider : IDefaultValueProvider
{
    public object GetDefaultValue()
    {
        return $"Server=(local);Database=AppDB;Integrated Security=true;";
    }
}
```

**When to Use:**
- Default values require computation
- Defaults depend on environment
- Value needs special initialization

---

#### `DoNotPersistAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Properties

**Purpose:** Prevents a configuration property from being saved to INI file.

**Parameters:** None

**Usage:**
```csharp
public class RuntimeSettings
{
    [ConfigName("ApiKey")]
    public string ApiKey { get; set; }  // Saved to INI
    
    [ConfigName("CachedToken")]
    [DoNotPersist]
    public string CachedAuthToken { get; set; }  // NOT saved
    
    [DoNotPersist]
    public DateTime LastLoadTime { get; set; }  // NOT saved
}
```

**Behavior:**
- Property still loaded from INI if present
- Only affects `Configuration.Save()`
- Useful for runtime-only values

**Common Uses:**
- Session tokens
- Cached computed values
- Temporary runtime state

---

#### `ArrayAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Classes

**Purpose:** Marks a configuration section as containing array elements.

**Parameters:**
- `ItemElementName` (string) - Name of array item in INI

**Usage:**
```csharp
[Array("Hosts")]
public class AllowedHostsConfig
{
    public List<string> Hosts { get; set; }
}

[Array("Items")]
public class DatabaseServersConfig
{
    [ConfigName("Server")]
    public List<string> ServerList { get; set; }
}
```

**INI Format:**
```ini
[AllowedHosts]
Hosts0=localhost
Hosts1=127.0.0.1
Hosts2=api.production.com

[DatabaseServers]
Server0=SERVER01
Server1=SERVER02
Server2=SERVER03
```

**Behavior:**
- Elements indexed as `name0`, `name1`, `name2`, etc.
- Loads into `List<T>` properties
- Flexible list lengths

---

#### `DictionaryAttribute`
**Namespace:** `ByteForge.Toolkit`
**Targets:** Classes

**Purpose:** Marks a section as containing key-value pairs (dictionary).

**Parameters:**
- None (uses all key-value pairs in section)

**Usage:**
```csharp
[Dictionary]
public class FeatureFlags
{
    public Dictionary<string, bool> Flags { get; set; }
}

[Dictionary]
public class ApiEndpoints
{
    [ConfigName("Endpoint")]
  public Dictionary<string, string> Endpoints { get; set; }
}
```

**INI Format:**
```ini
[FeatureFlags]
EnableNewUI=true
EnableAnalytics=false
BetaFeatures=false

[ApiEndpoints]
Endpoint.Users=https://api.example.com/users
Endpoint.Products=https://api.example.com/products
Endpoint.Orders=https://api.example.com/orders
```

**Behavior:**
- All key-value pairs become dictionary entries
- Keys are INI keys, values are INI values
- No strict schema required

---

## Complete Attribute Combination Example

```csharp
[Array("Database")]
public class DatabaseConnectionsConfig
{
    [ConfigName("Connection")]
    public List<DatabaseConnection> Connections { get; set; }
}

public class DatabaseConnection
{
    [ConfigName("Name")]
    public string Name { get; set; }
    
 [ConfigName("Server")]
    public string Server { get; set; }

    [ConfigName("Database")]
    [DefaultValue("master")]
    public string DatabaseName { get; set; }
    
    [ConfigName("Port")]
    [DefaultValue(1433)]
    public int Port { get; set; }
    
    [ConfigName("Username")]
    public string Username { get; set; }
    
    [ConfigName("Password")]
    public string EncryptedPassword { get; set; }
    
    [ConfigName("ConnectionTimeout")]
    [DefaultValue(30)]
    public int Timeout { get; set; }
    
    [ConfigName("LastConnected")]
    [DoNotPersist]
    public DateTime? LastConnectionTime { get; set; }
}

[Dictionary]
public class ApplicationFeatures
{
    public Dictionary<string, bool> Features { get; set; }
}
```

**INI File:**
```ini
[Database]
Connection0.Name=Production
Connection0.Server=prod-db.company.com
Connection0.Database=ProductionDB
Connection0.Port=1433
Connection0.Username=sa
Connection0.Password=encrypted_password_here
Connection0.ConnectionTimeout=60

Connection1.Name=Staging
Connection1.Server=staging-db.company.com
Connection1.Database=StagingDB
Connection1.Port=1433
Connection1.Username=sa
Connection1.Password=encrypted_password_here

[ApplicationFeatures]
FeatureNewDashboard=true
FeatureReporting=false
FeatureBetaAPI=true
FeatureExperimentalUI=false
```

---

## File Organization

Each attribute is defined in its own file under the `Attributes/` directory:

```
Attributes/
??? CSVColumnAttribute.cs         # CSV column mapping
??? DBColumnAttribute.cs   # Database column mapping
??? ConfigNameAttribute.cs        # INI configuration key mapping
??? DefaultValueAttribute.cs      # Default value specification
??? DefaultValueProviderAttribute.cs  # Complex default value provider
??? DoNotPersistAttribute.cs     # Exclude from persistence
??? ArrayAttribute.cs             # Array configuration
??? DictionaryAttribute.cs        # Dictionary configuration
```

---

## Reflection-Based Discovery

Attributes are discovered at runtime using reflection:

```csharp
// Get all CSV column attributes
var csvProperties = typeof(PersonRecord)
    .GetProperties()
    .Where(p => p.GetCustomAttribute<CSVColumnAttribute>() != null)
    .ToList();

// Get default values
foreach (var prop in csvProperties)
{
  var defaultAttr = prop.GetCustomAttribute<DefaultValueAttribute>();
    if (defaultAttr != null)
    {
        object defaultValue = defaultAttr.Value;
        // Use default value
    }
}

// Check if property should persist
var persist = prop.GetCustomAttribute<DoNotPersistAttribute>() == null;
```

---

## Best Practices

### 1. **Meaningful Names**
```csharp
// Good
[ConfigName("DatabaseServer", "Primary Database Server")]
public string Server { get; set; }

// Bad
[ConfigName("db")]
public string Server { get; set; }
```

### 2. **Consistent Naming**
```csharp
// Use consistent key naming convention
[ConfigName("ApplicationTitle")]
public string Title { get; set; }

[ConfigName("ApplicationVersion")]
public string Version { get; set; }

[ConfigName("ApplicationAuthor")]
public string Author { get; set; }
```

### 3. **Sensible Defaults**
```csharp
// Good defaults help with graceful degradation
[ConfigName("Port")]
[DefaultValue(8080)]
public int Port { get; set; }

[ConfigName("Timeout")]
[DefaultValue(30)]
public int TimeoutSeconds { get; set; }
```

### 4. **Documentation**
```csharp
/// <summary>
/// Database server address.
/// This should be the fully qualified domain name or IP address.
/// </summary>
[ConfigName("Server", "Database Server Address")]
public string Server { get; set; }
```

---

## Validation

Combine attributes with property validation:

```csharp
public class ValidatedSettings
{
    private string _server;
    
    [ConfigName("Server")]
 public string Server
    {
    get => _server;
        set
        {
     if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Server cannot be empty");
            _server = value;
        }
    }
  
    private int _port;
    
    [ConfigName("Port")]
    [DefaultValue(1433)]
    public int Port
  {
        get => _port;
        set
      {
         if (value < 1 || value > 65535)
 throw new ArgumentException("Port must be 1-65535");
            _port = value;
        }
    }
}
```

---

## Common Patterns

### Feature Flags Configuration
```csharp
[Dictionary]
public class FeatureFlags
{
    public Dictionary<string, bool> Features { get; set; }
    
    public bool IsEnabled(string featureName)
        => Features.TryGetValue(featureName, out var enabled) && enabled;
}

// Usage
var flags = Configuration.GetSection<FeatureFlags>();
if (flags.IsEnabled("NewUI"))
{
    // Use new UI
}
```

### Multi-Environment Configuration
```csharp
public class EnvironmentSettings
{
    [ConfigName("Environment")]
    [DefaultValue("Development")]
    public string Environment { get; set; }
    
[ConfigName("IsProduction")]
    public bool IsProduction => Environment == "Production";
    
    [ConfigName("IsStaging")]
    public bool IsStaging => Environment == "Staging";
}
```

### Array of Complex Objects
```csharp
[Array("Server")]
public class ClusterConfiguration
{
    public List<ClusterNode> Nodes { get; set; }
}

public class ClusterNode
{
    [ConfigName("Address")]
    public string Address { get; set; }
    
    [ConfigName("Port")]
  [DefaultValue(9200)]
  public int Port { get; set; }
    
    [ConfigName("Role")]
    public string Role { get; set; }
}
```

---

## Extension Points

### Creating Custom Attributes

Create custom attributes for application-specific needs:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class CustomAttribute : Attribute
{
    public string CustomProperty { get; set; }
    
    public CustomAttribute(string customProperty)
    {
        CustomProperty = customProperty;
    }
}

// Usage
public class MySettings
{
    [Custom("MyValue")]
  public string MyProperty { get; set; }
}
```

---

## Testing

Validate attributes work correctly:

```csharp
[TestMethod]
public void ConfigName_AttributeExists_OnServerProperty()
{
    var attr = typeof(DatabaseSettings)
  .GetProperty("Server")
        .GetCustomAttribute<ConfigNameAttribute>();
    
    Assert.IsNotNull(attr);
    Assert.AreEqual("DatabaseServer", attr.ColumnName);
}
```

---

## Summary

The Attributes module provides:

**Key Strengths:**
- ? Declarative configuration
- ? Reduces boilerplate code
- ? Enables reflection-based discovery
- ? Flexible and extensible
- ? Well-organized attribute hierarchy

**Best For:**
- ORM and data mapping scenarios
- Configuration management
- CSV/Database import/export
- Metadata-driven applications

**Common Uses:**
- Configuring database schema from C# classes
- Mapping CSV files to business objects
- INI-based configuration with type safety
- Feature flag and settings management

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                               | Description             |
|------------------------------------------------------|-------------------------|
| **[CLI](../../CommandLine/readme.md)**               | Command-line parsing    |
| **[Configuration](../../Configuration/readme.md)**   | INI-based configuration |
| **[Core](../../Core/readme.md)**                     | Core utilities          |
| **[DataStructures](../../DataStructures/readme.md)** | Collections & utilities |
| **[JSON](../../Json/readme.md)**                     | Delta serialization     |
| **[Logging](../../Logging/readme.md)**               | Structured logging      |
| **[Mail](../../Mail/readme.md)**                     | Email processing        |
| **[Net](../../Net/readme.md)**                       | Network file transfers  |
| **[Security](../../Security/readme.md)**             | Encryption & security   |
| **[Utils](../../Utilities/readme.md)**               | General utilities       |


