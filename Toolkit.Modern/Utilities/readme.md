# Utilities Module

The Utilities module is a collection of focused, general-purpose helper classes covering string manipulation, parsing, date/time operations, I/O, console output, name formatting, template processing, type reflection, and more.

---

## Class Overview

| Class | Description |
|-------|-------------|
| `Utils` | Phone number formatting, null-if-empty, sync-over-async runner |
| `StringUtil` | JavaScript escaping, list wrapping, PascalCase splitting |
| `IParser` / `Parser` | Type-safe string parsing with custom type registry |
| `BooleanParser` | Parses string representations of boolean values |
| `DateTimeParser` | Parses date/time strings across many formats |
| `DateTimeUtil` | Unix epoch conversions, time zone resolution |
| `EnumExtensions` | `GetDescription()` extension for enums |
| `HtmlUtil` | CSS minification |
| `IOUtils` | Multi-pattern file listing, UNC path resolution |
| `NameUtil` | Culturally-aware name capitalization |
| `ConsoleUtil` | Console availability check, progress bars, text alignment |
| `TemplateProcessor` | Regex-based `<key>` placeholder substitution |
| `TimingUtil` | Measures and logs method execution time |
| `TypeHelper` | Resolves and unwraps types from reflection members |
| `ValueConverterRegistry` | Named registry of `object → object` conversion functions |

---

## `Utils`

General-purpose methods.

| Method | Description |
|--------|-------------|
| `FormatUSPhoneNumber(string)` | Formats a US phone number to `(XXX) XXX-XXXX`. Handles 11-digit numbers with country code. Converts letter keypads (ABC→2, DEF→3, etc.) |
| `NullIfEmpty(string)` | Returns `null` if the string is null or whitespace; otherwise returns the original string |
| `RunSync<T>(Func<CancellationToken, Task<T>>)` | Runs an async function synchronously on the current thread |

---

## `StringUtil`

String manipulation helpers.

| Method | Description |
|--------|-------------|
| `EscapeForJavaScript(string)` | Escapes `\`, `'`, `"`, `\r`, `\n`, `\t`, `\b`, `\f` for safe embedding in JavaScript strings |
| `WrapList<T>(items, separator=", ", maxLineLength=200)` | Joins a collection to a string, inserting line breaks to keep lines under `maxLineLength` |
| `SplitPascalCase(string)` | Splits a PascalCase string at case transitions and digit/letter boundaries. Example: `"ExtendedPropertyLevel0Type"` → `"Extended Property Level 0 Type"` |

On .NET Framework targets, also provides a `Contains(string, StringComparison)` extension method (polyfill for the .NET 5+ overload).

---

## `IParser` and `Parser`

`IParser` is the interface; `Parser` is the full implementation.

### Built-in supported types

`bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `string`, `DateTime`, `TimeSpan`, `Guid`, `Uri`, `byte[]` (Base64), `Encoding`, `CultureInfo`, `Type`, `IPAddress`, all enum types, and Newtonsoft.Json-serializable types (fallback).

### Key methods

```csharp
// Parse a string to a given type
object? Parse(Type? type, string input)
T Parse<T>(string input)

// Try-parse variants
bool TryParse(Type? type, string input, out object? result)
bool TryParse<T>(string input, out T? result)

// Convert object to string
string? Stringify(Type? type, object? value)
string? Stringify<T>(object? value)

// Register a custom type parser
void RegisterType(Type type, Func<string, object> parser, Func<object, string> serializer)

