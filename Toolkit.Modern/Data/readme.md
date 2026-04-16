# Data Module

## Overview

The **Data** module is the central data processing layer of ByteForge.Toolkit.Modern. It provides utilities for reading and writing CSV files, interacting with SQL Server and ODBC databases, detecting audio file formats, and handling data conversion errors. The module is designed around attribute-driven mapping, strong typing, and clean separation of concerns across focused sub-modules.

---

## Sub-modules

| Sub-module | Description |
|------------|-------------|
| **[Attributes](Attributes/readme.md)** | `CSVColumnAttribute` and `DBColumnAttribute` — declarative mapping of C# properties to CSV columns and database columns |
| **[Audio](Audio/readme.md)** | `AudioFormatDetector` — magic-byte detection of MP3, WAV, FLAC, OGG, M4A, WMA, and AIFF from raw byte arrays |
| **[CSV](CSV/readme.md)** | `CSVReader`, `CSVWriter`, `CSVFormat`, `CSVRecord` — high-performance, format-flexible CSV reading and writing with built-in validation |
| **[Database](Database/readme.md)** | `DBAccess`, `BulkDbProcessor<T>`, `TypeConverter`, `DatabaseOptions` — full database access layer with bulk operations, transactions, and script execution |
| **[Exceptions](Exceptions/readme.md)** | `ParamArgumentsMismatchException`, `ConversionException` — structured exceptions with machine-readable context |

---

## Key Types at a Glance

| Type | Sub-module | Purpose |
|------|------------|---------|
| `CSVReader` | CSV | Reads CSV files/streams row-by-row via a callback delegate |
| `CSVWriter` | CSV | Writes CSV rows from typed records or raw string arrays |
| `CSVFormat` | CSV | Delimiter, quoting, and header settings; auto-detectable from sample lines |
| `CSVRecord` | CSV | Abstract base for typed CSV row models with reflection-based mapping and validation |
| `ICSVRecord` | CSV | Interface contract: `IsValid()`, `Validate()`, `ValidationErrors` |
| `CSVColumnAttribute` | Attributes | Maps a property to a named/indexed CSV column |
| `DBAccess` | Database | Executes queries, retrieves scalars/rows/result-sets, manages transactions |
| `DatabaseOptions` | Database | Connection configuration (type, server, credentials, timeouts, retry) |
| `DatabaseRootOptions` | Database | Points to the active `DatabaseOptions` section by name |
| `BulkDbProcessor<T>` | Database | High-throughput bulk insert, upsert, and delete via `SqlBulkCopy` |
| `TypeConverter` | Database | Maps `DataRow` values to typed properties; supports custom converters |
| `ScriptExecutionResult` | Database | Captures result sets, row counts, and exceptions from multi-batch SQL scripts |
| `DBTransaction` | Database | `TransactionScope` wrapper with auto-commit and nested-transaction support |
| `DBColumnAttribute` | Attributes | Maps a property to a database column; controls PK, identity, index, and nullability |
| `AudioFormatDetector` | Audio | Detects audio format from byte-array magic bytes; returns extension and MIME type |
| `ParamArgumentsMismatchException` | Exceptions | Thrown when SQL parameter count does not match placeholder count |
| `ConversionException` | Exceptions | Thrown when a CSV string value cannot be converted to the target property type |

---

## Architecture

### Attribute-driven mapping

Both the CSV and database layers share the same attribute model. Decorating a class with `[CSVColumn]` or `[DBColumn]` is all that is needed to opt a property into the mapping pipeline.

```csharp
public class OrderRecord : CSVRecord
{
    [CSVColumn(0, "OrderId")]  public int    OrderId  { get; set; }
    [CSVColumn(1, "Amount")]   public decimal Amount  { get; set; }

    public override void Validate()
    {
        if (Amount <= 0)
            ValidationErrors.Add("Amount", "Must be positive", Amount.ToString());
    }
}

public class OrderEntity
{
    [DBColumn("OrderId", isPrimaryKey: true)]
    public int OrderId { get; set; }

    [DBColumn("Amount")]
    public decimal Amount { get; set; }
}
```

### Layered data flow

```
Configuration (INI)
        |
        v
  DatabaseOptions
        |
        v
    DBAccess  ──── TypeConverter ──── DBColumnAttribute
        |
        v
  BulkDbProcessor<T>

  CSVReader/CSVWriter ──── CSVFormat (auto-detect)
        |
        v
   CSVRecord ──── CSVColumnAttribute ──── ValidationErrors
```

---

## Quick-start Examples

### Reading a CSV file

```csharp
CSVReader.ReadFile("orders.csv", (row, status, line) =>
{
    if (status != CSVReader.CSVRowStatus.OK) return true;
    Console.WriteLine($"Order {row["OrderId"]}: {row["Amount"]}");
    return true;
});
```

### Writing a CSV file

```csharp
var records = new List<OrderRecord> { /* ... */ };

using (var writer = new CSVWriter("output.csv"))
    writer.WriteRecords(records);
```

### Database access

```csharp
var db = new DBAccess();  // reads [Data Source] → SelectedDB from Configuration

int count = db.GetValue<int>("SELECT COUNT(*) FROM Orders WHERE Active = 1");

OrderEntity[] orders = db.GetRecords<OrderEntity>(
    "SELECT * FROM Orders WHERE CustomerId = ?", customerId);

bool ok = db.ExecuteQuery("UPDATE Orders SET Status = ? WHERE Id = ?", "Shipped", orderId);
```

### Bulk insert

```csharp
var processor = new BulkDbProcessor<OrderEntity>("Orders")
{
    DropDestinationTableIfExists = false
};

await processor.BulkInsertAsync(db, newOrders, CancellationToken.None);
```

### Detect audio format

```csharp
byte[] blob = /* ... from DB or stream ... */;
var format = AudioFormatDetector.DetectFormat(blob);
string ext  = AudioFormatDetector.GetFileExtension(format);  // e.g. ".mp3"
string mime = AudioFormatDetector.GetMimeType(format);        // e.g. "audio/mpeg"
```

---

## 📖 Documentation Links

### Data Sub-modules

| Sub-module | Description |
|------------|-------------|
| **[Attributes](Attributes/readme.md)** | Data mapping attributes |
| **[Audio](Audio/readme.md)** | Audio format detection |
| **[CSV](CSV/readme.md)** | CSV file processing |
| **[Database](Database/readme.md)** | Database operations |
| **[Exceptions](Exceptions/readme.md)** | Custom exceptions |

### Related Modules

| Module | Description |
|--------|-------------|
| **[CLI](../CommandLine/readme.md)** | Command-line parsing |
| **[Configuration](../Configuration/readme.md)** | INI-based configuration |
| **[Core](../Core/readme.md)** | Core utilities |
| **[DataStructures](../DataStructures/readme.md)** | Collections and URL utilities |
| **[JSON](../Json/readme.md)** | Delta serialization |
| **[Logging](../Logging/readme.md)** | Structured logging |
| **[Mail](../Mail/readme.md)** | Email processing |
| **[Net](../Net/readme.md)** | Network file transfers |
| **[Security](../Security/readme.md)** | Encryption and security |
| **[Utils](../Utilities/readme.md)** | General utilities |
