# Utils Unit Tests

Tests for `ByteForge.Toolkit.Utilities`.

**Test categories:** `Unit`, `Utils`
**Source module:** `Toolkit.Modern/Utils/` (namespace `ByteForge.Toolkit.Utilities`)

## Test Classes

| Class | Class Under Test | Description |
|-------|-----------------|-------------|
| `BooleanParserTests` | `BooleanParser` | Flexible boolean parsing from strings |
| `ConsoleUtilTests` | `ConsoleUtil` | Console-related utilities |
| `DateTimeParserTests` | `DateTimeParser` | Flexible date/time parsing |
| `DateTimeUtilTests` | `DateTimeUtil` | Date/time manipulation utilities |
| `EnumExtensionsTests` | `EnumExtensions` | Enum description and parsing helpers |
| `IOUtilsTests` | `IOUtils` | File, directory, and path utilities |
| `NameCapitalizerTests` | `NameUtil` | Name capitalization with linguistic rules |
| `ParserTests` | Various parsers | Generic type-conversion and parsing |
| `StringUtilTests` | `StringUtil` | String manipulation utilities |
| `TemplateProcessorTests` | `TemplateProcessor` | String template substitution |
| `TimingUtilTests` | `TimingUtil` | Stopwatch-based timing utilities |
| `UtilsTests` | Miscellaneous | General helper and type-conversion methods |

## Coverage Details

### BooleanParserTests

- `BooleanParser.Default` singleton returns the same instance on repeated access
- Parsing `"true"`, `"false"`, `"1"`, `"0"`, `"yes"`, `"no"`, `"on"`, `"off"` and equivalents
- Case-insensitive matching
- Custom true/false strings
- Null and empty input handling

### ConsoleUtilTests

- Console availability detection (redirected vs. interactive)
- Color-safe output methods that do not throw when console is unavailable
- Progress indicator creation

### DateTimeParserTests

- Parsing ISO 8601, common locale formats, and custom format strings
- Unambiguous vs. ambiguous date handling
- Culture-aware parsing (en-US, fr-FR, etc.)
- Parser result caching

### DateTimeUtilTests

- Unix timestamp to `DateTime` conversion (both directions)
- `DateTime` arithmetic helpers
- Formatting to common output strings
- Round-trip accuracy

### EnumExtensionsTests

- `GetDescription()` — reads `[Description]` attribute value; falls back to enum name
- `Parse<TEnum>(string)` — case-insensitive parsing; exception for unknown values
- `IsValidValue<TEnum>(value)` — valid and out-of-range values

### IOUtilsTests

- File pattern matching (`*.txt`, `**/*.cs`)
- Directory scanning with recursion options
- Path normalization (redundant separators, mixed slashes)
- UNC path detection

### NameCapitalizerTests

Validates `NameUtil.CapitalizeName`, which applies proper capitalization rules for human names:

| Test area | Coverage |
|-----------|---------|
| Null input | Returns `null` |
| Empty/whitespace | Returns input unchanged |
| Basic capitalization | `"john doe"` → `"John Doe"` |
| Particles | `"de"`, `"van"`, `"von"`, `"di"`, `"della"` remain lowercase when mid-name |
| Prefixes | `"mc"`, `"mac"`, `"o'"` — prefix and first letter after are capitalized |
| Hyphenated names | Each segment is independently capitalized |
| All-caps input | Normalized to title-case |
| Unicode | Non-Latin scripts pass through without modification |

### ParserTests

- Generic `Parse<T>(string, T defaultValue)` helpers
- Type conversion for `int`, `decimal`, `bool`, `DateTime`, `Guid`, `enum`
- Fallback default value on parse failure
- Null and empty input handling

### StringUtilTests

- Truncation with optional ellipsis
- Padding (left, right, center)
- Case conversion helpers
- Pattern matching and replacement
- Safe substring (no `IndexOutOfRangeException`)

### TemplateProcessorTests

- `{Key}` placeholder substitution from a `Dictionary<string, string>`
- Missing key handling (preserve, replace with empty, or throw)
- Escaped braces `{{` and `}}`
- Nested templates
- Performance with many placeholders

### TimingUtilTests

- Start / stop / elapsed timing
- Multiple independent timers
- Elapsed time formatted as `hh:mm:ss.fff`
- Zero-duration operations

### UtilsTests

- Miscellaneous helper methods
- Legacy utility methods maintained for compatibility

## Prerequisites

No external dependencies. `TempFileHelper` is used in IO-related tests.

## Running These Tests

```powershell
# All Utils tests
dotnet test --filter "TestCategory=Utils"

# A specific class
dotnet test --filter "FullyQualifiedName~NameCapitalizerTests"
dotnet test --filter "FullyQualifiedName~BooleanParserTests"
dotnet test --filter "FullyQualifiedName~TemplateProcessorTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
