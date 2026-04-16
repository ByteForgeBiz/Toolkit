# ByteForge.Toolkit.Modern

The primary library of the ByteForge Toolkit solution. It provides a comprehensive set of enterprise-grade utilities organised into focused modules, all compiled from a single codebase that targets .NET Framework 4.8, .NET 8.0, and .NET 9.0.

---

## Target Frameworks and Namespace

| Framework | TFM | Root namespace |
|-----------|-----|----------------|
| .NET Framework 4.8 | `net48` | `ByteForge.Toolkit` |
| .NET 8.0 | `net8.0` | `ByteForge.Toolkit` |
| .NET 9.0 | `net9.0` | `ByteForge.Toolkit` |

The project file is `ByteForge.Toolkit.Modern.csproj`. Assembly and documentation XML are emitted to `bin\ByteForge.Toolkit.Modern.xml`.

Conditional compilation uses `#if NETFRAMEWORK` for framework-specific code paths (e.g. `System.Web`, legacy data providers).

---

## Module Breakdown

### CommandLine (`ByteForge.Toolkit.CommandLine`)

Attribute-driven command-line parsing built on top of `System.CommandLine` (available on net8.0/net9.0; the naming convention binder is available on all targets).

**Key files:**

| File | Purpose |
|------|---------|
| `CommandParser.cs` | Parses `string[]` args into commands and options |
| `CommandBuilder.cs` | Fluent builder for wiring up commands |
| `RootCommandBuilder.cs` | Entry point — builds the root `System.CommandLine` command |
| `ParseResult.cs` | Encapsulates the outcome of a parse operation |
| `GlobalOption.cs` | Defines options available across all commands |
| `ConsoleSpinner.cs` / `ProgressSpinner.cs` | Console progress indicators |
| `Attributes/CommandAttribute.cs` | Marks a class as a command |
| `Attributes/OptionAttribute.cs` | Declares a command option with name, alias, description, and defaults |

**Pattern:**

```csharp
[Command("export", "Export records to CSV")]
public class ExportCommand
{
    [Option("output", "o", "Output file path", required: true)]
    public string OutputPath { get; set; } = "";

    [Option("limit", "l", "Maximum rows", defaultValue: 1000)]
    public int Limit { get; set; }
}
```

---

### Configuration (`ByteForge.Toolkit.Configuration`)

INI-based configuration management backed by `Microsoft.Extensions.Configuration.Ini`. The main class `Configuration` implements `IConfigurationManager` and provides both a singleton default instance and constructable instances for multi-config scenarios.

**Key files:**

| File | Purpose |
|------|---------|
| `Configuration.cs` | Main class — loads, reads, and saves INI files |
| `ConfigSection.cs` | Base class for strongly-typed section models |
| `Interfaces/IConfigSection.cs` | Section interface |
| `Interfaces/IConfigurationManager.cs` | Manager interface |
| `Models/GlobalizationInfo.cs` | Culture and formatting settings |
| `Helpers/ParsingHelper.cs` / `IParsingHelper.cs` | INI value parsing |
| `Helpers/DefaultValueHelper.cs` | Resolves attribute-declared default values |
| `Attributes/ArrayAttribute.cs` | Maps a property to a repeating section for array support |
| `Attributes/DictionaryAttribute.cs` | Maps a property to key/value sections |
| `Attributes/ConfigNameAttribute.cs` | Overrides the INI key name for a property |
| `Attributes/DoNotPersistAttribute.cs` | Excludes a property from being written back |
| `Attributes/DefaultValueProviderAttribute.cs` | Points to a provider class for complex defaults |
| `AssemblyException.cs` | Thrown when an attribute references a type not found in the assembly |

**Example INI:**

```ini
[Data Source]
SelectedDB=Production

[Production]
sType=SQLServer
sServer=PRODSERVER\INSTANCE1
sDatabaseName=MyApp
esUser=[encrypted_user]
esPass=[encrypted_password]
```

**Usage:**

