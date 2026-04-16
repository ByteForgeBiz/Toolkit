# Logging — Implementations

This directory contains all concrete logger classes. Each class is either an abstract base or a full implementation of the `ILogger` interface. Most extend `BaseLogger`, which handles log-level filtering and `LogEntry` construction.

---

## Class Overview

| Class | Base | Description |
|-------|------|-------------|
| `BaseLogger` | — (abstract) | Provides level filtering and `LogEntry` construction |
| `CompositeLogger` | `BaseLogger` | Dispatches entries to a list of child `ILogger` instances |
| `ConsoleLogger` | `BaseLogger` | Thread-safe, color-coded console output |
| `FileLogger` | `BaseLogger` | Async file output with rotation and retention |
| `SessionFileLogger` | `FileLogger` | Per-run session files with headers and footers |
| `DatabaseLogger` | `BaseLogger` | Persists entries to SQL Server or ODBC/Access |
| `NullLogger` | `ILogger` (direct) | No-op implementation for testing or silencing |
| `StaticLoggerAdapter` | `BaseLogger` | Wraps the static `Log` class as an `ILogger` |

---

## `BaseLogger`

Abstract base class for all concrete loggers.

**Constructor:** Takes a `name` string that becomes `Name`.

**Key behavior:**
- Default `MinLogLevel = LogLevel.Info`
- `Log(LogLevel, string, Exception?)` checks the level, builds a `LogEntry`, and calls abstract `RecordLogEntry(LogEntry)`
- `LogEntry` is populated with: `Timestamp = DateTime.Now`, `Level`, `Message`, `Source = level.ToString()`, `Exception`, `Properties` containing `LoggerName` and `ThreadId`
- Level check: `if (level < MinLogLevel) return;` — entries below `MinLogLevel` are silently dropped

**Subclasses implement:**
```csharp
protected internal abstract void RecordLogEntry(LogEntry entry);
```

---

## `CompositeLogger`

Extends `BaseLogger`, implements `IList<ILogger>` and `IDisposable`.

Manages a list of child `ILogger` instances and dispatches each `LogEntry` to all of them. The `Log` singleton is a `CompositeLogger`.

**Key members:**

| Member | Description |
|--------|-------------|
| `AddLogger(ILogger)` | Appends a logger to the internal list |
| `RemoveLogger(string)` | Removes by name |
| `RemoveLogger(ILogger)` | Removes by instance |
| `EnableMultiThreading` | Enables parallel dispatch via `Task.Run` + `SemaphoreSlim` (auto-disabled for IIS/W3SVC) |
| `_continueOnError` | If `true`, continues dispatching even when a child logger throws |

**Correlation stamping:**
Before dispatching, `CompositeLogger.RecordLogEntry()` calls `CorrelationContext.New()` and stamps the resulting ID onto the entry. All child loggers receive the same correlation ID for a given log call.

**Dispatch modes:**
- **Synchronous (default):** Iterates the logger list in order.
- **Multi-threaded:** Dispatches each logger on a `Task` with a `SemaphoreSlim` to bound concurrency.

---

## `ConsoleLogger`

Thread-safe console output with color-coded severity levels.

**Concurrency:** Uses a `static readonly object _lock` to serialize all `Console.Write*` calls across instances.

**Color mapping:**

| Level | Console color |
|-------|---------------|
| Verbose, Debug | Gray |
| Info | White |
| Warning | Yellow |
| Error | Red |
| Critical | DarkRed |
| Others | default |

**Properties:**

| Property | Description |
|----------|-------------|
| `ShowMessageOnly` | Omits timestamp and level prefix; only message is written |
| `ShowTimestamp` | Includes timestamp in prefix |
| `ShowStackTrace` | Appends exception stack trace |

Falls back silently if `ConsoleUtil.IsConsoleAvailable` returns `false` (no console window, or output is redirected).

---

## `FileLogger`

File-based logger with asynchronous queuing, daily/size rotation, and retention cleanup.

**Async queue:** Uses `BlockingCollection<LogEntry>` with a background consumer thread when `UseAsyncLogging = true`.

**File rotation:**
- **Daily:** Creates a new file when the date changes. Resets the file index counter.
- **Size:** Increments `_currentFileIndex` and opens a new file when the current file exceeds `MaxFileSizeMB`.

**File naming placeholders:**

| Placeholder | Resolved to |
|-------------|-------------|
| `{basename}` | Base name from configuration or constructor |
| `{date:format}` | `DateTime.Now.ToString(format)` |
| `{timestamp}` | Unix timestamp |
| `{index}` | Rotation index (integer) |
| `{pid}` | Process ID |
| `{guid}` | New GUID |
| `{sessionid}` | Session ID (from `SessionFileLogger`) |

**Log entry format:**
```
a1b2 - 2026-04-15 09:30:00.000 [Info     ] - Application started
       2026-04-15 09:30:00.000               Continuation line (aligned)
```

Exception format:
```
[ExceptionType] ExceptionMessage
  Stack trace line 1
  Stack trace line 2
  Data: key = value
  ---> InnerException: ...
```

**`Clear()`:** Truncates the current log file to zero bytes.

---

## `SessionFileLogger`

Extends `FileLogger`. Adds a per-run session identity that is embedded in the file name.

**Session ID formats (`SessionIdFormat` enum):**

