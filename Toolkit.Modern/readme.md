# ByteForge.Toolkit.Modern

**The core multi-targeting .NET library providing comprehensive enterprise utilities and tools.**

---

## 🎯 Overview

ByteForge.Toolkit.Modern is the consolidated, primary library of the ByteForge Toolkit solution. It combines enterprise-grade reliability with modern .NET performance, targeting multiple frameworks for maximum compatibility while maintaining a single, clean codebase.

### Target Frameworks
- **.NET Framework 4.8** - Enterprise and legacy system compatibility
- **.NET 8.0** - Current LTS version with performance optimizations  
- **.NET 9.0** - Latest stable version with cutting-edge features

### Architecture Philosophy
The library follows a **modular, enterprise-first design** with:
- **Clean separation of concerns** across functional modules
- **Attribute-based programming** for declarative configuration
- **Thread-safe operations** for concurrent environments
- **Comprehensive error handling** with detailed diagnostics
- **Performance optimization** for large-scale data processing

---

## 📦 Module Architecture

The library is organized into cohesive, focused modules, each with comprehensive documentation:

### 🖥️ **CLI** (`ByteForge.Toolkit.CommandLine`)
**Attribute-based command-line parsing and execution**
- Declarative command definitions using `[Command]` and `[Option]` attributes
- Automatic type conversion and validation
- Built-in help generation and error handling
- Support for subcommands and complex hierarchies

*Key Classes: `CommandParser`, `CommandBuilder`, `ParseResult`, `ConsoleSpinner`*

### ⚙️ **Configuration** (`ByteForge.Toolkit`)
**INI-based configuration with strong typing and advanced features**
- Strongly-typed configuration sections with automatic mapping
- Array and dictionary support via dedicated sections
- Encrypted credential storage for sensitive data
- Globalization and culture-specific formatting
- Thread-safe access with singleton pattern

*Key Classes: `Configuration`, `ConfigSection<T>`, `GlobalizationInfo`*

### 🗄️ **Database** (`ByteForge.Toolkit`)
**Multi-database access layer with enterprise features**
- Support for SQL Server, ODBC, and multiple database engines
- High-performance bulk operations with `BulkDbProcessor<T>`
- Parameterized queries with automatic SQL injection prevention
- Transaction support with rollback capabilities
- Async/await support with cancellation tokens

*Key Classes: `DBAccess` (partial classes), `BulkDbProcessor<T>`, `TypeConverter`*

### 📊 **CSV** (`ByteForge.Toolkit`)
**High-performance CSV processing with flexible formatting**
- Automatic format detection and intelligent parsing
- Progress reporting for large file operations
- Configurable quoting, delimiters, and encoding
- Attribute-based entity mapping with `[CSVColumn]`
- Memory-efficient streaming for large datasets

*Key Classes: `CSVReader`, `CSVWriter`, `CSVFormat`, `CSVRecord`*

### 📁 **Data Structures** (`ByteForge.Toolkit`)
**Specialized collections and data utilities**
- Binary search trees for sorted collections
- URL utilities for path and query string manipulation
- Performance-optimized data structures for specific use cases

*Key Classes: `BinarySearchTree<T>`, `Url`*

### 🔐 **Security** (`ByteForge.Toolkit`)
**Encryption and security utilities**
- AES encryption with secure key management
- Password hashing and validation utilities
- Secure configuration storage for credentials
- Support for various encryption algorithms

*Key Classes: `AESEncryption`, `Encryptor`, `VBMath`*

### 📝 **Logging** (`ByteForge.Toolkit.Logging`)
**Thread-safe, structured logging with multiple outputs**
- Multiple logger implementations (Console, File, Session, Composite)
- Structured logging with correlation contexts
- Async logging options for performance
- Configurable log levels and formatting

*Key Classes: `Log`, `FileLogger`, `ConsoleLogger`, `CompositeLogger`*

### 📧 **Mail** (`ByteForge.Toolkit`)
**Email processing with attachment handling**
- MIME message parsing and processing
- Attachment extraction and management
- Support for various email formats and encodings
- Progress reporting for bulk operations

