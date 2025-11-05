# Data Processing Module

**ByteForge.Toolkit.Data** is a comprehensive data processing module designed for .NET Framework 4.8 applications. It provides high-performance, secure, and flexible data handling capabilities across multiple domains including database operations, CSV processing, audio format detection, and specialized exception handling.

## 📚 Submodules

| Submodule                          | Description                                                                      |
|------------------------------------|----------------------------------------------------------------------------------|
| [CSV](.\CSV\readme.md)             | High-performance CSV reading/writing with format detection and streaming support |
| [Database](.\Database\readme.md)   | Secure database access layer with SQL Server 2000 and ODBC support               |
| [Audio](#-audio-format-detection)  | Audio file format detection from raw binary data                                 |
| [Exceptions](#-exception-handling) | Specialized exceptions for data processing operations                            |

---

## 🚀 CSV Processing

Stream-based CSV parsing with automatic format detection and comprehensive error handling.

### Features
- **Streaming Support**: Process large files efficiently without loading entire content into memory
- **Auto-Format Detection**: Automatically detect delimiters (comma, tab, semicolon, pipe)
- **Progress Tracking**: Real-time progress reporting via events
- **Quote Handling**: Support for quoted fields and multiline content
- **Validation**: Built-in validation with detailed error reporting
- **Custom Processing**: Configurable data processors for custom logic

### Core Classes
- **`CSVReader`**: Main streaming parser with progress events
- **`CSVWriter`**: High-performance CSV writing capabilities
- **`CSVFormat`**: Format configuration and delimiter detection
- **`CSVRecord`**: Represents individual CSV records with validation
- **`BufferedReader`**: Internal efficient line buffering system
- **`ValidationErrors`**: Comprehensive validation error collection

### Usage Example
```csharp
// Simple CSV reading with auto-format detection
CSVReader.ReadFile("data.csv", (headers, values, rawLine) => {
    Console.WriteLine($"Row: {string.Join(", ", values)}");
});

// Advanced usage with progress tracking
var reader = new CSVReader();
reader.Progress += (s, e) => Console.WriteLine($"Progress: {e.Progress}%");
reader.DataProcessor = (headers, values, raw) => {
    // Custom processing logic
    ValidateAndProcessRow(headers, values);
};
reader.ReadFile("large-dataset.csv");

// Custom format specification
var format = new CSVFormat {
    Delimiter = ';',
    HasHeader = true,
    QuoteChar = '"',
    TrimValues = true
};
reader.ReadFile("european-data.csv", format);
```

---

## 🗄️ Database Access Layer

Enterprise-grade database access layer with comprehensive security, bulk operations, and async support.

### Features
- **Multi-Database Support**: SQL Server 2000 and ODBC with automatic connection factory
- **Security**: Encrypted credential storage and SQL injection prevention
- **Bulk Operations**: High-performance bulk insert, upsert, and delete operations
- **Async Support**: Full async/await pattern implementation
- **Script Execution**: Advanced batch processing with GO statement parsing
- **Type Safety**: Intelligent type mapping and conversion
- **Monitoring**: Comprehensive logging and performance tracking

### Core Classes
- **`DBAccess`** (Modular partial classes):
  - `DBAccess.Core`: Connection management and configuration
  - `DBAccess.Factory`: Database provider factory
  - `DBAccess.Methods`: Core query execution (sync/async)
  - `DBAccess.ScriptExecution`: Advanced script processing
  - `DBAccess.TypeConverter`: Type mapping and conversion
  - `DBAccess.Logging`: Comprehensive operation logging
  - `DBAccess.Parameters`: Parameter binding and SQL injection prevention
- **`BulkDbProcessor<T>`**: Generic bulk operations with progress tracking
- **`DatabaseOptions`**: Secure configuration management
- **`ScriptExecutionResult`**: Detailed script execution results

### Usage Example
```csharp
// Basic database operations
var db = new DBAccess("Production");

// Simple queries with parameterization
int userCount = db.GetValue<int>("SELECT COUNT(*) FROM Users WHERE Active = @active", new object[] { true });
var user = db.GetRecord<User>("SELECT * FROM Users WHERE UserId = @userId", new object[] { 123 });

// Bulk operations with progress tracking
var processor = new BulkDbProcessor<Product>("Products");
processor.ProgressChanged += (progress) => Console.WriteLine($"Progress: {progress:F1}%");

bool success = await processor.BulkInsertAsync(db, products, cancellationToken);

// Script execution with batch handling
string script = @"
    CREATE TABLE TempData (Id int, Value varchar(255))
    GO
    INSERT INTO TempData SELECT * FROM SourceTable WHERE Active = 1
    GO
    SELECT COUNT(*) FROM TempData
";

var result = db.ExecuteScript(script, captureResults: true);
if (result.Success) {
    Console.WriteLine($"Executed {result.BatchResults.Count} batches");
}
```

---

## 🎵 Audio Format Detection

Lightweight utility for detecting audio file formats from raw binary data.

### Features
- **Format Detection**: Identify audio formats from file headers/magic bytes
- **Multiple Formats**: Support for MP3, WAV, FLAC, OGG, M4A, WMA, AIFF
- **Metadata Retrieval**: Get file extensions and MIME types for detected formats
- **Performance**: Fast detection using minimal data sampling

### Core Classes
- **`AudioFormatDetector`**: Main detection utility with static methods
- **`AudioFormat`**: Enumeration of supported audio formats

### Usage Example
```csharp
// Detect format from file
byte[] audioData = File.ReadAllBytes("unknown-audio-file");
var format = AudioFormatDetector.DetectFormat(audioData);

Console.WriteLine($"Detected format: {format}");
Console.WriteLine($"File extension: {AudioFormatDetector.GetFileExtension(format)}");
Console.WriteLine($"MIME type: {AudioFormatDetector.GetMimeType(format)}");

// Handle detection results
switch (format)
{
    case AudioFormat.MP3:
        ProcessMP3File(audioData);
        break;
    case AudioFormat.WAV:
        ProcessWAVFile(audioData);
        break;
    case AudioFormat.Unknown:
        HandleUnknownFormat(audioData);
        break;
}
```

### Supported Formats
- **MP3**: MPEG Layer III Audio (ID3v2 tags and frame sync detection)
- **WAV**: Waveform Audio File Format (RIFF/WAVE headers)
- **FLAC**: Free Lossless Audio Codec (fLaC signature)
- **OGG**: Ogg Vorbis Audio (OggS signature)
- **M4A**: MPEG-4 Audio (ftyp box detection)
- **WMA**: Windows Media Audio (ASF GUID detection)
- **AIFF**: Audio Interchange File Format (FORM/AIFF headers)

---

## ⚠️ Exception Handling

Specialized exception classes for robust error handling in data processing operations.

### Core Exceptions
- **`ParamArgumentsMismatchException`**: Parameter/argument mismatch in database operations
- **`DataProcessingException`**: General data processing errors (internal)
- **`ConversionException`**: Type conversion failures in CSV processing
- **`ValidationError`**: Individual validation error details

### Features
- **Serializable**: All exceptions support .NET serialization
- **Inner Exception Support**: Preserve exception chains for debugging
- **Contextual Information**: Detailed error messages with processing context

### Usage Example
```csharp
try
{
    // Database operation with parameters
    var result = db.ExecuteQuery("UPDATE Users SET Name = @name WHERE Id = @id", 
        new object[] { "John" }); // Missing second parameter
}
catch (ParamArgumentsMismatchException ex)
{
    Log.Error($"Parameter mismatch: {ex.Message}", ex);
    // Handle parameter count/type mismatch
}

try
{
    // CSV processing with validation
    reader.ProcessFile("data.csv");
}
catch (DataProcessingException ex)
{
    Log.Error($"Data processing failed: {ex.Message}", ex);
    // Handle data format or processing errors
}
```

---

## 🔧 Dependencies

### External Dependencies
- **.NET Framework 4.8**: Core framework requirement
- **System.Data.SqlClient**: SQL Server connectivity
- **System.Data.Odbc**: ODBC database connectivity

### Internal Module Dependencies
- **[Configuration](../Configuration/readme.md)**: Database configuration management
- **[Logging](../Logging/readme.md)**: Comprehensive operation logging  
- **[Security](../Security/readme.md)**: Credential encryption/decryption
- **[Utils](../Utils/readme.md)**: General utility functions

---

## 📋 Common Usage Patterns

### Database Configuration
```ini
[Data Source]
SelectedDB=Production

[Production]
sType=SQLServer
sServer=PRODSERVER\INSTANCE1
sDatabaseName=MyApplication
esUser=[encrypted_username]
esPass=[encrypted_password]
bEncrypt=true
iConnectionTimeout=60
iCommandTimeout=240
```

### CSV Processing Pipeline
```csharp
// Complete CSV processing with validation
var reader = new CSVReader();
var validationErrors = new ValidationErrors();

reader.Progress += (s, e) => UpdateProgressBar(e.Progress);
reader.DataProcessor = (headers, values, raw) => {
    try {
        var record = MapToEntity(headers, values);
        ValidateRecord(record, validationErrors);
        if (!validationErrors.HasErrors) {
            ProcessValidRecord(record);
        }
    }
    catch (ConversionException ex) {
        validationErrors.Add(ex.Field, ex.Value);
    }
};

reader.ReadFile("input.csv");

if (validationErrors.HasErrors) {
    ReportValidationErrors(validationErrors);
}
```

### Bulk Database Operations
```csharp
// Entity with database mapping
public class Customer
{
    [DBColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int CustomerId { get; set; }
    
    [DBColumn(MaxLength = 255)]
    public string Name { get; set; }
    
    [DBColumn(IsUnique = true)]
    public string Email { get; set; }
}

// Bulk processing with error handling
var processor = new BulkDbProcessor<Customer>("Customers");
processor.ProgressChanged += (progress) => Console.WriteLine($"Processing: {progress:F1}%");
processor.ErrorOccurred += (msg, ex) => Log.Error(msg, ex);

try {
    bool success = await processor.BulkUpsertAsync(db, customers, cancellationToken);
    if (success) {
        Log.Info($"Successfully processed {customers.Count} customers");
    }
}
catch (Exception ex) {
    Log.Error("Bulk operation failed", ex);
}
```

---

## 🛡️ Security Features

### Database Security
- **Encrypted Credentials**: Automatic encryption of database usernames and passwords
- **SQL Injection Prevention**: Parameterized queries with automatic parameter binding
- **Connection Encryption**: Optional TLS/SSL encryption for database connections
- **Trusted Authentication**: Support for Windows Authentication

### Data Validation
- **Input Sanitization**: Automatic trimming and validation of CSV data
- **Type Safety**: Strong typing with automatic type conversion and validation
- **Error Boundaries**: Comprehensive exception handling with detailed error context

---

## 📊 Performance Features

### CSV Processing
- **Streaming Architecture**: Process files of any size without memory constraints
- **Buffered Reading**: Efficient line buffering for optimal I/O performance
- **Progressive Loading**: Real-time progress reporting for user feedback
- **Format Caching**: Cache detected formats for repeated processing

### Database Operations
- **Bulk Operations**: SQL Server bulk copy for maximum throughput
- **Connection Pooling**: Efficient connection management and reuse
- **Async Processing**: Non-blocking operations with cancellation support
- **Batch Processing**: Automatic batching for large data sets

### Audio Detection
- **Minimal Sampling**: Fast detection using only file headers
- **Format Caching**: Cache detection results for repeated access
- **Memory Efficient**: Process large files without loading entire content

---

## 📝 Implementation Notes

### CSV Processing
- Auto-detects common delimiters: comma, tab, semicolon, pipe
- Handles quoted fields with embedded delimiters and newlines
- Supports both Windows (CRLF) and Unix (LF) line endings
- Provides detailed validation with field-level error reporting

### Database Access
- Designed for SQL Server 2000 compatibility while supporting modern versions
- Modular partial class design for maintainability and focused concerns
- Thread-safe credential access with proper locking mechanisms
- Comprehensive logging for debugging and audit trails

### Audio Format Detection
- Uses magic byte sequences for reliable format identification
- Minimal data requirements (typically first 16 bytes) for fast detection
- Extensible design allows easy addition of new format support
- No external dependencies - pure .NET implementation

### Exception Handling
- All custom exceptions implement standard .NET exception patterns
- Support for serialization enables proper error handling across application boundaries
- Inner exception preservation maintains complete error context
- Detailed error messages aid in debugging and troubleshooting

---

---

*The Data module is designed for enterprise applications requiring robust, secure, and high-performance data processing capabilities. All components follow established .NET patterns and provide comprehensive error handling and logging for production deployments.*

## 📚 Modules

| Module                                        | Description                                                                     |
|-----------------------------------------------|---------------------------------------------------------------------------------|
| [🏠 Home](../readme.md)                       | ByteForge.Toolkit main documentation                                            |
| [CommandLine](../CLI/readme.md)               | Attribute-based CLI parsing with aliasing, typo correction, and plugin support  |
| [Configuration](../Configuration/readme.md)   | INI-based configuration system with typed section support                       |
| [Data](../Data/readme.md)                     | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes                                                |
| [Logging](../Logging/readme.md)               | Thread-safe logging system with async file/console output                       |
| [Mail](../Mail/readme.md)                     | Email utility with HTML support and attachment handling                         |
| [Net](../Net/readme.md)                       | FTP/FTPS/SFTP high-level transfer client                                        |
| [Security](../Security/readme.md)             | AES-based string encryption with key generation and Galois Field logic          |
| [Utils](../Utils/readme.md)                   | Miscellaneous helpers: timing, path utilities, progress bar                     |
| [Core](../Core/readme.md)                     | Embedded resource deployment (WinSCP)                                           |
| [HTML](../HTML/readme.md)                     | Web UI framework components                                                     |
