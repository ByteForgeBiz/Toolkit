# ByteForge.Toolkit Database Access Layer

A comprehensive, secure, and high-performance database access layer designed for SQL Server 2000 and ODBC databases. Features enterprise-grade security with encrypted credentials, bulk operations, async support, and extensive logging capabilities.

## 🚀 Key Features

### Core Database Operations
- **Multi-Database Support**: SQL Server 2000 and ODBC with automatic connection factory
- **Parameterized Queries**: Built-in SQL injection protection with automatic parameter binding
- **Script Execution**: Advanced batch processing with GO statement parsing
- **Full Async Support**: Complete async/await pattern implementation for all operations
- **Type Conversion**: Intelligent type mapping between database and .NET types

### Bulk Operations
- **High-Performance Bulk Insert**: Efficient batch processing for large datasets
- **Bulk Upsert**: Update existing records, insert new ones in single operation
- **Bulk Delete**: Mass deletion operations using staging tables
- **Progress Tracking**: Real-time progress reporting for long-running operations
- **Automatic Table Creation**: Dynamic table generation based on entity mappings

### Security & Configuration
- **Encrypted Credentials**: Secure storage of database credentials using built-in encryption
- **Flexible Configuration**: INI-based configuration with multiple database support
- **Connection Security**: Optional TLS/SSL encryption for database connections
- **Trusted Connections**: Windows Authentication support

### Monitoring & Diagnostics
- **Comprehensive Logging**: Detailed execution logging with configurable verbosity
- **Error Tracking**: Rich exception handling with detailed error context
- **Performance Monitoring**: Automatic query execution timing
- **Batch Execution Tracking**: Progress monitoring for multi-batch operations

## 🏗️ Architecture

### Core Classes

#### `DBAccess` (Modular Design)
The main database access class is split into focused partial classes:

- **`DBAccess.Core`**: Primary configuration and connection management
- **`DBAccess.Factory`**: Database provider factory for SQL Server and ODBC
- **`DBAccess.Methods`**: Core query execution methods (sync and async)
- **`DBAccess.ScriptExecution`**: Advanced script processing with batch handling
- **`DBAccess.TypeConverter`**: Intelligent type conversion between database and .NET types
- **`DBAccess.Logging`**: Comprehensive logging and error tracking
- **`DBAccess.Parameters`**: Parameter binding and SQL injection prevention

#### `BulkDbProcessor<T>`
Generic bulk operations processor providing:
- Type-safe bulk insert, upsert, and delete operations
- Automatic table creation and schema management
- Progress reporting and error handling
- Support for primary keys, unique indexes, and identity columns

#### Supporting Classes
- **`DatabaseOptions`**: Configuration management with encrypted credential support
- **`DatabaseRootOptions`**: Multi-environment database configuration
- **`ScriptExecutionResult`**: Comprehensive script execution results and error tracking

## 📋 Usage Examples

### Basic Operations

```csharp
// Initialize database access
var db = new DBAccess("Production");

// Simple value retrieval
int userCount = db.GetValue<int>("SELECT COUNT(*) FROM Users WHERE Active = @active", new object[] { true });

// Record retrieval
var user = db.GetRecord<User>("SELECT * FROM Users WHERE UserId = @userId", new object[] { 123 });

// Multiple records
var activeUsers = db.GetRecords<User>("SELECT * FROM Users WHERE Active = @active", new object[] { true });

// Execute commands
bool success = db.ExecuteQuery("UPDATE Users SET LastLogin = @date WHERE UserId = @userId", 
    new object[] { DateTime.Now, 123 });
```

### Async Operations

```csharp
// Async record retrieval
var users = await db.GetRecordsAsync<User>("SELECT * FROM Users WHERE Department = @dept", new object[] { "IT" });

// Async command execution
bool result = await db.ExecuteQueryAsync("INSERT INTO AuditLog (Action, Timestamp) VALUES (@action, @time)", 
    new object[] { "UserLogin", DateTime.Now });
```

### Script Execution