```csharp
// Singleton access
Configuration.Initialize("appsettings.ini");
var section = Configuration.GetSection<DatabaseOptions>("Production");

// Instance access
var cfg = new Configuration("custom.ini");
cfg.Save();
```

---

### Core (`ByteForge.Toolkit`)

Low-level bootstrap utilities.

| File | Purpose |
|------|---------|
| `Core.cs` | Foundational helpers shared across modules |
| `Core.WinScpResourceManager.cs` | Extracts the embedded `WinSCP.exe` resource to a temp path for runtime use |

---

### Data

The Data module is split into three sub-areas:

#### Data.CSV (`ByteForge.Toolkit.Data`)

High-performance CSV reading and writing with format auto-detection.

| File | Purpose |
|------|---------|
| `CSVReader.cs` | Reads CSV files; supports progress reporting |
| `CSVWriter.cs` | Writes CSV files with configurable quoting and delimiters |
| `CSVRecord.cs` / `ICSVRecord.cs` | Record representation and interface |
| `CSVFormat.cs` | Format definitions (delimiter, quote char, encoding, etc.) |
| `BufferedReader.cs` | Low-level buffered file reader |
| `ConversionException.cs` | Thrown on type conversion failures |
| `DataProcessingException.cs` | General processing errors |
| `ValidationErrors.cs` | Accumulated validation error collection |

Attributes for entity mapping live in `Data/Attributes/CSVColumnAttribute.cs`.

#### Data.Database (`ByteForge.Toolkit.Data`)

Multi-database access layer with SQL Server and ODBC support. The two main classes (`DBAccess` and `BulkDbProcessor<T>`) are each split into focused partial classes.

**DBAccess partial classes:**

| File | Responsibility |
|------|----------------|
| `DBAccess.Core.cs` | Class declaration, configuration schema documentation |
| `DBAccess.Factory.cs` | Connection factory — SQL Server vs ODBC |
| `DBAccess.Methods.cs` | Query execution: `GetDataTable`, `ExecuteNonQuery`, scalar reads, etc. |
| `DBAccess.Parameters.cs` | Parameter building and SQL injection prevention |
| `DBAccess.Transactions.cs` | `BeginTransaction`, `Commit`, `Rollback`, `DBTransaction` wrapper |
| `DBAccess.ScriptExecution.cs` | Executes multi-statement SQL scripts with `GO` parsing |
| `DBAccess.Logging.cs` | Query timing and diagnostic logging |
| `DBAccess.Properties.cs` | Public properties: `LastException`, connection state, options |

**BulkDbProcessor<T> partial classes:**

| File | Responsibility |
|------|----------------|
| `BulkDbProcessor.Core.cs` | Core setup and configuration |
| `BulkDbProcessor.Operations.cs` | Bulk insert, update, delete with cancellation token support |
| `BulkDbProcessor.TableManagement.cs` | Temp table creation and management |
| `BulkDbProcessor.SqlGeneration.cs` | Generates INSERT/UPDATE/MERGE SQL from entity reflection |
| `BulkDbProcessor.Events.cs` | `ProgressReported` event and event args |

**Supporting types:**

| File | Purpose |
|------|---------|
| `DBOptions.cs` | Connection configuration (server, database, credentials, timeouts) |
| `DBTransaction.cs` | Lightweight transaction wrapper |
| `TypeConverter.cs` | Maps .NET types to database types and back |
| `ScriptExecutionResult.cs` | Result of a multi-statement script execution |

**DBAccess configuration keys** (loaded from INI sections):

| Key | Property | Description |
|-----|----------|-------------|
| `sType` | `DatabaseType` | `SQLServer` or `ODBC` |
| `sServer` | `Server` | Server name or IP |
| `sServerDSN` | `ServerDSN` | ODBC DSN |
| `sDatabaseName` | `DatabaseName` | Database name |
| `esUser` | `EncryptedUser` | AES-encrypted username |
| `esPass` | `EncryptedPassword` | AES-encrypted password |
| `sConnectionString` | `ConnectionString` | Direct connection string (overrides other settings) |
| `bEncrypt` | `UseEncryption` | Enable connection encryption |
| `iConnectionTimeout` | `ConnectionTimeout` | Seconds (default 60) |
| `iCommandTimeout` | `CommandTimeout` | Seconds (default 240) |
| `bTrustedConnection` | `UseTrustedConnection` | Windows auth (default false) |

