# ByteForge.Toolkit Changelog

All notable changes to this project will be documented in this file.

## [Latest] - Recent Changes

### [7542362] - 2025-09-21
**Refactor parsing and serialization utilities**
- Moved `DefaultValueHelper` to `Helpers` and added `IParsingHelper` and `ParsingHelper` for encapsulating parsing logic
- Introduced `IParsingHelper` interface to define contracts for parsing and populating objects via reflection
- Implemented `ParsingHelper` class with methods for shallow property population and nullable type resolution
- Enhanced `IParser` with generic overloads for parsing (`Parse<T>`, `TryParse<T>`) and stringifying (`Stringify<T>`)
- Updated `Parser` to support culture-specific parsing/formatting and added detailed documentation
- Refactored `JsonDeltaSerializer` into its own file for better modularity and clarity
- Improved code documentation, added defensive programming practices, and cleaned up unused imports
- Updated `ByteForge.Toolkit.csproj` to reflect new file structure and include new classes
- Ensured `Parser` respects culture settings for consistent behavior across locales
- Improved error handling with explicit exceptions for invalid arguments
- Highlighted thread safety considerations in `Parser` documentation

### [58e4c55] - 2025-09-21
**Update documentation for CLI, Config, Security, Utils**
- Added `readme.md` files for `HTML`, `Logging`, `Mail`, `Security`, and `Properties` in `ByteForge.Toolkit.csproj`
- Renamed and enhanced `readme.md` sections for CLI, Configuration, Security, and Utils modules with detailed descriptions, examples, and best practices
- Introduced advanced usage patterns for CLI, including environment variable support, logging configuration, and plugin-based commands
- Expanded Configuration module with examples for arrays, dictionaries, thread-safe access, and dynamic configuration
- Updated Security module with AES encryption examples, multi-key support, and integration with configuration systems
- Enhanced Utils module with new utilities for async-to-sync operations, boolean parsing, and enum description handling
- Deprecated or moved CSV library documentation
- Improved consistency, naming, and enterprise-grade focus across all documentation

### [f5804ed] - 2025-09-21
**Remove RunSettingsFilePath from test project**
- Removed the `<RunSettingsFilePath>` property from the `<PropertyGroup>` in `ByteForge.Toolkit.Tests.csproj`
- The removed property pointed to a `.runsettings` file used for test configuration
- This change indicates the project no longer relies on this specific `.runsettings` file, possibly due to a change in test setup or removal of fine-code-coverage tooling

### [726bb04] - 2025-09-21
**769 Tests. 769 Green.**
- Introduced `IParser` interface for modular parsing design
- Updated `Parser` to support stringification and JSON parsing
- Enhanced `RegisterType` for custom parsing and stringifying
- Added comprehensive file transfer system (FTP, SFTP, FTPS)
- Improved configuration system with dictionary support
- Enhanced thread safety in `ConfigSection` and `Configuration`
- Updated unit tests for new parser and configuration features
- Added `WinSCP.exe` and `WinSCPnet.dll` for file operations
- Updated documentation for CLI, logging, and utilities
- Adjusted `robo.rcj` to exclude additional files and directories
- Removed outdated `readme.md` for ZIP module

### [3681fdc] - 2025-09-21
**Remove `<RunSettingsFilePath>` from test project**
- Removed the `<RunSettingsFilePath>` property from the `<PropertyGroup>` in `ByteForge.Toolkit.Tests.csproj`
- The property previously pointed to a `.runsettings` file used for test configuration
- This change indicates the project no longer relies on this specific `.runsettings` file, possibly due to changes in test setup or tooling

### [3c11a95] - 2025-09-19
**725 Tests. 724 Green, 1 Skipped.**
- Introduced `[Dictionary]` attribute for dictionary properties
- Enhanced `ConfigSection<T>` to handle dictionary operations
- Refactored property parsing to support dictionaries
- Added comprehensive tests for dictionary functionality
- Updated `readme.md` with dictionary usage examples
- Replaced hardcoded defaults with constants for maintainability
- Improved edge case handling for circular refs and null values
- Updated INI handling for empty/missing dictionary sections
- Changed progress bar percentage type from `float` to `double`
- Removed deprecated permissions in `settings.local.json`
- Added fine-code-coverage integration to test project
- Updated assembly version to `10.32.0.0919`