```csharp
string deploymentScript = @"
    CREATE TABLE TempUsers (Id int, Name varchar(255))
    GO
    INSERT INTO TempUsers SELECT Id, Name FROM Users WHERE Active = 1
    GO
    SELECT COUNT(*) FROM TempUsers
    GO
    DROP TABLE TempUsers
";

var result = db.ExecuteScript(deploymentScript, captureResults: true);

if (result.Success)
{
    Console.WriteLine($"Executed {result.BatchResults.Count} batches");
    Console.WriteLine($"Records affected: {string.Join(", ", result.RecordsAffected)}");
    
    // Access result sets from SELECT statements
    foreach (var resultSet in result.ResultSets)
        Console.WriteLine($"Result set contains {resultSet.Rows.Count} rows");
}
else
{
    Console.WriteLine($"Script failed: {result.LastException.Message}");
}
```

### Bulk Operations

```csharp
// Define entity with database mappings
public class Product
{
    [DBColumn(IsPrimaryKey = true)]
    public int ProductId { get; set; }
    
    [DBColumn(MaxLength = 100)]
    public string Name { get; set; }
    
    [DBColumn]
    public decimal Price { get; set; }
    
    [DBColumn(IsUnique = true)]
    public string SKU { get; set; }
}

// Bulk insert
var processor = new BulkDbProcessor<Product>("Products");
var products = GetProductsFromSource(); // Your data source

processor.ProgressChanged += (progress) => Console.WriteLine($"Progress: {progress:F1}%");
processor.ErrorOccurred += (message, ex) => Console.WriteLine($"Error: {message} - {ex.Message}");

bool success = await processor.BulkInsertAsync(db, products, cancellationToken);

// Bulk upsert (update existing, insert new)
bool upsertSuccess = await processor.BulkUpsertAsync(db, products, cancellationToken);

// Bulk delete
bool deleteSuccess = await processor.BulkDeleteAsync(db, productsToDelete, cancellationToken);
```

## ⚙️ Configuration

### Database Configuration (INI Format)

```ini
[Data Source]
SelectedDB=Production

[Production]
sType=SQLServer
sServer=PRODSERVER\INSTANCE1
sDatabaseName=MyApplication
esUser=[encrypted_username]
esPass=[encrypted_password]
bEncrypt=true
bTrustedConnection=false
iConnectionTimeout=60
iCommandTimeout=240
bAutoTrimStrings=true

[Development]
sType=SQLServer
sServer=localhost
sDatabaseName=MyApp_Dev
esUser=[encrypted_dev_user]
esPass=[encrypted_dev_pass]
bEncrypt=false
bTrustedConnection=true

[ODBC_Example]
sType=ODBC
sServerDSN=MyODBCDSN
sServer=ODBCServer
sDatabaseName=LegacyDB
esUser=[encrypted_odbc_user]
esPass=[encrypted_odbc_pass]
```

### Configuration Properties

| Property               | Config Key           | Description                       | Default   |
|------------------------|----------------------|-----------------------------------|-----------|
| `DatabaseType`         | `sType`              | Database type (SQLServer/ODBC)    | -         |
| `Server`               | `sServer`            | Server name or IP address         | -         |
| `ServerDSN`            | `sServerDSN`         | ODBC DSN name                     | -         |
| `DatabaseName`         | `sDatabaseName`      | Database name                     | -         |
| `EncryptedUser`        | `esUser`             | Encrypted username                | -         |
| `EncryptedPassword`    | `esPass`             | Encrypted password                | -         |
| `ConnectionString`     | `sConnectionString`  | Direct connection string override | -         |
| `UseEncryption`        | `bEncrypt`           | Enable connection encryption      | false     |
| `ConnectionTimeout`    | `iConnectionTimeout` | Connection timeout (seconds)      | 60        |
| `CommandTimeout`       | `iCommandTimeout`    | Command timeout (seconds)         | 240       |
| `UseTrustedConnection` | `bTrustedConnection` | Use Windows Authentication        | false     |
| `AutoTrimStrings`      | `bAutoTrimStrings`   | Auto-trim string values           | true      |

## 🛡️ Security Features

### Credential Protection
- Database credentials stored encrypted in configuration
- Automatic encryption/decryption during access
- Thread-safe credential access with locking

### SQL Injection Prevention
- Automatic parameterization of all queries
- Parameter type inference and binding
- Regex-based parameter parsing with safety checks

### Connection Security
- Optional TLS/SSL encryption for database connections
- Support for Windows Authentication (trusted connections)
- Secure connection string generation

## 🔍 Entity Mapping

