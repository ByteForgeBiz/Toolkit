# ByteForge.Toolkit Changelog

All notable changes to this project will be documented in this file.

## [Latest] - Recent Changes

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