// Check if a type is known
bool IsKnownType(Type type)
```

`Parser` is culture-aware. The constructor takes an optional `CultureInfo` (defaults to `CultureInfo.InvariantCulture`).

### Static default

```csharp
Parser.Default  // Lazy<Parser> singleton with InvariantCulture
```

Static convenience wrappers (`Parser.Parse<T>()`, `Parser.TryParse<T>()`, `Parser.Stringify<T>()`) delegate to `Parser.Default`.

---

## `BooleanParser`

Parses strings to `bool` using configurable true/false word sets.

**Default true values:** `"true"`, `"t"`, `".T."`, `"yes"`, `"y"`, `"1"`, `"on"`, `"enabled"`

**Default false values:** `"false"`, `"f"`, `".F."`, `"no"`, `"n"`, `"0"`, `"off"`, `"disabled"`

Any non-zero integer string is treated as `true`. Matching is case-insensitive.

| Method | Description |
|--------|-------------|
| `Parse(string)` | Returns `bool` or throws if unrecognized |
| `TryParse(string, out bool)` | Returns `false` if unrecognized |
| `AddTrueValue(string)` | Adds a new true-word (throws if already a false-word) |
| `AddFalseValue(string)` | Adds a new false-word (throws if already a true-word) |

Static versions delegate to `BooleanParser.Default` (a `Lazy<BooleanParser>` singleton):

```csharp
bool.Parse("yes")    // true
bool.Parse("off")    // false
BooleanParser.RegisterTrueValue("affirmative");
```

---

## `DateTimeParser`

Parses date/time strings across a large set of format combinations.

**Format coverage:** 17 date formats × 7 time formats × 5 offset formats (tried in order of specificity). Falls back to `DateTime.TryParse` if no pattern matches.

**Performance:** After a successful parse, the winning format is cached in a `ConcurrentDictionary` keyed by the structural shape of the input (digits replaced with `n`, separators with `s`, AM/PM markers with `t`). Subsequent inputs with the same shape skip the format iteration.

**Timezone normalization:** Known timezone abbreviations (`EST`, `PST`, `UTC`, etc.) are stripped from the input before parsing.

| Method | Description |
|--------|-------------|
| `Parse(string)` | Returns `DateTime` or throws |
| `TryParse(string, out DateTime)` | Returns `false` if unparseable |
| `ClearCache()` | Clears the format cache |

Static delegates to `DateTimeParser.Default` singleton.

---

## `DateTimeUtil`

Extension and static methods for `DateTime`.

| Method | Description |
|--------|-------------|
| `FromUnixTime(long seconds)` | Converts Unix epoch seconds to `DateTime` |
| `FromUnixTimeMilliseconds(long ms)` | Converts Unix epoch milliseconds to `DateTime` |
| `ToUnixTime(this DateTime)` | Extension — converts to Unix epoch seconds |
| `ToUnixTimeMilliseconds(this DateTime)` | Extension — converts to Unix epoch milliseconds |
| `HasTimeComponent(this DateTime)` | Extension — `true` if the time-of-day portion is non-zero |
| `ToTimeZone(this DateTime, TimeZoneInfo)` | Extension — converts to the given time zone |
| `ToTimeZone(this DateTime, string)` | Extension — resolves the string as an IANA name (`"America/New_York"`) or timezone abbreviation (`"EST"`) before converting |

The `ToTimeZone(string)` overload consults two built-in dictionaries: `TimeZoneAbbreviations` (EST/PST/etc. → Windows TZ id) and `IanaToWindowsTimeZone` (IANA name → Windows TZ id).

---

## `EnumExtensions`

```csharp
public static string GetDescription(this Enum value)
```

Returns the `[Description("...")]` attribute value for the enum member, or `value.ToString()` if no attribute is present.

```csharp
public enum Status
{
    [Description("In Progress")] InProgress,
    [Description("Complete")]    Done
}