Use `DBColumnAttribute` to map entity properties to database columns:

```csharp
public class Customer
{
    [DBColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int CustomerId { get; set; }
    
    [DBColumn(Name = "customer_name", MaxLength = 255)]
    public string Name { get; set; }
    
    [DBColumn(IsUnique = true)]
    public string Email { get; set; }
    
    [DBColumn(HasIndex = true)]
    public string City { get; set; }
    
    [DBColumn(DbType = DbType.DateTime)]
    public DateTime CreatedDate { get; set; }
    
    // Properties without DBColumnAttribute are ignored
    public string TemporaryField { get; set; }
}
```

### Attribute Properties
- `Name`: Database column name (defaults to property name)
- `IsPrimaryKey`: Mark as primary key column
- `IsIdentity`: Auto-increment identity column
- `IsUnique`: Unique constraint
- `HasIndex`: Create index on column
- `MaxLength`: String column maximum length
- `DbType`: Explicit database type mapping
- `IdentitySeed`/`IdentityIncrement`: Identity column configuration

## 📊 Performance Features

### Bulk Operations
- SQL Server bulk copy for maximum throughput
- Configurable batch sizes (default: 1000 records)
- Progress reporting for long-running operations
- Automatic staging table management for upserts/deletes

### Connection Management
- Efficient connection factory pattern
- Automatic connection disposal
- Configurable timeouts for connections and commands

### Type Conversion
- Optimized type mapping with caching
- Support for nullable types, enums, and custom conversions
- Automatic handling of DBNull values

## 🔧 Error Handling

### Exception Management
- Comprehensive exception capture and logging
- Contextual error information with query details
- Parameter values included in error logs (when safe)

### Result Validation
- `LastException` property for immediate error access
- Boolean return values for success/failure indication
- Detailed script execution results with batch-level error tracking

## 🧪 Best Practices

### Connection Usage
```csharp
// Each method manages its own connection lifecycle
var db = new DBAccess("Production");
var result = db.GetValue<int>("SELECT COUNT(*) FROM Users");
// Connection is automatically opened, query executed, and closed

// Multiple operations can reuse the same DBAccess instance
var userCount = db.GetValue<int>("SELECT COUNT(*) FROM Users");
var activeUsers = db.GetRecords<User>("SELECT * FROM Users WHERE Active = 1");
bool success = db.ExecuteQuery("UPDATE Users SET LastAccess = @date WHERE Id = @id", 
    new object[] { DateTime.Now, userId });
```

### Async Operations
```csharp
// Prefer async methods for I/O bound operations
var users = await db.GetRecordsAsync<User>("SELECT * FROM Users WHERE Active = 1");

// Use cancellation tokens for long-running operations
var processor = new BulkDbProcessor<Product>("Products");
await processor.BulkInsertAsync(db, products, cancellationToken);
```

### Error Handling
```csharp
// Always check for errors after operations
bool success = db.ExecuteQuery("UPDATE Users SET LastLogin = @date WHERE Id = @id", 
    new object[] { DateTime.Now, userId });

if (!success)
{
    Log.Error("Failed to update user login", db.LastException);
    // Handle error appropriately
}
```

### Configuration Security
```csharp
// Encrypt credentials before storing in configuration
var options = new DatabaseOptions();
options.User = "myuser";        // Automatically encrypted
options.Password = "mypass";    // Automatically encrypted

// Access is automatically decrypted
string connectionString = options.GetConnectionString();
```

## 🔗 Dependencies

- **[Configuration Module](../../Configuration/readme.md)**: For INI-based configuration management
- **[Logging Module](../../Logging/readme.md)**: For comprehensive operation logging
- **[Security Module](../../Security/readme.md)**: For credential encryption/decryption
- **[Net Module](../../Net/readme.md)**: For complementary secure file transfer in ETL workflows
- **[Core Module](../../Core/readme.md)**: Embedded WinSCP extraction used indirectly by Net
- **System.Data.SqlClient**: For SQL Server connectivity
- **System.Data.Odbc**: For ODBC database connectivity

## 📝 Notes

- Designed specifically for SQL Server 2000 compatibility
- .NET Framework 4.8 compatible
- Thread-safe credential access with proper locking
- Extensive logging for debugging and monitoring
- Production-ready with enterprise security features
