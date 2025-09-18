# Data Tests

This directory contains unit tests for the ByteForge.Toolkit Data module, which provides functionality for working with various data formats and storage systems.

## Overview

The Data module in ByteForge.Toolkit includes components for working with audio files, CSV data, and databases. These tests ensure that all data processing operations work correctly under various scenarios.

## Subfolders

The Data tests are organized into subfolders by functionality:

- **Audio**: Tests for audio format detection and processing
- **CSV**: Tests for CSV data reading and writing
- **Database**: Tests for database access and operations

## Testing Strategy

The Data tests follow a comprehensive approach that covers:

1. **Core functionality**: Basic operations working correctly
2. **Format support**: Handling various data formats and conversions
3. **Error handling**: Proper response to invalid inputs and error conditions
4. **Performance**: Ensuring acceptable performance with large datasets
5. **Edge cases**: Testing unusual or extreme scenarios

## Test Helpers

These tests utilize helper classes located in the `Tests\ByteForge.Toolkit.Tests\Helpers` directory:

- **DatabaseTestHelper**: Provides utilities for database testing
- **TempFileHelper**: Manages temporary files for testing file operations
- **AssertionHelpers**: Contains custom assertions for data validation

## Test Data

The tests use various test data including:

- Audio file samples for format detection
- CSV files with different formats and content
- Database scripts and sample records

See the README.md files in each subfolder for more detailed information about the specific tests in each category.