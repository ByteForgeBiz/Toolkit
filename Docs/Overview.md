# ByteForge.Toolkit Overview

## Project Description

**ByteForge.Toolkit** is a comprehensive, modular .NET Framework 4.8 library designed to streamline enterprise application development. It provides high-performance utilities across multiple domains including command-line interfaces, configuration management, data processing, secure encryption, logging, networking, email handling, and user interface components. The toolkit emphasizes maintainability, performance, security, and developer productivity.

## Architecture & Design Principles

### Modular Architecture
The toolkit is organized into 11 independent modules, each focusing on specific functionality while maintaining seamless integration capabilities. Modules can be used individually or combined for comprehensive solutions.

### Core Design Principles
- **Enterprise-Grade**: Built for production environments with robust error handling and security
- **Thread-Safe**: Components designed for concurrent access in multi-threaded applications
- **Extensible**: Plugin systems, custom converters, and registration patterns throughout
- **Performance-Optimized**: Minimal overhead with efficient algorithms and caching
- **Security-First**: Encryption, input validation, and secure credential handling
- **Async Support**: Full async/await patterns for scalable operations

## Module Overview

### Core Infrastructure
| Module            | Description                                    | Key Features                                                                  |
|-------------------|------------------------------------------------|-------------------------------------------------------------------------------|
| **Core**          | Embedded resource deployment and core services | WinSCP binary extraction, dependency management                               |
| **Configuration** | INI-based configuration system                 | Strongly-typed sections, arrays, dictionaries, globalization support          |
| **Logging**       | Thread-safe logging framework                  | File rotation, async logging, session management, correlation tracking        |
| **Security**      | AES-based encryption system                    | Deterministic key generation, Galois Field mathematics, credential protection |

### Data Processing & Storage
| Module                | Description                         | Key Features                                                             |
|-----------------------|-------------------------------------|--------------------------------------------------------------------------|
| **Data**              | Comprehensive data processing       | CSV streaming, database access (SQL Server/ODBC), audio format detection |
| **DataStructures**    | Efficient data structures           | AVL tree implementation, URL utilities                                   |
| **Net**               | Cross-protocol file transfer        | FTP/FTPS/SFTP support, batching, progress reporting, concurrency         |

### User Interface & Interaction
| Module     | Description                          | Key Features                                                         |
|------------|--------------------------------------|----------------------------------------------------------------------|
| **CLI**    | Command-line interface framework     | Attribute-based parsing, aliasing, typo correction, plugin discovery |
| **Mail**   | Email processing system              | HTML support, intelligent attachments, automatic compression         |
| **HTML**   | NPD UI framework                     | Modals, calendar widgets, professional styling, dark/light themes    |
| **Utils**  | Utility collection                   | Type parsing, template processing, timing, console operations        |

## Key Capabilities

### Command-Line Interfaces
- Declarative command definition using attributes
- Intelligent auto-aliasing and typo correction
- Plugin-based architecture for extensibility
- Comprehensive help system and progress indicators

### Configuration Management
- Strongly-typed configuration sections with automatic type conversion
- Support for complex data structures (arrays, dictionaries)
- Comment preservation and structure maintenance
- Globalization-aware formatting and parsing

### Data Processing
- High-performance CSV processing with streaming support
- Enterprise database access with bulk operations and security
- Audio file format detection from binary data
- Specialized exception handling for data operations

### Security & Encryption
- Custom AES implementation with Galois Field mathematics
- Deterministic key generation for reproducible encryption
- Thread-safe encryption operations
- Integration with configuration and database modules

### Networking & File Transfer
- Unified API across FTP, FTPS, and SFTP protocols
- High-throughput batch operations with concurrency control
- Progress reporting and cancellation support
- Embedded WinSCP engine for cross-platform compatibility

### Logging & Monitoring
- Multi-target logging (console, file, session-based)
- Automatic file rotation and retention policies
- Async logging with configurable queue sizes
- Correlation tracking for distributed operations

### Email & Communication
- HTML and plain text email support
- Intelligent attachment processing with size limits
- Automatic compression for large file sets
- Secure SMTP configuration with encrypted credentials

