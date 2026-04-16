# Configuration / Models

This folder contains the model classes used by the configuration system.

---

## Types

### `ConfigSection<T>` (internal)

The internal implementation of `IConfigSection<T>`. Binds a named INI section to a POCO of type `T` and handles loading, saving, and all special property types (arrays, dictionaries, encrypted values, defaults).

This class is created and managed by `Configuration`/`IConfigurationManager`. It is not instantiated directly by application code.

**Key behaviours:**

- On construction, immediately calls `LoadFromConfiguration()` to populate `Value` from the `IConfigurationRoot`.
- Uses a `ReaderWriterLockSlim` (shared with the owning `Configuration` instance) to coordinate reads and writes.
- Properties decorated with `[Ignore]` are excluded entirely.
- Properties decorated with `[DoNotPersist]` are loaded but excluded from `SaveToConfiguration()`.
- Properties decorated with `[Encrypted]` are decrypted on load and encrypted on save using `Encryptor.Default`.
- Array properties (`[Array]`) are stored as indexed keys in a separate INI section (e.g., `[Section#PropertyArray]`).
- Dictionary properties (`[Dictionary]`) are stored as `key=value` lines in a separate INI section (e.g., `[Section#PropertyDict]`).
- Default values are resolved via `DefaultValueHelper` and applied when the INI key is absent or empty.
- Properties set to their default value are omitted from the saved INI file.

**Supported array element types:** Any type parseable by `Parser.Default`.

**Supported dictionary types:** `Dictionary<string,string>`, `IDictionary<string,string>`, `IReadOnlyDictionary<string,string>`, `ICollection<KeyValuePair<string,string>>`, `IEnumerable<KeyValuePair<string,string>>`, `IReadOnlyCollection<KeyValuePair<string,string>>`.

---

### `GlobalizationInfo` (public)

A configuration POCO that holds culture and formatting settings for the application. Automatically loaded from the `[Globalization]` section and exposed via `Configuration.Globalization`.

#### Culture

| Property | Default | Description |
|---|---|---|
| `CultureInfo` | `InvariantCulture` | The `CultureInfo` used in all `Format*` methods. |

#### Date formats

| Property | Default | Description |
|---|---|---|
| `DateFormat` | `MM/dd/yyyy` | Short date. |
| `LongDateFormat` | `dddd, MMMM d, yyyy` | Long date with day of week. |
| `DateTimeFormat` | `DateFormat + " " + TimeFormat` | Combined (computed, read-only). |
| `ShortDateTimeFormat` | `DateFormat + " " + ShortTimeFormat` | Short date + short time (computed). |
| `LongDateTimeFormat` | `LongDateFormat + " " + LongTimeFormat` | Long date + long time (computed). |
| `ShortDateTime12Format` | `DateFormat + " " + ShortTime12Format` | Short date + 12-hour short time (computed). |
| `LongDateTime12Format` | `LongDateFormat + " " + LongTime12Format` | Long date + 12-hour long time (computed). |

#### Time formats (24-hour)

| Property | Default | Description |
|---|---|---|
| `TimeFormat` | `HH:mm:ss` | Standard 24-hour time. |
| `ShortTimeFormat` | `HH:mm` | Hours and minutes only. |
| `LongTimeFormat` | `HH:mm:ss.fff` | With milliseconds. |

#### Time formats (12-hour)

| Property | Default | Description |
|---|---|---|
| `Time12Format` | `hh:mm:ss tt` | 12-hour time. |
| `ShortTime12Format` | `h:mm tt` | 12-hour hours + minutes. |
| `LongTime12Format` | `hh:mm:ss.fff tt` | 12-hour with milliseconds. |

#### Numeric formats

| Property | Default | Description |
|---|---|---|
| `CurrencyFormat` | `'$' #,##0.00` | Currency with thousands separator. |
| `IntegerFormat` | `#,##0` | Integer with thousands separator. |
| `NumberFormat` | `#,##0.00` | Decimal number with two decimal places. |
| `PercentFormat` | `#0.00'%'` | Percentage with two decimal places. |

#### Format methods

`GlobalizationInfo` provides `Format*` convenience methods for all format properties. All accept a nullable value and an optional `nullValue` string (default `""`).

| Method signature | Description |
|---|---|
| `FormatDate(DateTime? value, string nullValue)` | Format using `DateFormat`. |
| `FormatDateTime(DateTime? value, string nullValue)` | Format using `DateTimeFormat`. |
| `FormatLongDate(DateTime? value, string nullValue)` | Format using `LongDateFormat`. |
| `FormatShortTime(DateTime? value, string nullValue)` | Format using `ShortTimeFormat`. |
| `FormatTime(DateTime? value, string nullValue)` | Format using `TimeFormat`. |
| `FormatLongTime(DateTime? value, string nullValue)` | Format using `LongTimeFormat`. |
| `FormatTime12(DateTime? value, string nullValue)` | Format using `Time12Format`. |
| `FormatShortTime12(DateTime? value, string nullValue)` | Format using `ShortTime12Format`. |
| `FormatLongTime12(DateTime? value, string nullValue)` | Format using `LongTime12Format`. |
| `FormatShortDateTime(DateTime? value, string nullValue)` | Format using `ShortDateTimeFormat`. |
| `FormatLongDateTime(DateTime? value, string nullValue)` | Format using `LongDateTimeFormat`. |
| `FormatShortDateTime12(DateTime? value, string nullValue)` | Format using `ShortDateTime12Format`. |
| `FormatLongDateTime12(DateTime? value, string nullValue)` | Format using `LongDateTime12Format`. |
| `FormatInteger(int?/long?/short?/byte?/uint?/ulong?/ushort?/sbyte? value, string nullValue)` | Format using `IntegerFormat`. |
| `FormatNumber(double?/float?/decimal? value, string nullValue)` | Format using `NumberFormat`. |
| `FormatCurrency(double?/float?/decimal? value, string nullValue)` | Format using `CurrencyFormat`. |

---

### `AssemblyException` (internal)

A specialised exception type thrown when the configuration system cannot resolve the entry assembly for auto-detecting the INI file name. In practice this should never be encountered in normal application scenarios.

Inherits from `Exception` with the standard constructor overloads (default, message-only, message + inner exception).