### [3f61409] - 2025-09-18
**Add NPD UI Framework and enhance project structure**
- Introduced NPD UI Framework with modals, calendars, utilities
- Added `ByteForge-ui.css` for styling with dark/light mode support
- Implemented `ByteForge-calendar.js` for custom date pickers
- Added `ByteForge-modal.js` for accessible modal notifications
- Included `ByteForge-utilities.js` for validation and clipboard ops
- Updated `README.md` with framework usage and integration guides
- Added `settings.local.json` for Bash command permissions
- Incremented assembly version to `10.14.0.0918`
- Documented `ByteForge.Toolkit.Zip` module in `readme.md`

### [f7fa872] - 2025-09-16
**694 Tests. 693 Green, 1 Skipped.**
- Introduced a static lock (`_staticLock`) for thread safety in the `Save` method
- Added a thread-safe `Save` method and a `ThreadUnsafeSave` method to separate concerns
- Simplified null-coalescing assignments using `??=` in `AddSection<T>` and `GetSection<T>`
- Enhanced XML documentation for `Save` and `ThreadUnsafeSave` methods
- Removed redundant instance lock during file writing
- Improved test setup for concurrent save operations with unique sections
- Added assertions to validate file existence, content, and concurrency handling
- Introduced a timeout for `Task.WaitAll` in concurrent save tests
- Updated tests to use AwesomeAssertions for better readability
- Included `System.Text` namespace for `StringBuilder` usage in tests
- Refactored test logic to ensure unique section access per thread
- Verified all expected values are present after concurrent save operations

### [d7294c6] - 2025-09-16
**Refactor: Remove CSV and ZIP modules, streamline tests**
- Removed unused namespaces across multiple files
- Deleted the `Zip` folder and removed the ZIP module entirely
- Removed the `CSVReader` and `CSVWriter` classes and tests
- Simplified `ByteForge.Toolkit.csproj` by cleaning unused entries
- Updated and added detailed `README.md` for test helpers/models
- Removed outdated test helpers and models like `TempFileHelper`
- Deleted test cases for deprecated utilities and modules
- Updated namespaces to reflect the new project structure
- Cleaned up temporary file handling in test cases
- Streamlined the test suite by removing redundant tests
- Focused the project on core functionality, reducing technical debt

### [161f192] - 2025-09-16
**Enhance test suite with documentation and new test cases**
- Added detailed documentation to `README.md` files for all test modules, including overviews, strategies, and execution instructions
- Introduced new test cases across modules to improve coverage of edge cases, error handling, performance, and compatibility
- Expanded `Configuration` tests to include property mapping, default values, nullable types, array handling, and advanced attributes
- Enhanced `CSV` tests with attribute-based mapping, error handling, and large dataset performance validation
- Added `Data` tests for audio format detection, database operations, and CSV processing
- Expanded `DataStructures` tests for `BinarySearchTree` and `Url` classes, covering traversal, parsing, and edge cases
- Improved `Logging` tests for log levels, multiple targets, thread safety, and performance
- Focused `Mail` tests on attachment handling, including compression, size limits, and resource cleanup
- Added `Security` tests for AES encryption, password-based encryption, and secure data handling
- Introduced `Utils` tests for utility classes like `ConsoleUtil`, `IOUtils`, and `StringUtil`
- Enhanced database operation tests, including `DBAccessCoreTests`, `DBAccessMethodsSQLServerTests`, `DBAccessMethodsODBCTests`, and `BulkDbProcessorTests`
- Validated parameter parsing for SQL Server and ODBC, including named assignments and edge cases
- Added performance tests for critical components like bulk operations and parameter parsing
- Introduced helper classes like `AssertionHelpers` and `TempFileHelper` for streamlined test setup
- Improved test directory organization to align with library modules

### [147c6d2] - 2025-09-16
**Refactor tests to remove static config file dependencies**
- Removed references to `basic_config.ini`, `invalid_config.ini`, and `test_config.ini` from `ByteForge.Toolkit.Tests.csproj`
- Deleted the contents of the above configuration files
- Updated `LogTests.cs` to dynamically generate temporary `.ini` files for test configuration, eliminating reliance on static files
- Reorganized `using` directives in `LogTests.cs` for clarity
- Simplified test setup by replacing hardcoded paths with dynamically generated configuration data
- Improved maintainability by removing unused configuration files and their references