### User Interface Components
- Professional modal dialogs with accessibility support
- Custom calendar widgets with keyboard navigation
- Comprehensive CSS framework with theme support
- Form validation and utility functions

## Integration & Usage Patterns

### Basic Setup
```csharp
// Initialize core components
Configuration.Initialize("app.ini");
Log.EnableConsoleLogging();

// Use encryption for sensitive data
string encrypted = Encryptor.Default.Encrypt("sensitive data");
string decrypted = Encryptor.Default.Decrypt(encrypted);

// Database operations with security
var db = new DBAccess("Production");
var users = db.GetRecords<User>("SELECT * FROM Users");

// File transfer with progress
using var client = new FileTransferClient(cfg);
await client.ConnectAsync();
var results = await client.UploadFilesAsync(items, maxConcurrent: 4);
```

### Advanced Integration
```csharp
// CLI application with plugins
var parser = new RootCommandBuilder("MyTool")
    .AddAssembly(typeof(Program).Assembly)
    .SearchPlugins("plugins/*.dll")
    .UseHelp()
    .UseTypoCorrections()
    .Build();

// Configuration with complex types
public class AppConfig
{
    [Array("Servers")]
    public string[] ServerList { get; set; }
    
    [Dictionary("Settings")]
    public Dictionary<string, string> AppSettings { get; set; }
    
    [DefaultValueProvider(typeof(ConfigDefaults), nameof(ConfigDefaults.GetDataPath))]
    public string DataDirectory { get; set; }
}

var config = Configuration.GetSection<AppConfig>("Application");
```

### Web Application Integration
```html
<!-- Include UI framework -->
<link rel="stylesheet" href="CSS/npd-ui.css">
<script src="JavaScript/npdModal.js"></script>
<script src="JavaScript/calendar.js"></script>

<script>
// Use modal notifications
NPDModal.success('Operation completed successfully!');

// Initialize date pickers
initializeDatePickers();
</script>
```

## Performance Characteristics

- **Memory Efficient**: Streaming operations for large datasets
- **Thread-Safe**: Concurrent access support throughout
- **Async Operations**: Non-blocking I/O with cancellation support
- **Optimized Algorithms**: Custom implementations for performance-critical operations
- **Resource Management**: Automatic cleanup and connection pooling

## Security Features

- **Encrypted Credentials**: AES encryption for sensitive configuration data
- **SQL Injection Prevention**: Parameterized queries and input validation
- **Secure File Transfer**: TLS/SSL support with certificate validation
- **Input Sanitization**: XSS prevention and safe HTML rendering
- **Access Control**: Authentication and authorization patterns

## Development & Testing

### Project Structure
- **Main Library**: `ByteForge.Toolkit.csproj` - Core implementation
- **Test Suite**: `ByteForge.Toolkit.Tests.csproj` - Comprehensive unit tests
- **Documentation**: Individual `readme.md` files per module
- **Examples**: Usage examples and integration patterns

### Testing Approach
- Unit tests for individual components
- Integration tests for module interactions
- Performance benchmarks for critical operations
- Security testing for encryption and authentication

## Requirements & Dependencies

### System Requirements
- **.NET Framework**: 4.8 (minimum)
- **Operating System**: Windows (some features Windows-specific)
- **Memory**: Varies by usage; optimized for resource-constrained environments

### NuGet Dependencies
- **Microsoft.Extensions.Configuration**: Configuration module
- **System.CommandLine**: CLI module
- **System.Data.SqlClient**: Database operations
- **System.Data.Odbc**: ODBC database support

### Embedded Dependencies
- **WinSCP**: Automatically deployed by Core module for network operations

## Author & Maintenance

**Developed by Paulo Santos**

The toolkit is actively maintained with regular updates, security patches, and feature enhancements. Each module includes comprehensive documentation, usage examples, and best practices guidance.

## Getting Started

1. **Clone the repository**: `git clone https://github.com/PaulStSmith/ByteForge-Toolkit`
2. **Review module documentation**: Each module's `readme.md` contains detailed usage instructions
3. **Run tests**: Execute the test suite to verify functionality
4. **Integrate modules**: Add references and initialize components as needed
5. **Configure settings**: Set up configuration files and encryption keys

For detailed information about specific modules, refer to their individual documentation files.

