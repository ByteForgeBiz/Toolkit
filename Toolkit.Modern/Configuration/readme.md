# Configuration Module

## Overview

The **Configuration** module provides INI-file-based configuration management with strongly-typed section binding, transparent encryption/decryption, array and dictionary support, and thread-safe read/write access. It is built on top of `Microsoft.Extensions.Configuration`.

---

## Purpose

The module addresses the following requirements:

- Load application settings from a standard INI file without writing boilerplate parsing code.
- Map INI sections to plain C# classes automatically (attributes control the mapping).
- Store and retrieve collections (arrays, dictionaries) in dedicated INI sections.
- Transparently encrypt sensitive values (passwords, API keys) at rest.
- Support both static (singleton) and instance access patterns.
- Coordinate concurrent readers and exclusive writers using a `ReaderWriterLockSlim`.

---

## Architecture

```
INI file on disk
    |
    v  (Microsoft.Extensions.Configuration.IniFile provider)
IConfigurationRoot
    |
    +---> ConfigSection<T>  (one per registered section + type)
    |         loads/saves typed POCO
    |
    v
Configuration (static facade)  -----> IConfigurationManager (instance interface)
```

The `Configuration` class exposes a static API backed by a default `IConfigurationManager` instance. All static methods delegate to that instance, which can be replaced in tests by working with `IConfigurationManager` directly.

---

## Quick Start

```csharp
// 1. Define a typed section
public class DatabaseConfig
{
    public string Server { get; set; }
    public string DatabaseName { get; set; }

    [Encrypted]
    public string Password { get; set; }

    [DefaultValue(1433)]
    public int Port { get; set; }
}

// 2. Initialize (call once at startup)
Configuration.Initialize("appsettings.ini");

// 3. Read a section
var db = Configuration.GetSection<DatabaseConfig>("Database");
Console.WriteLine($"Connecting to {db.Server}:{db.Port}/{db.DatabaseName}");

// 4. Persist changes
db.Password = "new-password";
Configuration.Save();
```

Matching INI file:

```ini
[Database]
Server=PRODSERVER\INST1
DatabaseName=MyApp
Password=[encrypted_value]
Port=1433
```

---

## Key Types

### `Configuration`

The public static facade. All methods delegate to an internal `IConfigurationManager` instance.

| Member | Description |
|---|---|
| `Initialize(string path)` | Load the INI file at the given full path. |
| `Initialize(string directory, string fileName)` | Load by directory + file name. |
| `Initialize()` | Auto-detect: `{ExeName}.ini` beside the entry assembly (or `local.ini` for web apps). |
| `IsInitialized` | `true` once initialised. Initialization may only occur once per instance. |
| `GetSection<T>(string sectionName)` | Return the strongly-typed POCO bound to the named section. Creates the section on first call. |
| `AddSection<T>(string sectionName)` | Explicitly register a section (throws if already registered). |
| `GetString(section, key, defaultValue)` | Raw string read (with optional decryption). |
| `SetString(section, key, value)` | In-memory write (with optional encryption). Reloads affected typed sections. |
| `GetInt(section, key, defaultValue)` | Integer read. |
| `GetBool(section, key, defaultValue)` | Boolean read (uses `BooleanParser` for flexible true/false values). |
| `GetValue<T>(section, key, defaultValue)` | Generic read with type conversion. |
| `RegisterEncrypted(section, key)` | Mark a key as encrypted so `GetString`/`SetString` auto-decrypt/encrypt. |
| `RegisterEncrypted(string sectionKey)` | Mark using `"Section:Key"` format. |
| `GetSectionNames()` | List all section names, excluding internal array/dictionary sections. |
| `GetSectionValues(string sectionName)` | All key-value pairs in a section as raw strings. |
| `Save()` | Write the current in-memory state back to the INI file, preserving comments and structure. |
| `Globalization` | The `GlobalizationInfo` section (loaded from the `[Globalization]` section). |

---

## Attributes

See [`Attributes/readme.md`](Attributes/readme.md) for a complete reference. Summary:

| Attribute | Purpose |
|---|---|
| `[ConfigName("key")]` | Map a property to a different INI key name. |
| `[Array]` / `[Array("SectionName")]` | Store/load a list property from a dedicated INI section. |
| `[Dictionary]` / `[Dictionary("SectionName")]` | Store/load a `Dictionary<string,string>` property from a dedicated INI section. |
| `[Encrypted]` | Auto-encrypt on save and auto-decrypt on load using `Encryptor.Default`. |
| `[DefaultValue(value)]` | Provide a compile-time default (from `System.ComponentModel`). |
| `[DefaultValueProvider(type, methodName)]` | Provide a runtime default via a static factory method. |
| `[DoNotPersist]` | Skip persistence for a property (loaded but never written back). |
| `[Ignore]` | Skip both loading and saving. |

---

## Array and Dictionary Storage

Arrays and dictionaries are stored in separate INI sections. The section name is derived from the property or can be set explicitly via the attribute.

```ini
; A property named "AllowedHosts" with [Array] gets its own section
[MySection]
AllowedHosts=MySection#AllowedHostsArray

[MySection#AllowedHostsArray]
00=web01.example.com
01=web02.example.com
02=web03.example.com

; A property named "Headers" with [Dictionary] gets its own section
[MySection]
Headers=MySection#HeadersDict

[MySection#HeadersDict]
Content-Type=application/json
Authorization=Bearer abc123
```

```csharp
public class MySection
{
    [Array]
    public string[] AllowedHosts { get; set; }

    [Dictionary]
    public Dictionary<string, string> Headers { get; set; }
}
```

---

## Encryption

Properties marked with `[Encrypted]` use `Encryptor.Default` (AES-based, from the Security module). Keys can also be registered programmatically without modifying the POCO:

```csharp
// Programmatic registration (does not require [Encrypted] attribute)
Configuration.RegisterEncrypted("Database", "Password");
// or
Configuration.RegisterEncrypted("Database:Password");
```

The `Configuration.Save()` method writes the encrypted ciphertext to the INI file. Plaintext is never persisted for encrypted properties.

---

## Globalization

The `[Globalization]` INI section is loaded automatically into a `GlobalizationInfo` instance accessible as `Configuration.Globalization`. It provides culture-aware format strings and `Format*` helper methods for dates, times, numbers, currencies, and percentages.

See [`Models/readme.md`](Models/readme.md) for the full property list.

---

## Thread Safety

The module uses a `ReaderWriterLockSlim` to coordinate access:

- Multiple threads can call `GetSection`, `GetString`, etc. concurrently (read lock).
- `SetString` and `Save` acquire a write lock, blocking readers until complete.
- `GetSection` uses a double-check lock pattern for efficient section creation.

---

## Related Modules

| Module | Description |
|---|---|
| **[CommandLine](../CommandLine/readme.md)** | Command-line parsing |
| **[Core](../Core/readme.md)** | WinSCP resource management |
| **[Security](../Security/readme.md)** | AES encryption used by `[Encrypted]` |
