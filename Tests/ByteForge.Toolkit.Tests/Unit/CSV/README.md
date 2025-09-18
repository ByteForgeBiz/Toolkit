# CSV Tests

This directory contains unit tests for the ByteForge.Toolkit CSV module, which provides functionality for reading and writing CSV (Comma-Separated Values) files.

## Overview

The CSV module allows applications to process CSV data with support for custom delimiters, headers, data types, and error handling. These tests ensure that CSV reading and writing operations work correctly under various scenarios.

## Test Classes

### CSVReaderTests

Tests for the CSVReader class, which reads and parses CSV data:

- Constructor functionality with different parameters
- Reading from files and streams
- Column mapping and data conversion
- Header detection and processing
- Error handling for malformed CSV data
- Performance with large datasets
- Support for different delimiters and formats
- Handling of quoted values and escaping
- Skipping empty lines and handling whitespace

### CSVWriterTests

Tests for the CSVWriter class, which generates CSV data:

- Creating CSV output to files and streams
- Writing headers and data rows
- Custom delimiters and formatting options
- Handling of special characters and escaping
- Quote handling for fields with delimiters
- Performance with large datasets
- Proper resource disposal
- Handling of null values and empty fields

## Testing Strategy

The CSV tests follow a comprehensive testing approach that covers:

1. **Functional correctness**: Basic reading and writing working correctly
2. **Format handling**: Support for various CSV formats and delimiters
3. **Data conversion**: Proper type conversion and error handling
4. **Edge cases**: Handling of special characters, empty files, missing headers
5. **Performance**: Ensuring acceptable performance with large files
6. **Resource management**: Proper disposal of resources

## Test Helpers

These tests utilize helper classes to assist with test setup and validation:

- **AssertionHelpers.AssertCSVRoundTrip**: Validates that data can be written to CSV and read back accurately
- **TempFileHelper**: Manages temporary files for testing CSV file operations

## Test Data

The tests use various test data including:

- Simple CSV files with standard formatting
- Complex CSV files with quoted values and special characters
- Malformed CSV files for error testing
- Large CSV datasets for performance testing

## Notes

The ByteForge.Toolkit includes two CSV implementations:
1. The original implementation in the `ByteForge.Toolkit.CSV` namespace
2. A newer implementation in the `ByteForge.Toolkit.Data.CSV` namespace

Both implementations are tested with similar test cases but may have different features and behaviors.