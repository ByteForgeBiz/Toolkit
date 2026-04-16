# Configuration / Interfaces

This folder contains the interfaces that define the contracts for the configuration system.

---

## Interfaces

### `IConfigurationManager` (public)

The full contract for a configuration manager instance. Implemented by the `Configuration` class. Expose this interface in tests or when injecting configuration as a dependency.

| Member | Description |
|---|---|
| `IsInitialized` | `true` once `Initialize` has been called. |
| `Globalization` | The `GlobalizationInfo` bound to the `[Globalization]` INI section. |
| `Initialize(string path)` | Load from a full file path. |
| `Initialize(string directory, string fileName)` | Load by directory + file name. |
| `Initialize()` | Auto-detect the INI file name from the entry assembly. |
| `AddSection<T>(string sectionName)` | Register a new typed section; throws if already registered. |
| `GetSection<T>(string sectionName)` | Get (or lazily create) a typed section. |
| `GetString(section, key, defaultValue)` | Read a raw string value (decrypting if registered). |
| `SetString(section, key, value)` | Write a string value in memory (encrypting if registered). |
| `GetInt(section, key, defaultValue)` | Read and parse an integer value. |
| `GetBool(section, key, defaultValue)` | Read and parse a boolean value. |
| `GetValue<T>(section, key, defaultValue)` | Read and convert to any supported type. |
| `RegisterEncrypted(section, key)` | Mark a key for automatic encryption/decryption. |
| `RegisterEncrypted(sectionKey)` | Mark using `"Section:Key"` format. |
| `GetSectionNames()` | Enumerate all user-visible section names (excluding internal array/dictionary sections). |
| `GetSectionValues(string sectionName)` | Return all key-value pairs in a section as raw strings. |
| `Save()` | Persist the current in-memory state to the INI file. |

---

### `IConfigSection` (internal)

Non-generic base interface for all typed configuration section wrappers. Used internally by `Configuration` to trigger load and save operations without knowing the concrete type parameter.

| Member | Description |
|---|---|
| `Value` | The wrapped configuration data as `object`. |
| `LoadFromConfiguration()` | Re-read values from the `IConfigurationRoot` into the POCO. |
| `SaveToConfiguration()` | Write the POCO's current property values back to the `IConfigurationRoot`. |

---

### `IConfigSection<T>` (internal, extends `IConfigSection`)

Strongly-typed variant of `IConfigSection`. Provides compile-time access to the POCO without casting.

| Member | Description |
|---|---|
| `Value` | The wrapped configuration data as `T` (hides the `object` version from `IConfigSection`). |

---

## Notes

- `IConfigSection` and `IConfigSection<T>` are `internal`. External code interacts with the configuration system only through `IConfigurationManager` or the `Configuration` static facade.
- `IConfigurationManager` is designed to enable unit testing: inject it as a constructor dependency and supply a test double in tests.
