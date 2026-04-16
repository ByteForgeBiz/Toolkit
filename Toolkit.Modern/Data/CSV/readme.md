# CSV Module

## Overview

The **CSV** module provides high-performance, format-flexible reading and writing of CSV files. It supports automatic format detection (delimiter, quoting style), multi-line quoted fields, row-level validation, and progress reporting. The module is built around a delegate/callback pattern that keeps memory usage low even for very large files.

---

## Key Types

| Type | Kind | Description |
|------|------|-------------|
| `CSVReader` | Class | Reads and parses a CSV file or stream, dispatching each row to a user-supplied delegate |
| `CSVWriter` | Class (IDisposable) | Writes CSV rows to a file, stream, or `TextWriter` |
| `CSVFormat` | Class | Describes delimiter, quoting, and header settings; includes auto-detection logic |
| `CSVRecord` | Abstract class | Base class for strongly-typed CSV row models with built-in validation |
| `ICSVRecord` | Interface | Contract for CSV record objects: `IsValid()`, `Validate()`, `ValidationErrors` |
| `ValidationErrors` | Class | Collection of `ValidationError` instances accumulated during validation |
| `ValidationError` | Class | Represents a single validation failure (field name, message, offending value) |
| `ConversionException` | Exception | Thrown when a string value cannot be converted to the target property type |
| `DataProcessingException` | Exception (internal) | Thrown internally when row processing encounters an unrecoverable error |

All types live in the `ByteForge.Toolkit.Data` namespace.

---

## CSVFormat

`CSVFormat` holds the parsing/writing configuration and can be auto-detected from sample lines.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Delimiter` | `char` | `','` | Field separator character |
| `QuoteChar` | `char?` | `'"'` | Quote character (`null` = no quoting) |
| `HasHeader` | `bool` | `true` | Whether the first row is a header |
| `HeaderQuoted` | `bool` | `true` | Whether header fields are quoted |
| `DataQuoted` | `bool` | `true` | Whether data fields are quoted |
| `TrimValues` | `bool` | `true` | Strip leading/trailing whitespace from values |

**Static factories:**

```csharp
CSVFormat.Default   // comma-delimited, double-quoted, with header

CSVFormat.DetectFormat(string[] sampleLines)  // infers all settings from sample rows
```

Auto-detection evaluates `,`, `|`, `\t`, and `;` as candidate delimiters, then checks whether fields are enclosed in `"` or `'`.

---

## CSVReader

`CSVReader` processes a file or stream line-by-line. Each row is delivered to the `RowHandler` delegate; returning `false` from the delegate stops processing early.

### Row handler delegate

```csharp
delegate bool CSVRowProcessorDelegate(
    IDictionary<string, string> row,   // column name → value (case-insensitive)
    CSVRowStatus status,               // OK | Malformed | EOF
    string rawLine);                   // the raw line as read
```

`CSVRowStatus.EOF` is signalled with an empty dictionary after the last line.

### Instance usage

```csharp
var reader = new CSVReader();

reader.Progress += (_, e) => Console.Write($"\r{e.Progress:F0}%");

reader.RowHandler = (row, status, line) =>
{
    if (status == CSVReader.CSVRowStatus.Malformed)
    {
        Console.Error.WriteLine($"Skipping malformed row: {line}");
        return true; // continue
    }
    if (status == CSVReader.CSVRowStatus.EOF)
        return true;

    Console.WriteLine(row["Name"]);
    return true;
};

reader.ReadFile("customers.csv");
```

### Static convenience overloads

```csharp
// One-liner: no instance required
CSVReader.ReadFile("data.csv", (row, status, line) => { /* ... */ return true; });

CSVReader.ReadStream(stream, (row, status, line) => { /* ... */ return true; });
```

### Format override

