# ByteForge Toolkit

**A comprehensive, enterprise-grade .NET library providing modular utilities for configuration management, database access, CLI parsing, security, data processing, and more.**

---

## 🎯 Project Overview

ByteForge.Toolkit is a modern, multi-targeting .NET library designed for enterprise applications. It combines the reliability of .NET Framework 4.8 with the performance and features of modern .NET (8.0+ and 9.0). The solution consists of three main projects working together to provide comprehensive functionality.

### Target Frameworks
- **.NET Framework 4.8** - Enterprise compatibility
- **.NET 8.0** - Current LTS version  
- **.NET 9.0** - Latest stable version

---

## 📦 Solution Architecture

The solution follows a clean, modular architecture with clear separation of concerns:

```
ByteForge.Toolkit.sln
├── ByteForge.Toolkit.Modern     # Main library (multi-target)
├── WinSCPnet                    # SFTP/FTP functionality (multi-target) 
└── ByteForge.Toolkit.Modern.Tests  # Comprehensive test suite (.NET 8+)
```

### Core Projects

#### **ByteForge.Toolkit.Modern** 
*Primary library with modular components*
- **Configuration**: INI-based configuration with strong typing and section support
- **CLI**: Attribute-based command-line parsing with automatic help generation  
- **Database**: Multi-database access layer with bulk operations and transactions
- **CSV**: High-performance CSV reading/writing with format auto-detection
- **Security**: AES encryption, secure credential storage, and security utilities
- **Logging**: Thread-safe, multi-target logging with structured output
- **Mail**: Email processing with attachment handling and MIME support
- **Net**: File transfer operations with progress monitoring
- **Utils**: Comprehensive utility collection for parsing, string manipulation, I/O, and more
- **Data Structures**: Binary search trees, URL utilities, and specialized collections
- **JSON**: Delta serialization and contract resolution for efficient data exchange

#### **WinSCPnet**
*Enterprise file transfer wrapper*
- Complete .NET wrapper for WinSCP functionality
- SFTP, FTP, FTPS, and SCP protocol support
- Progress monitoring and event handling
- Multi-target compatibility (.NET Framework 4.8, .NET 8.0, .NET 9.0)

#### **ByteForge.Toolkit.Modern.Tests**
*Comprehensive test suite*
- 100+ test classes covering all modules
- Integration and unit tests
- Code coverage reporting with OpenCover and ReportGenerator
- Database testing with Access and SQL Server scenarios

---

## 🚀 Quick Start

### Build Requirements
- **.NET SDK 9.0+** (includes .NET Framework 4.8 targeting)
- **Visual Studio 2022** or **VS Code** with C# extensions
- **PowerShell** (recommended for build scripts)

### Building the Solution

**Recommended approach:**
```powershell
# Build Debug configuration (default) - builds all target frameworks
.\BuildSolution.bat

# Build Release configuration  
.\BuildSolution.bat Release
```

**Manual build:**
```powershell
dotnet restore
dotnet build --configuration Debug
```

### Running Tests
```powershell
# After building
dotnet test "Toolkit.Modern.Tests\ByteForge.Toolkit.Modern.Tests.csproj" --no-build

# With code coverage
cd "Toolkit.Modern.Tests"
.\RunCoverage.bat  # Generates HTML coverage report
```

---

## 📚 Module Documentation

Each module contains comprehensive documentation and examples:

| Module | Purpose | Key Features |
|--------|---------|-------------|
| [**CLI**](Toolkit.Modern/CLI/readme.md) | Command-line parsing | Attribute-based, type-safe, help generation |
| [**Configuration**](Toolkit.Modern/Configuration/readme.md) | INI configuration management | Strong typing, sections, arrays, encryption |
| [**Data**](Toolkit.Modern/Data/readme.md) | Database & file processing | Multi-DB support, CSV, audio detection, bulk ops |
| [**Security**](Toolkit.Modern/Security/readme.md) | Encryption & security | AES encryption, secure storage, validation |
| [**Logging**](Toolkit.Modern/Logging/readme.md) | Structured logging | Thread-safe, multi-target, configurable output |
| [**Utils**](Toolkit.Modern/Utils/readme.md) | General utilities | Parsing, string manipulation, I/O, templates |

---

## 🔧 Key Features

### Multi-Framework Support
- Single codebase targeting .NET Framework 4.8, .NET 8.0, and .NET 9.0
- Platform-specific optimizations and compatibility layers
- Modern C# language features with nullable reference types

### Enterprise-Grade Components
- **Configuration Management**: INI-based with strong typing, encryption, and globalization
- **Database Access**: Multi-database support (SQL Server, ODBC) with bulk operations
- **Security**: AES encryption, secure credential storage, validation utilities
- **File Processing**: High-performance CSV, audio format detection, email attachments

### Developer Experience  
- **Comprehensive Documentation**: Each module has detailed readme with examples
- **Extensive Testing**: 100+ test classes with high code coverage
- **Modern Tooling**: Support for latest IDE features and debugging tools
- **Clean Architecture**: Modular design with clear separation of concerns

