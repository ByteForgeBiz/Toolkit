# Test Helpers

This directory contains helper classes that provide common functionality and utilities for the ByteForge.Toolkit test suite.

## Overview

The helper classes in this directory simplify test setup, provide assertions, manage temporary resources, and offer other supporting functionality to make tests more readable, maintainable, and effective.

## Helper Classes

### TempFileHelper

```csharp
public static class TempFileHelper
{
    public static string CreateTempFile(string content, string extension = ".tmp")
    public static string CreateTempDirectory()
    public static void CleanupTempFiles()
}
```

Provides utilities for creating and managing temporary files and directories during tests:

- **CreateTempFile**: Creates a temporary file with specified content and extension
- **CreateTempDirectory**: Creates a unique temporary directory
- **CleanupTempFiles**: Removes temporary files created during testing

### AssertionHelpers

```csharp
public static class AssertionHelpers
{
    public static void AssertEncryptionRoundTrip(string plaintext, AESEncryption encryption)
    public static void AssertCSVRoundTrip(List<object> data, CSVFormat format)
    public static void AssertConfigurationRoundTrip<T>(T original, string configFile)
    public static void AssertThrows<TException>(Action action, string context = null)
}
```

Provides specialized assertion methods for common testing scenarios:

- **AssertEncryptionRoundTrip**: Verifies encryption and decryption operations maintain data integrity
- **AssertCSVRoundTrip**: Validates CSV write/read operations maintain data integrity
- **AssertConfigurationRoundTrip**: Ensures configuration save/load operations preserve values
- **AssertThrows**: Helper for validating exception throwing with context

### DatabaseTestHelper

```csharp
public static class DatabaseTestHelper
{
    public static DBAccess CreateTestDBAccess()
    public static void AssertTestDatabaseSetup(DBAccess dbAccess)
    public static string GenerateTestString(string prefix)
    public static void CleanupTestData(DBAccess dbAccess, string tableName, string condition)
    public static void AssertExecutionTime(Action action, int maxMilliseconds, string operationName)
}
```

Facilitates database testing with utilities for:

- **CreateTestDBAccess**: Creates a DBAccess instance pointing to the test database
- **AssertTestDatabaseSetup**: Validates that the test database is properly configured
- **GenerateTestString**: Creates unique test data identifiers
- **CleanupTestData**: Removes test data after test execution
- **AssertExecutionTime**: Validates performance of database operations

### ConfigurationTestHelper

```csharp
public static class ConfigurationTestHelper
{
    public static string CreateTempConfigFile(string content)
    public static void InitializeConfiguration(string content = null)
    public static T GetDefaultConfigSection<T>() where T : class, IConfigSection, new()
}
```

Assists with configuration testing:

- **CreateTempConfigFile**: Creates a temporary configuration file with specified content
- **InitializeConfiguration**: Sets up the Configuration system for testing
- **GetDefaultConfigSection**: Retrieves a typed configuration section with default values

### AwesomeAssertionsExtensions

Extends the AwesomeAssertions library with additional assertion methods specifically tailored for ByteForge.Toolkit testing.

## Usage Examples

### Using TempFileHelper

```csharp
[TestMethod]
public void TestFileOperation()
{
    // Create a temporary file for testing
    string testFilePath = TempFileHelper.CreateTempFile("Test content", ".txt");
    
    // Use the file in tests
    string content = File.ReadAllText(testFilePath);
    content.Should().Be("Test content");
    
    // Cleanup is automatic, but can be forced
    TempFileHelper.CleanupTempFiles();
}
```

### Using AssertionHelpers

```csharp
[TestMethod]
public void EncryptionTest()
{
    var encryption = new AESEncryption("password");
    
    // Verify encryption round-trip
    AssertionHelpers.AssertEncryptionRoundTrip("Sensitive data", encryption);
}
```

### Using DatabaseTestHelper

```csharp
[TestMethod]
public void DatabaseOperationTest()
{
    // Create database access for testing
    var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
    
    // Generate unique test identifier
    string testName = DatabaseTestHelper.GenerateTestString("UserTest");
    
    try
    {
        // Perform database operations
        bool success = dbAccess.ExecuteQuery(
            "INSERT INTO TestEntities (Name) VALUES (@name)",
            testName);
            
        success.Should().BeTrue();
    }
    finally
    {
        // Clean up test data
        DatabaseTestHelper.CleanupTestData(dbAccess, "TestEntities", 
            $"Name = '{testName}'");
    }
}
```

## Notes

- Helper classes are designed to be used across multiple test fixtures
- They encapsulate common testing patterns to reduce duplication
- Most helpers include proper cleanup mechanisms to prevent test pollution
- The helpers focus on making tests more readable and maintainable

---

## 📖 Documentation Links

### 🏠 Project Navigation
| Location           | Description           | Documentation                                                       |
|--------------------|-----------------------|---------------------------------------------------------------------|
| **Root**           | Solution overview     | [📘 ../../readme.md](../../readme.md)                               |
| **Toolkit.Modern** | Core library overview | [📘 ../../Toolkit.Modern/readme.md](../../Toolkit.Modern/readme.md) |
| **Tests**          | Test suite overview   | [📘 ../README.md](../README.md)                                     |

### 🧪 Related Test Modules
| Module                  | Description           | Documentation                                                         |
|-------------------------|-----------------------|-----------------------------------------------------------------------|
| **Unit Tests**          | Unit test suite       | [📘 ../Unit/readme.md](../Unit/readme.md)                             |
| **Test Models**         | Test data models      | [📘 ../Models/README.md](../Models/README.md)                         |
| **CLI Tests**           | CLI module tests      | [📘 ../Unit/CLI/readme.md](../Unit/CLI/readme.md)                     |
| **Configuration Tests** | Configuration tests   | [📘 ../Unit/Configuration/readme.md](../Unit/Configuration/readme.md) |
| **Data Tests**          | Data module tests     | [📘 ../Unit/Data/readme.md](../Unit/Data/readme.md)                   |
| **Security Tests**      | Security module tests | [📘 ../Unit/Security/readme.md](../Unit/Security/readme.md)           |

### 🏗️ Production Modules
| Module            | Description                | Documentation                                                                                   |
|-------------------|----------------------------|-------------------------------------------------------------------------------------------------|
| **CLI**           | Command-line parsing       | [📘 ../../Toolkit.Modern/CommandLine/readme.md](../../Toolkit.Modern/CommandLine/readme.md)     |
| **Configuration** | INI-based configuration    | [📘 ../../Toolkit.Modern/Configuration/readme.md](../../Toolkit.Modern/Configuration/readme.md) |
| **Data**          | Database & file processing | [📘 ../../Toolkit.Modern/Data/readme.md](../../Toolkit.Modern/Data/readme.md)                   |
| **Logging**       | Structured logging         | [📘 ../../Toolkit.Modern/Logging/readme.md](../../Toolkit.Modern/Logging/readme.md)             |
| **Security**      | Encryption & security      | [📘 ../../Toolkit.Modern/Security/readme.md](../../Toolkit.Modern/Security/readme.md)           |

