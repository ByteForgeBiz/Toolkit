# Database Module

## Overview

The **Database** module is the primary data-access layer for ByteForge.Toolkit. It supports SQL Server and ODBC databases, provides parameterised queries, transactions, bulk operations, multi-batch script execution, and attribute-driven ORM mapping. The module is split into focused partial classes for maintainability.

---

## Key Types

| Type | Description |
|------|-------------|
| `DBAccess` | Main database access class. Executes queries, retrieves scalar values, rows, and result sets |
| `DatabaseOptions` | Configuration model for a single database connection (type, server, credentials, timeouts, retry) |
| `DatabaseRootOptions` | Selects which named `DatabaseOptions` section is active |
| `BulkDbProcessor<T>` | Generic bulk insert, upsert, and delete using `SqlBulkCopy` |
| `TypeConverter` | Converts `DataRow` columns to typed object properties via reflection and `DBColumnAttribute` |
| `ScriptExecutionResult` | Holds the outcome of a multi-batch SQL script execution |
| `DBTransaction` | Wraps a `TransactionScope` for use with `DBAccess`; supports auto-commit and nested transactions |

All types live in the `ByteForge.Toolkit.Data` namespace.

---

## DBAccess

`DBAccess` is a partial class composed of seven files:

| File | Responsibility |
|------|----------------|
| `DBAccess.Core.cs` | Constructor, enums (`DataBaseType`, `SqlErrorNumber`), core `ExecuteCommand<T>`, retry logic |
| `DBAccess.Methods.cs` | `GetValue`, `GetRecord`, `GetRecords`, `ExecuteQuery` — synchronous and async variants |
| `DBAccess.Factory.cs` | Creates `IDbConnection`, `IDbCommand`, and data-adapter instances by database type |
| `DBAccess.Parameters.cs` | Converts positional `object?[]` arguments into typed `IDbDataParameter` objects |
| `DBAccess.Transactions.cs` | `BeginTransaction`, `CommitTransaction`, `RollbackTransaction`, transaction stack |
| `DBAccess.ScriptExecution.cs` | `ExecuteScript` — parses GO-separated batches, captures multiple result sets |
| `DBAccess.Logging.cs` | Logs query timing, errors, and parameter values |
| `DBAccess.Properties.cs` | Additional properties exposed on the class |

### Constructors

```csharp
// Reads [Data Source] → SelectedDB from the active Configuration
new DBAccess()

// Reads a named section from the active Configuration
new DBAccess("Production")

// Constructed directly from options
new DBAccess(new DatabaseOptions { ... })
```

### Data retrieval

```csharp
var db = new DBAccess();

// Scalar value
int count = db.GetValue<int>("SELECT COUNT(*) FROM Orders WHERE Active = 1");

// Single typed row
var order = db.GetRecord<Order>("SELECT * FROM Orders WHERE Id = ?", orderId);

// Multiple typed rows
Order[] orders = db.GetRecords<Order>("SELECT * FROM Orders WHERE CustomerId = ?", customerId);

// Raw DataRowCollection
DataRowCollection rows = db.GetRecords("SELECT Id, Name FROM Products");
```

All retrieval methods have async variants (`GetValueAsync`, `GetRecordAsync`, `GetRecordsAsync`).

### Executing queries

```csharp
bool ok = db.ExecuteQuery("UPDATE Orders SET Status = ? WHERE Id = ?", "Shipped", orderId);

if (!ok)
    Console.WriteLine(db.LastException?.Message);

Console.WriteLine($"Rows affected: {db.RecordsAffected}");
```

### Connection testing

```csharp
bool alive = db.TestConnection();
bool alive = await db.TestConnectionAsync();
```

### Retry policy

`DBAccess` can automatically retry transient errors (command timeout, transport error). Configure via `DatabaseOptions`:

```csharp
var options = new DatabaseOptions
{
    RetryEnabled     = true,
    RetryMaxAttempts = 3,      // 1 attempt + 2 retries
    RetryDelayMs     = 500,    // exponential: 500ms, 1000ms, …
};
```

---

## Transactions

`DBTransaction` wraps `TransactionScope` and integrates with `DBAccess`'s transaction stack.

```csharp
using var tx = db.BeginTransaction(autoCommit: true);
try
{
    db.ExecuteQuery("INSERT INTO Log (Msg) VALUES (?)", "Start");
    db.ExecuteQuery("UPDATE Stock SET Qty = Qty - ? WHERE ProductId = ?", qty, productId);
    tx.Commit();   // explicit commit; autoCommit also commits on Dispose if no exception
}
catch
{
    tx.Rollback(); // discards all changes, clears the entire transaction stack
    throw;
}
```

`DBTransaction` properties:

| Property | Description |
|----------|-------------|
| `Id` | Short GUID string for tracing |
| `AutoCommit` | When `true`, commits automatically on `Dispose` if no exception has been recorded |

---

## DatabaseOptions

`DatabaseOptions` is the strongly-typed configuration model populated from an INI section via the Configuration module.