### [cf27d38] - 2025-09-16
**751 Tests. 750 Green, 1 Skipped.**
- Upgraded NuGet packages to latest versions for compatibility
- Replaced FluentAssertions with AwesomeAssertions across tests
- Refactored ConsoleSpinner for improved initialization and threading
- Enhanced thread safety in Configuration class with locking
- Introduced DefaultValueProvider in GlobalizationInfo
- Incremented assembly version for new release
- Updated test infrastructure and assertion methods for consistency
- Improved error handling in tests to enforce stricter validation
- Refactored progress bar logic in ConsoleUtil for maintainability
- Enhanced globalization tests with culture-specific formatting
- Updated database tests for compatibility with AwesomeAssertions
- Replaced FluentAssertions references in TestingPlan.md
- Performed general code cleanup and formatting improvements
- Improved logging and validation in DBAccessScriptExecutionTests
- Refactored helper classes for better assertion logic
- Enhanced code readability with comments and refactoring

### [fb6d5a2] - 2025-09-16
**751 Tests. 736 Green, 14 Red, 1 Skipped.**
- Introduced `IConfigurationManager` interface for better modularity, testability, and adherence to the Dependency Inversion Principle
- Refactored `Configuration` class to implement `IConfigurationManager` and replaced static fields with instance-based fields for thread safety
- Added lazy initialization for globalization settings and improved error handling for invalid configurations
- Enhanced array and collection support, including `List<T>`, `IList<T>`, and `IEnumerable<T>`, with normalized array key handling
- Added culture-aware globalization support for formatting dates, times, numbers, and currency
- Updated `CLAUDE.md` and `readme.md` to document array handling, globalization, and best practices
- Added comprehensive unit tests for core functionality, arrays, globalization, error handling, and performance
- Improved thread safety for concurrent access and save operations
- Updated `ByteForge.Toolkit.csproj` and incremented version to `10.7.0.0916`
- Added `ConfigurationTestHelper` and `ConfigurationTestModels` for isolated testing
- Enhanced handling of Unicode, special characters, and escaped values in configuration files
- Updated `BuildSolution.bat` and related documentation to reflect build process changes

### [89cfd6e] - 2025-09-16
**Add ProgressSpinner and enhance console utilities**
- Introduced `ProgressSpinner` for long-running operations
- Updated `ByteForge.Toolkit.csproj` to include `ProgressSpinner`
- Enhanced `CommandParser` for better argument processing
- Refactored `ConsoleSpinner` to use `ConsoleUtil.IsConsoleAvailable`
- Replaced `HasConsole()` with `IsConsoleAvailable` in `ConsoleUtil`
- Added new console utilities: line drawing, centering, alignment
- Enhanced `DrawProgressBar` to support indeterminate progress
- Simplified `EmailAttachmentHandler.Dispose` for better cleanup
- Improved logging in `DBAccess` for better readability
- Added `WrapList` method to `StringUtil` for formatted lists
- Updated assembly version to `10.6.0.0916`
- Removed redundant and unused code across multiple files

### [d2c1629] - 2025-09-16
**Enhance command-line parsing and type safety**
- Introduced `<ThisDir>` property in `ByteForge.Tasks.targets` for improved path handling
- Updated `CommandParser` to accept `RootCommandBuilder` for better configuration
- Added `GlobalOption` class for defining global command-line options
- Enhanced `RootCommandBuilder` to support global options and banner actions
- Modified `ConsoleSpinner` to check for console redirection
- Updated array syntax in `BulkDbProcessor` and related tests for conciseness
- Introduced nullable reference types across various files for improved type safety
- Updated assembly version to `9.32.0.0916`
- Added `#pragma warning disable` directives in tests to suppress nullable warnings
- Improved readability in `AssertCollectionsEquivalent` method
- Ensured compatibility of tests with updated `BulkDbProcessor`
- Simplified `ConvertTo<T>` method signatures in `TypeConverter`
- Enhanced handling of nullable types in `DBAccess` related tests

### [ca1410c] - 2025-09-16
**Update permissions in settings and modify solution items**
- Added permission for `Bash(rm:*)` and updated existing permissions in `settings.local.json`
- Removed permissions for `Bash(msbuild:*)` and `Bash(sqlcmd:*)`, while retaining `Bash(dotnet restore:*)`
- Updated the order of permissions, moving `Bash(mkdir:*)` to a different position
- Modified the `ByteForge.Toolkit.sln` file to include `.claude\settings.local.json` and added `robo.rcj`
- Removed the entry for `CLAUDE.md` from the project section

