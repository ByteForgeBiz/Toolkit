# CSV Processing Library

A robust, flexible CSV processing library for .NET 4.8 that handles various CSV formats with automatic format detection and streaming capabilities.

## Features

- Automatic CSV format detection
- Support for multiple delimiters (comma, tab, semicolon, pipe)
- Custom quote character handling
- Multi-line field support
- Progress tracking
- Memory-efficient streaming through buffered reading
- Comprehensive error handling
- Compatible with .NET 4.8

## Components

### CSVReader

The main class for reading and processing CSV files. It provides both static methods for quick operations and instance methods for more controlled processing.

Key features:
- Stream-based processing for memory efficiency
- Progress reporting through events
- Automatic format detection
- Support for custom data processors

Example usage:

```csharp
// Simple usage with static method
CSVReader.ReadFile("data.csv", (headers, values, rawLine) => {
    // Process each row here
    foreach (var value in values)
        Console.WriteLine(value);
});

// Advanced usage with progress tracking
var reader = new CSVReader();
reader.Progress += (sender, e) => Console.WriteLine($"Progress: {e.Progress}%");
reader.DataProcessor = (headers, values, rawLine) => {
    // Custom processing logic
};
reader.ReadFile("data.csv");
```

### CSVFormat

Handles the configuration and detection of CSV file formats. Supports automatic detection of:
- Delimiter character
- Quote character
- Header presence
- Header and data quoting styles

```csharp
// Use default format
var format = CSVFormat.Default;

// Or detect format from sample data
var customFormat = CSVFormat.DetectFormat(sampleLines);

// Or create custom format
var format = new CSVFormat {
    Delimiter = ',',
    QuoteChar = '"',
    HasHeader = true,
    HeaderQuoted = true,
    DataQuoted = true,
    TrimValues = true
};
```

### BufferedReader

An internal class that provides buffered reading capabilities for efficient CSV processing. Features include:
- Sample reading for format detection
- Buffered line reading
- Peek functionality
- Proper resource management through IDisposable

## Installation

1. Add the ByteForge.Toolkit namespace to your project
2. Reference the required assemblies
3. Import the namespace in your code:
```csharp
using ByteForge.Toolkit;
```

## Error Handling

The library includes comprehensive error handling for common scenarios:
- File not found
- Invalid CSV format
- Inconsistent field counts
- Unexpected end of file within quoted values
- Null or empty input validation

## Performance Considerations

- Uses buffered reading for memory efficiency
- Streams data instead of loading entire file
- Efficient string parsing and handling
- Minimal object allocation during processing

## Limitations

- Compatible with .NET Framework 4.8
- Designed for reading operations only
- Single-threaded processing

## Credits

Developed by Paulo Santos
With colaboration by Claude.ai