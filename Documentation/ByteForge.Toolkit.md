# ByteForge.Toolkit

A comprehensive .NET utility library targeting .NET Framework 4.8, providing a rich set of tools and components for building robust applications. ByteForge.Toolkit includes command-line parsing, configuration management, database access, CSV processing, data structures, logging, encryption, and various utility functions.

## Features Overview

- **Command-Line Interface**: Attribute-based command parsing with support for nested commands and options
- **Configuration Management**: INI file handling with section support and type-safe configuration
- **Database Access**: SQL Server 2000 and ODBC support with comprehensive security features
- **CSV Processing**: Robust CSV file handling with format detection and streaming capabilities
- **Data Structures**: Efficient implementations of common data structures like AVL trees
- **Logging Framework**: Flexible logging system with multiple outputs and rotation policies
- **Encryption**: AES encryption implementation with key generation and Galois Field operations
- **Utility Classes**: Various helper functions for console, datetime, IO, email, and performance timing

## Components

### Command-Line Interface

Build sophisticated command-line applications using attribute-based definitions:

```csharp
[Command("Creates a new project", "create", "new")]
public class CreateCommand
{
    [Command("Creates a new console project", "console")]
    public void CreateConsole(
        [Option("The name of the project")] string name,
        [Option("The output directory")] string output = null)
    {
        // Implementation
    }
}
```

### Configuration Management

Handle application settings with strong typing and section support:

```csharp
// Initialize configuration
Configuration.Initialize("settings.ini");

// Access typed configuration sections
var dbSettings = Configuration.GetSection<DatabaseSettings>();
```

### Database Access

Secure and efficient database operations for SQL Server 2000:

```csharp
var db = new DBAccess();

// Execute parameterized queries
bool success = db.ExecuteQuery(
    "UPDATE Users SET Status = @status WHERE ID = @id",
    new object[] { "Active", 1 }
);

// Async operations
var count = await db.GetValueAsync<int>("SELECT COUNT(*) FROM Users");
```

### CSV Processing

Handle CSV files with automatic format detection:

```csharp
// Simple usage
CSVReader.ReadFile("data.csv", (headers, values, rawLine) => {
    foreach (var value in values)
        Console.WriteLine(value);
});

// With progress tracking
var reader = new CSVReader();
reader.Progress += (sender, e) => 
    Console.WriteLine($"Progress: {e.Progress}%");
reader.ReadFile("data.csv");
```

### Data Structures

Efficient implementations for common programming needs:

```csharp
var tree = new BinarySearchTree<int>();
tree.Insert(5);
tree.Insert(3);
tree.Insert(7);

// URL handling
string url = Url.Combine("http://example.com", "api", "v1");
```

### Logging Framework

Comprehensive logging capabilities:

```csharp
var log = Log.Instance;

// Log at different levels
Log.Info("General information");
Log.Warning("Warning message");
Log.Error("Error occurred", exception);

// Configure file logging
var options = new FileLoggerOptions
{
    UseDaily = true,
    MaxFileSizeMB = 10,
    RetentionDays = 30,
    UseAsyncLogging = true
};

var fileLogger = new FileLogger("path/to/logfile.log", options);
```

### Encryption

Secure data encryption using AES:

```csharp
var encryptor = new Encryptor(seed: 12345, size: 32);

string encrypted = encryptor.Encrypt("Sensitive data");
string decrypted = encryptor.Decrypt(encrypted);
```

### Utility Functions

Various helper functions for common tasks:

```csharp
// Console progress bar
ConsoleUtil.DrawProgressBar(50, "Processing...");

// DateTime parsing
var date = DateTimeParser.Parse("2024-02-11 15:30:00");

// Email sending
MailUtil.SendMail(
    "recipient@example.com",
    "Test Subject",
    "<h1>Hello World!</h1>",
    isHtml: true,
    attachments: new[] { "document.pdf" }
);

// Performance timing
var timer = new TimingUtil();
timer.Time(() => {
    // Code to measure
}, "Operation completed in");
```

## Requirements

- .NET Framework 4.8
- Windows environment
- SQL Server 2000 (for database components)
- Appropriate permissions for features like network access and file operations

## Best Practices

1. **Command-Line Interface**
   - Keep command groups focused and logically organized
   - Provide clear descriptions for commands and options
   - Use meaningful aliases that are easy to remember

2. **Configuration**
   - Initialize the configuration system before use
   - Use strongly-typed sections for related settings
   - Use `[DoNotPersist]` for temporary properties

3. **Database Access**
   - Always use parameterized queries
   - Dispose of connections properly
   - Use async methods for long-running operations
   - Handle exceptions appropriately

4. **Logging**
   - Choose appropriate log levels
   - Configure suitable retention policies
   - Enable async logging for high-throughput scenarios
   - Use correlation IDs for tracking related entries

5. **Security**
   - Use sufficiently large encryption key sizes
   - Store encryption seeds securely
   - Follow secure coding practices
   - Handle sensitive data appropriately

## Credits

Developed by Paulo Santos with contributions from Calude.ai.