### [b4a104b] - 2025-09-15
**Refactor BulkDbProcessor and enhance documentation**
- Modified several files in the `ByteForge.Toolkit` project, focusing on the `BulkDbProcessor` class and its functionalities
- Added commands in `settings.local.json` for SQL execution and file removal to enhance the build/test process
- Replaced `BulkDbProcessor.cs` with `BulkDbProcessor.Core.cs`, `BulkDbProcessor.Operations.cs`, and `BulkDbProcessor.SqlGeneration.cs` for better code organization
- Introduced a generic class in `BulkDbProcessor.Core.cs` for handling bulk database operations, including detailed documentation
- Implemented bulk insert and upsert operations in `BulkDbProcessor.Operations.cs` using `SqlBulkCopy` for efficiency
- Added SQL script generation for table creation and operations in `BulkDbProcessor.SqlGeneration.cs`
- Included methods for managing DataTable structure and property mappings in `BulkDbProcessor.TableManagement.cs`
- Removed the old `BulkDbProcessor.cs` file, indicating significant restructuring
- Improved documentation in `DBOptions.cs` by clarifying the behavior of a property related to allowing null strings

### [f3fb23e] - 2025-09-15
**Enhance BulkDbProcessor and related components**
- Updated `settings.local.json` to modify build commands and add SQL command
- Added `using System.Diagnostics;` for process management in `BulkDbProcessor.cs`
- Refined `MaxLength` logic to handle edge cases in `BulkDbProcessor.cs`
- Introduced `GenerateCreateTableSql` for SQL table creation and validation
- Added utility methods for SQL object name formatting in `BulkDbProcessor.cs`
- Updated `GetRecord` and `GetRecords` to allow null strings in data conversion
- Introduced `AllowNullStrings` property in `DBOptions.cs` for database handling
- Enhanced `TypeConverter.cs` to support flexible data handling with null strings
- Added comprehensive unit tests for edge cases in `BulkDbProcessorEdgeCaseTests.cs` and `BulkDbProcessorTests.cs`
- Implemented async methods for bulk operations in `BulkDbProcessorTests.cs`
- Improved error handling for bulk operations, especially for ODBC and invalid connections

### [e0cbba9] - 2025-09-15
**607 Tests. 606 Green, 1 Skipped.**
- Updated `settings.local.json` to add permissions for new Bash commands including `BuildSolution.bat` and others
- Added null and empty query handling in `DBAccess.Parameters.cs` to improve parameter parsing robustness
- Created `DBAccessParameterParsingSimpleTests.cs` for unit tests covering various parameter parsing scenarios
- Introduced `DBAccessParameterParsingTests.cs` with additional tests for named parameter assignments, ODBC behavior, and performance

### [3ba34f3] - 2025-09-15
**Refactor CSV processing and enhance database access**
- Replaced obsolete `DataProcessor` with `RowHandler` in `CSVReader.cs`, removing outdated methods and improving column name handling
- Introduced a new dictionary for column-to-property mapping in `CSVRecord.cs`, updated constructors for better flexibility, and added a `ToDictionary` method
- Simplified `ParseParameters` in `DBAccess.Logging.cs` by removing the `allowRepetition` parameter
- Overhauled parameter handling in `DBAccess.Parameters.cs`, renaming `AddParameters` to `AddParametersToCommand` and refining parsing logic for SQL Server
- Added comprehensive unit tests for script execution scenarios in `DBAccess.ScriptExecutionTests.cs`
- Updated version number in `AssemblyInfo.cs` from `9.8.0.0915` to `9.23.0.0915`
- Streamlined invalid test inputs in `BooleanParserTests.cs`
- Enhanced `Parser.cs` with additional parsing logic for `byte` types and improved error handling

### [8f68ae3] - 2025-09-15
**564 Tests. 563 Green, 1 Skipped.**
- Introduced `AddParametersToCommand` for better parameter handling
- Replaced inline parameter logic in `CreateCommand` with the new method
- Added `ParseParameters` to extract parameter names from SQL queries
- Updated `LogQueryError` to utilize the new parameter parsing logic
- Ensured consistency in `DBAccess.ScriptExecution` with new parameter handling
- Made minor adjustments in `TypeConverter` for nullable type handling
- Enhanced maintainability, readability, and functionality of the database access layer

### [6bcfa66] - 2025-09-15
**Update timing method, documentation, and versioning**
- Refactored `TimeFunc<T>` to use `Stopwatch` for better timing precision
- Renamed parameter in `AddParameters` from `batch` to `query` for clarity
- Updated assembly version from `9.7.0.0914` to `9.8.0.0915`
- Added "TestUnitDB Setup Instructions" to `03_DatabaseSetupInstructions.md`
- Introduced "TestAccess.accdb Database Setup Guide" in `04_CreateTestAccess.md`
- Enhanced `Database_Testing_Analysis.md` with detailed testing strategy
- Updated `TestingPlan.md` to outline comprehensive testing strategy and coverage goals

