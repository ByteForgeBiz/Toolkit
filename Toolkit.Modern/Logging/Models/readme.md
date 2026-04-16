# Logging — Models

This directory contains the data models, configuration classes, and enumerations used throughout the Logging module.

---

## `LogLevel`

Severity level enum. Controls which entries a logger processes.

| Value | Ordinal | Description |
|-------|---------|-------------|
| `All` | `int.MinValue` | Enables all entries (minimum filter) |
| `Trace` | 0 | Finest detail — execution path tracing |
| `Debug` | 1 | Diagnostic information |
| `Verbose` | 2 | Detailed informational output |
| `Info` | 3 | Normal application flow (default minimum) |
| `Notice` | 4 | Important events worth highlighting |
| `Warning` | 5 | Unexpected situations, still recoverable |
| `Error` | 6 | Operation failed, application continues |
| `Critical` | 7 | System instability, immediate attention required |
| `Fatal` | 8 | Unrecoverable failure |
| `None` | `int.MaxValue` | Disables all entries |

**Filtering:** `if (level < MinLogLevel) return;` — entries with a lower ordinal than `MinLogLevel` are dropped. Setting `MinLogLevel = LogLevel.Warning` allows Warning through Fatal but drops Trace through Notice.

---

## `LogEntry`

Immutable data carrier that flows from `BaseLogger.Log()` through `CompositeLogger` to each child logger's `RecordLogEntry()`.

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | Time the entry was created (set by `BaseLogger`) |
| `CorrelationId` | `string?` | 4-hex-digit ID stamped by `CompositeLogger.RecordLogEntry()` |
| `Level` | `LogLevel` | Severity level |
| `Message` | `string?` | Log message text |
| `Source` | `string?` | Set to `level.ToString()` by `BaseLogger` |
| `Exception` | `Exception?` | Associated exception, if any |
| `Properties` | `IDictionary<string, object>` | Extensible key-value bag; contains `LoggerName` and `ThreadId` by default |

---

## `CorrelationContext`

A lightweight value type holding a correlation ID. Used to group log entries from a single logical operation across different targets.

**ID generation:** Auto-incrementing 16-bit counter (`0000`–`ffff` in hex, wrapping). Thread-safe increment via `Interlocked`.

**Important:** `CorrelationContext` is **not** `IDisposable`. It is a plain value holder, not a scope. A new ID is stamped by `CompositeLogger` onto every `LogEntry` at dispatch time — it is not set by the caller.

```csharp
var ctx = CorrelationContext.New(); // e.g., Id = "03a7"
Console.WriteLine(ctx.Id);         // "03a7"
```

---

## `LogSettings`

Top-level logging configuration, loaded from the `[Logging]` config section by `Log` on initialization.

| Config key | Property | Default | Description |
|------------|----------|---------|-------------|
| `sLogFile` | `LogFilePath` | `{AssemblyDir}\{AssemblyName}.log` | Path to the log file |
| `eLogLevel` | `TraceLogLevel` | `All` | Minimum level for file and console loggers |
| `bClearLogOnStartup` | `ClearLogOnStartup` | `false` | Truncate the log file at startup |
| `bUseSessionLogging` | `UseSessionLogging` | `false` | Use `SessionFileLogger` instead of `FileLogger` |
| `bUseDatabaseLogging` | `UseDatabaseLogging` | `false` | Add a `DatabaseLogger` to the composite |

The default `LogFilePath` is resolved from the entry assembly's directory and name (e.g., `C:\App\MyApp.log`).

---

## `AsyncOptions`

Abstract base class for file-based logger option classes. Provides shared async queue settings.

| Property | Default | Description |
|----------|---------|-------------|
| `RetentionDays` | 0 | Number of days to retain log files (0 = keep all) |
| `UseAsyncLogging` | `false` | Queue entries to a `BlockingCollection` and write on a background thread |
| `AsyncQueueSize` | 1000 | Maximum number of entries that can queue before blocking |

---

## `FileLoggerOptions`

Extends `AsyncOptions`. Configuration for `FileLogger`.

| Config key | Property | Default | Description |
|------------|----------|---------|-------------|
| `bUseDaily` | `UseDaily` | `false` | Rotate to a new file each day |
| `sEncoding` | `FileEncoding` | UTF-8 | Output file encoding |
| `iMaxFileSizeMB` | `MaxFileSizeMB` | 0 | Rotate when file exceeds this size in MB (0 = unlimited) |
| `sFileNamingPattern` | `FileNamingPattern` | `"{basename}"` | Pattern with substitution placeholders |
| — | `CustomFileNameProvider` | `null` | `Func<string, FileLoggerOptions, string>` for fully custom names |

**File naming placeholders:**

| Placeholder | Resolves to |
|-------------|-------------|
| `{basename}` | Base file name (from constructor or config) |
| `{date:format}` | `DateTime.Now.ToString(format)` |
| `{timestamp}` | Unix timestamp (seconds) |
| `{index}` | Rotation index (integer, increments on rotation) |
| `{pid}` | Current process ID |
| `{guid}` | A new random GUID |
| `{sessionid}` | Session ID (populated by `SessionFileLogger`) |

---

## `SessionFileLoggerOptions`

Extends `FileLoggerOptions`. Adds session identity configuration for `SessionFileLogger`.

Overrides `FileNamingPattern` default to `"{basename}_{sessionid}"`.

| Property | Default | Description |
|----------|---------|-------------|
| `SessionIdFormat` | `Timestamp` | Controls how the session ID string is generated |
| `WriteSessionHeader` | `true` | Write metadata block (app, version, machine, user, PID) at startup |
| `WriteSessionFooter` | `true` | Write duration block at `EndSession()` |
| `CleanupOldSessions` | `true` | Delete old session log files on startup (respects `RetentionDays`) |
| `CustomSessionIdProvider` | `null` | `Func<string>` called when `SessionIdFormat = Custom` |

**`SessionIdFormat` enum values:**

| Value | Session ID example |
|-------|--------------------|
| `Timestamp` | `20260415-093000` |
| `TimestampWithMilliseconds` | `20260415-093000-123` |
| `Guid` | `3f7a1b2c` (8-char) |
| `ProcessId` | `12345` |
| `Custom` | result of `CustomSessionIdProvider()` |

---

## `DatabaseLoggerOptions`

Configuration for `DatabaseLogger`, loaded from the `[DatabaseLogger]` config section.

| Config key | Property | Default | Description |
|------------|----------|---------|-------------|
| `bEnabled` | `Enabled` | `true` | Whether the database logger is active |
| `sDatabaseSection` | `DatabaseSection` | `null` | Named config section for database connection |
| `sTableName` | `TableName` | `"__ApplicationLogs__"` | Table to write log entries into |
| `bAutoCreateTable` | `AutoCreateTable` | `true` | Create the table if it does not exist |
| `eMinLogLevel` | `MinLogLevel` | `All` | Minimum level written to the database |

The table name is validated against `^[A-Za-z_][A-Za-z0-9_]*$`. Invalid names disable the logger.
