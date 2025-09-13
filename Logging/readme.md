# ByteForge Logging Framework

A comprehensive, extensible logging system designed for .NET 4.8 applications with advanced features including session management, file rotation, async operations, correlation tracking, and multi-target logging capabilities.

## 🚀 Key Features

### Core Logging Capabilities
- **Multiple Logger Types**: Console, File, Session-based, and Composite loggers
- **Rich Log Levels**: 10 distinct levels from Trace to Fatal with None/All controls
- **Exception Handling**: Full stack trace logging with nested exception support
- **Thread Safety**: Built-in synchronization for multi-threaded applications
- **Correlation IDs**: Automatic correlation tracking across log entries

### Advanced File Management
- **Daily File Rotation**: Automatic daily log file creation with custom date formats
- **Size-based Rotation**: Configurable file size limits with automatic rollover
- **Retention Policies**: Automatic cleanup of old log files based on age
- **Session Logging**: Unique log files per application run with session metadata
- **Async Logging**: High-performance asynchronous logging with configurable queue sizes

### Enterprise Features
- **Configuration-Driven**: Full integration with ByteForge configuration system
- **Web Application Support**: Automatic detection and optimization for web apps
- **Composite Logging**: Multi-target logging with parallel processing capabilities
- **Fallback Mechanisms**: Alternate logging paths when primary logging fails
- **Performance Optimized**: Minimal overhead with async processing options

## 🧱 Architecture Overview

### Logger Hierarchy
```
ILogger (Interface)
└── BaseLogger (Abstract Base)
    ├── ConsoleLogger (Console output with color coding)
    ├── FileLogger (File-based with rotation/async)
    |   └── SessionFileLogger (Session-specific file logging)
    └── CompositeLogger (Multi-target aggregation)
```

### Log Levels (Ascending Severity)
- **Trace**: Extremely detailed execution tracing
- **Debug**: Development debugging information
- **Verbose**: Detailed operational information
- **Info**: General application flow information
- **Notice**: Noteworthy but normal events
- **Warning**: Potential issues that don't stop execution
- **Error**: Failures in current operation
- **Critical**: Severe errors requiring immediate attention
- **Fatal**: Unrecoverable errors causing termination
- **Special**: None (disable), All (enable all)

## 🧪 Quick Start Examples

### Basic Static Logging
```csharp
// Simple logging through static Log class
Log.Info("Application started successfully");
Log.Warning("Configuration file missing, using defaults");
Log.Error("Database connection failed", ex);

// Enable/disable console output
Log.EnableConsoleLogging();
Log.DisableConsoleLogging();

// Control log levels globally
Log.LogLevel = LogLevel.Warning; // Only warnings and above
```

### File Logger with Advanced Options
```csharp
var fileLogger = new FileLogger("app.log", new FileLoggerOptions
{
    UseDaily = true,                    // Create daily log files
    DateFormat = "yyyy-MM-dd",          // Custom date format
    MaxFileSizeMB = 50,                 // Size-based rotation
    RetentionDays = 30,                 // Keep 30 days of logs
    UseAsyncLogging = true,             // High-performance async
    AsyncQueueSize = 2000               // Queue size for async
});

fileLogger.LogInfo("File logger initialized");
```

### Session-Based Logging
```csharp
var sessionLogger = new SessionFileLogger("app.log", 
    new SessionFileLoggerOptions
    {
        SessionIdFormat = SessionIdFormat.TimestampWithMilliseconds,
        FileNamingPattern = "{basename}_{timestamp}_{pid}",
        WriteSessionHeader = true,       // App metadata at start
        WriteSessionFooter = true,       // Duration info at end
        CleanupOldSessions = true,       // Auto-cleanup old files
        RetentionDays = 14
    });

// Session automatically ends on disposal
sessionLogger.LogInfo("Session logging active");
```

### Composite Multi-Target Logging
```csharp
var consoleLogger = new ConsoleLogger { ShowTimestamp = true };
var fileLogger = new FileLogger("debug.log");
var sessionLogger = new SessionFileLogger("session.log");

var composite = new CompositeLogger(consoleLogger, fileLogger, sessionLogger)
{
    EnableMultiThreading = true         // Parallel logging
};

composite.LogInfo("Message goes to all three targets");
```