### [f2bfea6] - 2025-09-14
**Enhance documentation and improve CSV handling**
- Updated XML documentation in `DBColumnAttribute.cs` and added new constructors for better initialization
- Modified `OptionAttribute.cs` to allow `Name` property to be set and improved documentation
- Created new documentation files for Configuration Management, CSV Processing Library, and various components
- Refined logic in `BulkDbProcessor.cs` for setting `MaxLength` and improved primary key validation
- Enhanced error handling in `DBAccess.Methods.cs` for better logging of execution time
- Updated `TypeConverter.cs` to handle nullable types more effectively
- Introduced comprehensive documentation for ByteForge.Toolkit, including CLI, Data Structures, Logging, and Security
- Updated `AssemblyInfo.cs` with a new version number and cleaned up the test project configuration
- Added unit tests for CSV reading and writing functionalities in `CSVReaderTests.cs` and `CSVWriterTests.cs`
- Created a comprehensive testing plan in `TestingPlan.md` to ensure significant test coverage

### [2c5a944] - 2025-09-13
**507 Tests. 506 Green. 1 Skipped.**
- Updated `settings.local.json` to allow additional Bash permissions
- Improved `CreateCommand` method in `DBAccess.Factory.cs` with better parameter handling and configurable command timeout
- Enhanced database type handling in `DBAccess.Parameters.cs` for ODBC compatibility
- Added `04_CreateTestAccess.md` for comprehensive ODBC testing setup instructions
- Updated `ByteForge.Toolkit.Tests.csproj` to ensure `TestUnitDB.accdb` is copied to output
- Introduced new constants and methods in `DatabaseTestHelper.cs` for Access database configuration and verification
- Added `TestUnitDB.accdb` as the test database for ODBC tests
- Created `DBAccessMethodsODBCTests.cs` for unit testing `DBAccess` with Access database
- Refined `DBAccessMethodsSQLServerTests.cs` for clarity and exception handling, renaming the class for specificity
- Overall improvements enhance functionality and test coverage for `DBAccess`, particularly with ODBC and Access

### [526245a] - 2025-09-13
**478 Tests. 477 Green, 1 Skipped.**
- Updated `settings.local.json` to allow `Bash(mkdir:*)` and removed specific permissions
- Changed `MaxLength` in `DBColumnAttribute.cs` to a non-nullable integer and added new constructors
- Modified `ByteForge.Toolkit.csproj` to include a new `CHANGELOG.md`
- Updated `BulkDbProcessor.cs` to check for `MaxLength` as `0` instead of `null`
- Changed default constructor in `DBAccess.Core.cs` to initialize with an empty string
- Added comments in `EmailAttachmentHandler.cs` regarding optional parameters
- Updated assembly version in `AssemblyInfo.cs` to `9.0.0.0913`
- Created new SQL scripts for setting up and seeding the test database
- Added `03_DatabaseSetupInstructions.md` for detailed setup guidance
- Introduced `Database_Testing_Analysis.md` to outline a comprehensive testing strategy
- Created `TestConfiguration.ini` for database connection configurations
- Updated `TestingPlan.md` to reflect a new testing strategy for higher code coverage
- Added helper classes and test entities for improved database testing
- Introduced `FluentAssertionsExtensions.cs` for additional assertion methods
- Improved test structure in `CSVReaderTests.cs` and `CSVWriterTests.cs`
- Created `DBAccessCoreTests.cs` and `DBAccessMethodsTests.cs` for core functionality testing

### [d3040e6] - 2025-09-13
**429 Tests. 428 Green, 1 Skipped.**
- Removed DotNetZip from project
- Added new constructor to `CSVColumnAttribute` for default initialization
- Improved build process in `ByteForge.Tasks.targets` with new targets and logging
- Updated `readme.md` to reflect enhancements in the Mail module
- Introduced `TempFileAttachment` class for managing temporary files

