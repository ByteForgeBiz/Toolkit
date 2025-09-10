# ByteForge.Toolkit

ByteForge.Toolkit is a comprehensive .NET utility library designed for .NET Framework 4.8, providing various helper classes for common programming tasks. This toolkit includes utilities for console operations, datetime parsing, IO operations, email handling, performance timing, and general utility functions.

## Components

### ConsoleUtil

A static utility class that provides console-related functionality, particularly focused on progress bar visualization.

Key features:
- `DrawProgressBar`: Renders a customizable progress bar in the console with optional message display
- Automatically handles console width constraints
- Uses Unicode block characters for a visually appealing display

### DateTimeParser

A sophisticated datetime parsing utility that handles various date and time formats flexibly.

Key features:
- Supports multiple date formats (US, UK, ISO, etc.)
- Handles different time formats (12-hour, 24-hour)
- Timezone aware parsing
- Performance optimized with format caching
- Thread-safe implementation using ConcurrentDictionary
- Extensive format support including:
  - Date formats (MM/dd/yyyy, yyyy-MM-dd, etc.)
  - Time formats (HH:mm:ss, hh:mm tt, etc.)
  - Timezone and offset formats

### IOUtils

Provides utility methods for IO operations, particularly focused on path resolution.

Key features:
- `GetUniversalPath`: Converts local paths to universal paths (UNC)
- Handles network drive resolution
- Windows-specific path normalization
- Error logging integration

### MailUtil

A comprehensive email utility for sending emails using SMTP.

Key features:
- Configurable SMTP settings
- Secure credential management with encryption
- Support for HTML and plain text emails
- Multiple recipient handling
- File attachment support
- TLS/SSL support
- Comprehensive error handling and logging

### TimingUtil

A utility class for performance measurement and timing operations.

Key features:
- Measures execution time of synchronous and asynchronous operations
- Flexible logging options
- Support for both void and value-returning operations
- Millisecond precision
- Asynchronous operation support
- Custom logger integration

### Utils

General utility methods for common programming tasks.

Key features:
- String handling utilities
- Synchronous wrapper for async operations
- Common constants

## Requirements

- .NET Framework 4.8
- Windows platform (for some IO operations)

## Usage Examples

### Console Progress Bar
```csharp
for (int i = 0; i <= 100; i++)
{
    ConsoleUtil.DrawProgressBar(i, "Processing...");
    Thread.Sleep(100);
}
```

### DateTime Parsing
```csharp
var dateString = "2024-02-11 15:30:00";
var parsedDate = DateTimeParser.Parse(dateString);
```

### Sending Email
```csharp
MailUtil.SendMail(
    "recipient@example.com",
    "Test Subject",
    "<h1>Hello World!</h1>",
    true,
    new[] { "attachment.pdf" }
);
```

### Performance Timing
```csharp
var timer = new TimingUtil();
timer.Time(() => {
    // Your code here
}, "Operation completed in");
```

## Notes

- This toolkit is optimized for Windows environments
- Some features require specific permissions (e.g., network access for UNC paths)
- Email functionality requires valid SMTP server configuration