# Logging Module

## Overview

The **Logging** module provides a comprehensive, thread-safe logging framework with support for multiple output targets (console, file, session-based). It's designed for enterprise applications requiring detailed diagnostics and operational logging.

---

## Purpose & Design Philosophy

### Why This Module?

Enterprise applications need logging that:
1. **Goes everywhere** - Multiple simultaneous outputs (console + file)
2. **Handles volume** - High-frequency logging without blocking
3. **Is configurable** - Change log levels and outputs at runtime
4. **Tracks context** - Correlate related operations
5. **Survives crashes** - Persists before application failure

The Logging module provides all this through:
- **Composite pattern** - Multiple loggers working together
- **Singleton access** - Simple `Log.Info()` static API
- **Thread-safe** - Safe for multi-threaded applications
- **Configurable** - Loaded from configuration file
- **Extensible** - Easy to add custom logger implementations

### Architecture

```
Log.Info("message")
   ↓
[CompositeLogger dispatches to all registered loggers]
        ↓→ ConsoleLogger (output to console)
        ↓→ FileLogger (write to log file)
        ↓→ SessionFileLogger (rotate files per session)
        ↓
[Each logger filters by LogLevel]
    ↓
[Output to appropriate target]
```

---

## Key Classes

### `Log`
**Purpose:** Main entry point providing static logging methods (singleton pattern).

**Static Methods:**
```csharp
// Log level specific methods
Log.Trace(string message);
Log.Debug(string message);
Log.Verbose(string message);
Log.Info(string message);
Log.Notice(string message);
Log.Warning(string message);
Log.Warning(string message, Exception ex);
Log.Error(string message);
Log.Error(Exception ex);
Log.Error(string message, Exception ex);
Log.Critical(string message);
Log.Critical(Exception ex);
Log.Critical(string message, Exception ex);
Log.Fatal(string message);
Log.Fatal(Exception ex);
Log.Fatal(string message, Exception ex);

// Control methods
Log.EnableConsoleLogging();
Log.DisableConsoleLogging();
Log.EndSession();  // For session-based logging

// Properties
static bool IsInitialized { get; }
static bool IsConsoleLoggingEnabled { get; }
static LogLevel LogLevel { get; set; }
static ConsoleLogger Console { get; }
static SessionFileLogger SessionLogger { get; }
static string CurrentLogFile { get; }
```

**Features:**
- Lazy singleton initialization
- Static facade for ease of use
- Automatic logger composition
- Configurable at runtime

### `CompositeLogger`
**Purpose:** Combines multiple `ILogger` implementations into a single composite.

**Responsibilities:**
- Maintain list of child loggers
- Dispatch log messages to all children
- Track minimum log level across all loggers
- Support dynamic logger addition/removal

**Key Methods:**
```csharp
void AddLogger(ILogger logger);
void RemoveLogger(ILogger logger);
IList<ILogger> Loggers { get; }
```

### `BaseLogger`
**Purpose:** Abstract base class for all logger implementations.

**Responsibilities:**
- Enforce common logger interface
- Provide default implementation
- Handle log level filtering
- Format log messages

**Key Properties:**
```csharp
string Name { get; set; }
LogLevel MinLogLevel { get; set; }
bool ShowMessageOnly { get; set; }
```

### `ConsoleLogger`
**Purpose:** Logs messages to the console (stdout/stderr).

**Features:**
- Color-coded output by log level
- Optional message-only output
- Enable/disable at runtime
- Thread-safe console writes

**Usage:**
```csharp
var consoleLogger = new ConsoleLogger
{
    Name = "Console",
    MinLogLevel = LogLevel.Verbose,
    ShowMessageOnly = false
};
compositeLogger.AddLogger(consoleLogger);
```

### `FileLogger`
**Purpose:** Logs messages to a file with optional rotation.

**Features:**
- Automatic file rotation
- Timestamped log entries
- Thread-safe file writes
- Empty file handling (clears on startup)
- Path validation

**Usage:**
```csharp
var fileLogger = new FileLogger("./logs/app.log")
{
    Name = "File",
    MinLogLevel = LogLevel.Info
};
compositeLogger.AddLogger(fileLogger);
```

### `SessionFileLogger`
**Purpose:** Creates separate log files for each application session.

**Features:**
- Separate file per session
- Session ID in filename
- Timestamp tracking
- Automatic session management
- Clean session end handling

**Usage:**
```csharp
var sessionLogger = new SessionFileLogger(
    "./logs", 
    new SessionFileLoggerOptions 
    { 
        FileNameFormat = "app_{timestamp}.log" 
  }
)
{
    Name = "SessionFile",
    MinLogLevel = LogLevel.Debug
};
```