*Key Classes: `EmailAttachmentHandler`, `MailUtil`, `TempFileAttachment`*

### 🌐 **Net** (`ByteForge.Toolkit`)
**Network file transfer operations**
- File transfer progress monitoring
- Support for multiple protocols via WinSCP integration
- Batch operation support with error handling
- Configurable retry logic and timeouts

*Key Classes: `FileTransferClient`, `FileTransferResult`, `RemoteFileInfo`*

### 🔧 **Utils** (`ByteForge.Toolkit`)
**Comprehensive utility collection**
- **Parsing**: Type-safe parsers for booleans, dates, enums, and custom types
- **String Manipulation**: Advanced string utilities, templates, and formatting
- **I/O Operations**: File and directory utilities with progress monitoring
- **Type Conversion**: Registry-based value converters for flexible data handling
- **Console Utilities**: Enhanced console operations and user interaction

*Key Classes: `BooleanParser`, `DateTimeParser`, `StringUtil`, `IOUtils`, `TemplateProcessor`*

### 📋 **JSON** (`ByteForge.Toolkit`)
**Delta serialization and advanced JSON processing**
- Delta contract resolution for efficient data exchange
- Customizable JSON serialization with change tracking
- Performance-optimized for large object graphs

*Key Classes: `JsonDeltaSerializer`, `DeltaContractResolver`*

---

## 🏗️ Technical Architecture

### Partial Class Design
Large, complex classes are split into focused partial classes for maintainability:

- **`DBAccess`**: Split into Core, Factory, Methods, ScriptExecution, TypeConverter, Logging, and Parameters
- **`BulkDbProcessor<T>`**: Split into Core, Operations, TableManagement, and SqlGeneration
- Each partial class handles a specific functional area

### Attribute-Based Configuration
Extensive use of attributes for declarative programming:

- **CLI commands**: `[Command]` and `[Option]` attributes for command-line parsing
- **Database entities**: `[DBColumn]` attributes for ORM mapping
- **CSV mapping**: `[CSVColumn]` attributes for file processing
- **Configuration sections**: Various attributes for behavior control

### Dependency Management
- **Costura.Fody** for embedding dependencies into single assemblies
- **Microsoft.Extensions.Configuration** for robust configuration abstractions
- **System.CommandLine** for enterprise-grade CLI functionality
- **Embedded Resources** including WinSCP.exe for file transfer operations

### Performance Optimizations
- **Memory-efficient streaming** for large file operations
- **Async/await patterns** throughout for non-blocking operations
- **Thread-safe collections** and concurrent access patterns
- **Bulk operation support** for high-throughput scenarios

---

## 🚀 Usage Patterns

### Configuration Management
```csharp
// Initialize configuration system
Configuration.Initialize("appsettings.ini");

// Access strongly-typed sections
var dbConfig = Configuration.GetSection<DatabaseOptions>("Database");
var connString = dbConfig.ConnectionString;

// Save configuration changes
Configuration.Save();
```

### Command-Line Applications
```csharp
[Command("process-data", "Process input data files")]
public class ProcessDataCommand
{
    [Option("input", "i", "Input file path", required: true)]
    public string InputPath { get; set; }
    
    [Option("output", "o", "Output directory", defaultValue: "output")]
    public string OutputDir { get; set; }
    
    [Option("batch-size", "b", "Processing batch size", defaultValue: 1000)]
    public int BatchSize { get; set; }
    
    public async Task ExecuteAsync()
    {
        var processor = new DataProcessor();
        await processor.ProcessFileAsync(InputPath, OutputDir, BatchSize);
    }
}
```

### Database Operations
```csharp
// Initialize database access
var db = new DBAccess(connectionString);

// Execute parameterized queries
var activeUsers = db.GetDataTable(
    "SELECT * FROM Users WHERE Active = @active AND Created > @date",
    new { active = true, date = DateTime.Now.AddDays(-30) }
);

// Bulk operations with progress reporting
var processor = new BulkDbProcessor<User>(db);
processor.ProgressReported += (sender, e) => 
    Console.WriteLine($"Processed {e.ProcessedCount}/{e.TotalCount} records");

await processor.BulkInsertAsync(users);
```

