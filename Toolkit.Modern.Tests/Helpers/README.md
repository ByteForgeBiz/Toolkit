# Test Helpers

Shared infrastructure classes used across the entire test suite. All classes are `static` and live in the `ByteForge.Toolkit.Tests.Helpers` namespace.

## Helper Classes

### TempFileHelper

Manages temporary files and directories that must survive for the duration of a test and be cleaned up afterwards.

| Method | Description |
|--------|-------------|
| `CreateTempFile(string content, string extension)` | Creates a temp file with UTF-8 text content (default extension `.tmp`) |
| `CreateTempFile(byte[] data, string extension)` | Creates a temp file with binary content (default extension `.dat`) |
| `CreateTempCsvFile(string csvContent)` | Shorthand — creates a `.csv` temp file |
| `CreateTempIniFile(string iniContent)` | Shorthand — creates a `.ini` temp file |
| `GetTempFilePath(string extension)` | Returns a unique path without creating the file; path is still registered for cleanup |
| `CreateTempDirectory()` | Creates a unique temp directory under `Path.GetTempPath()` |
| `CleanupTempFiles()` | Deletes all tracked files and directories; silently ignores errors |

Files are tracked in static lists. Callers typically call `CleanupTempFiles()` in `[TestCleanup]`.

### AssertionHelpers

Domain-specific assertion utilities that wrap AwesomeAssertions.

| Method | Description |
|--------|-------------|
| `AssertEncryptionRoundTrip(string plaintext, Func<string,string> encryptMethod, Func<string,string> decryptMethod)` | Verifies that encrypt → decrypt produces the original value and that the encrypted form is different |
| `AssertCsvRoundTrip<T>(List<T> originalData, string csvContent, List<T> parsedData)` | Validates CSV write/read produces the same record count |
| `AssertCollectionsEquivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual, string because)` | Order-independent collection equality |
| `AssertJavaScriptEscaping(string original, string escaped)` | Checks that backslash, quote, newline, carriage-return, and tab characters are properly escaped |
| `AssertBinarySearchTreeOrdering<T>(BinarySearchTree<T> tree, IEnumerable<T> values)` | Verifies all values are present and that in-order traversal produces a sorted sequence |
| `AssertThrows<TException>(Action action, string? expectedMessage)` | Asserts a specific exception type is thrown, optionally matching message content |
| `AssertInRange<T>(T actual, T min, T max, string because)` | Asserts a value falls within an inclusive range |

### DatabaseTestHelper

Centralizes connection configuration, test data management, and performance utilities for all database tests.

#### Connection factory methods

| Method | Description |
|--------|-------------|
| `CreateTestDBAccess()` | `DBAccess` pointing at `TestUnitDB` on `(local)` using Windows Authentication, or CI environment variables if set |
| `CreateTestDatabaseOptions()` | `DatabaseOptions` for the SQL Server test database |
| `CreateTestDatabaseOptions(int connectionTimeout, int commandTimeout, bool autoTrimStrings)` | `DatabaseOptions` with custom timeouts |
| `CreateInvalidDatabaseOptions()` | `DatabaseOptions` pointing at a non-existent database, for error-path tests |
| `CreateTestAccessDBAccess()` | `DBAccess` using ODBC to the Access test database |
| `CreateTestAccessDatabaseOptions()` | `DatabaseOptions` for the Access test database (resolves `.accdb` vs `.mdb`) |
| `CreateTestAccessDatabaseOptionsWithDSN()` | `DatabaseOptions` using a named DSN `TestAccessDB` |
| `CreateInvalidAccessDatabaseOptions()` | `DatabaseOptions` with a non-existent DSN |

**CI environment variables read by `DatabaseTestHelper`:**

| Variable | Purpose |
|----------|---------|
| `BYTEFORGE_TEST_SQL_SERVER` | SQL Server host |
| `BYTEFORGE_TEST_SQL_PORT` | SQL Server port |
| `BYTEFORGE_TEST_SQL_USER` | SQL login user |
| `BYTEFORGE_TEST_SQL_PASSWORD` | SQL login password |
| `BYTEFORGE_TEST_SQL_CONNECTION_STRING` | Full connection string (highest priority) |
| `BYTEFORGE_TEST_SQL_DATABASE` | Database name |
| `BYTEFORGE_TEST_ACCESS_FORMAT` | `accdb` or `mdb` to select the Access file format |

#### Database validation

| Method | Description |
|--------|-------------|
| `AssertTestDatabaseSetup(DBAccess dbAccess)` | Fails the test if `TestUnitDB` is not reachable or missing expected seed data |
| `AssertAccessDatabaseSetup(DBAccess dbAccess)` | Fails the test if the Access database is not reachable or missing expected tables |
| `VerifyTestDatabaseSetup(DBAccess dbAccess)` | Returns `bool` — used internally by the assert variant |
| `VerifyAccessDatabaseSetup(DBAccess dbAccess)` | Returns `bool` |
| `VerifyAccessDatabaseFileExists()` | Returns `true` if the resolved Access database file exists on disk |