### [b933e99] - 2025-09-13
**Enhance documentation across ByteForge.Toolkit**
- Updated `ByteForge.Toolkit.csproj` to include new readme files for `Data` and `Zip` directories; removed outdated documentation
- Enhanced `readme.md` with a structured modules section for better navigation
- Revised `Configuration.md` to clarify features and usage examples
- Updated `INI Configuration Code-Review.md` to reflect design improvements and error handling enhancements
- Expanded `Logging.md` to detail features, core components, and best practices for effective implementation
- Improved `Security.md` with comprehensive information on AES encryption classes and security considerations
- Enhanced `Utils.md` to describe available utility classes and their functionalities
- Revised `DataStructures.md` for clearer overviews and usage examples
- Updated `README.md` to reflect the latest changes across all modules
- Improved `CLI.md` with detailed explanations of the command-line parsing toolkit
- Clarified functionalities in `Data_CSV.md` and `Data_Database.md` with usage examples
- Enhanced `Mail` module documentation to include intelligent attachment handling and security features
- Overall, these changes significantly improve documentation clarity, usability, and organization

### [5f04828] - 2025-09-13
**Merged DotNetZip library by Dino Chiesa**
- Added DotNetZip compression library with classes for compression and decompression
- Updated `settings.local.json` to include user-specific directories
- Refactored `RootCommandBuilder.cs` for improved command-line parser options
- Enhanced documentation in `readme.md` for new features and best practices
- Improved `EmailAttachmentHandler` for better temporary file management
- Updated unit tests to reflect changes in `EmailAttachmentHandler`
- Introduced new UI elements in `PasswordDialog` for secure password entry
- Added `WinFormsSelfExtractorStub` for self-extracting zip functionality
- Comprehensive documentation updates across multiple files for clarity and usability
- Enhanced error handling and usability in `ZipFile` and related classes

### [5c9565a] - 2025-09-12
**432 Tests. 424 Green, 7 Reds, 1 Skipped.**
- Improved formatting and clarity in CSV Library documentation, adding usage examples and best practices
- Simplified `using` statement syntax in `EmailAttachmentHandler.cs` and removed redundant file existence checks
- Made `EmailAttachmentHandler` class public in `FileBucket.cs` for better accessibility
- Added reference to `System.IO.Compression` in `ByteForge.Toolkit.Tests.csproj` for zip file operations
- Expanded `EmailAttachmentHandlerTests.cs` with comprehensive unit tests covering various scenarios and edge cases
- Cleaned up `Utils.cs` by removing unnecessary using directives

### [507e576] - 2025-09-12
**411 Tests. 410 Green, 1 Skipped.**
- Refactored `GetFileExtension` and `GetMimeType` methods in `AudioFormatDetector.cs` to use C# 8.0 switch expressions for improved readability
- Added a comprehensive suite of unit tests in `AudioFormatDetectorTests.cs` to validate the functionality of `DetectFormat`, `GetFileExtension`, and `GetMimeType`
- Tests cover various audio formats, edge cases, and performance scenarios, including handling of null inputs, empty arrays, and corrupted headers
- Verified that all enum values have proper mappings for file extensions and MIME types, and assessed performance for repeated detection calls and large audio data handling

### [ce1b7e2] - 2025-09-12
**386 Tests. 385 Green, 1 Skipped.**
- Implemented tests for initialization and default values
- Verified adding and removing key-value pairs
- Tested processing of templates with placeholders
- Handled escape sequences and edge cases
- Ensured correct behavior with null, empty, and case-insensitive inputs
- Assessed performance with large templates and multiple operations

### [f14073e] - 2025-09-12
**353 Tests. 352 Green, 1 Skipped.**
- Updated `IsConsoleLoggingEnabled` to use null-conditional operator for safety
- Changed `logger` in `TimingUtil` to be readonly and used `Log.Instance` directly
- Added `EnumExtensionsTests` for testing enum description retrieval
- Enhanced `IOUtilsTests` with various scenarios for `GetFiles` method
- Introduced `TimingUtilTests` to validate timing functionality and performance

### [ec9fd5c] - 2025-09-12
**307 Tests. 306 Green, 1 Skipped.**
- Implemented tests for Unix timestamp to DateTime conversion
- Added tests for DateTime to Unix timestamp conversion
- Included tests for time zone conversions and edge cases
- Ensured methods handle extreme values and maintain accuracy