### CSV Processing
```csharp
// Read CSV with automatic format detection
var reader = new CSVReader();
reader.ProgressReported += (sender, e) => 
    Console.WriteLine($"Reading: {e.PercentComplete:F1}%");

reader.ReadFile("data.csv");

// Process records
foreach (var record in reader.Records)
{
    var user = new User
    {
        Name = record["Name"],
        Email = record["Email"],
        CreatedDate = DateTime.Parse(record["Created"])
    };
    
    ProcessUser(user);
}
```

---

## 📁 Directory Structure

```
Toolkit.Modern/
├── 📁 Attributes/                     # Cross-cutting attributes
│   ├── CSVColumnAttribute.cs          # CSV entity mapping
│   ├── DBColumnAttribute.cs           # Database entity mapping
│   └── readme.md                      # Attribute documentation
├── 📁 CLI/                           # Command-line interface
│   ├── 📁 Attributes/                # CLI-specific attributes
│   │   ├── CommandAttribute.cs       # Command definition
│   │   └── OptionAttribute.cs        # Option definition
│   ├── CommandBuilder.cs             # Fluent command builder
│   ├── CommandParser.cs              # Argument parser
│   ├── ConsoleSpinner.cs             # Progress indicators
│   ├── GlobalOption.cs               # Global options
│   ├── ParseResult.cs                # Parse results
│   ├── ProgressSpinner.cs            # Progress reporting
│   ├── RootCommandBuilder.cs         # Root command builder
│   └── readme.md                     # CLI documentation
├── 📁 Configuration/                 # Configuration management
│   ├── 📁 Attributes/                # Configuration attributes
│   │   ├── ArrayAttribute.cs         # Array section mapping
│   │   ├── ConfigNameAttribute.cs    # Custom name mapping
│   │   ├── DefaultValueProviderAttribute.cs  # Default value providers
│   │   ├── DictionaryAttribute.cs    # Dictionary section mapping
│   │   └── DoNotPersistAttribute.cs  # Persistence control
│   ├── 📁 Helpers/                   # Configuration helpers
│   │   ├── DefaultValueHelper.cs     # Default value resolution
│   │   ├── IParsingHelper.cs         # Parsing interface
│   │   └── ParsingHelper.cs          # Parsing implementation
│   ├── 📁 Interfaces/                # Configuration interfaces
│   │   ├── IConfigSection.cs         # Section interface
│   │   └── IConfigurationManager.cs  # Manager interface
│   ├── 📁 Models/                    # Configuration models
│   │   ├── ConfigSection.cs          # Base section class
│   │   └── GlobalizationInfo.cs      # Culture settings
│   ├── AssemblyException.cs          # Assembly-related exceptions
│   ├── Configuration.cs              # Main configuration manager
│   └── readme.md                     # Configuration documentation
├── 📁 Core/                          # Core utilities
│   ├── Core.cs                       # Core functionality
│   ├── Core.WinScpResourceManager.cs # WinSCP resource management
│   └── readme.md                     # Core documentation
├── 📁 Data/                          # Data processing
│   ├── 📁 Audio/                     # Audio format detection
│   │   └── AudioFormatDetector.cs    # Audio format utilities
│   ├── 📁 CSV/                       # CSV processing
│   │   ├── BufferedReader.cs         # Buffered file reading
│   │   ├── CSVFormat.cs              # Format definitions
│   │   ├── CSVReader.cs              # CSV file reader
│   │   ├── CSVRecord.cs              # CSV record representation
│   │   ├── CSVWriter.cs              # CSV file writer
│   │   ├── ConversionException.cs    # Conversion errors
│   │   ├── DataProcessingException.cs # Processing errors
│   │   ├── ICSVRecord.cs             # Record interface
│   │   └── ValidationErrors.cs       # Validation error handling
│   ├── 📁 Database/                  # Database access
│   │   ├── BulkDbProcessor.Core.cs   # Bulk processor core
│   │   ├── BulkDbProcessor.Events.cs # Bulk processor events
│   │   ├── BulkDbProcessor.Operations.cs # Bulk operations
│   │   ├── BulkDbProcessor.SqlGeneration.cs # SQL generation
│   │   ├── BulkDbProcessor.TableManagement.cs # Table management
│   │   ├── DBAccess.Core.cs          # Database access core
│   │   ├── DBAccess.Factory.cs       # Connection factory
│   │   ├── DBAccess.Logging.cs       # Database logging
│   │   ├── DBAccess.Methods.cs       # Database methods
│   │   ├── DBAccess.Parameters.cs    # Parameter handling
│   │   ├── DBAccess.Properties.cs    # Database properties
│   │   ├── DBAccess.ScriptExecution.cs # Script execution
│   │   ├── DBAccess.Transactions.cs  # Transaction support
│   │   ├── DBOptions.cs              # Database options
│   │   ├── DBTransaction.cs          # Transaction wrapper
│   │   ├── ScriptExecutionResult.cs  # Script results
│   │   └── TypeConverter.cs          # Type conversion
│   ├── 📁 Exceptions/                # Data exceptions
│   │   └── ParamArgumentsMismatchException.cs # Parameter errors
│   └── readme.md                     # Data documentation
├── 📁 DataStructures/                # Data structures
│   ├── BinarySearchTree.cs           # Binary search tree
│   ├── Url.cs                        # URL utilities
│   └── readme.md                     # DataStructures documentation
├── 📁 Dependencies/                  # Embedded dependencies
│   └── WinSCP.exe                    # Embedded WinSCP executable
├── 📁 Json/                          # JSON processing
│   ├── DeltaContractResolver.cs      # Delta contract resolution
│   ├── JsonDeltaSerializer.cs        # Delta serialization
│   └── readme.md                     # JSON documentation
├── 📁 Logging/                       # Logging system
│   ├── 📁 Implementations/           # Logger implementations
│   │   ├── BaseLogger.cs             # Base logger class
│   │   ├── CompositeLogger.cs        # Multi-target logger
│   │   ├── ConsoleLogger.cs          # Console output
│   │   ├── FileLogger.cs             # File output
│   │   ├── NullLogger.cs             # No-op logger
│   │   ├── SessionFileLogger.cs      # Session-specific files
│   │   └── StaticLoggerAdapter.cs    # Static access adapter
│   ├── 📁 Interfaces/                # Logging interfaces
│   │   └── ILogger.cs                # Logger interface
│   ├── 📁 Models/                    # Logging models
│   │   ├── AsyncOptions.cs           # Async configuration
│   │   ├── CorrelationContext.cs     # Correlation tracking
│   │   ├── FileLoggerOptions.cs      # File logger options
│   │   ├── LogEntry.cs               # Log entry model
│   │   ├── LogLevel.cs               # Log levels
│   │   ├── LogSettings.cs            # Logging settings
│   │   └── SessionFileLoggerOptions.cs # Session logger options
│   ├── Log.cs                        # Static logging facade
│   └── readme.md                     # Logging documentation
├── 📁 Mail/                          # Email processing
│   ├── AttachmentProcessResult.cs    # Attachment results
│   ├── EmailAttachmentHandler.cs     # Attachment processing
│   ├── MailUtil.cs                   # Mail utilities
│   ├── PartInfo.cs                   # MIME part information
│   ├── ProcessingMethod.cs           # Processing methods
│   ├── SkippedFile.cs                # Skipped file tracking
│   ├── TempFileAttachment.cs         # Temporary attachments
│   └── readme.md                     # Mail documentation
├── 📁 Net/                           # Network operations
│   ├── FileOperationProgress.cs      # Operation progress
│   ├── FileOperationResult.cs        # Operation results
│   ├── FileTransferClient.cs         # Transfer client
│   ├── FileTransferConfig.cs         # Transfer configuration
│   ├── FileTransferException.cs      # Transfer exceptions
│   ├── FileTransferItem.cs           # Transfer items
│   ├── FileTransferProgress.cs       # Transfer progress
│   ├── FileTransferResult.cs         # Transfer results
│   ├── RemoteFileInfo.cs             # Remote file information
│   ├── TransferProtocol.cs           # Protocol definitions
│   └── readme.md                     # Net documentation
├── 📁 Security/                      # Security utilities
│   ├── AESEncryption.cs              # AES encryption
│   ├── Encryptor.cs                  # General encryption
│   ├── VBMath.cs                     # Mathematical utilities
│   └── readme.md                     # Security documentation
├── 📁 Utils/                         # General utilities
│   ├── BooleanParser.cs              # Boolean parsing
│   ├── ConsoleUtil.cs                # Console utilities
│   ├── DateTimeParser.cs             # DateTime parsing
│   ├── DateTimeUtil.cs               # DateTime utilities
│   ├── EnumExtensions.cs             # Enum extensions
│   ├── HtmlUtil.cs                   # HTML utilities
│   ├── IOUtils.cs                    # I/O utilities
│   ├── IParser.cs                    # Parser interface
│   ├── NameUtil.cs                   # Name utilities
│   ├── Parser.cs                     # Generic parser
│   ├── StringUtil.cs                 # String utilities
│   ├── TemplateProcessor.cs          # Template processing
│   ├── TimingUtil.cs                 # Timing utilities
│   ├── TypeHelper.cs                 # Type utilities
│   ├── Utils.cs                      # General utilities
│   ├── ValueConverterRegistry.cs     # Value conversion
│   └── readme.md                     # Utils documentation
├── ByteForge.Toolkit.Modern.csproj   # Project file
├── FodyWeavers.xml                   # Fody configuration
├── FodyWeavers.xsd                   # Fody schema
└── readme.md                         # This file
```