| Format | Example |
|--------|---------|
| `Timestamp` | `20260415-093000` |
| `TimestampWithMilliseconds` | `20260415-093000-123` |
| `Guid` | `3f7a1b2c` (8-char truncated GUID) |
| `ProcessId` | `12345` |
| `Custom` | Result of `CustomSessionIdProvider()` |

**Default `FileNamingPattern`:** `"{basename}_{sessionid}"` (overrides `FileLogger` default of `"{basename}"`).

**Session lifecycle:**

| Event | Behavior |
|-------|----------|
| Startup | Calls `WriteSessionHeader()` if `WriteSessionHeader = true`. Writes separator lines with app name, version, machine name, user name, PID. Also cleans up old session files if `CleanupOldSessions = true`. |
| `EndSession()` | Calls `WriteSessionFooter()` if `WriteSessionFooter = true`. Writes separator lines with elapsed duration. |

---

## `DatabaseLogger`

Persists log entries to a relational database table. Implemented as a `partial class` (additional helpers are split into a companion file).

**Initialization:** Lazy, double-checked with a `lock (_syncRoot)`. On first `RecordLogEntry()` call:
1. Validates `Settings.Enabled` and `Settings.TableName` (must match `^[A-Za-z_][A-Za-z0-9_]*$`)
2. Resolves database connection (see below)
3. Optionally auto-creates the log table

If any step fails, `IsDisabled` is set to `true` and no further logging is attempted.

**Connection resolution order:**
1. Explicit `DatabaseOptions` passed to the constructor
2. Named config section specified in `Settings.DatabaseSection`
3. Root `[Data Source]` → `SelectedDatabase` config section

**Auto-create DDL:**

SQL Server:
```sql
IF OBJECT_ID(N'TableName', N'U') IS NULL
BEGIN
    CREATE TABLE [TableName]
    (
        [LogTimestamp] DATETIME2 NOT NULL,
        [LogLevel]     NVARCHAR(32) NOT NULL,
        ...
    );
END
```

ODBC/Access:
```sql
CREATE TABLE [TableName]
(
    [LogTimestamp] DATETIME NOT NULL,
    [LogLevel]     TEXT(32) NOT NULL,
    [Message]      MEMO,
    ...
)
```

**Auto-create eligibility:** `CanAutoCreate()` returns `true` for SQL Server unconditionally, and for ODBC only when `IsKnownOdbcProvider()` detects `.mdb`, `.accdb`, `Access Driver`, `Microsoft Access`, `ACE`, or `Jet OLEDB` in the connection string.

**ODBC table existence check:** Executes `SELECT * FROM [TableName] WHERE 1 = 0` — success means the table exists.

**Recursion guard:** Uses `Log.BeginSuppressedScope()` (which increments an `AsyncLocal<int>` suppression depth) before executing any DB call. Any `Log.*` calls made while the scope is active are silently dropped for that async flow.

**Table columns:**

| Column | SQL Server type | ODBC type |
|--------|-----------------|-----------|
| `LogTimestamp` | `DATETIME2` | `DATETIME` |
| `LogLevel` | `NVARCHAR(32)` | `TEXT(32)` |
| `LoggerName` | `NVARCHAR(255)` | `TEXT(255)` |
| `Source` | `NVARCHAR(255)` | `TEXT(255)` |
| `Message` | `NVARCHAR(MAX)` | `MEMO` |
| `CorrelationId` | `NVARCHAR(255)` | `TEXT(255)` |
| `ThreadId` | `INT` | `INTEGER` |
| `ExceptionType` | `NVARCHAR(255)` | `TEXT(255)` |
| `ExceptionMessage` | `NVARCHAR(MAX)` | `MEMO` |
| `ExceptionStackTrace` | `NVARCHAR(MAX)` | `MEMO` |

**Virtual methods** (for testing / subclassing):
- `CreateDbAccess(DatabaseOptions)` — creates the `DBAccess` instance
- `ExecuteNonQuery(DBAccess, string, params object?[]?)` — executes a SQL command
- `DoesTableExist(DBAccess, DataBaseType)` — checks table existence

---

## `NullLogger`

Implements `ILogger` directly (not via `BaseLogger`). All methods are no-ops. Default `MinLogLevel = LogLevel.None`.

Use cases:
- Unit testing where logging output is unwanted
- Silencing a specific logger slot in a `CompositeLogger` without removing it
- Default/placeholder implementation

```csharp
Log.Instance.AddLogger(new NullLogger());  // All log calls to this logger are silently ignored
```

---

## `StaticLoggerAdapter`

Internal class. Adapts the static `Log` class as an `ILogger` implementation.

- `MinLogLevel` getter/setter delegates to `Log.LogLevel`
- `Log(LogLevel, string, Exception?)` dispatches to the matching `Log.Xxx()` static method
- Throws `InvalidOperationException` if `Log` has not been initialized

Used when code requires an `ILogger` instance but only the static `Log` singleton is available.

---

## Adding a Custom Logger

```csharp
public class SignalRLogger : BaseLogger
{
    private readonly IHubContext _hub;

    public SignalRLogger(IHubContext hub) : base("SignalR")
    {
        _hub = hub;
        MinLogLevel = LogLevel.Warning;
    }

    protected internal override void RecordLogEntry(LogEntry entry)
    {
        _hub.Clients.All.SendAsync("Log", entry.Level.ToString(), entry.Message);
    }
}

// Register
Log.Instance.AddLogger(new SignalRLogger(hubContext));
```