#### Test data management

| Method | Description |
|--------|-------------|
| `GenerateTestString(string prefix)` | Produces a unique, 50-character-max string in the form `{prefix}_{yyyyMMdd_HHmmss}_{guid}` |
| `GenerateTestCode()` | Produces a short unique code `TEST{yyyyMMddHHmmss}` |
| `CleanupTestData(DBAccess dbAccess, string tableName, string whereClause)` | Deletes rows matching the clause; returns affected count |
| `CleanupTestEntities(DBAccess dbAccess)` | Removes rows from `TestEntities` where `Name LIKE 'Test_%'` or description contains `unit test` |
| `CleanupBulkTestEntities(DBAccess dbAccess)` | Removes rows from `BulkTestEntities` where `Code LIKE 'TEST%'` or name matches |
| `GetRecordCount(DBAccess dbAccess, string tableName)` | Returns the row count for the specified table |
| `AssertRecordCount(DBAccess dbAccess, string tableName, int expectedCount, int tolerance)` | Asserts count is within `expectedCount ± tolerance` |
| `ValidateTestEntity(DBAccess dbAccess, int entityId, string expectedName)` | Returns `true` if the entity exists with the expected name |

#### Performance utilities

| Method | Description |
|--------|-------------|
| `MeasureExecutionTime(Action operation)` | Returns elapsed milliseconds |
| `MeasureExecutionTime<T>(Func<T> operation)` | Returns `(T Result, long ElapsedMs)` |
| `AssertExecutionTime(Action operation, long maxMilliseconds, string operationName)` | Fails the test if the operation exceeds the time limit |

### ConfigurationTestHelper

Creates isolated `Configuration` instances from inline INI content.

| Method | Description |
|--------|-------------|
| `CreateTestConfiguration(string iniContent)` | Writes INI content to a temp file and returns an initialized `IConfigurationManager` |
| `CreateConfiguration()` | Returns an uninitialized `Configuration` instance |
| `CreateStandardTestConfiguration()` | Returns a pre-configured instance with `[TestSection]`, `[DatabaseSettings]`, and `[GlobalizationSettings]` sections |

### TestConfigurationHelper

Low-level utilities for creating temp config files and constructing INI content.

| Method | Description |
|--------|-------------|
| `CreateTempConfigFile(string content)` | Delegates to `TempFileHelper.CreateTempIniFile` |
| `CreateTestSection<T>(string sectionName, Dictionary<string, object> values)` | Builds a single-section INI file from a dictionary |
| `CreateConfigWithArray(string sectionName, string arrayName, string[] arrayItems, ...)` | Builds a two-section INI file: a main section and a numeric-keyed array section |
| `CreateStandardTestConfig()` | Creates a multi-section config with `[TestSection]`, `[TestArray]`, `[DatabaseSettings]`, `[EmptySection]`, and `[SpecialCharacters]` |
| `CreateEncryptedTestConfig()` | Creates a config with `es`-prefixed (encrypted string) keys for security testing |
| `CleanupTempFiles()` | Delegates to `TempFileHelper.CleanupTempFiles()` |
| `ValidateConfigurationRoundTrip<T>(T original, string configFile)` | Placeholder — checks that the file exists |

### AwesomeAssertionsExtensions

`internal static` class extending AwesomeAssertions fluent interfaces with `SatisfyAny` and `SatisfyAll` predicate combinators.

Both methods are overloaded for all common assertion subject types: `object`, `ReferenceType`, `bool`, `bool?`, `DateTime`, `DateTime?`, `DateTimeOffset`, `DateTimeOffset?`, `Guid`, `Guid?`, `TimeSpan`, `TimeSpan?`, and any `Enum` or `Enum?` type.

| Extension | Description |
|-----------|-------------|
| `.SatisfyAny(string because, params Func<TSubject,bool>[] predicates)` | Passes if at least one predicate returns `true` |
| `.SatisfyAll(string because, params Func<TSubject,bool>[] predicates)` | Passes if all predicates return `true` |

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Root** | Solution overview | [../../readme.md](../../readme.md) |
| **Tests** | Test suite overview | [../README.md](../README.md) |
| **Models** | Test model classes | [../Models/README.md](../Models/README.md) |
| **Unit** | Unit test overview | [../Unit/readme.md](../Unit/readme.md) |
| **Data/Database** | Database tests | [../Unit/Data/Database/readme.md](../Unit/Data/Database/readme.md) |
