# Configuration Unit Tests

Tests for `ByteForge.Toolkit.Configuration`. The module provides an INI-based configuration system with typed section mapping, array support, dictionary support, globalization, migration, and thread safety.

**Source module:** `Toolkit.Modern/Configuration/`

## Test Classes

| Class | Description |
|-------|-------------|
| `ConfigurationCoreTests` | Initialization, section registration, save/load round-trips |
| `ConfigSectionTests` | Property-to-key mapping, attribute control, default values |
| `ConfigurationArrayTests` | Array and collection persistence with `[Array]` attribute |
| `ConfigurationDictionaryTests` | Dictionary property support |
| `ConfigurationDictionaryEdgeCaseTests` | Edge cases and error paths for dictionary handling |
| `ConfigurationAdvancedTests` | Complex naming conventions, inheritance, real-world patterns |
| `ConfigurationEnhancementTests` | Enhanced features and recent additions |
| `ConfigurationMigrationTests` | Configuration file migration and version compatibility |
| `ConfigurationParserIntegrationTests` | Integration-level parsing with real INI content |
| `ConfigurationThreadSafetyTests` | Concurrent read/write safety |
| `ConfigurationErrorTests` | Error handling: file system errors, type conversion errors, missing sections |
| `GlobalizationTests` | Culture loading, date/number/currency formatting methods |

## Key Scenarios

### ConfigurationCoreTests

- Initialization from an explicit file path
- Prevention of double initialization
- Handling of invalid or missing file paths
- Section registration and retrieval
- Save and load round-trip accuracy
- Formatting preservation (comments, blank lines)

### ConfigSectionTests

Tests typed section models (`BasicTestConfig`, `DatabaseConfig`) decorated with control attributes:

| Attribute | Behavior Tested |
|-----------|----------------|
| `[DefaultValue(value)]` | Value is used when the INI key is absent |
| `[ConfigName("key")]` | Property maps to a different key name |
| `[DoNotPersist]` | Property is readable but skipped on save |
| `[Ignore]` | Property is excluded from both read and write |

Primitive types tested: `string`, `int`, `double`, `bool`, `DateTime`, `enum`.

### ConfigurationArrayTests

Tests the `[Array]` and `[Array("SectionName")]` attributes on these collection types: `string[]`, `List<string>`, `List<int>`, `IList<string>`, `IEnumerable<string>`.

Covers:
- Default section naming convention (property name)
- Custom section name via attribute parameter
- Persistence and clearing
- Null element handling

### GlobalizationTests

- `CultureInfo` loading and storage
- `FormatDate`, `FormatNumber`, and `FormatCurrency` methods
- Default culture behavior when none is configured
- Culture-aware parsing

## Test Data

Tests construct inline INI content and write it to temporary files via `TempFileHelper.CreateTempIniFile()`. No external INI files are required. A representative sample:

```ini
[TestSection]
StringValue=Test String
IntValue=42
BoolValue=true
DoubleValue=3.14159
DateValue=2024-01-15T10:30:00
ArrayValue=TestArray

[TestArray]
0=Item1
1=Item2
2=Item3

[DatabaseSettings]
Server=localhost
Port=1433
Username=testuser
Password=testpass
Timeout=30
UseSSL=true
```

## Helpers Used

- `ConfigurationTestHelper` — creates isolated `Configuration` instances from inline INI content
- `TestConfigurationHelper` — low-level INI file construction helpers
- `TempFileHelper` — manages temp files so they are cleaned up after each test

## Prerequisites

No external resources. All tests are self-contained and use temporary files.

## Running These Tests

```powershell
# All Configuration tests
dotnet test --filter "FullyQualifiedName~Unit.Configuration"

# A specific class
dotnet test --filter "FullyQualifiedName~ConfigurationCoreTests"
dotnet test --filter "FullyQualifiedName~GlobalizationTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
| **Models** | Configuration test models | [../../Models/README.md](../../Models/README.md) |
| **Configuration source** | Production module | [../../../Toolkit.Modern/Configuration/readme.md](../../../Toolkit.Modern/Configuration/readme.md) |
