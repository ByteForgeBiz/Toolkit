# Logging Module

The Logging module provides a structured, multi-target logging system with support for simultaneous output to console, file, session-scoped file, and database targets. It features configurable log levels, async queuing, file rotation, and suppression scopes to prevent recursive logging.

---

## Architecture

The module uses a composite dispatch pattern:

```
Log (singleton CompositeLogger)
├── ConsoleLogger
├── FileLogger  -or-  SessionFileLogger
└── DatabaseLogger
```

- `Log` is the static entry point and a singleton `CompositeLogger`.
- `CompositeLogger` holds a list of `ILogger` implementations and dispatches each `LogEntry` to all of them.
- Individual loggers inherit from `BaseLogger` (or implement `ILogger` directly for `NullLogger`).
- Log entries are correlated using an auto-incrementing 4-hex-digit `CorrelationContext` ID stamped per entry by `CompositeLogger`.
- If a child logger fails, `Log` writes to a fallback file at `%TEMP%\LogError.{guid}.log`.

---

## Quick Start

```csharp
// Static methods work immediately — Log initializes from config on first access
Log.Info("Application started");
Log.Warning("Disk space low");
Log.Error(ex, "Unhandled exception");

// Add a custom logger at runtime
Log.Instance.AddLogger(new ConsoleLogger());

// Adjust level at runtime
Log.LogLevel = LogLevel.Debug;

// End a session (writes footer for SessionFileLogger)
Log.EndSession();
```

---

## Log Levels

`LogLevel` is an enum with special values at the extremes and positional ordering in between. **Lower ordinal = more verbose** (not more severe).

| Value     | Ordinal       | Notes                         |
|-----------|---------------|-------------------------------|
| `All`     | `int.MinValue`| Enables all levels            |
| `Trace`   | 0             | Finest detail                 |
| `Debug`   | 1             |                               |
| `Verbose` | 2             |                               |
| `Info`    | 3             | Default minimum level         |
| `Notice`  | 4             |                               |
| `Warning` | 5             |                               |
| `Error`   | 6             |                               |
| `Critical`| 7             |                               |
| `Fatal`   | 8             |                               |
| `None`    | `int.MaxValue`| Disables all logging          |

**Filtering rule:** `if (level < MinLogLevel) return;` — entries whose `LogLevel` ordinal is less than `MinLogLevel` are dropped. Setting `MinLogLevel = LogLevel.Warning` suppresses Trace through Notice.

---

## Key Types

### `Log` — Static Entry Point

A singleton `CompositeLogger` accessible via static methods. Auto-initializes from the `[Logging]` config section on first use.

| Member | Description |
|--------|-------------|
| `Instance` | Gets the singleton logger |
| `LogLevel` | Gets/sets effective level across all loggers |
| `Trace/Debug/Verbose/Info/Notice/Warning/Error/Critical/Fatal(message)` | Static log methods |
| `Trace/.../Fatal(exception, message)` | Overloads with exception context |
| `EnableConsoleLogging()` / `DisableConsoleLogging()` | Adds/removes `ConsoleLogger` at runtime |
| `EndSession()` | Writes session footer (for `SessionFileLogger`) |
| `BeginSuppressedScope()` | Returns a scope that suppresses recursive log dispatch |
| `Configure(LogSettings)` | Reconfigures the singleton |

On initialization, `Log` reads `LogSettings` from config and creates:
- A `FileLogger` or `SessionFileLogger` (based on `UseSessionLogging`)
- A `DatabaseLogger` (if `UseDatabaseLogging` is true and a database is configured)
- A `ConsoleLogger` (unless running under IIS/W3SVC)

---

### `CompositeLogger`

Extends `BaseLogger`, implements `IList<ILogger>` and `IDisposable`. Manages child loggers and dispatches entries to all of them.

- Stamps a `CorrelationContext.New()` ID on every `LogEntry` before dispatch.
- `EnableMultiThreading`: enables parallel dispatch using `Task.Run` + `SemaphoreSlim` (auto-disabled for web apps).
- `AddLogger(ILogger)` / `RemoveLogger(name or instance)` for runtime management.