### `NullLogger`
**Purpose:** No-op logger for testing and disabled logging scenarios.

**Usage:**
```csharp
// Disable logging without changing code
compositeLogger.AddLogger(new NullLogger());
```

### `StaticLoggerAdapter`
**Purpose:** Adapts the static `Log` API to an `ILogger` interface.

**Usage:** Internal - wraps the static Log for use in interfaces expecting `ILogger`.

---

## Models & Enums

### `LogLevel`
**Purpose:** Enumeration of logging severity levels.

**Values (in order of severity):**
```csharp
public enum LogLevel
{
    None = 0, // Logging disabled
    Fatal = 1,     // System failure, immediate shutdown
    Critical = 2,// Critical error, system unstable
    Error = 3,     // Error condition, operation failed
    Warning = 4,   // Warning, unusual but recoverable
    Notice = 5,    // Important information
    Info = 6, // Informational messages
    Verbose = 7,   // Verbose details
    Debug = 8,     // Debug information
    Trace = 9,     // Trace execution path
    All = 10       // All messages (lowest filter)
}
```

**Filtering:**
- Logger only outputs if `message.Level <= Logger.MinLogLevel`
- `MinLogLevel = LogLevel.Warning` logs Warning + Error + Critical + Fatal
- `MinLogLevel = LogLevel.Debug` logs Debug + Info + Warning + Error + Critical + Fatal
- `MinLogLevel = LogLevel.None` disables the logger

### `LogEntry`
**Purpose:** Represents a single log entry with metadata.

**Properties:**
```csharp
DateTime Timestamp { get; set; }
LogLevel Level { get; set; }
string Message { get; set; }
Exception? Exception { get; set; }
string LoggerName { get; set; }
int ThreadId { get; set; }
```

### `LogSettings`
**Purpose:** Configuration for logging loaded from INI file.

**Properties:**
```csharp
string LogFilePath { get; set; }          // Path to log file
LogLevel TraceLogLevel { get; set; }      // Min log level
bool ClearLogOnStartup { get; set; }      // Clear file on app start
bool UseSessionLogging { get; set; }      // Use session-based files
```

### `FileLoggerOptions`
**Purpose:** Configuration for `FileLogger`.

**Properties:**
```csharp
int MaxFileSizeKB { get; set; }   // Rotation threshold
int MaxBackupFiles { get; set; }          // How many rotated files to keep
string DateFormat { get; set; }     // Log entry date format
```

### `SessionFileLoggerOptions`
**Purpose:** Configuration for `SessionFileLogger`.

**Properties:**
```csharp
string FileNameFormat { get; set; }       // Pattern for session filenames
bool AppendSessionId { get; set; }        // Include session ID in filename
int SessionTimeoutMinutes { get; set; }   // Auto-end session after timeout
```

### `AsyncOptions`
**Purpose:** Configuration for asynchronous logging operations.

**Properties:**
```csharp
bool UseAsyncWrite { get; set; }          // Queue writes to background thread
int QueueCapacity { get; set; }           // Max pending writes
```

### `CorrelationContext`
**Purpose:** Track related operations across log entries.

**Usage:**
```csharp
using (var context = new CorrelationContext("request-123"))
{
    Log.Info("Processing request");  // Logs include correlation ID
    await ProcessAsync();
 Log.Info("Request complete");    // Same correlation ID
}
```

### `ILogger`
**Purpose:** Interface for all logger implementations.

**Key Methods:**
```csharp
void Log(LogLevel level, string message, Exception? ex = null);
void LogTrace(string message);
void LogDebug(string message);
void LogVerbose(string message);
void LogInfo(string message);
void LogNotice(string message);
void LogWarning(string message);
void LogError(string message);
void LogError(Exception ex);
void LogCritical(string message);
void LogFatal(string message);
void Dispose();
```

---

## Usage Patterns

### Basic Initialization

The `Log` class auto-initializes on first use:

```csharp
// Application startup (optional - explicit initialization)
Configuration.Initialize();  // Loads logging config from INI

// Later in code - uses auto-initialized singleton
Log.Info("Application started");
```

### Logging at Different Levels

```csharp
// Trace - Most detailed, rarely enabled in production
Log.Trace("Entering method CalculateTotal");

// Debug - Diagnostic information
Log.Debug("Loaded 150 records from database");

// Verbose - Detailed but important information
Log.Verbose("Processing batch starting");

// Info - Standard informational messages
Log.Info("Server listening on port 8080");

// Notice - Important events
Log.Notice("Configuration reloaded from disk");

// Warning - Potentially problematic situations
Log.Warning("Database connection pool at 95% capacity");

// Error - Error conditions that operation can recover from
Log.Error("Failed to process request", exception);

// Critical - Critical errors indicating system instability
Log.Critical("Database connection lost", exception);

// Fatal - Fatal errors requiring immediate shutdown
Log.Fatal("Out of memory - system shutdown", exception);
```

