# ByteForge.Toolkit Database Access Layer

A robust and flexible database access layer built for .NET Framework 4.8, supporting SQL Server 2000 and ODBC connections.

## Overview

This database access layer provides a comprehensive set of tools for database operations while maintaining high security standards and clean code practices. It supports both SQL Server and ODBC connections, with built-in support for connection encryption, parameter handling, and script execution.

## Features

- Support for SQL Server 2000 and ODBC connections
- Asynchronous database operations
- Secure password encryption
- Parameterized queries to prevent SQL injection
- Comprehensive logging system
- Script execution with GO statement parsing
- Batch operations support
- Type-safe value conversion
- Connection string management
- Error handling and debugging support

## Core Components

### DBAccess

The main class that handles database connections and operations. It's split into several partial classes for better organization:

- **DBAccess.Core**: Contains the core functionality and configuration setup
- **DBAccess.Factory**: Handles creation of database-specific objects
- **DBAccess.Methods**: Implements the main database operations
- **DBAccess.Parameters**: Manages parameter handling and type mapping
- **DBAccess.ScriptExecution**: Provides script execution capabilities
- **DBAccess.TypeConverter**: Handles type conversion for database operations
- **DBAccess.Logging**: Implements logging functionality

### DatabaseOptions

Manages database connection configuration with support for:
- Server configuration
- Database credentials (encrypted)
- Connection timeouts
- Trusted connections
- Connection string building

### ScriptExecutionResult

Represents the result of a script execution, including:
- Success status
- Batch results
- Result sets
- Records affected
- Exception information

## Usage Examples

### Basic Connection Setup

```csharp
// Initialize database access with default configuration
var db = new DBAccess();

// Or specify a custom configuration section
var db = new DBAccess("CustomDB");
```

### Execute a Query

```csharp
// Simple query execution
bool success = db.ExecuteQuery("UPDATE Users SET Status = @status WHERE ID = @id",
    new object[] { "Active", 1 });

// Get a single value
int count = db.GetValue<int>("SELECT COUNT(*) FROM Users");

// Get multiple records
var records = db.GetRecords("SELECT * FROM Users WHERE Status = @status",
    new object[] { "Active" });
```

### Async Operations

```csharp
// Async value retrieval
var count = await db.GetValueAsync<int>("SELECT COUNT(*) FROM Users");

// Async record retrieval
var records = await db.GetRecordsAsync("SELECT * FROM Users");
```

### Script Execution

```csharp
string script = @"
    CREATE TABLE #TempUsers (ID INT, Name NVARCHAR(100));
    GO
    INSERT INTO #TempUsers VALUES (1, 'John');
    GO
    SELECT * FROM #TempUsers;";

var result = db.ExecuteScript(script, captureResults: true);
```

## Configuration

The database configuration uses INI format. Here's an example configuration:

```ini
[Data Source]
SelectedDB=MainDatabase

[MainDatabase]
sType=SQLServer
sServer=ServerName
sDatabaseName=DatabaseName
esUser=[encrypted-user]
esPass=[encrypted-password]
bEncrypt=true
iConnectionTimeout=60
iCommandTimeout=240
bTrustedConnection=false

[ReportDB]
sType=ODBC
sServerDSN=ReportDSN
sServer=ReportServer
sDatabaseName=ReportDB
esUser=[encrypted-user]
esPass=[encrypted-password]
```

### Configuration Options

- **sType**: Database type (SQLServer or ODBC)
- **sServer**: Server name or IP address
- **sServerDSN**: DSN name (for ODBC connections)
- **sDatabaseName**: Database name
- **esUser**: Encrypted username
- **esPass**: Encrypted password
- **bEncrypt**: Enable connection encryption (true/false)
- **iConnectionTimeout**: Connection timeout in seconds
- **iCommandTimeout**: Command timeout in seconds
- **bTrustedConnection**: Use Windows authentication (true/false)

## Security Features

- Password encryption using custom encryption algorithm
- Support for encrypted connections
- Parameterized queries to prevent SQL injection
- Secure credential handling
- Connection string protection

## Error Handling

The system provides comprehensive error handling:
- Exception logging
- Last exception tracking
- Detailed error messages
- Query execution logging
- Parameter logging for debugging

## Best Practices

1. Always use parameterized queries instead of string concatenation
2. Dispose of database connections properly using `using` statements
3. Handle exceptions appropriately
4. Use async methods for long-running operations
5. Check the `LastException` property when operations fail
6. Use appropriate timeout values for your operations

## Requirements

- .NET Framework 4.8
- SQL Server 2000 or later
- ODBC Driver (if using ODBC connections)

## Notes

- This library is specifically optimized for SQL Server 2000
- All database operations are logged for debugging purposes
- Connection strings are built securely based on configuration
- The system supports both synchronous and asynchronous operations
- Script execution supports GO statement batch separation