---

### `BaseLogger`

Abstract base for all concrete loggers.

- Default `MinLogLevel = LogLevel.Info`
- `Log()` validates the level, builds a `LogEntry` (Timestamp=DateTime.Now, Level, Message, Source=level name, Exception, Properties with LoggerName and ThreadId), then calls abstract `RecordLogEntry(LogEntry)`.
- Subclasses implement `RecordLogEntry` to write to their target.

---

### `ConsoleLogger`

Thread-safe console output using a static lock.

| Property | Description |
|----------|-------------|
| `ShowMessageOnly` | Suppresses timestamp and level prefix |
| `ShowTimestamp` | Includes timestamp |
| `ShowStackTrace` | Includes exception stack trace |

Color scheme: Verbose/Debug → Gray, Info → White, Warning → Yellow, Error → Red, Critical → DarkRed. Falls back silently if no console is available.

---

### `FileLogger`

Asynchronous file logging with a `BlockingCollection<LogEntry>` queue, file rotation, and retention cleanup.

| Property | Default | Description |
|----------|---------|-------------|
| `FileNamingPattern` | `"{basename}"` | Pattern with placeholders: `{basename}`, `{date:format}`, `{timestamp}`, `{index}`, `{pid}`, `{guid}`, `{sessionid}` |
| `UseDaily` | `false` | Rotates to a new file each day |
| `MaxFileSizeMB` | 0 | Rotates when file exceeds this size (0 = unlimited) |
| `RetentionDays` | 0 | Deletes log files older than this many days (0 = keep all) |
| `UseAsyncLogging` | `false` | Queues entries asynchronously via `BlockingCollection` |
| `AsyncQueueSize` | 1000 | Maximum async queue depth |
| `FileEncoding` | UTF-8 | Output file encoding |
| `Clear()` | — | Truncates the current log file |

**Log entry format:**
```
a1b2 - 2026-04-15 09:30:00.000 [Info     ] - Application started
```

Exception output includes type, message, stack trace, `Data` dictionary entries, and recursively formatted inner exceptions with increasing indentation.

---

### `SessionFileLogger`

Extends `FileLogger` with per-run session identity. Each application launch gets a unique session ID embedded in the file name.

| Property | Default | Description |
|----------|---------|-------------|
| `SessionId` | auto | Generated from `SessionIdFormat` |
| `SessionIdFormat` | `Timestamp` | `Timestamp`, `TimestampWithMilliseconds`, `Guid` (8-char), `ProcessId`, or `Custom` |
| `FileNamingPattern` | `"{basename}_{sessionid}"` | Override of FileLogger default |
| `WriteSessionHeader` | `true` | Writes app name, version, machine, user, PID at startup |
| `WriteSessionFooter` | `true` | Writes elapsed duration at `EndSession()` |
| `CleanupOldSessions` | `true` | Deletes old session files on startup |
| `CustomSessionIdProvider` | `null` | `Func<string>` for fully custom session IDs |

---

### `DatabaseLogger`

Persists log entries to a relational database table. Lazy, double-checked initialization; disables itself permanently on any failure.

**Connection resolution order:**
1. Explicit `DatabaseOptions` passed to constructor
2. Named config section from `DatabaseSection` setting
3. Root `[Data Source]` → `SelectedDatabase` setting

**Supported databases:**

| Database | Auto-create table | Table column types |
|----------|------------------|--------------------|
| SQL Server | Yes (with `IF OBJECT_ID IS NULL` guard) | `DATETIME2`, `NVARCHAR`, `INT` |
| ODBC / Access (`.mdb`, `.accdb`, ACE, Jet) | Yes | `DATETIME`, `TEXT`, `MEMO`, `INTEGER` |