### Exception Logging

```csharp
try
{
    // Some operation
    var result = DangerousOperation();
}
catch (ApplicationException ex)
{
    // Log with context
    Log.Error("Operation failed due to application error", ex);
}
catch (Exception ex)
{
    // Generic error handler
    Log.Critical("Unexpected error occurred", ex);
}
```

### Runtime Log Level Control

```csharp
// Increase verbosity for troubleshooting
Log.LogLevel = LogLevel.Debug;
Log.Debug("Detailed debugging now enabled");

// Later, reduce back to normal
Log.LogLevel = LogLevel.Info;
```

### Disabling Console Output

```csharp
// In production, disable console logging to file
Log.DisableConsoleLogging();

// Later, if needed
Log.EnableConsoleLogging();
```

### Session Management

```csharp
// Start of application
Application.Run();

// At application shutdown
Log.EndSession();  // Finalize session log file
```

---

## INI Configuration

```ini
[Logging]
; Minimum log level (Trace=9, Debug=8, Verbose=7, Info=6, Notice=5, Warning=4, Error=3, Critical=2, Fatal=1)
Level=Info

; Path to log file
FilePath=.\logs\app.log

; Clear log on startup
ClearLogOnStartup=false

; Use session-based logging (separate file per session)
UseSessionLogging=false

[FileLogger]
; Maximum log file size before rotation (KB)
MaxFileSizeKB=10240

; Number of rotated files to keep
MaxBackupFiles=5

; Date format for log entries
DateFormat=yyyy-MM-dd HH:mm:ss.fff

[SessionFileLogger]
; Format for session log filenames
FileNameFormat=app_{timestamp}.log

; Append session ID to filename
AppendSessionId=true

; Auto-end session after timeout (minutes)
SessionTimeoutMinutes=1440
```

---

## File Organization

### `Log.cs`
Main static logger providing the public API.

**Contains:**
- Static facade methods
- Singleton initialization
- Configuration loading
- Console logger toggle

### `BaseLogger.cs`
Abstract base class for logger implementations.

### `ConsoleLogger.cs`
Logs to console output.

### `FileLogger.cs`
Logs to files with rotation.

### `SessionFileLogger.cs`
Logs to session-specific files.

### `CompositeLogger.cs`
Combines multiple loggers.

### `NullLogger.cs`
No-op logger for testing.

### `StaticLoggerAdapter.cs`
Adapts static API to ILogger interface.

### `Models/`
- `LogEntry.cs` - Individual log entry
- `LogLevel.cs` - Severity enumeration
- `LogSettings.cs` - Configuration model
- `FileLoggerOptions.cs` - File logger configuration
- `SessionFileLoggerOptions.cs` - Session logger configuration
- `AsyncOptions.cs` - Async logging configuration
- `CorrelationContext.cs` - Operation correlation

---

## Thread Safety

The Logging module is fully thread-safe:

### 1. **Logger Composition**
- ConcurrentBag holds loggers
- Thread-safe iteration during logging
- Multiple threads can log simultaneously

### 2. **Console Output**
- Lock protecting System.Console writes
- Prevents interleaved/corrupted output

### 3. **File I/O**
- ReaderWriterLockSlim for file access
- Multiple readers allowed
- Exclusive access for writes
- Prevents file corruption

### 4. **Log Level Changes**
- Atomic operations through properties
- Changes visible to all threads immediately

**Example - Concurrent Logging:**
```csharp
// Safe - multiple threads can log simultaneously
Parallel.For(0, 100, i => 
{
    Log.Info($"Message {i} from thread {Thread.CurrentThread.ManagedThreadId}");
});
```

---

## Performance Considerations

### 1. **Filtering**
- Filter by log level BEFORE formatting
- Expensive operations only for enabled levels

```csharp
// Good - only formats if Debug enabled
if (Log.LogLevel <= LogLevel.Debug)
    Log.Debug($"Expensive calculation: {ExpensiveDebugInfo()}");

// Better - use conditional compilation
#if DEBUG
Log.Debug($"Debug info");
#endif
```

### 2. **File I/O**
- File writes are synchronous by default
- Consider async options for high-volume logging
- Batch related operations

### 3. **String Formatting**
- Avoid string concatenation in hot paths
- Use interpolation for better performance
- Consider structured logging for complex data

```csharp
// Inefficient
Log.Info("User " + user.Id + " logged in from " + ipAddress);

// Better
Log.Info($"User {user.Id} logged in from {ipAddress}");
```

---

## Error Handling

### Fallback Logger

