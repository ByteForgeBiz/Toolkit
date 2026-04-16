# Logging Unit Tests

Tests for `ByteForge.Toolkit.Logging`.

**Test classes:** `LogTests`, `DatabaseLoggerTests`, `DatabaseLoggerLiveTests` (which contains `DatabaseLoggerSqlServerLiveTests`)
**Test categories:** `Unit`, `Logging`; live SQL Server tests also carry `SQLServer`
**Source module:** `Toolkit.Modern/Logging/`

## Test Classes

### LogTests

Validates the static `Log` facade and the underlying `ILogger` infrastructure.

A static constructor in `LogTests` initializes the global `Configuration` singleton from a temp INI file before any test runs, so that `Log` can load its settings:

```ini
[Logging]
eTraceLogLevel=All
bUseSessionLogging=False
sLogFile=<temp_path>.log
```

| Test area | Coverage |
|-----------|---------|
| Singleton | `Log.Instance` returns the same reference on repeated access |
| `LogLevel` property | Readable and writable; restored after each test |
| Static log methods | `Log.Trace`, `Log.Debug`, `Log.Info`, `Log.Notice`, `Log.Warning`, `Log.Error`, `Log.Fatal` |
| `AddLogger` / `RemoveLogger` | Custom `ILogger` implementations receive dispatched messages |
| Log level filtering | Messages below `Log.LogLevel` are not dispatched |
| `BeginSuppressedScope()` | Messages logged inside the scope are suppressed |
| Nested suppressed scopes | Inner scope extends the outer scope; messages resume only after all scopes are exited |
| Thread safety | Concurrent logging from multiple threads |
| Logger lifecycle | Logger added and removed within a single test |

`[TestInitialize]` stores the original `LogLevel` and calls `TempFileHelper.CleanupTempFiles()`. `[TestCleanup]` restores the `LogLevel` and cleans temp files.

### DatabaseLoggerTests

Tests the `DatabaseLogger` class in isolation using an in-process `CaptureLogger` implementation (defined inside the test file).

| Test area | Coverage |
|-----------|---------|
| `BeginSuppressedScope()` | Suppresses messages dispatched through `Log.Info` while the scope is active |
| Nested scopes | Multiple nested `BeginSuppressedScope()` calls — messages resume only after all are disposed |
| Scope interaction with multiple loggers | `CaptureLogger` sees messages outside the scope; `DatabaseLogger` would be suppressed |
| `Log.Instance.AddLogger` / `RemoveLogger` | Dynamic logger attachment and detachment |
| `LastException` | Set when a logger throws during dispatch |

The tests use reflection where necessary to access internal state.

### DatabaseLoggerSqlServerLiveTests (in `DatabaseLoggerLiveTests.cs`)

**Category:** `Unit`, `Logging`, `SQLServer`

Live integration tests that verify `DatabaseLogger` writes to a real SQL Server table. Each test creates a dedicated table (`DatabaseLoggerTestEntries`) in `TestUnitDB`, exercises the logger, and drops the table in `[TestCleanup]`.

**Requires:** SQL Server `TestUnitDB` accessible via `DatabaseTestHelper`.

| Test | Description |
|------|-------------|
| `DatabaseLogger_SQLServer_ShouldAutoCreateTableAndPersistInfoEntry` | Logger with `AutoCreateTable = true` creates the log table and writes one `Info` row |
| `DatabaseLogger_SQLServer_ShouldPersistExceptionDetails` | Exception message and stack trace are stored in the log row |
| Additional tests | Various log levels, correlation IDs, structured log fields, and multi-entry scenarios |

Each test verifies:
- Table was auto-created
- Row count matches expected
- Stored row has correct `Level`, `Message`, `LoggerName`, `Source`, `CorrelationId`, and `Exception` columns

## Log Levels

The logging system defines levels in ascending severity order:

`Trace` → `Debug` → `Info` → `Notice` → `Warning` → `Error` → `Fatal`

Filtering is inclusive: setting `LogLevel = Warning` dispatches `Warning`, `Error`, and `Fatal` only.

## Running These Tests

```powershell
# All logging tests (no SQL Server required — LogTests and DatabaseLoggerTests run in isolation)
dotnet test --filter "TestCategory=Logging&TestCategory!=SQLServer"

# SQL Server live tests only
dotnet test --filter "TestCategory=SQLServer&FullyQualifiedName~Logging"

# All logging tests
dotnet test --filter "TestCategory=Logging"

# Specific class
dotnet test --filter "FullyQualifiedName~LogTests"
dotnet test --filter "FullyQualifiedName~DatabaseLoggerTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
| **Logging source** | Production module | [../../../Toolkit.Modern/Logging/readme.md](../../../Toolkit.Modern/Logging/readme.md) |