#### Data.Audio (`ByteForge.Toolkit.Data`)

| File | Purpose |
|------|---------|
| `AudioFormatDetector.cs` | Detects audio file formats from byte signatures |

#### Data.Attributes

| File | Purpose |
|------|---------|
| `DBColumnAttribute.cs` | Maps a class property to a database column name and type |
| `CSVColumnAttribute.cs` | Maps a class property to a CSV column header |

---

### DataStructures (`ByteForge.Toolkit`)

| File | Purpose |
|------|---------|
| `BinarySearchTree.cs` | Generic binary search tree with in-order traversal |
| `Url.cs` | URL construction, path manipulation, and query string helpers |

---

### Json (`ByteForge.Toolkit`)

Delta-aware JSON serialization using `Newtonsoft.Json`.

| File | Purpose |
|------|---------|
| `JsonDeltaSerializer.cs` | Serializes only changed properties between two object snapshots |
| `DeltaContractResolver.cs` | Custom `IContractResolver` that drives the delta comparison |

---

### Logging (`ByteForge.Toolkit.Logging`)

Thread-safe, multi-target logging system. All modules use the static `Log` facade or inject `ILogger`.

**Logger implementations:**

| Class | Output |
|-------|--------|
| `BaseLogger` | Abstract base — manages level filtering and async dispatch |
| `CompositeLogger` | Fans out to multiple `ILogger` instances simultaneously |
| `ConsoleLogger` | Writes to stdout with optional colour |
| `FileLogger` | Appends to a log file with configurable rotation |
| `SessionFileLogger` | Creates a new dated file per session |
| `NullLogger` | Discards all output (useful in tests) |
| `StaticLoggerAdapter` | Bridges an `ILogger` instance to the static `Log` API |

**`Log` class** (`Log.cs`) extends `CompositeLogger` and pre-wires a `ConsoleLogger`, a `FileLogger`, and an optional `DatabaseLogger`. It is the default entry point for application-level logging.

**`LoggingScopeContext`** (`LoggingScopeContext.cs`) — internal `AsyncLocal`-based suppression scope, prevents recursive logging during log writes.

**Models:**

| Class | Purpose |
|-------|---------|
| `LogEntry` | A single log record: timestamp, level, message, exception |
| `LogLevel` | Enum: `Verbose`, `Debug`, `Info`, `Warning`, `Error`, `Fatal` |
| `LogSettings` | Configuration model loaded from INI — path, level, clear-on-start |
| `FileLoggerOptions` | File logger settings: path, max size, rotation policy |
| `SessionFileLoggerOptions` | Per-session file settings |
| `AsyncOptions` | Controls async dispatch queue size and overflow behaviour |
| `CorrelationContext` | Ambient correlation ID for request tracing |

---

### Mail (`ByteForge.Toolkit`)

Email and MIME attachment processing.

| File | Purpose |
|------|---------|
| `EmailAttachmentHandler.cs` | Extracts attachments from raw MIME messages |
| `MailUtil.cs` | Email validation and address utilities |
| `TempFileAttachment.cs` | Manages a decoded attachment written to a temp file |
| `AttachmentProcessResult.cs` | Result of an attachment extraction operation |
| `PartInfo.cs` | Metadata for a single MIME part |
| `ProcessingMethod.cs` | Enum: how an attachment was handled (saved, skipped, error) |
| `SkippedFile.cs` | Records an attachment that was intentionally skipped |

---

### Net (`ByteForge.Toolkit`)

File transfer client built on top of `WinSCPnet`.

