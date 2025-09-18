# ByteForge.Toolkit.Tests

This directory contains the test suite for the ByteForge.Toolkit library, providing comprehensive validation of functionality, performance, and reliability.

## Overview

The ByteForge.Toolkit.Tests project is a structured testing framework designed to validate all aspects of the ByteForge.Toolkit library. It includes unit tests, integration tests, and performance benchmarks that ensure the library functions correctly and efficiently.

## Test Structure

The tests are organized into the following structure:

- **Unit**: Tests for individual components in isolation
  - **Configuration**: Tests for the INI-based configuration system
  - **CSV**: Tests for CSV reading and writing
  - **Data**: Tests for data processing (Audio, CSV, Database)
  - **DataStructures**: Tests for data structures like BinarySearchTree
  - **Logging**: Tests for the logging framework
  - **Mail**: Tests for email and attachment handling
  - **Security**: Tests for encryption and security features
  - **Utils**: Tests for utility functions and helpers
- **Helpers**: Support classes for test setup and validation
- **Models**: Test data models used across test cases
- **TODO**: Documentation of testing plans and strategies

## Testing Approach

The test suite follows these key principles:

1. **Comprehensive coverage**: Tests cover both typical use cases and edge cases
2. **Isolation**: Unit tests isolate components to identify issues precisely
3. **Repeatability**: Tests produce consistent results across different environments
4. **Performance awareness**: Tests validate both functionality and performance
5. **Self-contained**: Tests create and manage their own test data and resources

## Test Helpers

The test project includes several helper classes to facilitate testing:

- **TempFileHelper**: Creates and manages temporary files and directories
- **DatabaseTestHelper**: Provides database connectivity and test data management
- **ConfigurationTestHelper**: Assists with configuration testing
- **AssertionHelpers**: Provides custom assertions for complex validations

## Running Tests

Tests can be executed using the Visual Studio Test Explorer or via the command line:

```bash
# Build the solution first
cd "Tests\ByteForge.Toolkit.Tests"
BuildSolution.bat

# Run all tests
dotnet test "ByteForge.Toolkit.Tests.csproj" --no-build

# Run tests with a specific category
dotnet test "ByteForge.Toolkit.Tests.csproj" --no-build --filter "Category=Utils"

# Run tests with code coverage
# (Requires OpenCover and ReportGenerator packages)
packages\OpenCover.4.7.1221\tools\OpenCover.Console.exe -target:"dotnet.exe" -targetargs:"test ByteForge.Toolkit.Tests.csproj --no-build" -output:coverage.xml -register:user -filter:"+[ByteForge.Toolkit*]*"
packages\ReportGenerator.5.1.26\tools\net47\ReportGenerator.exe -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html
```

## Test Categories

Tests are marked with categories using the `[TestCategory]` attribute:

- **Unit**: Basic unit tests
- **Integration**: Tests that integrate multiple components
- **Performance**: Tests that measure and validate performance
- **Manual**: Tests that require manual verification or setup
- Module-specific categories: **Configuration**, **Utils**, **Security**, etc.

## Test Data

The test project includes various test data files in the appropriate test directories. These include:

- Sample configuration files
- CSV test data
- Audio file samples
- Database scripts and sample data

## Coverage Goals

The test suite aims for high code coverage with specific targets by module:

| Module         | Target Coverage | Priority | Complexity |
|----------------|-----------------|----------|------------|
| DataStructures |             95% | High     | Low        |
| Security       |             90% | High     | Medium     |
| Configuration  |             85% | High     | Medium     |
| Utils          |             85% | High     | Low        |
| CSV            |             75% | Medium   | Medium     |
| Logging        |             70% | Medium   | Medium     |
| CLI            |             60% | Medium   | High       |
| Database       |             30% | Low      | Very High  |

## Contributing

When adding new tests:

1. Follow the existing naming conventions and folder structure
2. Ensure tests are isolated and repeatable
3. Use appropriate test categories
4. Add XML documentation to test classes and methods
5. Update relevant README.md files with new test information