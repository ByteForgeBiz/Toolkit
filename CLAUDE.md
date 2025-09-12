# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ByteForge.Toolkit is a modular .NET Framework 4.8 library providing high-performance utilities for enterprise applications. The project is organized into cohesive modules for CLI parsing, configuration management, database access, CSV processing, logging, security, and general utilities.

## Build Commands

### Building the Project

**Recommended approach using the build script:**
```bash
# Build Debug configuration (default) - builds both AnyCPU and x86
cd "Tests\ByteForge.Toolkit.Tests"
BuildSolution.bat

# Build Release configuration
BuildSolution.bat Release
```

**Manual MSBuild commands (if needed):**
```bash
# Auto-detect and use appropriate MSBuild version
# Build Debug AnyCPU
msbuild ByteForge.Toolkit.sln /t:Restore /p:Configuration=Debug /p:Platform="Any CPU"
msbuild ByteForge.Toolkit.sln /p:Configuration=Debug /p:Platform="Any CPU"

# Build Release AnyCPU  
msbuild ByteForge.Toolkit.sln /t:Restore /p:Configuration=Release /p:Platform="Any CPU"
msbuild ByteForge.Toolkit.sln /p:Configuration=Release /p:Platform="Any CPU"

# Build x86 versions
msbuild ByteForge.Toolkit.sln /t:Restore /p:Configuration=Debug /p:Platform=x86
msbuild ByteForge.Toolkit.sln /p:Configuration=Debug /p:Platform=x86
```

**Note:** The BuildSolution.bat script auto-detects Visual Studio installations and handles NuGet restore automatically. Build logs are saved to `Tests\ByteForge.Toolkit.Tests\bin\Debug\Build.log` or `bin\Release\Build.log`.

### Package Management
- Uses PackageReference format in .csproj file
- Package restoration: `nuget restore` or `dotnet restore`
- No package.json or external package managers

### Testing and Code Coverage

**Prerequisites:** Build the solution first using BuildSolution.bat

```bash
# Run all tests (after building)
dotnet test "Tests\ByteForge.Toolkit.Tests\ByteForge.Toolkit.Tests.csproj" --no-build

# Run tests with code coverage using OpenCover
# First, ensure packages are restored and project is built
cd "Tests\ByteForge.Toolkit.Tests"
BuildSolution.bat

# Run coverage analysis
packages\OpenCover.4.7.1221\tools\OpenCover.Console.exe -target:"dotnet.exe" -targetargs:"test ByteForge.Toolkit.Tests.csproj --no-build --configuration Debug" -output:coverage.xml -register:user -filter:"+[ByteForge.Toolkit*]*"

# Generate HTML coverage report  
packages\ReportGenerator.5.1.26\tools\net47\ReportGenerator.exe -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html
```

**Alternative using MSTest runner:**
```bash
# After building with BuildSolution.bat
dotnet test "Tests\ByteForge.Toolkit.Tests\ByteForge.Toolkit.Tests.csproj" --no-build --configuration Debug --logger:trx --collect:"XPlat Code Coverage"
```

The test project includes comprehensive tests for:
- CSV processing functionality
- Data structures (Binary Search Tree, URL utilities)
- Logging system
- Security (AES encryption)
- Utility functions

### Build Artifacts
- Output directory: `bin\Debug\` or `bin\Release\`
- Documentation: `bin\ByteForge.Toolkit.xml` (auto-generated)
- Platform-specific builds: `bin\x86\Debug\` or `bin\x86\Release\`

### Custom Build System
The project includes a sophisticated build system in `ByteForge.Tasks.targets` that:
- Copies files between central location and project directories
- Automatically updates version numbers based on file changes
- Cleans up obsolete files
- Uses robocopy for efficient file synchronization

## Architecture Overview

### Modular Design
The library is organized into focused modules, each with its own namespace and documentation:

- **CLI** (`ByteForge.Toolkit.CommandLine`): Attribute-based command-line parsing
- **Configuration** (`ByteForge.Toolkit`): INI-based configuration with strongly-typed sections
- **Database** (`ByteForge.Toolkit`): Multi-database access layer with bulk operations
- **CSV** (`ByteForge.Toolkit`): High-performance CSV reading/writing
- **DataStructures** (`ByteForge.Toolkit`): Binary search trees and URL utilities
- **Logging** (`ByteForge.Toolkit.Logging`): Thread-safe logging with multiple outputs
- **Mail** (`ByteForge.Toolkit`): Email processing with attachment handling
- **Security** (`ByteForge.Toolkit`): AES encryption and security utilities
- **Utils** (`ByteForge.Toolkit`): General-purpose helper classes

### Key Architectural Patterns

#### Partial Classes
Large classes are split into focused partial classes:
- `DBAccess` is split into Core, Factory, Methods, ScriptExecution, TypeConverter, Logging, and Parameters
- Each partial class handles a specific concern

#### Attribute-Based Configuration
- CLI commands use `[Command]` and `[Option]` attributes
- Database entities use `[DBColumn]` attributes for ORM mapping
- CSV mapping uses `[CSVColumn]` attributes
- Configuration sections use various attributes for control

#### Singleton Pattern
- `Configuration` class uses thread-safe singleton pattern
- `Log` class provides static access to logging functionality

#### Factory Pattern
- Database connections use factory pattern for SQL Server vs ODBC
- Logger implementations use composite pattern for multiple outputs

### Configuration Management

The configuration system is built around INI files with these capabilities:
- Strongly-typed section mapping
- Array/collection support with dedicated sections
- Encrypted credential storage
- Dynamic access through Configuration.Dynamic
- Globalization support for formatting

#### Example Configuration Structure:
```ini
[Data Source]
SelectedDB=Production

[Production]
sType=SQLServer
sServer=PRODSERVER\INSTANCE1
sDatabaseName=MyApplication
esUser=[encrypted_username]
esPass=[encrypted_password]
```

### Database Access Layer

The DBAccess class provides:
- Multi-database support (SQL Server 2000, ODBC)
- Parameterized queries with automatic SQL injection prevention
- Bulk operations via `BulkDbProcessor<T>`
- Script execution with GO statement parsing
- Full async/await support
- Comprehensive logging and error handling

### Security Features

- AES encryption for sensitive data (passwords, connection strings)
- Secure credential storage in configuration
- SQL injection prevention through parameterization
- Support for encrypted database connections

## Module Dependencies

Key inter-module dependencies:
- Database module depends on Configuration, Logging, and Security
- Configuration module has minimal dependencies (Microsoft.Extensions.Configuration)
- Logging module is used throughout for diagnostics
- CLI module uses System.CommandLine NuGet package

## Common Development Patterns

### Error Handling
- Most methods return boolean success indicators
- `LastException` property provides detailed error information
- Comprehensive logging of errors with context

### Async Support
- All database operations have async variants
- Bulk operations support cancellation tokens
- Progress reporting for long-running operations

### Type Safety
- Generic methods for type-safe data retrieval
- Attribute-based mapping for compile-time validation
- Extensive use of strongly-typed configuration sections

## File Organization

- Each module has its own directory with readme.md documentation
- Attributes are in dedicated Attributes/ subdirectories
- Models and interfaces are in dedicated subdirectories
- Comprehensive documentation in Documentation/ directory

## Important Notes

- Project targets .NET Framework 4.8 specifically
- Uses legacy packages.config alongside PackageReference
- Designed for Windows environments (some features are Windows-specific)
- Custom build system handles file synchronization and versioning
- Extensive inline documentation and modular design for maintainability