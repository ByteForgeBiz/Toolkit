# Configuration Tests

This directory contains unit tests for the ByteForge.Toolkit Configuration module, which provides an INI-based configuration system with section support, strong typing, dynamic access, and array support.

## Overview

The Configuration module is a core component of ByteForge.Toolkit, enabling applications to store and retrieve configuration data with strong typing and organized sections. These tests ensure that all aspects of the Configuration system work correctly.

## Test Classes

### ConfigurationCoreTests

Tests for the core functionality of the Configuration class, including:

- Initialization with file paths, directories, and auto-detection
- Prevention of double initialization
- Invalid directory and missing file handling
- Section management (adding, retrieving, duplicate prevention)
- Thread safety
- Save and load operations
- Round-trip accuracy and formatting preservation

### ConfigSectionTests

Tests for the typed section functionality, including:

- Basic property mapping for various types (string, numeric, boolean, DateTime, enum)
- Default value handling with DefaultValueAttribute
- Custom default value providers
- Property control attributes (DoNotPersist, Ignore, ConfigName)
- Mixed attribute scenarios

### ConfigurationArrayTests

Tests for array and collection support, including:

- Basic array support for various types
- Array section naming conventions
- Array key formats and normalization
- Array persistence and clearing
- Null element handling

### GlobalizationTests

Tests for culture and formatting support, including:

- CultureInfo loading and management
- Date, number, and currency formatting
- Default culture behavior
- Formatting methods (FormatCurrency, FormatDate, FormatNumber)
- Culture-aware parsing

### ConfigurationErrorTests

Tests for error handling and edge cases, including:

- File system errors (permissions, corruption)
- Type conversion errors
- Configuration structure errors
- Missing sections and circular references

## Testing Strategy

The configuration tests follow a comprehensive testing strategy that covers:

1. **Core functionality**: Basic operations working correctly
2. **Advanced features**: Array support, attributes, globalization
3. **Error handling**: Validation of proper error responses
4. **Edge cases**: Handling unusual or extreme scenarios
5. **Performance**: Ensuring acceptable performance with large configurations

## Test Helpers

These tests make use of helper classes in the `Tests\ByteForge.Toolkit.Tests\Helpers` directory:

- **ConfigurationTestHelper**: Provides utilities for creating test configuration files and sections
- **TempFileHelper**: Assists with temporary file management for configuration tests

## Test Data

Test data includes sample INI files and model classes:

- **BasicTestConfig**: Simple properties of various types
- **ArrayTestConfig**: Various array and collection properties
- **ComplexTestConfig**: Nested objects and advanced scenarios