### [8b1960a] - 2025-09-11
**290 Tests. 289 Green. 1 Skipped (Manual)**
- Updated `settings.local.json` to include a new "permissions" section for Bash commands
- Improved documentation and constructor in `CSVColumnAttribute.cs`, removing the `DbType` property
- Modified `ByteForge.Toolkit.csproj` to conditionally set `PathMap` based on configuration
- Clarified build commands and testing instructions in `CLAUDE.md`
- Initialized `Delimiter` property with a default value in `CSVFormat.cs`
- Refactored `CSVReader.cs` to remove logger dependency and simplify CSV handling
- Implemented `IsValid` method in `CSVRecord.cs` for better validation
- Simplified `Flush` method in `CSVWriter.cs`
- Updated `ICSVRecord.cs` with new validation methods and improved documentation
- Refactored `BinarySearchTree.cs` for better readability
- Enhanced logging classes for improved configuration handling
- Optimized performance in `AESEncryption.cs` for encryption and decryption methods
- Added a new `.runsettings` file for test configuration and code coverage
- Enhanced `BuildSolution.bat` for better feedback and configuration handling
- Updated `ByteForge.Toolkit.Tests.csproj` with new testing tool references
- Improved test coverage and performance in various test files
- Added methods in `BooleanParser.cs` for removing recognized true and false values
- Updated `ConsoleUtil.cs` to check for console window presence
- Simplified time zone conversion logic in `DateTimeUtil.cs`
- Enhanced `Utils.cs` to format US phone numbers and handle letter-to-number conversions

## [Version History] - Major Releases

### [2113d06] - 2025-09-11
**Add XML documentation to test methods**
- Introduced comprehensive XML documentation comments for various test methods in the ByteForge.Toolkit.Tests project
- Enhanced code readability and maintainability with clear descriptions and remarks for each test
- Updates cover tests related to compilation, CSV reading, binary search trees, logging, encryption, and date/time parsing
- Each test method now includes a summary of its purpose and additional context for better understanding

### [f99c35d] - 2025-09-11
**Refactor ByteForge.Toolkit with enhancements and fixes**
- Simplified command building logic in `CommandBuilder.cs`
- Improved clarity and functionality in `CSVReader.cs` with added comments and updated method signatures
- Refactored type mapping in `BulkDbProcessor.cs` using pattern matching
- Streamlined connection creation in `DBAccess.Factory.cs` by removing unnecessary parameters
- Introduced new asynchronous methods in `DBAccess.Methods.cs` for better database interaction
- Updated logging methods in `BaseLogger.cs`, `NullLogger.cs`, and `StaticLoggerAdapter.cs` to simplify calls
- Changed default connection timeout in `DBOptions.cs` and improved error handling
- Enhanced object population from `DataRow` in `TypeConverter.cs` with better custom converter support
- Updated assembly version in `AssemblyInfo.cs` for the new release

### [4ff32a7] - 2025-09-10
**Refactor error handling and improve logging features**
- In `CSVReader.cs`, inverted exception handling logic for malformed rows based on `RowHandler`
- Refactored color determination in `ConsoleLogger.cs` to use a switch expression and added `HasConsole` method
- Updated `_PreviousConsoleLoggerLevel` in `Log.cs` to be nullable and improved logging methods for null handling
- Enhanced error handling in `AESEncryption.cs` to throw `CryptographicException` on decryption failure
- Improved formatting and added progress updates in `TestingPlan.md`
- Modified `ByteForge.Toolkit.Tests.csproj` to include additional configuration files for testing
- Cleaned up several CSV files to remove extra fields and ensure proper formatting
- Simplified constructor test in `CSVReaderTests.cs` and removed unnecessary malformed CSV test
- Added static constructor in `LogTests.cs` for initializing logging configuration
- Updated tests in `AESEncryptionTests.cs` and `EncryptorTests.cs` to ensure specific exceptions are thrown for invalid inputs
- Clarified exception messages in `DateTimeParserTests.cs` and removed flawed time parsing test

### [bc5d4d6] - 2025-09-10
**Refactor and enhance ByteForge.Toolkit functionality**
- Removed specific permission settings in `settings.local.json`
- Updated `excludeFolders` in `ByteForge.Tasks.targets` to include "tests"
- Added new compile items for `NullLogger.cs` and `StaticLoggerAdapter.cs` in `ByteForge.Toolkit.csproj`
- Modified solution file to include a reference for `ByteForge.Toolkit.Tests`
- Replaced `_initialized` field with `IsInitialized` in `Configuration.cs`
- Enhanced `BufferedReader.cs` with a new `Peek` method
- Significant updates to `CSVReader.cs` for improved logging and CSV handling
- Added new methods for node insertion and removal in `BinarySearchTree.cs`
- Updated `Log.cs` to indicate if the static Log instance is initialized
- Made internal members visible to the test assembly in `AssemblyInfo.cs`
- Created `ByteForge.Toolkit.Tests.csproj` for a new test project
- Added and updated multiple test files to ensure comprehensive testing
- Created new test data files for various scenarios and edge cases
- Documented test implementation details in `TestImplementationSummary.md`
- Improved error handling and parsing logic in `DateTimeParser.cs`