### Console Logger Customization
```csharp
var console = new ConsoleLogger
{
    ShowMessageOnly = false,            // Show full formatting
    ShowTimestamp = true,               // Include timestamps
    ShowStackTrace = true,              // Show exception stacks
    MinLogLevel = LogLevel.Debug        // Filter level
};

console.LogError("Console error with full details", ex);
```

## ⚙️ Configuration Integration

### Configuration File Setup
```json
{
  "Logging": {
    "sLogFile": "C:\\Logs\\MyApp.log",
    "eTraceLogLevel": "Info",
    "bClearLogOnStartup": false,
    "bUseSessionLogging": true
  },
  "FileLogger": {
    "UseDaily": true,
    "MaxFileSizeMB": 100,
    "RetentionDays": 30,
    "UseAsyncLogging": true,
    "AsyncQueueSize": 1000
  },
  "SessionLogger": {
    "SessionIdFormat": "ProcessId",
    "FileNamingPattern": "{basename}_{sessionid}",
    "WriteSessionHeader": true,
    "CleanupOldSessions": true,
    "RetentionDays": 7
  }
}
```

### Accessing Configured Loggers
```csharp
// Static logger uses configuration automatically
var options = Configuration.GetSection<LogOptions>("Logging");
Log.Info($"Logging to: {Log.CurrentLogFile}");

// Access session features if enabled
if (Log.SessionLogger != null)
{
    Log.Info($"Session ID: {Log.SessionLogger.SessionId}");
    Log.EndSession(); // Graceful session termination
}
```

## 🎯 Advanced Features

### Correlation Context Tracking
```csharp
// Correlation IDs automatically generated for composite loggers
var context = CorrelationContext.New();
// All related log entries share the same correlation ID
```

### Exception Handling with Nested Details
```csharp
try
{
    // Complex operation
}
catch (Exception ex)
{
    // Logs full exception chain with data and stack traces
    Log.Fatal("Critical system failure", ex);
}
```

### Async Queue Management
```csharp
var options = new FileLoggerOptions
{
    UseAsyncLogging = true,
    AsyncQueueSize = 5000              // Handle high-volume logging
};

// Logger handles queue overflow gracefully
```

### Web Application Optimization
```csharp
// Framework automatically detects web context
// Disables multi-threading in web apps for safety
// Optimizes for IIS/ASP.NET hosting scenarios
```

## ✅ Best Practices

### Performance Optimization
- Use **async logging** for high-frequency applications
- Set appropriate **queue sizes** based on expected log volume
- Enable **daily rotation** for long-running applications
- Use **size-based rotation** for applications with variable log volumes

### Log Level Strategy
- **Trace/Debug**: Use sparingly, disable in production
- **Verbose**: Detailed troubleshooting information
- **Info**: Normal application flow and business events
- **Warning**: Potential issues that should be monitored
- **Error/Critical/Fatal**: Always investigate these immediately

### File Management
- Configure **retention policies** to prevent disk space issues
- Use **session logging** for applications that restart frequently
- Enable **cleanup** features to maintain log directories
- Monitor log file sizes in production environments

### Exception Logging
- Always include the **original exception** object
- Use appropriate **log levels** based on severity
- Consider **correlation IDs** for tracking related failures
- Implement **fallback logging** for critical systems

## 🔧 Integration Points

---

## 📚 Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [CommandLine](../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |

### Disposal and Cleanup
```csharp
// Always dispose loggers properly
using (var logger = new FileLogger("app.log"))
{
    logger.LogInfo("Processing started");
    // Automatic cleanup on disposal
}

// Static logger cleanup
Log.EndSession();           // End session logging
Log.Instance.Dispose();     // Clean shutdown
```

## 📊 Performance Characteristics

- **Synchronous Logging**: ~10-50µs per log entry
- **Asynchronous Logging**: ~1-5µs per log entry (queued)
- **Memory Usage**: ~1-10MB for typical queue sizes
- **File I/O**: Batched writes for optimal performance
- **Thread Safety**: Lock-free where possible, minimal contention

This logging framework provides enterprise-grade capabilities while maintaining simplicity for basic use cases, making it suitable for everything from small utilities to large-scale applications.