If file logging fails (permissions, disk full, etc.), logs are written to alternate temporary file:

```csharp
// If file logger fails, writes to:
// %TEMP%\LogError.{guid}.log
```

This ensures logs aren't lost even if primary logger fails.

### Exception Recovery

```csharp
try
{
    Log.Info("Some message");
}
catch (Exception ex)
{
    // Even if logging fails, application continues
    // Writes to alternate logger instead
}
```

---

## Testing

The module includes comprehensive tests covering:
- All log levels
- Exception logging with stack traces
- File and console output verification
- Concurrent logging from multiple threads
- Log level filtering
- Configuration loading
- Session management
- Resource cleanup

See `Toolkit.Modern.Tests/Unit/Logging/` for test examples.

---

## Extension Points

### Custom Logger Implementation

```csharp
public class DatabaseLogger : BaseLogger
{
    private readonly string _connectionString;
    
    public DatabaseLogger(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public override void Log(LogLevel level, string message, Exception? ex = null)
    {
     if (level > MinLogLevel) return;
        
   // Write to database
        using (var conn = new SqlConnection(_connectionString))
     {
            // Insert into log table
        }
    }
}

// Usage
Log.Instance.AddLogger(new DatabaseLogger(connString));
```

### Custom Formatting

Override BaseLogger to implement custom formatting:

```csharp
public class JsonLogger : BaseLogger
{
    public override void Log(LogLevel level, string message, Exception? ex = null)
    {
        if (level > MinLogLevel) return;
        
        var entry = new
{
 Timestamp = DateTime.UtcNow,
            Level = level.ToString(),
      Message = message,
            Exception = ex?.ToString()
        };
    
   var json = JsonConvert.SerializeObject(entry);
        Console.WriteLine(json);
    }
}
```

---

## Common Patterns

### Startup Logging

```csharp
static void Main()
{
    Log.Info("=== Application Starting ===");
    Log.Info($"Version: {GetVersion()}");
    Log.Info($"Runtime: {RuntimeInformation.FrameworkDescription}");
    
    try
  {
        Initialize();
        Run();
    }
 catch (Exception ex)
    {
        Log.Fatal("Application crashed", ex);
     throw;
    }
    finally
    {
        Log.Info("=== Application Shutdown ===");
        Log.EndSession();
  }
}
```

### Diagnostic Logging

```csharp
public void PerformanceTrace(string operation)
{
    var sw = Stopwatch.StartNew();
  
    Log.Verbose($"Starting {operation}");
    try
    {
        // Do work
  }
    finally
    {
      sw.Stop();
        if (sw.ElapsedMilliseconds > 1000)
            Log.Warning($"{operation} took {sw.ElapsedMilliseconds}ms");
     else
Log.Verbose($"{operation} completed in {sw.ElapsedMilliseconds}ms");
    }
}
```

### Context Tracing

```csharp
public async Task ProcessRequest(string requestId)
{
    Log.Info($"[{requestId}] Processing request");
    
    try
    {
   var result = await FetchData(requestId);
    Log.Info($"[{requestId}] Data retrieved successfully");
        return result;
    }
    catch (Exception ex)
    {
        Log.Error($"[{requestId}] Request processing failed", ex);
        throw;
 }
}
```

---

## Summary

The Logging module provides a production-ready logging framework with:

**Key Strengths:**
- ✓ Simple static API (`Log.Info()`)
- ✓ Thread-safe for concurrent applications
- ✓ Multiple simultaneous outputs
- ✓ Configurable log levels
- ✓ Runtime control and adjustments
- ✓ Exception and stack trace logging
- ✓ Extensible architecture
- ✓ Session-based log file management

**Best For:**
- Enterprise applications
- Multi-threaded services
- Applications requiring detailed diagnostics
- Systems with complex operational logging needs

**Not Ideal For:**
- Simple scripts with minimal logging needs
- Applications requiring centralized log aggregation (use third-party solutions)
- High-frequency logging with extreme performance constraints

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                            | Description                |
|---------------------------------------------------|----------------------------|
| **[CLI](../CommandLine/readme.md)**               | Command-line parsing       |
| **[Configuration](../Configuration/readme.md)**   | INI-based configuration    |
| **[Core](../Core/readme.md)**                     | Core utilities             |
| **[Data](../Data/readme.md)**                     | Database & file processing |
| **[DataStructures](../DataStructures/readme.md)** | Collections & utilities    |
| **[JSON](../Json/readme.md)**                     | Delta serialization        |
| **[Mail](../Mail/readme.md)**                     | Email processing           |
| **[Net](../Net/readme.md)**                       | Network file transfers     |
| **[Security](../Security/readme.md)**             | Encryption & security      |
| **[Utils](../Utilities/readme.md)**               | General utilities          |
