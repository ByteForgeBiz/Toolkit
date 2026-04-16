# Logging — Interfaces

This directory contains the core `ILogger` interface that all logging implementations must satisfy.

---

## `ILogger`

Defines the contract for any logging target in the ByteForge logging system.

```csharp
public interface ILogger
{
    string Name { get; set; }
    LogLevel MinLogLevel { get; set; }

    void Log(LogLevel level, string message, Exception? exception = null);

    void LogTrace(string message, Exception? exception = null);
    void LogDebug(string message, Exception? exception = null);
    void LogVerbose(string message, Exception? exception = null);
    void LogInfo(string message, Exception? exception = null);
    void LogNotice(string message, Exception? exception = null);
    void LogWarning(string message, Exception? exception = null);
    void LogError(string message, Exception? exception = null);
    void LogCritical(string message, Exception? exception = null);
    void LogFatal(string message, Exception? exception = null);
}
```

### Members

| Member | Description |
|--------|-------------|
| `Name` | Human-readable identifier for this logger (e.g., `"Console"`, `"File"`) |
| `MinLogLevel` | Minimum `LogLevel` that this logger will process; entries below this level are dropped |
| `Log(level, message, exception)` | Core method; all level-specific methods delegate to this |
| `LogTrace` through `LogFatal` | Convenience methods for each `LogLevel` value |

### Level Filtering

Loggers are expected to drop entries whose level is below `MinLogLevel`:

```csharp
// Filtering rule used by BaseLogger:
if (level < MinLogLevel) return;
```

`LogLevel` uses positional ordering where lower ordinal = more verbose (Trace=0) and higher ordinal = more severe (Fatal=8), with `None = int.MaxValue` (disables all) and `All = int.MinValue` (enables all).

### Implementing the Interface

The recommended approach is to extend `BaseLogger` rather than implement `ILogger` directly. `BaseLogger` handles level filtering, `LogEntry` construction, and the `Properties` dictionary (LoggerName, ThreadId). Subclasses only need to implement `RecordLogEntry(LogEntry)`.

Direct `ILogger` implementation is only appropriate for special cases (see `NullLogger`).

```csharp
// Preferred: extend BaseLogger
public class MyLogger : BaseLogger
{
    public MyLogger() : base("MyLogger") { }

    protected internal override void RecordLogEntry(LogEntry entry)
    {
        // Write entry.Message, entry.Level, entry.Exception, etc.
    }
}

// Direct implementation (only for special cases like NullLogger)
public class NullLogger : ILogger
{
    public string Name { get; set; } = "Null";
    public LogLevel MinLogLevel { get; set; } = LogLevel.None;
    public void Log(LogLevel level, string message, Exception? exception = null) { }
    public void LogTrace(string message, Exception? exception = null) { }
    // ... all other methods are no-ops
}
```

### Registration

Register any `ILogger` with the singleton `CompositeLogger`:

```csharp
Log.Instance.AddLogger(new MyLogger());
Log.Instance.RemoveLogger("MyLogger");  // Remove by name
```
