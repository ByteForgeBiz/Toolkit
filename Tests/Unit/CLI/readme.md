# CLI Module Tests

This directory contains comprehensive unit tests for the ByteForge.Toolkit CLI module components.

## Test Coverage

### CommandAttributeTests
- Constructor validation with various parameter combinations
- Property initialization and read-only validation
- Edge cases including null, empty, and special character handling
- Attribute usage validation for class and method targets
- Unicode and long string handling

### OptionAttributeTests  
- Constructor tests for description-only and description-with-aliases variants
- Property settability validation (Name property can be set, others are read-only)
- Edge cases with null, empty, whitespace, and special characters
- Attribute usage validation for parameter targets
- Integration scenarios with full configuration

### GlobalOptionTests
- Constructor tests for both Action and Action<string> variants
- Name normalization (stripping dash and slash prefixes)
- Input validation with proper exception handling
- Alias generation logic and conflict resolution
- Pattern matching functionality with case-insensitive matching
- Integration workflows

### ConsoleSpinnerTests
- Constructor tests with various parameter combinations
- Property management (Message, Color, IsRunning)
- Start/Stop lifecycle management
- Thread safety validation for concurrent operations
- Disposal pattern implementation
- Edge cases with special characters, long messages, and extreme delay values
- Performance characteristics under rapid start/stop operations

## Test Categories

All tests are categorized with:
- `[TestCategory("Unit")]` - Indicates unit test level
- `[TestCategory("CLI")]` - Indicates CLI module component

## Test Patterns

### AAA Pattern
All tests follow the Arrange-Act-Assert pattern for clarity and consistency.

### Exception Testing
Uses `Assert.ThrowsException<T>()` for expected exception scenarios.

### Thread Safety
Includes specific tests for thread safety using concurrent task execution.

### Resource Management
Tests proper disposal patterns and resource cleanup.

### Edge Case Coverage
Comprehensive testing of boundary conditions, null values, empty strings, and special characters.

## Running Tests

Execute CLI tests specifically:
```powershell
dotnet test --filter "TestCategory=CLI" --verbosity normal --no-build
```

Execute all tests in the CLI test directory:
```powershell
dotnet test --filter "FullyQualifiedName~CLI" --verbosity normal --no-build
```