### Performance & Reliability
- **Thread-Safe Operations**: All core components support concurrent access
- **Memory Efficient**: Optimized for large data processing scenarios
- **Error Handling**: Comprehensive exception handling with detailed diagnostics
- **Progress Monitoring**: Built-in progress reporting for long-running operations

---

## 📁 Solution Structure

```
├── 📁 Toolkit.Modern/                 # Main library source
│   ├── 📁 CLI/                        # Command-line interface components
│   ├── 📁 Configuration/              # Configuration management
│   ├── 📁 Data/                       # Database, CSV, audio processing
│   │   ├── 📁 Audio/                  # Audio format detection
│   │   ├── 📁 CSV/                    # CSV reading/writing
│   │   ├── 📁 Database/               # Database access layer
│   │   └── 📁 Exceptions/             # Data processing exceptions
│   ├── 📁 Security/                   # Encryption and security
│   ├── 📁 Logging/                    # Structured logging system
│   ├── 📁 Mail/                       # Email processing
│   ├── 📁 Net/                        # Network file transfers
│   ├── 📁 Utils/                      # General utilities
│   └── 📁 Dependencies/               # Embedded resources (WinSCP.exe)
├── 📁 WinSCP/                         # SFTP/FTP wrapper library
├── 📁 Toolkit.Modern.Tests/          # Test suite
│   ├── 📁 Unit/                       # Unit tests by module
│   ├── 📁 Helpers/                    # Test helper classes
│   ├── 📁 Models/                     # Test data models
│   └── 📁 TestData/                   # Test databases and files
├── 📁 docs/                           # Solution documentation
├── BuildSolution.bat                  # Build automation script
├── ByteForge.Tasks.targets            # Custom MSBuild targets  
└── CLAUDE.md                          # AI assistant instructions
```

---

## 🛠️ Development Guidelines

### Code Standards
- **C# 12** language features with nullable reference types enabled
- **Comprehensive XML documentation** for all public APIs
- **Attribute-based configuration** for declarative programming  
- **Partial classes** for large components (DBAccess, BulkDbProcessor)

### Testing Standards
- **Unit tests** for all public methods
- **Integration tests** for database and file operations
- **Code coverage** reporting with OpenCover
- **Test data** management with embedded resources

### Build System
- **Multi-target builds** with framework-specific optimizations
- **Automated version management** based on file changes
- **Dependency embedding** with Costura.Fody
- **Clean build artifacts** with custom MSBuild targets

---

## 📋 Build Artifacts

| Configuration | Output Location | Description |
|---------------|-----------------|-------------|
| **Debug** | `bin\Debug\netX.X\` | Development builds with symbols |
| **Release** | `bin\Release\netX.X\` | Optimized production builds |
| **Documentation** | `bin\ByteForge.Toolkit.xml` | Auto-generated API documentation |
| **Test Results** | `TestResults\` | Coverage reports and test output |

---

## 🔗 Dependencies

### Core Dependencies
- **Microsoft.Extensions.Configuration** (9.0.10) - Configuration abstractions
- **System.CommandLine** (2.0.0-beta4) - CLI parsing foundation
- **Newtonsoft.Json** (13.0.4) - JSON serialization
- **RestSharp** (112.1.0) - HTTP client operations

### Framework-Specific
- **.NET Framework 4.8**: System.Data.SqlClient, legacy framework support
- **.NET 8.0+**: Modern BCL packages, improved performance APIs
- **WinSCP Integration**: Embedded WinSCP.exe for file transfer operations

---

## 📝 Version History

The solution has undergone significant evolution:
- **Legacy Architecture**: Original single .NET Framework 4.8 library
- **Modern Multi-Target**: Current architecture supporting .NET Framework 4.8 + .NET 8.0/9.0
- **Build System**: Custom MSBuild targets for file synchronization and versioning
- **Test Suite**: Comprehensive testing with coverage reporting

---

## 🎯 Usage Examples

### Quick Configuration Access
```csharp
// Initialize and access strongly-typed configuration
Configuration.Initialize("appsettings.ini");
var dbConfig = Configuration.GetSection<DatabaseOptions>("Database");
string connectionString = dbConfig.ConnectionString;
```

### Command-Line Application
```csharp
[Command("process", "Process data files")]
public class ProcessCommand
{
    [Option("input", "i", "Input file path", required: true)]
    public string InputFile { get; set; }
    
    [Option("format", "f", "Output format", defaultValue: "csv")]  
    public string Format { get; set; }
}
```

### Database Operations  
```csharp
var db = new DBAccess(connectionString);
var results = db.GetDataTable("SELECT * FROM Users WHERE Active = @active", 
    new { active = true });
```

### CSV Processing
```csharp
var reader = new CSVReader();
reader.ReadFile("data.csv");
foreach (var record in reader.Records)
{
    Console.WriteLine($"{record["Name"]}: {record["Email"]}");
}
```

---

For detailed documentation on specific modules, see the individual readme files in each module directory.