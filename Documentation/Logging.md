# Logging Framework

A flexible, extensible, and thread-safe logging framework for .NET applications. This framework provides comprehensive logging capabilities with support for multiple output destinations, log levels, and advanced features like correlation IDs and asynchronous logging.

## Features

- Multiple logging destinations (Console, File, and Composite logging)
- Configurable log levels (Debug, Verbose, Info, Warning, Error, Critical)
- Thread-safe logging operations
- Asynchronous logging support
- Daily log file rotation
- Size-based log file rotation
- Log file retention policies
- Correlation ID tracking
- Exception logging with stack traces
- Support for both synchronous and asynchronous logging
- Automatic log file cleanup based on retention policy

## Core Components

### Log Levels

The framework supports the following log levels in order of increasing severity:

1. Debug - For development-time debugging
2. Verbose - Detailed information
3. Info - General operational information
4. Warning - Potential issues that don't stop execution
5. Error - Issues that affect functionality but don't crash the application
6. Critical - Severe issues requiring immediate attention
7. None - Used to disable logging

### Logger Types

#### BaseLogger

An abstract base class that implements core logging functionality and serves as the foundation for all logger implementations. It provides:

- Basic logging infrastructure
- Log level filtering
- Standard logging methods for each severity level
- Thread ID tracking

#### FileLogger

A robust file-based logger with advanced features:

- Daily log file rotation
- Size-based log file rotation
- Configurable retention policies
- Asynchronous logging support
- Automatic directory creation
- File locking mechanisms for thread safety

#### ConsoleLogger

A console-based logger with formatting options:

- Color-coded output based on log level
- Configurable timestamp display
- Optional stack trace display
- Message-only mode support

#### CompositeLogger

A logger that can combine multiple loggers:

- Parallel logging support
- Configurable error handling
- Dynamic logger management
- Flexible logger combinations

## Usage Examples

### Basic Logging

```csharp
// Initialize the logger
var log = Log.Instance;

// Log messages at different levels
Log.Debug("Debugging information");
Log.Info("General information");
Log.Warning("Warning message");
Log.Error("Error occurred", exception);
Log.Critical("Critical error", exception);
```

### Console Logging Configuration

```csharp
var consoleLogger = Log.Console;
consoleLogger.ShowTimestamp = true;
consoleLogger.ShowStackTrace = true;
consoleLogger.ShowMessageOnly = false;
```

### File Logger Configuration

```csharp
var options = new FileLoggerOptions
{
    UseDaily = true,
    DateFormat = "yyyy-MM-dd",
    MaxFileSizeMB = 10,
    RetentionDays = 30,
    UseAsyncLogging = true,
    AsyncQueueSize = 1000
};

var fileLogger = new FileLogger("path/to/logfile.log", options);
```

## Configuration

The framework can be configured through configuration files:

```xml
<configuration>
  <Logging>
    <LogFilePath>logs/application.log</LogFilePath>
    <TraceLogLevel>Info</TraceLogLevel>
    <ClearLogOnStartup>false</ClearLogOnStartup>
  </Logging>
  <FileLogger>
    <UseDaily>true</UseDaily>
    <DateFormat>yyyy-MM-dd</DateFormat>
    <MaxFileSizeMB>10</MaxFileSizeMB>
    <RetentionDays>30</RetentionDays>
    <UseAsyncLogging>true</UseAsyncLogging>
    <AsyncQueueSize>1000</AsyncQueueSize>
  </FileLogger>
</configuration>
```

## Best Practices

1. **Log Level Selection**
   - Use Debug for temporary debugging information
   - Use Verbose for detailed troubleshooting
   - Use Info for normal operational events
   - Use Warning for potential issues
   - Use Error for actual errors that affect operation
   - Use Critical for severe issues requiring immediate attention

2. **Performance Considerations**
   - Enable async logging for high-throughput scenarios
   - Configure appropriate log rotation and retention policies
   - Use correlation IDs for tracking related log entries
   - Disable console logging in production environments

3. **File Management**
   - Configure appropriate retention policies
   - Monitor log file sizes
   - Use daily rotation for easier log management
   - Set up appropriate backup procedures

## Thread Safety

The framework is designed to be thread-safe:
- File operations are protected by locks
- Console output is synchronized
- Composite logging handles parallel execution
- Async logging queues are thread-safe

## Error Handling

The framework includes robust error handling:
- Failed log attempts are caught and handled
- Alternative logging paths are provided
- Disk full scenarios are managed
- Queue overflow protection is implemented

## Requirements

- .NET Framework 4.8
- Windows environment
- Write permissions for log file locations
- Adequate disk space for log files
