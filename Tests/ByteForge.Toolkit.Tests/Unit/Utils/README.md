# Utils Tests

This directory contains unit tests for the ByteForge.Toolkit Utils module, which provides various utility functions and helper classes.

## Overview

The Utils module offers a wide range of utility functions for common programming tasks, from string manipulation to date/time handling to IO operations. These tests ensure that all utility functions work correctly across various scenarios.

## Test Classes

### BooleanParserTests

Tests for the BooleanParser class, which provides enhanced boolean parsing functionality:

- Parsing various string representations of boolean values
- Case sensitivity options
- Custom true/false value recognition
- Handling of null and empty inputs
- Performance of boolean parsing operations

### ConsoleUtilTests

Tests for the ConsoleUtil class, which provides console-related utilities:

- Console availability detection
- Console output formatting
- Color management
- Progress indicators and spinners
- User input handling

### DateTimeParserTests

Tests for the DateTimeParser class, which provides flexible date and time parsing:

- Parsing various date/time formats
- Culture-specific date parsing
- Handling of ambiguous dates
- Performance with various format strings
- Caching of parser results

### DateTimeUtilTests

Tests for the DateTimeUtil class, which provides date and time manipulation utilities:

- Unix timestamp conversion
- Time zone handling
- Date arithmetic
- Date formatting
- Performance of date/time operations

### EnumExtensionsTests

Tests for the EnumExtensions class, which provides enhanced enum functionality:

- Retrieving enum descriptions
- Enum parsing with various options
- Enum value validation
- Performance of enum operations

### IOUtilsTests

Tests for the IOUtils class, which provides file and path utilities:

- File pattern matching
- Directory searching
- Path normalization
- UNC path handling
- Network path mapping

### ParserTests

Tests for various parsing utilities:

- Generic parsing functions
- Type conversion
- String parsing with fallback values
- Error handling during parsing

### StringUtilTests

Tests for the StringUtil class, which provides string manipulation utilities:

- String formatting and transformation
- Truncation and padding
- Case conversion
- Pattern matching and replacement
- Performance of string operations

### TemplateProcessorTests

Tests for the TemplateProcessor class, which handles string templates with placeholders:

- Template parsing and processing
- Placeholder substitution
- Nested templates
- Escaping mechanisms
- Performance with complex templates

### TimingUtilTests

Tests for the TimingUtil class, which provides timing and performance measurement:

- Operation timing
- Elapsed time formatting
- Performance benchmarking
- Multiple timer management

### UtilsTests

Tests for miscellaneous utility functions:

- Various helper methods
- Type conversion utilities
- General-purpose functions
- Legacy utility methods

## Testing Strategy

The Utils tests follow a comprehensive approach that covers:

1. **Functionality**: Basic operation of each utility
2. **Edge cases**: Handling of unusual or extreme inputs
3. **Performance**: Efficient operation with various inputs
4. **Error handling**: Proper response to invalid inputs
5. **Compatibility**: Consistent behavior across different environments

## Test Helpers

These tests may utilize various helper classes:

- **AssertionHelpers**: Contains custom assertions for utility validation
- **TempFileHelper**: Manages temporary files for IO testing

## Notes

The Utils module contains some of the most frequently used functionality in the ByteForge.Toolkit library. These utilities are designed to be simple, efficient, and robust, with comprehensive error handling and performance optimization.