# Toolkit.Modern.Tests

Automated test suite for the `ByteForge.Toolkit.Modern` library. Uses MSTest as the test framework and AwesomeAssertions for fluent assertions.

## Target Frameworks

| Framework | Notes |
|-----------|-------|
| `net48` (.NET Framework 4.8) | Matches the production library target |
| `net9.0` | Cross-platform modern runtime |

## Project Structure

```
Toolkit.Modern.Tests/
├── Helpers/         — Shared test infrastructure (helpers, extensions)
├── Models/          — Test entity and configuration model classes
├── TestData/        — Static test data files copied to output on build
├── Unit/            — All unit tests, organized by module
│   ├── CLI/
│   ├── Configuration/
│   ├── Data/
│   │   ├── Audio/
│   │   ├── CSV/
│   │   └── Database/
│   ├── DataStructures/
│   ├── Logging/
│   ├── Mail/
│   ├── Security/
│   └── Utils/
└── TODO/            — Planning documents and database setup scripts
```

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `MSTest` | 4.0.1 | Test framework and runner |
| `Microsoft.NET.Test.Sdk` | 18.0.0 | Test host |
| `AwesomeAssertions` | 9.3.0 | Fluent assertion library |
| `coverlet.collector` | 6.0.4 | Code coverage collection |
| `OpenCover` | 4.7.1221 | Coverage reporting (included for scripting) |
| `ReportGenerator` | 5.4.18 | HTML coverage report generation |

## TestData Files

| File | Purpose |
|------|---------|
| `TestData/TestUnitDB.accdb` | Access 2007+ database used by ODBC tests |
| `TestData/TestUnitDB.mdb` | Legacy Access 97–2003 database (fallback for older ODBC driver) |
| `TestData/largeDummy.file` | Large binary file for IO and performance tests |

Both Access database files are copied to the output directory on every build (`Always`).

## Test Categories

Tests are tagged with `[TestCategory]` attributes. The defined categories are:

| Category | Description |
|----------|-------------|
| `Unit` | All standard unit tests |
| `CLI` | Command-line parsing component tests |
| `Data` | Data-handling component tests (Audio, CSV) |
| `Audio` | Audio format detection tests |
| `ODBC` | Tests requiring an Access ODBC driver |
| `SQLServer` | Tests requiring a live SQL Server connection |
| `Logging` | Logging framework tests |
| `Mail` | Email attachment handler tests |
| `Security` | Encryption and security tests |
| `Utils` | Utility class tests |
| `DataStructures` | Binary search tree and URL utility tests |

## Running Tests

Build the solution first, then run tests with `dotnet test`. The project file does not include `--no-build` as a default, so a prior build is required.

```powershell
# Run all tests (both net48 and net9.0)
dotnet test "Toolkit.Modern.Tests\ByteForge.Toolkit.Modern.Tests.csproj"

# Run only tests that do NOT require external dependencies
dotnet test --filter "TestCategory!=ODBC&TestCategory!=SQLServer"

# Run only ODBC tests (requires Access ODBC driver)
dotnet test --filter "TestCategory=ODBC"

# Run only SQL Server live tests (requires a running SQL Server instance)
dotnet test --filter "TestCategory=SQLServer"

# Run tests for a specific module
dotnet test --filter "TestCategory=CLI"
dotnet test --filter "TestCategory=Security"

# Run with code coverage (XPlat)
dotnet test --collect:"XPlat Code Coverage"
```

## Prerequisites for Integration Tests

### SQL Server Tests (`TestCategory=SQLServer`)

Several test classes (`DBAccessCoreTests`, `DBAccessMethodsSQLServerTests`, `DBAccessParameterParsingTests`, `DBAccessTransactionTests`, `DBAccessScriptExecutionTests`, `BulkDbProcessorTests`, `DatabaseLoggerSqlServerLiveTests`) require a SQL Server instance with the `TestUnitDB` database set up.

**Local default:** `(local)` with Windows Authentication (Integrated Security).

**CI/environment variable overrides:**

| Variable | Purpose |
|----------|---------|
| `BYTEFORGE_TEST_SQL_SERVER` | SQL Server hostname or IP |
| `BYTEFORGE_TEST_SQL_PORT` | Port (optional, default 1433) |
| `BYTEFORGE_TEST_SQL_USER` | SQL login username (leave unset for trusted auth) |
| `BYTEFORGE_TEST_SQL_PASSWORD` | SQL login password |
| `BYTEFORGE_TEST_SQL_CONNECTION_STRING` | Full connection string (overrides all other variables) |
| `BYTEFORGE_TEST_SQL_DATABASE` | Database name (default `TestUnitDB`) |

The `TestUnitDB` database must contain the tables `TestEntities`, `TestDataTypes`, and `BulkTestEntities` seeded with test data. SQL scripts for creating and seeding the database are in the `TODO/` folder.

### ODBC Tests (`TestCategory=ODBC`)

`DBAccessMethodsODBCTests` connects to an Access database via ODBC. The helper resolves the database file automatically:

1. If `BYTEFORGE_TEST_ACCESS_FORMAT=accdb`, uses `TestData/TestUnitDB.accdb` with the `Microsoft Access Driver (*.mdb, *.accdb)` driver.
2. If `BYTEFORGE_TEST_ACCESS_FORMAT=mdb`, uses `TestData/TestUnitDB.mdb` with the `Microsoft Access Driver (*.mdb)` driver.
3. If the variable is unset, falls back to `.accdb` if it exists, otherwise `.mdb`.

The `Microsoft Access Database Engine` ODBC driver must be installed on the machine. The 32-bit and 64-bit editions must match the test runner's bitness.

## Code Coverage

The `.runsettings` file configures both Visual Studio Live Code Coverage and XPlat Code Coverage:

- Includes: `ByteForge.Toolkit*.dll`
- Excludes: test assemblies, auto-generated property accessors, and compiler-generated code

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Root** | Solution overview | [../../readme.md](../../readme.md) |
| **Toolkit.Modern** | Core library | [../../Toolkit.Modern/readme.md](../../Toolkit.Modern/readme.md) |
| **TestBed** | Manual exploration console app | [../../TestBed/readme.md](../../TestBed/readme.md) |

### Unit Test Modules

| Module | Documentation |
|--------|---------------|
| Helpers | [Helpers/README.md](Helpers/README.md) |
| Models | [Models/README.md](Models/README.md) |
| Unit (overview) | [Unit/readme.md](Unit/readme.md) |
| CLI | [Unit/CLI/readme.md](Unit/CLI/readme.md) |
| Configuration | [Unit/Configuration/readme.md](Unit/Configuration/readme.md) |
| Data | [Unit/Data/readme.md](Unit/Data/readme.md) |
| Data/Audio | [Unit/Data/Audio/readme.md](Unit/Data/Audio/readme.md) |
| Data/CSV | [Unit/Data/CSV/readme.md](Unit/Data/CSV/readme.md) |
| Data/Database | [Unit/Data/Database/readme.md](Unit/Data/Database/readme.md) |
| DataStructures | [Unit/DataStructures/readme.md](Unit/DataStructures/readme.md) |
| Logging | [Unit/Logging/readme.md](Unit/Logging/readme.md) |
| Mail | [Unit/Mail/readme.md](Unit/Mail/readme.md) |
| Security | [Unit/Security/readme.md](Unit/Security/readme.md) |
| Utils | [Unit/Utils/readme.md](Unit/Utils/readme.md) |
