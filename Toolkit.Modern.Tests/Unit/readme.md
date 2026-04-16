# Unit Tests

All unit tests for `ByteForge.Toolkit.Modern`, organized by module. Each subdirectory mirrors the module structure of the production library.

## Structure

| Folder | Module Tested | Test Classes |
|--------|---------------|--------------|
| `CLI/` | `ByteForge.Toolkit.CommandLine` | `CommandAttributeTests`, `OptionAttributeTests`, `GlobalOptionTests`, `ConsoleSpinnerTests` |
| `Configuration/` | `ByteForge.Toolkit.Configuration` | `ConfigurationCoreTests`, `ConfigSectionTests`, `ConfigurationArrayTests`, `ConfigurationAdvancedTests`, `ConfigurationEnhancementTests`, `ConfigurationMigrationTests`, `ConfigurationDictionaryTests`, `ConfigurationDictionaryEdgeCaseTests`, `ConfigurationErrorTests`, `ConfigurationParserIntegrationTests`, `ConfigurationThreadSafetyTests`, `GlobalizationTests` |
| `Data/Audio/` | `ByteForge.Toolkit.Data` | `AudioFormatDetectorTests` |
| `Data/CSV/` | `ByteForge.Toolkit.Data` | `CSVReaderTests`, `CSVWriterTests` |
| `Data/Database/` | `ByteForge.Toolkit.Data` | `DBAccessCoreTests`, `DBAccessMethodsSQLServerTests`, `DBAccessMethodsODBCTests`, `DBAccessParameterParsingTests`, `DBAccessParameterParsingSimpleTests`, `DBAccessExtendedPropertiesTests`, `DBAccessScriptExecutionTests`, `DBAccessTransactionTests`, `BulkDbProcessorTests`, `BulkDbProcessorEdgeCaseTests` |
| `DataStructures/` | `ByteForge.Toolkit.DataStructures` | `BinarySearchTreeTests`, `UrlTests` |
| `Logging/` | `ByteForge.Toolkit.Logging` | `LogTests`, `DatabaseLoggerTests`, `DatabaseLoggerLiveTests` (contains `DatabaseLoggerSqlServerLiveTests`) |
| `Mail/` | `ByteForge.Toolkit.Mail` | `EmailAttachmentHandlerTests` |
| `Security/` | `ByteForge.Toolkit.Security` | `AESEncryptionTests`, `EncryptorTests` |
| `Utils/` | `ByteForge.Toolkit.Utilities` | `BooleanParserTests`, `ConsoleUtilTests`, `DateTimeParserTests`, `DateTimeUtilTests`, `EnumExtensionsTests`, `IOUtilsTests`, `NameCapitalizerTests`, `ParserTests`, `StringUtilTests`, `TemplateProcessorTests`, `TimingUtilTests`, `UtilsTests` |

## Testing Conventions

### Arrange-Act-Assert

All test methods follow the AAA pattern. Sections are separated by blank lines or region blocks.

### Categories

Every test class carries at least `[TestCategory("Unit")]` plus a module-specific category (e.g. `[TestCategory("CLI")]`). Tests that require external infrastructure carry additional categories:

- `[TestCategory("SQLServer")]` — requires a running SQL Server instance
- `[TestCategory("ODBC")]` — requires the Microsoft Access ODBC driver

### Assertion library

`AwesomeAssertions` is used exclusively. `Assert.ThrowsException<T>()` from MSTest is used in a small number of early tests; most exception assertions use `.Should().Throw<T>()`.

### Lifecycle attributes

- `[ClassInitialize]` — validates that the required database or resource is reachable before any test in the class runs
- `[TestInitialize]` / `[TestCleanup]` — create and tear down per-test state
- `[ClassCleanup]` — disposes class-level resources such as `CancellationTokenSource`

## Running Specific Modules

```powershell
# All CLI tests
dotnet test --filter "TestCategory=CLI"

# All tests except those requiring live databases
dotnet test --filter "TestCategory!=SQLServer&TestCategory!=ODBC"

# All database tests (ODBC + SQL Server)
dotnet test --filter "FullyQualifiedName~Data.Database"

# A specific test class
dotnet test --filter "FullyQualifiedName~BinarySearchTreeTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../README.md](../README.md) |
| **Helpers** | Shared test infrastructure | [../Helpers/README.md](../Helpers/README.md) |
| **Models** | Test entity and config models | [../Models/README.md](../Models/README.md) |
