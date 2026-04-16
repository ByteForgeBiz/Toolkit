# Exceptions Module

## Overview

The **Exceptions** module defines custom exception types used across the Data module. These exceptions carry structured context (column name, value, query) so that callers and logging infrastructure can produce actionable error messages without inspecting raw exception messages.

---

## Exception Types

### `ParamArgumentsMismatchException`

**Namespace:** `ByteForge.Toolkit.Data`

Thrown when the number of SQL parameters supplied to a `DBAccess` method does not match the number of placeholders in the query string.

| Constructor | Description |
|-------------|-------------|
| `ParamArgumentsMismatchException()` | Default |
| `ParamArgumentsMismatchException(string message)` | With message |
| `ParamArgumentsMismatchException(string message, Exception inner)` | With message and inner exception |

**When you see this exception:** Check that every `?` or `@param` placeholder in your query has a corresponding argument in the `arguments` array passed to `ExecuteQuery`, `GetValue`, `GetRecord`, or `GetRecords`.

```csharp
// Correct — one placeholder, one argument
db.ExecuteQuery("DELETE FROM Orders WHERE Id = ?", orderId);

// Incorrect — two placeholders, one argument → ParamArgumentsMismatchException
db.ExecuteQuery("UPDATE Orders SET Status = ? WHERE Id = ?", "Shipped");
```

---

### `ConversionException`

**Namespace:** `ByteForge.Toolkit.Data`
**Defined in:** `Data/CSV/ConversionException.cs`

Thrown by `CSVRecord` constructors when a string value from a CSV row cannot be converted to the target property type.

| Constructor | Description |
|-------------|-------------|
| `ConversionException(string message)` | Basic |
| `ConversionException(string message, Exception inner)` | With inner exception |
| `ConversionException(string message, string columnName, string value)` | Adds `ColumnName` and `Value` to `Data` dictionary |
| `ConversionException(string message, string columnName, string value, Exception inner)` | Full context |

The structured constructors populate `Exception.Data`:

| Key | Value |
|-----|-------|
| `"ColumnName"` | Name of the CSV column where conversion failed |
| `"Value"` | The raw string that could not be converted |

```csharp
try
{
    var record = new OrderRecord(csvRow);
}
catch (ConversionException ex)
{
    Console.WriteLine($"Column:   {ex.Data["ColumnName"]}");
    Console.WriteLine($"Value:    {ex.Data["Value"]}");
    Console.WriteLine($"Message:  {ex.Message}");
}
```

---

### `DataProcessingException`

**Namespace:** `ByteForge.Toolkit.Data`
**Defined in:** `Data/CSV/DataProcessingException.cs`
**Visibility:** `internal`

An internal exception thrown by `CSVReader` when an unrecoverable error occurs while processing a data row. Consumers of `CSVReader` do not need to catch this type directly; it is logged and re-thrown as the cause of errors visible through the `LastException` pattern or standard exception propagation.

---

## Design Principles

- **Structured context:** Exceptions carry machine-readable data in `Exception.Data` rather than burying context inside free-form message strings.
- **Standard hierarchy:** All exceptions extend `System.Exception` so they integrate naturally with existing catch blocks and logging.
- **Serialisability:** `ParamArgumentsMismatchException` is marked `[Serializable]` for cross-AppDomain compatibility.

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[Data](../readme.md)** | Data module overview |
| **[CSV](../CSV/readme.md)** | Where `ConversionException` originates |
| **[Database](../Database/readme.md)** | Where `ParamArgumentsMismatchException` originates |
| **[Attributes](../Attributes/readme.md)** | Data mapping attributes |
