# Test Models

Model classes used across the test suite. They live in the `ByteForge.Toolkit.Tests.Models` namespace and mirror the table schema of the `TestUnitDB` test database.

## Model Classes

### TestEntity

Maps to the `TestEntities` table. Used by SQL Server CRUD, transaction, and parameter-parsing tests.

| Property | DB Column | Type | Notes |
|----------|-----------|------|-------|
| `Id` | `Id` | `int` | Primary key, identity |
| `Name` | `Name` | `string` | Max 100 chars |
| `Description` | `Description` | `string` | Max 255 chars |
| `CreatedDate` | `CreatedDate` | `DateTime` | Defaults to `DateTime.Now` |
| `IsActive` | `IsActive` | `bool` | Defaults to `true` |
| `TestValue` | `TestValue` | `decimal` | |
| `TestGuid` | `TestGuid` | `Guid` | Auto-generated in constructor |

Factory methods: `TestEntity.Create(name, description)` and `TestEntity.Create(name, description, testValue, isActive)`.

### BulkTestEntity

Maps to the `BulkTestEntities` table. Designed for bulk-insert, upsert, and delete testing.

| Property | DB Column | Type | Notes |
|----------|-----------|------|-------|
| `Id` | `Id` | `Guid` | Primary key |
| `Code` | `Code` | `string` | Unique constraint, max 50 chars |
| `Name` | `Name` | `string?` | Max 200 chars |
| `Category` | `Category` | `string?` | Max 100 chars |
| `Value` | `Value` | `decimal` | |
| `Priority` | `Priority` | `int` | Defaults to 1 |
| `IsActive` | `IsActive` | `bool` | Defaults to `true` |
| `CreatedDate` | `CreatedDate` | `DateTime` | |
| `ModifiedDate` | `ModifiedDate` | `DateTime` | |

Factory methods: `BulkTestEntity.Create(code, name)`, `BulkTestEntity.Create(code, name, category, value, priority, isActive)`, and `BulkTestEntity.CreateBatch(count, prefix)` which generates an array of entities cycling through five categories.

Additional utility: `Touch()` refreshes `ModifiedDate`; `CreateUpdatedCopy(newName, newValue)` produces a copy sharing the same `Id` and `Code` for upsert testing.

### TestDataTypeEntity

Defined in `TestDataTypeEntity.cs`. Contains one property for each of the common .NET primitive types to exercise the database layer's type conversion:

`int`, `long`, `decimal`, `double`, `float`, `bool`, `DateTime`, `Guid`, `string`, `byte[]`, `TimeSpan`, and a test enum value.

### Configuration test models (`ConfigurationTestModels.cs`)

| Model | Purpose |
|-------|---------|
| `BasicTestConfig` | String, int, bool, double, DateTime properties; also tests `[DefaultValue]`, `[ConfigName]`, `[DoNotPersist]`, and `[Ignore]` attributes |
| `ArrayTestConfig` | `string[]`, `List<string>`, `List<int>`, `IList<string>`, and `IEnumerable<string>` properties, all decorated with `[Array]` |
| `DatabaseConfig` | Realistic connection-settings model with `[DefaultValue]`, `[ConfigName]`, `[DoNotPersist]`, and a computed `[Ignore]` property |

Configuration models use attribute-based control from `ByteForge.Toolkit.Configuration`:

| Attribute | Effect |
|-----------|--------|
| `[DefaultValue(value)]` | Supplies the default when the key is absent in the INI file |
| `[ConfigName("key")]` | Maps the property to a different key name in the INI section |
| `[DoNotPersist]` | Property is readable but never written back to the file |
| `[Ignore]` | Property is completely skipped during both read and write |
| `[Array]` | Marks a collection property; values are stored in a dedicated INI section |
| `[Array("SectionName")]` | Same as `[Array]` but with an explicit section name |

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Root** | Solution overview | [../../readme.md](../../readme.md) |
| **Tests** | Test suite overview | [../README.md](../README.md) |
| **Helpers** | Test helper classes | [../Helpers/README.md](../Helpers/README.md) |
| **Data/Database** | Database tests | [../Unit/Data/Database/readme.md](../Unit/Data/Database/readme.md) |
| **Configuration** | Configuration tests | [../Unit/Configuration/readme.md](../Unit/Configuration/readme.md) |
