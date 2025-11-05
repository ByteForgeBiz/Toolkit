# Data CSV Tests

This directory contains unit tests for the ByteForge.Toolkit Data CSV module, which provides enhanced functionality for reading and writing CSV (Comma-Separated Values) files.

## Overview

The Data CSV module is an improved implementation of CSV processing in ByteForge.Toolkit, offering better performance, more features, and enhanced error handling compared to the original CSV module. These tests ensure that all CSV operations work correctly under various scenarios.

## Test Classes

### CSVReaderTests

Tests for the enhanced CSVReader class, which reads and parses CSV data:

- Constructor options and configuration
- Reading from files, streams, and strings
- Attribute-based column mapping with CSVColumnAttribute
- Strong typing and data conversion
- Header detection and column mapping
- Custom delimiter support
- Handling of quoted values and escape sequences
- Error handling and validation
- Performance with large datasets

### CSVWriterTests

Tests for the enhanced CSVWriter class, which generates CSV data:

- Writing to files, streams, and string builders
- Header generation from model classes
- Attribute-based column mapping
- Custom formatting and delimiters
- Proper escaping and quoting
- Performance with large datasets
- Thread safety
- Resource management

## Testing Strategy

The Data CSV tests follow a comprehensive approach that covers:

1. **Core functionality**: Basic reading and writing operations
2. **Attribute mapping**: Using CSVColumnAttribute for declarative mapping
3. **Data conversion**: Type conversion and validation
4. **Format handling**: Various CSV formats and delimiters
5. **Error scenarios**: Handling of malformed CSV data
6. **Performance**: Large dataset processing
7. **Resource management**: Proper disposal of resources

## Test Helpers

These tests utilize helper classes:

- **AssertionHelpers.AssertCSVRoundTrip**: Validates data integrity through write-read cycles
- **TempFileHelper**: Manages temporary files for testing

## Test Data

The tests use various test data including:

- Simple CSV files with standard formatting
- Complex CSV with quoted values and special characters
- Malformed CSV for error testing
- Models with CSVColumnAttribute decorations
- Large datasets for performance testing

## Comparison with Original CSV Module

The Data.CSV module offers several advantages over the original CSV module:

1. Attribute-based mapping for cleaner model integration
2. Better performance with large datasets
3. Enhanced error handling and validation
4. Improved memory usage
5. More flexible configuration options

Both implementations are maintained and tested for backward compatibility.