| INI key | Property | Default | Description |
|---------|----------|---------|-------------|
| `sType` | `DatabaseType` | — | `SQLServer` or `ODBC` |
| `sServer` | `Server` | `""` | Server name or IP |
| `sServerDSN` | `ServerDSN` | `""` | ODBC DSN name |
| `sDatabaseName` | `DatabaseName` | `""` | Database name |
| `esUser` | `EncryptedUser` | `""` | AES-encrypted username |
| `esPass` | `EncryptedPassword` | `""` | AES-encrypted password |
| `sConnectionString` | `ConnectionString` | `""` | Overrides all other settings if set |
| `bEncrypt` | `UseEncryption` | `false` | TLS/SSL connection encryption |
| `iConnectionTimeout` | `ConnectionTimeout` | `10` | Seconds before connection times out |
| `iCommandTimeout` | `CommandTimeout` | `240` | Seconds before a command times out |
| `bTrustedConnection` | `UseTrustedConnection` | `false` | Use Windows Authentication |
| `bAutoTrimStrings` | `AutoTrimStrings` | `true` | Trim string values read from DB |
| `bAllowNullStrings` | `AllowNullStrings` | `false` | Allow null strings (vs. empty string) |
| `bRetryEnabled` | `RetryEnabled` | `false` | Enable transient-error retry |
| `iRetryMaxAttempts` | `RetryMaxAttempts` | `3` | Max total attempts (including first) |
| `iRetryDelayMs` | `RetryDelayMs` | `1000` | Base delay for exponential back-off |

`User` and `Password` properties decrypt `EncryptedUser` / `EncryptedPassword` on access using `Encryptor.Default`.

#### INI example

```ini
[Data Source]
SelectedDB=Production

[Production]
sType=SQLServer
sServer=PRODSERVER\INSTANCE1
sDatabaseName=MyApplication
esUser=[encrypted]
esPass=[encrypted]
iConnectionTimeout=15
iCommandTimeout=300
bRetryEnabled=true
iRetryMaxAttempts=3
```

---

## BulkDbProcessor\<T\>

`BulkDbProcessor<T>` provides high-throughput insert, upsert, and delete operations for SQL Server using `SqlBulkCopy`. Entity properties must be decorated with `[DBColumn]`.

### Bulk insert

```csharp
public class ProductRecord
{
    [DBColumn("ProductId", isPrimaryKey: true, isIdentity: true)]
    public int Id { get; set; }

    [DBColumn("Name")]
    public string Name { get; set; }

    [DBColumn("Price")]
    public decimal Price { get; set; }
}

var processor = new BulkDbProcessor<ProductRecord>("Products")
{
    DropDestinationTableIfExists = true,   // drop & recreate table
    BulkCopyTimeout = 300
};

bool ok = processor.BulkInsert(db, records);
// or
bool ok = await processor.BulkInsertAsync(db, records, cancellationToken);
```

### Bulk upsert

Requires at least one non-identity primary key **or** unique index on the entity. Uses a temporary staging table + MERGE statement.

```csharp
bool ok = await processor.BulkUpsertAsync(db, records, cancellationToken);
```

### Bulk delete

Matches records by primary key or unique index columns and deletes them.

```csharp
bool ok = processor.BulkDelete(db, recordsToDelete);
```

### Configuration properties

| Property | Default | Description |
|----------|---------|-------------|
| `DestinationTableName` | *(ctor arg)* | Target table |
| `CreateDestinationTable` | `true` | Create table if absent |
| `DropDestinationTableIfExists` | `true` | Drop and recreate the table |
| `BatchSize` | `1000` | Records per `SqlBulkCopy` batch |
| `BulkCopyTimeout` | `600` | Seconds before bulk copy times out |
| `NullStringsAreEmpty` | `false` | Treat null strings as empty |
| `LastException` | — | Last exception from a failed operation |

---

## TypeConverter

`TypeConverter` converts `DataRow` values to typed object properties, caching the property→column mapping per type for performance.

```csharp
// Convert a DataRow to a typed object
var product = TypeConverter.ConvertDataRowTo<Product>(row, allowNullStrings: false);

// Populate an existing object
TypeConverter.PopulateObjectFromDataRow(row, existingProduct);

// Generic scalar conversion
decimal price = TypeConverter.ConvertTo<decimal>(rawValue);
object value  = TypeConverter.ConvertTo(typeof(DateTime), rawValue);
```

Handles nullable types, enums, `DateTime` (string parse + OA date), `Guid`, `TimeSpan`, and culture-invariant numeric strings. Falls back to `Convert.ChangeType` for all other types.

---

## ScriptExecutionResult

Returned by `DBAccess.ExecuteScript`, this object captures the full outcome of a multi-batch SQL script.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | `true` if all batches completed without exceptions |
| `BatchResults` | `List<object?>` | Raw return value from each batch |
| `ResultSets` | `List<DataTable>` | `DataTable` per `SELECT` batch |
| `RecordsAffected` | `List<int>` | Rows affected per batch |
| `LastException` | `Exception?` | Exception that caused a failure, or `null` |

```csharp
var result = db.ExecuteScript(@"
    SELECT * FROM Customers;
    GO
    UPDATE Products SET Price = Price * 1.1;
    GO
");

if (result.Success)
{
    var customers = result.ResultSets[0];  // first SELECT
    Console.WriteLine($"Products updated: {result.RecordsAffected[1]}");
}
else
{
    Console.WriteLine(result.LastException?.Message);
}
```

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[Data](../readme.md)** | Data module overview |
| **[Attributes](../Attributes/readme.md)** | `DBColumnAttribute` reference |
| **[CSV](../CSV/readme.md)** | CSV file processing |
| **[Exceptions](../Exceptions/readme.md)** | Custom exceptions |
| **[Configuration](../../Configuration/readme.md)** | INI-based configuration |
| **[Security](../../Security/readme.md)** | Encryption for credentials |
| **[Logging](../../Logging/readme.md)** | Structured logging |
