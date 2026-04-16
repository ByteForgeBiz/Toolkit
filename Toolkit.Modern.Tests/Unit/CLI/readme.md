# CLI Unit Tests

Tests for `ByteForge.Toolkit.CommandLine`. All test classes carry `[TestCategory("Unit")]` and `[TestCategory("CLI")]`.

**Source module:** `Toolkit.Modern/CommandLine/`

## Test Classes

### CommandAttributeTests

Validates the `CommandAttribute` constructor, property immutability, and attribute target declarations.

| Test area | Coverage |
|-----------|---------|
| Constructor | Valid parameters, omitted aliases, null name, null description, empty strings, multiple aliases |
| Property immutability | `Name`, `Description`, and `Aliases` are read-only (verified via reflection) |
| Alias reference stability | Multiple reads of `Aliases` return the same array reference |
| Edge cases | Whitespace-only strings, special characters, Unicode and emoji, strings of 500–1000 characters |
| `AttributeUsage` targets | `AttributeTargets.Class` and `AttributeTargets.Method` are both declared |

### OptionAttributeTests

Validates the `OptionAttribute` constructor, alias handling, and the settable `Name` property.

| Test area | Coverage |
|-----------|---------|
| Constructor variants | Description-only and description-with-aliases |
| Null/empty inputs | Null description, empty description, null aliases, empty alias array |
| Alias deduplication | Duplicate aliases in the constructor |
| Alias edge cases | Whitespace-only aliases, special characters, Unicode |
| `Name` settability | `Name` is the only public writable property; `Description` and `Aliases` are read-only |
| `AttributeUsage` target | `AttributeTargets.Parameter` |

### GlobalOptionTests

Validates `GlobalOption` construction, name normalization, alias generation, and pattern matching.

| Test area | Coverage |
|-----------|---------|
| Constructor — no-value action | `Action` delegate, `ExpectsValue = false`, empty `CustomAliases` |
| Constructor — value action | `Action<string>` delegate, `ExpectsValue = true`, custom aliases |
| Name normalization | Leading `--`, `-`, and `/` are stripped from the option name |
| Input validation | `ArgumentNullException` thrown for null name; `ArgumentException` for empty/whitespace name |
| Alias generation | Auto-generated aliases from name; custom aliases preserved |
| Pattern matching | `Matches(string)` performs case-insensitive comparison against name and all aliases |
| Action execution | Delegates are callable through the stored `Action` property |

### ConsoleSpinnerTests

Validates `ConsoleSpinner` lifecycle, property access, thread safety, and disposal.

| Test area | Coverage |
|-----------|---------|
| Constructors | Default, message-only, message + style, message + style + delay, null message, empty message |
| `SpinnerStyle` enum | All defined enum values are valid; all styles are accepted by the constructor |
| `Message` property | Settable including null and strings containing line breaks |
| `Color` property | Settable including null |
| `IsRunning` | Initially `false`; becomes `true` after `Start()` and `false` after `Stop()` |
| Start/Stop lifecycle | Start when not running, stop when running, start when already running (no-op), stop when not running (no-op), multiple start/stop cycles |
| Thread safety | 10 concurrent tasks calling `Start()`/`Stop()` with no exceptions |
| Concurrent `Message` writes | 10 concurrent writes do not throw |
| Disposal | `Dispose()` stops a running spinner; `Dispose()` on a stopped spinner does not throw; multiple `Dispose()` calls are safe; `using` statement disposes correctly |
| Edge cases | 1000-character message, special characters and emoji in messages, delay = 0, delay = −1 |
| Performance | 100 rapid start/stop cycles without memory leaks |

A `[ClassInitialize]` method captures the `TestContext.CancellationTokenSource` to propagate test cancellation into async tasks.

## Running These Tests

```powershell
# All CLI tests
dotnet test --filter "TestCategory=CLI"

# A specific class
dotnet test --filter "FullyQualifiedName~CommandAttributeTests"
dotnet test --filter "FullyQualifiedName~ConsoleSpinnerTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **CLI source** | Production module | [../../../Toolkit.Modern/CommandLine/readme.md](../../../Toolkit.Modern/CommandLine/readme.md) |