Status.InProgress.GetDescription()  // "In Progress"
```

---

## `HtmlUtil`

| Method | Description |
|--------|-------------|
| `MinifyCSS(string)` | Removes CSS comments, collapses whitespace around `{`, `}`, `:`, `;`, `,`, removes trailing semicolons before `}`, and collapses multiple spaces to one |

---

## `IOUtils`

File system helpers.

| Method | Description |
|--------|-------------|
| `GetFiles(path, searchPattern, searchOption)` | Lists file paths. `searchPattern` supports multiple patterns separated by `;` (e.g., `"*.csv;*.txt"`) |
| `GetFileInfos(path, searchPattern, searchOption)` | Same as `GetFiles` but returns `FileInfo[]` |
| `GetUniversalPath(string drivePath)` | Resolves a mapped drive letter to its UNC path using `WNetGetConnection` P/Invoke (mpr.dll) |

---

## `NameUtil`

Culturally-aware personal name formatting.

| Method | Description |
|--------|-------------|
| `CapitalizeName(string)` | Capitalizes a single name part, handling particles (`de`, `da`, `van`, `von`, `von der`, etc. remain lowercase unless they are the first word), prefixes (`Mc`→`McDonald`, `Mac`, `O'`→`O'Connor`, `D'`), and hyphens |
| `CapitalizeFullName(string)` | Capitalizes a full name. Handles `"Last, First"` format (comma-separated) and space-separated names |
| `NormalizeUserName(string)` | Strips `DOMAIN\` prefix and `@domain` suffix, then lowercases |

---

## `ConsoleUtil`

Console availability and output helpers.

| Member | Description |
|--------|-------------|
| `IsConsoleAvailable` | `true` if a console window exists (`GetConsoleWindow() != IntPtr.Zero`) and neither stdout nor stderr is redirected |
| `DrawLine(char ch='-', int length=0)` | Draws a horizontal line (defaults to console width) |
| `CenterText(string)` | Writes text centered on the console |
| `RightAlignText(string)` | Writes text right-aligned on the console |
| `DrawProgressBar(int pct, string message)` | Draws a progress bar. Positive `pct` (0–100): `[████░░░░] 75%`. Negative `pct`: animated indeterminate bar with a moving `█` block |

---

## `TemplateProcessor`

Simple `<key>` placeholder substitution using a case-insensitive dictionary.

```csharp
var tp = new TemplateProcessor();
tp.Add("Name", "World");
tp.Add("Date", DateTime.Today.ToShortDateString());

string result = tp.Process("Hello, <Name>! Today is <Date>.");
// "Hello, World! Today is 4/15/2026."
```

| Member | Description |
|--------|-------------|
| `Add(key, value)` | Registers a placeholder. Keys must not contain `<` or `>` |
| `Process(text)` | Replaces all `<key>` occurrences with registered values |
| `UseEscapeSequences` | If `true`, applies `Regex.Unescape()` to the output after substitution |

---

## `TimingUtil`

Measures execution time and logs the result.

```csharp
// Logs: "LoadData (142ms)" at Debug level
timingUtil.Time(() => LoadData(), "LoadData");

// Async variant
await timingUtil.TimeAsync(() => FetchAsync(), "FetchAsync");

// Returns the result
var result = timingUtil.Time(() => ComputeValue(), "ComputeValue");
```

**Constructors:**

| Constructor | Logger target |
|-------------|---------------|
| `TimingUtil()` | Uses `Log.Instance` |
| `TimingUtil(ILogger)` | Uses the supplied `ILogger` |
| `TimingUtil(string logFilePath)` | Creates a `FileLogger` at the given path |

Log message format: `"{message} ({elapsed.TotalMilliseconds}ms)"` at `LogLevel.Debug`.

---

## `TypeHelper`

Resolves the effective type of a reflection member, unwrapping `Nullable<T>`.

```csharp
// Overloads for PropertyInfo, FieldInfo, ParameterInfo, EventInfo, MethodInfo, Type
Type? resolved = TypeHelper.ResolveType(propertyInfo);

// Get the default value for a type
object? def = TypeHelper.GetDefault(typeof(int));   // 0
object? def = TypeHelper.GetDefault(typeof(string)); // null
```

---

## `ValueConverterRegistry`

A static registry mapping named string keys to `Func<object, object>` converters.

| Method | Description |
|--------|-------------|
| `RegisterConverter(name, func)` | Registers a converter. Throws if a different function is already registered under the same name (case-insensitive) |
| `GetConverter(name?)` | Returns the converter or `null` if not found |
| `HasConverter(name)` | Whether a converter is registered |
| `RemoveConverter(name)` | Removes a converter by name |
| `ClearConverters()` | Removes all converters |

```csharp
ValueConverterRegistry.RegisterConverter("ToUpper", v => v.ToString()!.ToUpper());
var fn = ValueConverterRegistry.GetConverter("ToUpper");
var result = fn?.Invoke("hello");  // "HELLO"
```

---

## Related Modules

| Module | Description |
|--------|-------------|
| [Logging](../Logging/readme.md) | Used by `TimingUtil` and `ConsoleUtil` |
| [Configuration](../Configuration/readme.md) | Uses `Parser` for type-safe config section deserialization |
