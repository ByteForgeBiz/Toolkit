# Database Unit Tests

Tests for `DBAccess` and `BulkDbProcessor<T>` in `ByteForge.Toolkit.Data`. Coverage spans SQL Server operations, ODBC/Access operations, parameter parsing, script execution, transactions, and bulk processing.

**Source module:** `Toolkit.Modern/Data/`

## Test Classes

| Class | Category | Description |
|-------|----------|-------------|
| `DBAccessCoreTests` | (none) | Constructor, options, connection string generation, property access |
| `DBAccessMethodsSQLServerTests` | (none) | CRUD operations, parameter handling, async variants — SQL Server |
| `DBAccessMethodsODBCTests` | `Unit`, `ODBC` | CRUD operations, parameter handling — Access via ODBC |
| `DBAccessParameterParsingTests` | (none) | Named parameter detection via private method reflection |
| `DBAccessParameterParsingSimpleTests` | (none) | Simple parameter parsing scenarios |
| `DBAccessExtendedPropertiesTests` | (none) | `RecordsAffected`, `LastException`, and other properties |
| `DBAccessScriptExecutionTests` | (none) | Multi-statement scripts, `GO` batch separator, error handling |
| `DBAccessTransactionTests` | (none) | `BeginTransaction`, `Commit`, `Rollback`, read consistency |
| `BulkDbProcessorTests` | (none) | Bulk insert, upsert, delete with `BulkTestEntity` |
| `BulkDbProcessorEdgeCaseTests` | (none) | Empty collections, null mappings, duplicate keys |

Classes without an explicit `[TestCategory]` still require SQL Server (they use `DatabaseTestHelper.AssertTestDatabaseSetup` in `[ClassInitialize]`).

## Prerequisites

### SQL Server Tests (most classes)

A SQL Server instance must be reachable with the `TestUnitDB` database set up. The `[ClassInitialize]` method in each SQL Server test class calls `DatabaseTestHelper.AssertTestDatabaseSetup()`, which verifies:

- Basic connectivity (`TestConnection()` returns `true`)
- `TestEntities` table has at least 15 rows
- `TestDataTypes` table has at least 3 rows
- `BulkTestEntities` table has at least 40 rows

SQL scripts for creating and seeding the database are in the `TODO/` folder at the test project root.

**Default connection:** `(local)` with Windows Authentication.

**CI environment variable overrides** (read by `DatabaseTestHelper`):

| Variable | Description |
|----------|-------------|
| `BYTEFORGE_TEST_SQL_SERVER` | Hostname or IP of the SQL Server instance |
| `BYTEFORGE_TEST_SQL_PORT` | Port number (default 1433) |
| `BYTEFORGE_TEST_SQL_USER` | SQL login user (omit for trusted/Windows auth) |
| `BYTEFORGE_TEST_SQL_PASSWORD` | SQL login password |
| `BYTEFORGE_TEST_SQL_CONNECTION_STRING` | Full connection string (takes priority over all others) |
| `BYTEFORGE_TEST_SQL_DATABASE` | Database name (default `TestUnitDB`) |

### ODBC Tests (`DBAccessMethodsODBCTests`, category `ODBC`)

Requires the **Microsoft Access Database Engine ODBC driver** (`Microsoft Access Driver (*.mdb, *.accdb)` for `.accdb`, or `Microsoft Access Driver (*.mdb)` for legacy `.mdb`).

The test database files are copied to the output directory by the build:

| File | Description |
|------|-------------|
| `TestData/TestUnitDB.accdb` | Preferred format (Access 2007+) |
| `TestData/TestUnitDB.mdb` | Legacy format fallback |

The helper resolves which file to use automatically:

1. If `BYTEFORGE_TEST_ACCESS_FORMAT=accdb` → uses `.accdb` with the `*.mdb, *.accdb` driver
2. If `BYTEFORGE_TEST_ACCESS_FORMAT=mdb` → uses `.mdb` with the `*.mdb` driver
3. If the variable is unset → prefers `.accdb` if it exists, falls back to `.mdb`

The `[ClassInitialize]` for `DBAccessMethodsODBCTests` calls `DatabaseTestHelper.AssertAccessDatabaseSetup()`, which verifies:

- Connectivity via ODBC
- `TestEntities` has at least 3 rows
- `TestDataTypes` has at least 1 row
- `BulkTestEntities` has at least 3 rows

## Coverage Details

### DBAccessCoreTests

- Constructor with `DatabaseOptions` — properties initialized correctly
- Constructor with null options — `ArgumentNullException` thrown
- Different database types (`SQLServer`, `ODBC`)
- `DbType`, `ConnectionString`, `Options` properties
- `TestConnection()` — true for valid, false for invalid
- `LastException` and `RecordsAffected` tracking

### DBAccessMethodsSQLServerTests / DBAccessMethodsODBCTests

Both cover the same API surface against different backends:

| Method | Coverage |
|--------|---------|
| `TestConnection()` | Valid and invalid connections |
| `GetValue<T>()` | Returns correct typed scalar value |
| `GetRecord()` | Returns first-row `DataRow` |
| `GetRecords()` | Returns `DataTable` with multiple rows |
| `ExecuteQuery()` | INSERT, UPDATE, DELETE; returns `bool`; sets `RecordsAffected` |
| `GetValue<T>()` async | Async scalar retrieval |
| `GetRecords()` async | Async tabular retrieval |
| `ExecuteQuery()` async | Async DML |

### DBAccessParameterParsingTests

Tests the private `ParseParameters` method via reflection. Covers:

- Named parameters (`@paramName`) — detected and returned as a list
- Named assignment syntax (`@paramName = @valueParam`) — used by stored procedure calls
- Query with no parameters — returns empty list
- Null/empty query — safe handling
- Parameters in different positions in the SQL text

### DBAccessTransactionTests

- `BeginTransaction()` returns a transaction object
- `Commit()` — data is visible after commit
- `Rollback()` — data is not visible after rollback
- Exception during transaction — `LastException` is set; data is rolled back
- Read consistency — `GetValue` within a transaction sees uncommitted data from the same connection

### DBAccessScriptExecutionTests

- Multi-statement scripts with `GO` batch separator
- Error in one batch — subsequent batches are not executed
- Transaction management within script execution

### BulkDbProcessorTests

- Bulk insert: inserts all entities using `BulkTestEntity` (`[DBColumn]`-decorated)
- Upsert: updates existing rows based on the `Code` unique column; inserts new rows
- Bulk delete: removes rows matching specified keys
- Attribute mapping: `[DBColumn(isPrimaryKey:true)]`, `[DBColumn(isUnique:true)]`, `[DBColumn(MaxLength=...)]`

### BulkDbProcessorEdgeCaseTests

- Empty collection — no SQL executed; no exceptions
- Collection with null elements
- Missing `[DBColumn]` attributes
- Duplicate unique keys in the source collection

## Test Models

| Model | Table | Purpose |
|-------|-------|---------|
| `TestEntity` | `TestEntities` | General CRUD and transaction tests |
| `BulkTestEntity` | `BulkTestEntities` | Bulk operation tests |
| `TestDataTypeEntity` | `TestDataTypes` | Type conversion tests |

See [Models/README.md](../../../Models/README.md) for property details.

## Running These Tests

```powershell
# All database tests
dotnet test --filter "FullyQualifiedName~Data.Database"

# Only SQL Server tests (no ODBC driver required)
dotnet test --filter "FullyQualifiedName~Data.Database&TestCategory!=ODBC"

# Only ODBC tests
dotnet test --filter "TestCategory=ODBC"

# Specific class
dotnet test --filter "FullyQualifiedName~BulkDbProcessorTests"
dotnet test --filter "FullyQualifiedName~DBAccessTransactionTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../../README.md](../../../README.md) |
| **Unit overview** | Unit test organization | [../../readme.md](../../readme.md) |
| **Data overview** | Data tests overview | [../readme.md](../readme.md) |
| **Helpers** | Database test helper | [../../../Helpers/README.md](../../../Helpers/README.md) |
| **Models** | Test entity models | [../../../Models/README.md](../../../Models/README.md) |
| **Data source** | Production module | [../../../../Toolkit.Modern/Data/readme.md](../../../../Toolkit.Modern/Data/readme.md) |
