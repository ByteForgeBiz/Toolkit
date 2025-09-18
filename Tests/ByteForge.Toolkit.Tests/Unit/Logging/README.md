# Logging Tests

This directory contains unit tests for the ByteForge.Toolkit Logging module, which provides a flexible and extensible logging framework.

## Overview

The Logging module offers a centralized logging system with support for multiple log targets, log levels, and formatting options. These tests ensure that logging functionality works correctly across various scenarios.

## Test Classes

### LogTests

Tests for the Log class, which serves as the primary entry point for logging operations:

- Log initialization and configuration
- Log level filtering
- Logger instance management (static and instance-based)
- Log formatting
- Different log targets (console, file, custom)
- Error handling during logging
- Performance of logging operations
- Thread safety in concurrent logging scenarios
- Logger lifecycle management

## Testing Strategy

The Logging tests follow a comprehensive approach that covers:

1. **Core functionality**: Basic logging operations at different levels
2. **Configuration**: Logger initialization with various settings
3. **Performance**: Efficient logging with minimal overhead
4. **Threading**: Thread-safe logging operations
5. **Error handling**: Graceful handling of logging failures
6. **Integration**: Interaction with other system components

## Test Helpers

These tests may utilize helper classes:

- **TempFileHelper**: Manages temporary log files
- Custom log targets for testing specific scenarios
- Log output verification utilities

## Log Levels

The logging system supports multiple log levels, all of which are tested:

- **Trace**: Most detailed level for fine-grained debugging
- **Debug**: Detailed information for debugging
- **Info**: General information about application flow
- **Notice**: Important but normal information
- **Warning**: Potential issues that don't prevent normal operation
- **Error**: Errors that allow the application to continue
- **Fatal**: Critical errors that may cause application termination

## Logger Implementations

The tests cover various logger implementations:

- **ConsoleLogger**: Logs to the console with color-coded output
- **FileLogger**: Logs to files with rotation support
- **NullLogger**: No-operation logger for performance optimization
- **StaticLoggerAdapter**: Adapter for static logging methods

## Notes

The Logging module is designed to be highly extensible, allowing custom logger implementations to be plugged in as needed. The tests verify both the core logging functionality and the extensibility mechanisms.