**Table columns:** `LogTimestamp`, `LogLevel`, `LoggerName`, `Source`, `Message`, `CorrelationId`, `ThreadId`, `ExceptionType`, `ExceptionMessage`, `ExceptionStackTrace`.

Default table name: `__ApplicationLogs__` (configurable via `sTableName`).

Suppresses its own `Log.*` calls via `Log.BeginSuppressedScope()` to prevent infinite recursion.

---

### `NullLogger`

Implements `ILogger` directly. All methods are no-ops. Default `MinLogLevel = LogLevel.None`. Use for testing or to silence a logger slot without removing it from a composite.

---

### `StaticLoggerAdapter`

Internal adapter that wraps the static `Log` class as an `ILogger`. `MinLogLevel` get/set delegates to `Log.LogLevel`. Throws `InvalidOperationException` if `Log` is not yet initialized.

---

## Models

### `LogEntry`

Data carrier passed through the logging pipeline.

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | Time the entry was created |
| `CorrelationId` | `string?` | 4-hex-digit ID stamped by `CompositeLogger` |
| `Level` | `LogLevel` | Severity level |
| `Message` | `string?` | Log message text |
| `Source` | `string?` | Set to the level name by `BaseLogger` |
| `Exception` | `Exception?` | Associated exception |
| `Properties` | `IDictionary<string,object>` | Key-value bag; contains `LoggerName` and `ThreadId` by default |

---

### `CorrelationContext`

Lightweight value type holding a correlation ID. IDs are auto-incrementing 4-hex-digit values (`0000`–`ffff`, wrapping). **Not `IDisposable`** — it is a plain value holder, not a scope.

```csharp
var ctx = CorrelationContext.New(); // e.g., Id = "03a7"
```

---

### `LogSettings`

Loaded from the `[Logging]` config section.

| Config key | Property | Default |
|------------|----------|---------|
| `sLogFile` | `LogFilePath` | `{AssemblyDir}\{AssemblyName}.log` |
| `eLogLevel` | `TraceLogLevel` | `All` |
| `bClearLogOnStartup` | `ClearLogOnStartup` | `false` |
| `bUseSessionLogging` | `UseSessionLogging` | `false` |
| `bUseDatabaseLogging` | `UseDatabaseLogging` | `false` |

---

### `AsyncOptions`

Abstract base for file-based logger option classes.

| Property | Default | Description |
|----------|---------|-------------|
| `RetentionDays` | 0 | Days to keep log files (0 = unlimited) |
| `UseAsyncLogging` | `false` | Enable background write queue |
| `AsyncQueueSize` | 1000 | Max queued entries |

---

### `FileLoggerOptions` (extends `AsyncOptions`)

| Config key | Property | Default |
|------------|----------|---------|
| `bUseDaily` | `UseDaily` | `false` |
| `sEncoding` | `FileEncoding` | UTF-8 |
| `iMaxFileSizeMB` | `MaxFileSizeMB` | 0 (unlimited) |
| `sFileNamingPattern` | `FileNamingPattern` | `"{basename}"` |
| — | `CustomFileNameProvider` | `null` (`Func<string, FileLoggerOptions, string>`) |

---

### `SessionFileLoggerOptions` (extends `FileLoggerOptions`)

Overrides `FileNamingPattern` default to `"{basename}_{sessionid}"`.

| Property | Default | Description |
|----------|---------|-------------|
| `SessionIdFormat` | `Timestamp` | How the session ID is generated |
| `WriteSessionHeader` | `true` | Metadata block on startup |
| `WriteSessionFooter` | `true` | Duration block at shutdown |
| `CleanupOldSessions` | `true` | Deletes old session files on startup |
| `CustomSessionIdProvider` | `null` | `Func<string>` for custom session IDs |

---

### `DatabaseLoggerOptions`

| Config key | Property | Default |
|------------|----------|---------|
| `bEnabled` | `Enabled` | `true` |
| `sDatabaseSection` | `DatabaseSection` | `null` |
| `sTableName` | `TableName` | `"__ApplicationLogs__"` |
| `bAutoCreateTable` | `AutoCreateTable` | `true` |
| `eMinLogLevel` | `MinLogLevel` | `All` |