| File | Purpose |
|------|---------|
| `FileTransferClient.cs` | Main client — connect, upload, download, list remote files |
| `FileTransferConfig.cs` | Connection configuration: protocol, host, credentials, timeout |
| `TransferProtocol.cs` | Enum: `SFTP`, `FTP`, `FTPS`, `SCP` |
| `FileTransferItem.cs` | Describes a file to transfer (source, destination, metadata) |
| `FileTransferResult.cs` | Overall result of a transfer batch |
| `FileOperationResult.cs` | Result for a single file operation |
| `FileTransferProgress.cs` / `FileOperationProgress.cs` | Progress reporting models |
| `RemoteFileInfo.cs` | Metadata about a file on the remote server |
| `FileTransferException.cs` | Thrown on unrecoverable transfer errors |

---

### Security (`ByteForge.Toolkit`)

| File | Purpose |
|------|---------|
| `AESEncryption.cs` | AES-256 encrypt/decrypt with configurable key and IV |
| `Encryptor.cs` | Higher-level wrapper — used by `Configuration` and `DBAccess` to store encrypted credentials |
| `VBMath.cs` | Port of VBA `Rnd`/`Randomize` for compatibility with legacy encrypted data |

---

### Utilities (`ByteForge.Toolkit.Utilities`)

General-purpose helpers.

| File | Key capabilities |
|------|-----------------|
| `BooleanParser.cs` | Parses strings to `bool`: handles `yes/no`, `1/0`, `true/false`, locale variants |
| `DateTimeParser.cs` | Flexible date/time parsing with format lists and culture awareness |
| `DateTimeUtil.cs` | Date arithmetic, quarter/week calculations, formatting helpers |
| `StringUtil.cs` | Padding, truncation, similarity scoring, casing utilities |
| `IOUtils.cs` | Safe file copy, directory creation, progress-reporting stream wrappers |
| `TemplateProcessor.cs` | Token-substitution templating (`{{token}}` replacement) |
| `ConsoleUtil.cs` | Prompting, table rendering, colour helpers |
| `NameUtil.cs` | Name normalisation and display formatting |
| `HtmlUtil.cs` | HTML entity encoding/decoding and tag stripping |
| `EnumExtensions.cs` | `GetDescription()`, `Parse<T>()`, flag utilities |
| `TypeHelper.cs` | Reflection utilities for type comparison and generic constraints |
| `ValueConverterRegistry.cs` | Registry of `Func<string, T>` converters for dynamic type resolution |
| `Parser.cs` / `IParser.cs` | Generic parse interface and base implementation |
| `TimingUtil.cs` | `Stopwatch`-based block timing with optional log output |
| `Utils.cs` | Miscellaneous helpers that do not fit a dedicated class |

---

## Architectural Patterns

### Partial classes

Large classes are divided into partial class files by concern so each file remains focused and easy to navigate:

- `DBAccess` — 8 partials (Core, Factory, Methods, Parameters, Transactions, ScriptExecution, Logging, Properties)
- `BulkDbProcessor<T>` — 5 partials (Core, Operations, TableManagement, SqlGeneration, Events)

### Attribute-based programming

Configuration, CSV mapping, database ORM, and CLI definitions all use custom attributes for declarative, compile-time-checked configuration rather than convention strings.

### Costura.Fody dependency embedding

The `FodyWeavers.xml` configuration applies Costura.Fody to the build, embedding all referenced assemblies (including `WinSCPnet`) directly into the output DLL. This produces a single-file deployment artefact. The `Documentation` build configuration disables Fody so that Sandcastle Help File Builder (SHFB) can resolve dependencies normally.

### Thread safety

- `Configuration` uses a thread-safe singleton pattern with `ConcurrentDictionary` for section caching.
- All `BaseLogger` subclasses marshal writes through a lock or async queue.
- `LoggingScopeContext` uses `AsyncLocal<int>` for per-async-flow suppression depth.
- `BulkDbProcessor<T>` accepts `CancellationToken` on all long-running operations.

### `InternalsVisibleTo`

Internal APIs are accessible to both `ByteForge.Toolkit.Tests` and `ByteForge.Toolkit.Modern.Tests` so that test projects can exercise non-public behaviour without making those APIs public.