---

## 🛠️ Development Guidelines

### Code Standards
- **Modern C# 12** language features with nullable reference types
- **Comprehensive XML documentation** for all public APIs
- **Consistent naming conventions** following .NET guidelines
- **Attribute-driven programming** for declarative configurations

### Architecture Principles
- **Single responsibility** - Each class has a focused purpose
- **Dependency inversion** - Depend on abstractions, not concretions
- **Open/closed principle** - Open for extension, closed for modification
- **Interface segregation** - Many specific interfaces are better than one general-purpose interface

### Performance Considerations
- **Memory efficiency** - Streaming for large data operations
- **Async patterns** - Non-blocking operations where appropriate
- **Resource management** - Proper disposal and cleanup
- **Caching strategies** - Cache expensive operations appropriately

---

## 🔗 Integration Points

### WinSCP Integration
The library includes embedded WinSCP.exe resources and integrates with the WinSCPnet project for comprehensive file transfer capabilities.

### Configuration Integration
All modules support configuration through the centralized Configuration system, enabling consistent settings management across the entire library.

### Logging Integration
All modules integrate with the centralized logging system, providing consistent diagnostic information and troubleshooting capabilities.

---

For comprehensive examples and detailed API documentation, refer to the individual module readme files and the solution-level documentation.

---

## 📖 Documentation Links

### 🏗️ Core Modules
| Module                                         | Description                |
|------------------------------------------------|----------------------------|
| **[CLI](CommandLine/readme.md)**               | Command-line parsing       |
| **[Configuration](Configuration/readme.md)**   | INI-based configuration    |
| **[Core](Core/readme.md)**                     | Core utilities             |
| **[Data](Data/readme.md)**                     | Database & file processing |
| **[DataStructures](DataStructures/readme.md)** | Collections & utilities    |
| **[JSON](Json/readme.md)**                     | Delta serialization        |
| **[Logging](Logging/readme.md)**               | Structured logging         |
| **[Mail](Mail/readme.md)**                     | Email processing           |
| **[Net](Net/readme.md)**                       | Network file transfers     |
| **[Security](Security/readme.md)**             | Encryption & security      |
| **[Utils](Utilities/readme.md)**               | General utilities          |

### 📊 Data Modules
| Module                                           | Description             |
|--------------------------------------------------|-------------------------|
| **[Data.Attributes](Data/Attributes/readme.md)** | Data mapping attributes |
