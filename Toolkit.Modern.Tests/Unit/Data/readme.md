# Data Unit Tests

Tests for the data-handling components in `ByteForge.Toolkit.Data`. The module covers audio format detection, CSV reading/writing, and database access.

**Source module:** `Toolkit.Modern/Data/`

## Subdirectories

| Folder | Module Area | Documentation |
|--------|-------------|---------------|
| `Audio/` | Audio format detection (`AudioFormatDetector`) | [Audio/readme.md](Audio/readme.md) |
| `CSV/` | CSV reading and writing (`CSVReader`, `CSVWriter`) | [CSV/readme.md](CSV/readme.md) |
| `Database/` | Database access (`DBAccess`, `BulkDbProcessor<T>`) | [Database/readme.md](Database/readme.md) |

## Test Category Summary

| Category | Requirement |
|----------|------------|
| `Unit` + `Data` + `Audio` | No external dependencies |
| `Unit` + `Data` (CSV) | No external dependencies; uses temp files |
| No category / `SQLServer` | SQL Server `TestUnitDB` database |
| `ODBC` | Microsoft Access ODBC driver + `TestData/TestUnitDB.accdb` or `.mdb` |

## Helpers Used

- `DatabaseTestHelper` — connection factories and data management for database tests
- `TempFileHelper` — temp file lifecycle for CSV and script tests
- `AssertionHelpers` — `AssertCsvRoundTrip`, `AssertEncryptionRoundTrip`, `AssertBinarySearchTreeOrdering`

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
| **Data source** | Production module | [../../../Toolkit.Modern/Data/readme.md](../../../Toolkit.Modern/Data/readme.md) |