---

## Dependencies

| Package | Version | Condition |
|---------|---------|-----------|
| `Costura.Fody` | 6.0.0 | Build-time only |
| `Fody` | 6.9.3 | Build-time only |
| `Microsoft.Extensions.Configuration` | 9.0.10 | All targets |
| `Microsoft.Extensions.Configuration.Ini` | 9.0.10 | All targets |
| `Newtonsoft.Json` | 13.0.4 | All targets |
| `RestSharp` | 112.1.0 | All targets |
| `System.CommandLine` | 2.0.0-beta4 | net8.0, net9.0 only |
| `System.CommandLine.NamingConventionBinder` | 2.0.0-beta4 | All targets |
| `System.Data.SqlClient` | 4.9.0 | All targets |
| `System.Data.Odbc` | 9.0.10 | All targets |
| `System.Text.Json` | 9.0.10 | All targets |
| `System.IO.Pipelines` | 9.0.10 | All targets |
| `System.Memory` / `System.Buffers` | 4.6.x | All targets |
| `Microsoft.Bcl.AsyncInterfaces` | 9.0.10 | All targets |
| `System.ValueTuple` | 4.6.1 | All targets |
| `System.IO.Compression` | (framework ref) | net48 only |
| `System.Transactions` | (framework ref) | net48 only |

Project reference: `WinSCP\WinSCPnet.csproj`

---

## Directory Structure

