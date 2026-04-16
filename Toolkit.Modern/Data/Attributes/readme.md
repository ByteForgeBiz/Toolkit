# Attributes Module

## Overview

The **Attributes** module defines custom attributes used throughout the ByteForge Toolkit for declarative data mapping. Two attributes live here and are applied to C# properties to control how they map to CSV columns and database columns respectively.

---

## Attributes

### `CSVColumnAttribute`

**Namespace:** `ByteForge.Toolkit.Data`
**Target:** Properties

Maps a C# property to a column in a CSV file. Used by `CSVReader`, `CSVWriter`, and `CSVRecord` to discover which properties participate in CSV serialisation and what column header name they correspond to.

| Constructor | Signature | Description |
|-------------|-----------|-------------|
| Default | `CSVColumnAttribute()` | No column name or index. Property name is used as fallback. |
| By name | `CSVColumnAttribute(string name)` | Maps property to a column with the given header name |
| By index and name | `CSVColumnAttribute(int index, string? name = null)` | Maps property to a zero-based column position, with optional name |

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | Header name of the CSV column |
| `Index` | `int` | Zero-based column position (`-1` = not set) |

When both `Index` and `Name` are provided, the writer orders columns by `Index` and uses `Name` as the header. During reading, mapping is by name when headers are present.

```csharp
public class PersonRecord : CSVRecord
{
    [CSVColumn(0, "First Name")]
    public string FirstName { get; set; }

    [CSVColumn(1, "Last Name")]
    public string LastName { get; set; }

    [CSVColumn(2, "Birth Date")]
    public DateTime BirthDate { get; set; }

    // No attribute — excluded from CSV mapping
    public string InternalNote { get; set; }

    public override void Validate()
    {
        if (string.IsNullOrEmpty(FirstName))
            ValidationErrors.Add("First Name", "Required", FirstName);
    }
}
```

---

### `DBColumnAttribute`

**Namespace:** `ByteForge.Toolkit.Data`
**Target:** Properties

Maps a C# property to a database column. Used by `TypeConverter` (DataRow-to-object mapping) and `BulkDbProcessor<T>` (table creation, bulk copy, upsert, delete).

| Constructor | Signature | Description |
|-------------|-----------|-------------|
| By name | `DBColumnAttribute(string name)` | Maps to the named column; no PK, index, or unique constraint |
| By PK flag | `DBColumnAttribute(bool isPrimaryKey)` | PK flag only; column name defaults to the property name |
| Name + PK | `DBColumnAttribute(string name, bool isPrimaryKey)` | Named column with PK flag |
| Full | `DBColumnAttribute(string? name, bool isPrimaryKey, bool hasIndex, bool isUnique)` | All structural options |

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string?` | `null` | DB column name. If `null`, the property name is used |
| `DbType` | `DbType?` | `null` | ADO.NET `DbType` for parameter creation |
| `IsPrimaryKey` | `bool` | `false` | Marks the column as part of the primary key |
| `IsIdentity` | `bool` | `false` | Auto-increment / IDENTITY column (excluded from upsert matching) |
| `IdentitySeed` | `long` | `1` | IDENTITY seed value |
| `IdentityStep` | `long` | `1` | IDENTITY increment step |
| `HasIndex` | `bool` | `false` | Create an index on this column (auto-true when PK or unique) |
| `IsUnique` | `bool` | `false` | Create a unique index on this column |
| `IsNullable` | `bool` | `true` | Column allows NULL |
| `MaxLength` | `int` | `0` | Max string length (0 = use default, typically 255 or `nvarchar(max)`) |
| `ConverterName` | `string?` | `null` | Name of a registered `ValueConverterRegistry` function applied during DataRow mapping |
| `Converter` | `Func<object,object>?` | — | Resolved converter function (read-only; resolved via `ConverterName`) |

```csharp
public class OrderEntity
{
    [DBColumn("OrderId", isPrimaryKey: true, isIdentity: true)]
    public int Id { get; set; }

    [DBColumn("CustomerId", isPrimaryKey: false, hasIndex: true, isUnique: false)]
    public int CustomerId { get; set; }

    [DBColumn("Status", isNullable: false, maxLength: 20)]
    public string Status { get; set; }

    [DBColumn("TotalAmount")]
    public decimal Total { get; set; }

    [DBColumn("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    // No attribute — ignored by BulkDbProcessor and TypeConverter
    public string ComputedLabel { get; set; }
}
```

#### Using a custom converter

```csharp
// Register a converter once at startup
ValueConverterRegistry.Register("UpperCase", v => v?.ToString()?.ToUpperInvariant() ?? "");

// Apply it via the attribute
public class ProductRecord
{
    [DBColumn("SKU", ConverterName = "UpperCase")]
    public string Sku { get; set; }
}
```

---

## How the Attributes Are Used

| Consumer | Attribute | How |
|----------|-----------|-----|
| `CSVReader` | `CSVColumnAttribute` | Discovers mapped properties; fills values by column name |
| `CSVWriter.WriteRecords<T>` | `CSVColumnAttribute` | Orders columns by `Index`, uses `Name` as header |
| `CSVRecord` constructor | `CSVColumnAttribute` | Builds property→column map; converts and assigns values |
| `TypeConverter.ConvertDataRowTo<T>` | `DBColumnAttribute` | Maps `DataRow` column names (or property names) to properties; applies `Converter` |
| `BulkDbProcessor<T>` | `DBColumnAttribute` | Determines table schema, PKs, indexes, identity columns, and column order |

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[Data](../readme.md)** | Data module overview |
| **[CSV](../CSV/readme.md)** | CSVReader, CSVWriter, CSVRecord |
| **[Database](../Database/readme.md)** | DBAccess, BulkDbProcessor, TypeConverter |
| **[Exceptions](../Exceptions/readme.md)** | ConversionException and others |
| **[Configuration](../../Configuration/readme.md)** | INI-based configuration attributes |
| **[Utilities](../../Utilities/readme.md)** | ValueConverterRegistry |
