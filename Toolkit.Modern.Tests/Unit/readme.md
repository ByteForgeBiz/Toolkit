# Unit Tests

This directory contains unit tests for the ByteForge.Toolkit library. The tests are organized into folders that correspond to the modules and components in the main library.

## Overview

The unit tests in this directory focus on testing individual components in isolation, ensuring that each component functions correctly according to its specification. The tests are designed to be comprehensive, covering not only typical use cases but also edge cases and error conditions.

## Test Structure

Tests are organized into the following categories:

- **Configuration**: Tests for the INI-based configuration system with section support, strong typing, and array handling.
- **CSV**: Tests for CSV reading and writing functionality.
- **Data**: Tests for data processing components including audio format detection, CSV handling, and database access.
- **DataStructures**: Tests for core data structures like BinarySearchTree and URL handling.
- **Logging**: Tests for the logging framework.
- **Mail**: Tests for email and attachment handling.
- **Security**: Tests for encryption and security-related features.
- **Utils**: Tests for various utility classes and helper functions.

## Testing Methodology

Each test class follows a consistent pattern:

1. **Arrange**: Set up the test environment, create necessary objects and test data.
2. **Act**: Execute the functionality being tested.
3. **Assert**: Verify that the results match the expected outcomes.

The tests make use of the AwesomeAssertions library for clear and expressive assertions.

## Test Categories

Tests are marked with categories using the `[TestCategory]` attribute. Common categories include:

- `Unit`: Indicates that the test is a unit test.
- Module-specific categories like `Configuration`, `Utils`, `Security`, etc.
- Behavior categories like `Performance`, `ErrorHandling`, etc.

## Helper Classes

The test project includes several helper classes located in the `Tests\ByteForge.Toolkit.Tests\Helpers` directory:

- **TempFileHelper**: Assists with creating and managing temporary files for testing.
- **DatabaseTestHelper**: Provides utilities for database testing.
- **ConfigurationTestHelper**: Assists with configuration testing.
- **AssertionHelpers**: Provides custom assertions for specific scenarios.

## Running Tests

Tests can be executed using the Visual Studio Test Explorer or via the command line:

```bash
dotnet test "Tests\ByteForge.Toolkit.Tests\ByteForge.Toolkit.Tests.csproj"
```

For performance testing or tests that require specific resources, some tests may be marked with `[TestCategory("Manual")]` and require manual execution.