```csharp
// Force pipe-delimited, no header
var reader = new CSVReader
{
    Format = new CSVFormat { Delimiter = '|', HasHeader = false }
};
reader.RowHandler = (row, status, line) => { /* ... */ return true; };
reader.ReadFile("export.psv");
```

---

## CSVWriter

`CSVWriter` wraps a file path, stream, or `TextWriter` and writes rows with proper quoting and escaping. It implements `IDisposable` — wrap in a `using` block.

### Writing typed records (ICSVRecord)

```csharp
public class PersonRecord : CSVRecord
{
    [CSVColumn("Name")]  public string Name { get; set; }
    [CSVColumn("Age")]   public int    Age  { get; set; }

    public override void Validate()
    {
        if (string.IsNullOrEmpty(Name))
            ValidationErrors.Add("Name", "Required", Name);
        if (Age < 0 || Age > 150)
            ValidationErrors.Add("Age", "Out of range", Age.ToString());
    }
}

var people = new List<PersonRecord>
{
    new PersonRecord { Name = "Alice", Age = 30 },
    new PersonRecord { Name = "Bob",   Age = 25 },
};

using (var writer = new CSVWriter("output.csv"))
{
    writer.Progress += (_, e) => Console.Write($"\r{e.Progress:F0}%");
    writer.WriteRecords(people);
}
```

### Writing raw rows

```csharp
using (var writer = new CSVWriter("report.csv"))
{
    writer.WriteHeader(new[] { "Id", "Product", "Price" });
    writer.WriteRow(new[]   { "1",  "Widget",   "9.99"  });
    writer.WriteRow(new[]   { "2",  "Gadget",   "24.99" });
}
```

### Writing arbitrary objects

```csharp
using (var writer = new CSVWriter(outputStream))
    writer.WriteObjects(myList, includeHeader: true);
```

---

## CSVRecord and ICSVRecord

`CSVRecord` is the recommended base class for typed CSV row models. It uses reflection over `[CSVColumn]`-decorated properties and handles string-to-type conversion automatically.

```csharp
public class OrderRecord : CSVRecord
{
    [CSVColumn(0, "OrderId")]   public int      OrderId   { get; set; }
    [CSVColumn(1, "Date")]      public DateTime Date      { get; set; }
    [CSVColumn(2, "Amount")]    public decimal  Amount    { get; set; }
    [CSVColumn(3, "Customer")]  public string   Customer  { get; set; }

    public override void Validate()
    {
        if (Amount <= 0)
            ValidationErrors.Add("Amount", "Must be positive", Amount.ToString());
        if (string.IsNullOrEmpty(Customer))
            ValidationErrors.Add("Customer", "Required", Customer);
    }
}
```

`CSVRecord` constructors:

| Constructor | Purpose |
|-------------|---------|
| `CSVRecord()` | Default, sets up column mapping |
| `CSVRecord(IDictionary<string, object>)` | Populates from object values |
| `CSVRecord(IDictionary<string, string>)` | Populates and converts strings to target types; throws `ConversionException` on failure |

---

## Validation

```csharp
var record = new OrderRecord(row);  // from IDictionary<string, string>

if (!record.IsValid())
{
    foreach (var error in record.ValidationErrors)
        Console.WriteLine(error);  // "Amount: Must be positive (0)"
}
```

---

## Exception Types

| Exception | When thrown |
|-----------|-------------|
| `ConversionException` | String value cannot be converted to a property's target type during `CSVRecord` construction. Includes `ColumnName` and `Value` in `Data`. |
| `DataProcessingException` | Internal: unrecoverable error during row processing in `CSVReader`. Includes `Format` and `IsFormatDetected` in `Data`. |

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[Data](../readme.md)** | Data module overview |
| **[Attributes](../Attributes/readme.md)** | `CSVColumnAttribute` reference |
| **[Database](../Database/readme.md)** | Database operations |
| **[Audio](../Audio/readme.md)** | Audio format detection |
| **[Exceptions](../Exceptions/readme.md)** | Custom exceptions |
