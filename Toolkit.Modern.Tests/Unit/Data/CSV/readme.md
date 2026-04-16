# CSV Unit Tests

Tests for the CSV reading and writing components in `ByteForge.Toolkit.Data`.

**Test classes:** `CSVReaderTests`, `CSVWriterTests`
**Source module:** `Toolkit.Modern/Data/`

## Test Classes

### CSVReaderTests

Covers the `CSVReader` class, which parses CSV data into typed objects.

| Test area | Coverage |
|-----------|---------|
| Constructor options | Default delimiter, custom delimiter, header detection settings |
| Input sources | Reading from a file path, a `Stream`, and an in-memory string |
| Attribute-based mapping | `[CSVColumn]` attribute maps CSV column names to properties |
| Strong typing | Automatic conversion to `int`, `decimal`, `bool`, `DateTime`, `Guid` |
| Header detection | Auto-detection of header row vs. data-only mode |
| Custom delimiters | Tab-separated, semicolon-separated, and pipe-separated values |
| Quoted values | Values enclosed in double quotes, including embedded commas |
| Escape sequences | Escaped quote characters inside quoted fields |
| Error handling | Malformed CSV, type conversion errors, missing required columns |
| Performance | Large datasets (many rows) |

### CSVWriterTests

Covers the `CSVWriter` class, which serializes objects to CSV format.

| Test area | Coverage |
|-----------|---------|
| Output targets | Writing to a file path, a `Stream`, and a `StringBuilder` |
| Header generation | Auto-generation from class property names |
| Attribute-based mapping | `[CSVColumn]` controls column name and ordering |
| Custom delimiters | Writing with non-default delimiters |
| Quoting and escaping | Values containing commas, quotes, and newlines are properly escaped |
| Performance | Large datasets |
| Thread safety | Concurrent writes |
| Resource management | `Dispose()` / `using` patterns |

## Test Helpers Used

- `AssertionHelpers.AssertCsvRoundTrip` — validates that a write/read cycle preserves the record count
- `TempFileHelper` — creates and cleans up temporary `.csv` files

## Prerequisites

No external databases or drivers required. All tests use temp files or in-memory streams.

## Running These Tests

```powershell
# All CSV tests
dotnet test --filter "FullyQualifiedName~Data.CSV"

# By class
dotnet test --filter "FullyQualifiedName~CSVReaderTests"
dotnet test --filter "FullyQualifiedName~CSVWriterTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../../README.md](../../../README.md) |
| **Unit overview** | Unit test organization | [../../readme.md](../../readme.md) |
| **Data overview** | Data tests overview | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../../Helpers/README.md](../../../Helpers/README.md) |