### [1c490db] - 2025-09-10
**Update permissions and add testing plan**
- Updated `settings.local.json` to allow `Bash(mkdir:*)` command
- Modified `ByteForge.Toolkit.sln` to include new solution items: `ByteForge.Tasks.targets` and `CLAUDE.md`
- Introduced a comprehensive testing plan in `TestingPlan.md`, outlining strategies, coverage targets, required packages, and future enhancements for the `ByteForge.Toolkit` library

### [11f0133] - 2025-09-10
**Add CLAUDE.md for project documentation**
- Introduced `CLAUDE.md` to provide guidance on the ByteForge.Toolkit project
- Included sections on project overview, build commands, and package management
- Documented architecture, configuration management, and database access features
- Outlined security features, module dependencies, and common development patterns
- Provided notes on file organization and important project details

### [55ce397] - 2025-09-10
**Enhance ByteForge.Toolkit with new features and fixes**
- Updated `CSVColumnAttribute` and `DBColumnAttribute` to include new properties for better metadata handling
- Improved cleanup logic in `ByteForge.Tasks.targets` for unused files after builds
- Changed project target framework to 4.8 and updated language version to 8.0 in `ByteForge.Toolkit.csproj`
- Added command-line parsing support with new classes like `CommandParser` and `ConsoleSpinner`
- Refactored `Configuration` class for better structure and functionality
- Enhanced CSV processing library with support for custom delimiters and improved error handling
- Introduced `BulkDbProcessor` for bulk database operations with SQL Server support
- Updated documentation to reflect new features and best practices
- Refactored methods in `DBAccess` for improved signatures and functionality, including better async support
- Enhanced logging framework with session-based logging capabilities
- Improved `MailUtil.cs` for robust email sending, including attachment handling
- Added new utility classes like `DateTimeUtil`, `EnumExtensions`, and `StringUtil` for enhanced functionality
- Updated `readme.md` to provide clearer guidance on usage and configuration

## [Major Version] - 4.43.0.0410

### [7b0817a] - 2025-04-10
**Update the library repository**
- Introduced new attribute-based mapping system:
  - Added `CSVColumnAttribute` and `DBColumnAttribute` for property-level metadata
  - Updated project file to include these new attributes
- Enhanced CSV processing infrastructure:
  - Introduced `CSVRecord`, `ICSVRecord`, and comprehensive validation support
  - Added robust error handling with `ConversionException` and `DataProcessingException`
- Expanded CSV detection and parsing capabilities:
  - Improved `CSVFormat.DetectFormat` logic
  - Implemented `ToString`, `Equals`, and `GetHashCode` for `CSVFormat`
- Implemented attachment handling for email:
  - Created `EmailAttachmentHandler` with compression and multi-part support
  - Added related models: `AttachmentProcessResult`, `SkippedFile`, `FileBucket`, `PartInfo`, and `ProcessingMethod`
- Overhauled logging system:
  - Extended `ILogger` interface and `BaseLogger` for fine-grained levels (Trace, Notice, Fatal)
  - Added contextual information and correlation support
  - Improved log formatting and exception data capture
- Refined INI configuration system:
  - Now supports `DefaultValueAttribute` for property defaults
  - Skips default-valued properties when saving
  - Preserves structure and comments in INI files
  - Added comprehensive documentation and refactored core logic
- Enhanced database access layer:
  - Added `TimeFunc` helpers for measuring execution time
  - Expanded XML docs across DBAccess and DBOptions for clarity
- Miscellaneous improvements:
  - Added `BooleanParser` with extended truthy/falsy value recognition
  - Introduced `EnumExtensions` for retrieving enum descriptions
  - Improved `DateTimeParser` to support more flexible formats with caching
- Added new solution file `ByteForge.Toolkit.sln`
- Included new references and files in `ByteForge.Toolkit.csproj`
- Updated `.gitignore` and `AssemblyInfo.cs` version to 4.43.0.0410

## [Historical Releases]

### [7bf7351] - 2025-02-14
**Syncing library**
- Library synchronization update

### [6d55366] - 2024-12-14
**Uploading initial files**
- Initial file upload and project setup

### [fb5f8e7] - 2024-12-14
**Initial commit**
- Repository initialization
- Basic project structure established

---

*This changelog is automatically generated from git commit messages and may be updated as needed.*