---

## Suppression Scope

`DatabaseLogger` uses `Log.BeginSuppressedScope()` to prevent the database write from triggering additional log calls. The scope uses `AsyncLocal<int>` so suppression is flow-local and does not affect other async branches.

```csharp
// Used internally by DatabaseLogger:
using var _ = Logging.Log.BeginSuppressedScope();
// Any Log.Xxx() calls here are silently dropped for this async flow
```

---

## Custom Logger

Implement `ILogger` (or extend `BaseLogger`) and add to the composite:

```csharp
public class SplunkLogger : BaseLogger
{
    public SplunkLogger() : base("Splunk") { }

    protected internal override void RecordLogEntry(LogEntry entry)
    {
        // Send entry to Splunk endpoint
    }
}

// Register at startup
Log.Instance.AddLogger(new SplunkLogger());
```

---

## INI Configuration

```ini
[Logging]
sLogFile=.\logs\app.log
eLogLevel=Info
bClearLogOnStartup=false
bUseSessionLogging=false
bUseDatabaseLogging=false

[DatabaseLogger]
bEnabled=true
sDatabaseSection=Production
sTableName=__ApplicationLogs__
bAutoCreateTable=true
eMinLogLevel=All
```

---

## Design Patterns

| Pattern | Usage |
|---------|-------|
| Singleton | `Log.Instance` — thread-safe lazy initialization |
| Composite | `CompositeLogger` dispatches to a list of `ILogger` implementations |
| Template Method | `BaseLogger.Log()` builds `LogEntry`; `RecordLogEntry()` is the extension hook |
| Null Object | `NullLogger` — safe no-op implementation of `ILogger` |
| Adapter | `StaticLoggerAdapter` wraps the static `Log` class as `ILogger` |
| Double-Checked Locking | `DatabaseLogger.EnsureInitialized()` for thread-safe lazy init |
| Async-Local Scope | `LoggingScopeContext` uses `AsyncLocal<int>` for flow-isolated log suppression |

---

## File Organization

```
Logging/
├── Log.cs                          # Static entry point, singleton CompositeLogger
├── LoggingScopeContext.cs          # Internal async-local suppression scope
├── Implementations/
│   ├── BaseLogger.cs               # Abstract base with level filtering and entry building
│   ├── CompositeLogger.cs          # Dispatches to multiple ILogger instances
│   ├── ConsoleLogger.cs            # Thread-safe color console output
│   ├── DatabaseLogger.cs           # Persists entries to SQL Server or ODBC/Access
│   ├── FileLogger.cs               # Async file output with rotation and retention
│   ├── NullLogger.cs               # No-op logger for testing
│   ├── SessionFileLogger.cs        # Per-session file output with headers/footers
│   └── StaticLoggerAdapter.cs      # Adapts static Log as ILogger
├── Interfaces/
│   └── ILogger.cs                  # Core logging interface
└── Models/
    ├── AsyncOptions.cs             # Abstract base for async options
    ├── CorrelationContext.cs       # Auto-incrementing 4-hex-digit correlation ID
    ├── DatabaseLoggerOptions.cs    # Configuration for DatabaseLogger
    ├── FileLoggerOptions.cs        # Configuration for FileLogger
    ├── LogEntry.cs                 # Log entry data carrier
    ├── LogLevel.cs                 # Severity level enum
    ├── LogSettings.cs              # Top-level logging configuration
    └── SessionFileLoggerOptions.cs # Configuration for SessionFileLogger
```

---

## Related Modules

| Module | Description |
|--------|-------------|
| [Configuration](../Configuration/readme.md) | INI-based configuration used to load log settings |
| [Data](../Data/readme.md) | Database access used by `DatabaseLogger` |
| [Utilities](../Utilities/readme.md) | `ConsoleUtil.IsConsoleAvailable` used by `ConsoleLogger` |