```
Toolkit.Modern/
├── CommandLine/
│   ├── Attributes/
│   │   ├── CommandAttribute.cs
│   │   └── OptionAttribute.cs
│   ├── CommandBuilder.cs
│   ├── CommandParser.cs
│   ├── ConsoleSpinner.cs
│   ├── GlobalOption.cs
│   ├── ParseResult.cs
│   ├── ProgressSpinner.cs
│   └── RootCommandBuilder.cs
├── Configuration/
│   ├── Attributes/
│   │   ├── ArrayAttribute.cs
│   │   ├── ConfigNameAttribute.cs
│   │   ├── DefaultValueProviderAttribute.cs
│   │   ├── DictionaryAttribute.cs
│   │   └── DoNotPersistAttribute.cs
│   ├── Helpers/
│   │   ├── DefaultValueHelper.cs
│   │   ├── IParsingHelper.cs
│   │   └── ParsingHelper.cs
│   ├── Interfaces/
│   │   ├── IConfigSection.cs
│   │   └── IConfigurationManager.cs
│   ├── Models/
│   │   ├── ConfigSection.cs
│   │   └── GlobalizationInfo.cs
│   ├── AssemblyException.cs
│   └── Configuration.cs
├── Core/
│   ├── Core.cs
│   └── Core.WinScpResourceManager.cs
├── Data/
│   ├── Attributes/
│   │   ├── CSVColumnAttribute.cs
│   │   └── DBColumnAttribute.cs
│   ├── Audio/
│   │   └── AudioFormatDetector.cs
│   ├── CSV/
│   │   ├── BufferedReader.cs
│   │   ├── ConversionException.cs
│   │   ├── CSVFormat.cs
│   │   ├── CSVReader.cs
│   │   ├── CSVRecord.cs
│   │   ├── CSVWriter.cs
│   │   ├── DataProcessingException.cs
│   │   ├── ICSVRecord.cs
│   │   └── ValidationErrors.cs
│   ├── Database/
│   │   ├── BulkDbProcessor.Core.cs
│   │   ├── BulkDbProcessor.Events.cs
│   │   ├── BulkDbProcessor.Operations.cs
│   │   ├── BulkDbProcessor.SqlGeneration.cs
│   │   ├── BulkDbProcessor.TableManagement.cs
│   │   ├── DBAccess.Core.cs
│   │   ├── DBAccess.Factory.cs
│   │   ├── DBAccess.Logging.cs
│   │   ├── DBAccess.Methods.cs
│   │   ├── DBAccess.Parameters.cs
│   │   ├── DBAccess.Properties.cs
│   │   ├── DBAccess.ScriptExecution.cs
│   │   ├── DBAccess.Transactions.cs
│   │   ├── DBOptions.cs
│   │   ├── DBTransaction.cs
│   │   ├── ScriptExecutionResult.cs
│   │   └── TypeConverter.cs
│   └── Exceptions/
│       └── ParamArgumentsMismatchException.cs
├── DataStructures/
│   ├── BinarySearchTree.cs
│   └── Url.cs
├── Dependencies/
│   └── WinSCP.exe                  (EmbeddedResource)
├── Json/
│   ├── DeltaContractResolver.cs
│   └── JsonDeltaSerializer.cs
├── Logging/
│   ├── Implementations/
│   │   ├── BaseLogger.cs
│   │   ├── CompositeLogger.cs
│   │   ├── ConsoleLogger.cs
│   │   ├── FileLogger.cs
│   │   ├── NullLogger.cs
│   │   ├── SessionFileLogger.cs
│   │   └── StaticLoggerAdapter.cs
│   ├── Interfaces/
│   │   └── ILogger.cs
│   ├── Models/
│   │   ├── AsyncOptions.cs
│   │   ├── CorrelationContext.cs
│   │   ├── FileLoggerOptions.cs
│   │   ├── LogEntry.cs
│   │   ├── LogLevel.cs
│   │   ├── LogSettings.cs
│   │   └── SessionFileLoggerOptions.cs
│   ├── Log.cs
│   └── LoggingScopeContext.cs
├── Mail/
│   ├── AttachmentProcessResult.cs
│   ├── EmailAttachmentHandler.cs
│   ├── MailUtil.cs
│   ├── PartInfo.cs
│   ├── ProcessingMethod.cs
│   ├── SkippedFile.cs
│   └── TempFileAttachment.cs
├── Net/
│   ├── FileOperationProgress.cs
│   ├── FileOperationResult.cs
│   ├── FileTransferClient.cs
│   ├── FileTransferConfig.cs
│   ├── FileTransferException.cs
│   ├── FileTransferItem.cs
│   ├── FileTransferProgress.cs
│   ├── FileTransferResult.cs
│   ├── RemoteFileInfo.cs
│   └── TransferProtocol.cs
├── Properties/
│   └── AssemblyInfo.cs
├── Security/
│   ├── AESEncryption.cs
│   ├── Encryptor.cs
│   └── VBMath.cs
├── Utilities/
│   ├── BooleanParser.cs
│   ├── ConsoleUtil.cs
│   ├── DateTimeParser.cs
│   ├── DateTimeUtil.cs
│   ├── EnumExtensions.cs
│   ├── HtmlUtil.cs
│   ├── IOUtils.cs
│   ├── IParser.cs
│   ├── NameUtil.cs
│   ├── Parser.cs
│   ├── StringUtil.cs
│   ├── TemplateProcessor.cs
│   ├── TimingUtil.cs
│   ├── TypeHelper.cs
│   ├── Utils.cs
│   └── ValueConverterRegistry.cs
├── ByteForge.Toolkit.Modern.csproj
├── FodyWeavers.xml
├── FodyWeavers.xsd
└── version.txt
```

---

## Module Documentation

| Module | readme |
|--------|--------|
| CommandLine | [CommandLine/readme.md](CommandLine/readme.md) |
| Configuration | [Configuration/readme.md](Configuration/readme.md) |
| Core | [Core/readme.md](Core/readme.md) |
| Data | [Data/readme.md](Data/readme.md) |
| DataStructures | [DataStructures/readme.md](DataStructures/readme.md) |
| Json | [Json/readme.md](Json/readme.md) |
| Logging | [Logging/readme.md](Logging/readme.md) |
| Mail | [Mail/readme.md](Mail/readme.md) |
| Net | [Net/readme.md](Net/readme.md) |
| Security | [Security/readme.md](Security/readme.md) |
| Utilities | [Utilities/readme.md](Utilities/readme.md) |
