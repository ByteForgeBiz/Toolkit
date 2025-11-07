# Data

This folder contains the data processing and storage components for ByteForge.Toolkit, including support for audio formats, CSV files, and database operations. It provides robust utilities for reading, writing, converting, and managing various data types and sources.

## Subfolders & Modules

- **Audio**: Audio format detection and metadata extraction (e.g., `AudioFormatDetector`).
- **CSV**: Reading, writing, and processing CSV files with flexible format detection, quoting, and error handling (`CSVReader`, `CSVWriter`, `CSVFormat`, `CSVRecord`, `ICSVRecord`, `CSVColumnAttribute`, exceptions).
- **Database**: Database access, bulk operations, transactions, parameter handling, type conversion, and script execution (`DBAccess` partial classes, `BulkDbProcessor`, `TypeConverter`, `ScriptExecutionResult`, `ParamArgumentsMismatchException`, etc.).
- **Exceptions**: Custom exceptions for data conversion and processing errors.

## Key Classes
- **DBAccess**: Main database access class, split into partials for core, methods, factory, parameters, logging, transactions, and script execution.
- **BulkDbProcessor**: High-performance bulk operations for databases.
- **TypeConverter**: DataRow to object mapping and type conversion utilities.
- **CSVReader/CSVWriter**: Flexible CSV file reading and writing with format auto-detection and progress reporting.
- **AudioFormatDetector**: Detects audio file formats from raw data and provides file extension/MIME type utilities.
- **DatabaseOptions/DatabaseRootOptions**: Configuration models for database connections and selection.
- **Custom Exceptions**: `ConversionException`, `DataProcessingException`, `ParamArgumentsMismatchException` for robust error handling.

## Features
- Multi-format data support: audio, CSV, database
- Strongly-typed and attribute-driven data mapping
- Bulk and transactional database operations
- Format auto-detection and error handling for CSV/audio
- Thread-safe and performance-optimized utilities

## Usage Example
```csharp
// Read a CSV file
var reader = new CSVReader();
reader.ReadFile("data.csv");

// Database access
var dbOptions = Configuration.GetSection<DatabaseOptions>("Database");
var db = new DBAccess(dbOptions);
var result = db.GetValue("SELECT COUNT(*) FROM TestEntities");

// Audio format detection
var format = AudioFormatDetector.DetectFormat(audioBytes);
```

See subfolders for specialized modules and additional documentation.