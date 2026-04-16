# TestBed

A console application used for manual, interactive exploration of `ByteForge.Toolkit.Modern` features. It is **not** an automated test project — it has no test framework, no assertions, and no CI integration. Its purpose is to let developers run library code against a live environment and observe output directly in the terminal.

## Project Details

| Property | Value |
|----------|-------|
| Project type | Console executable (`OutputType = Exe`) |
| Target framework | `net9.0-windows` |
| Entry point | `TestBed.Program.Main(string[] args)` |
| Reference | `Toolkit.Modern/ByteForge.Toolkit.Modern.csproj` |

## How to Run

```powershell
# From the solution root
dotnet run --project TestBed\TestBed.csproj

# Or build first and run the executable
dotnet build TestBed\TestBed.csproj
.\TestBed\bin\Debug\net9.0-windows\TestBed.exe
```

## Source Files

### Program.cs

The entry point. Runs a sequence of CLI-focused exercises that demonstrate the `ByteForge.Toolkit.CommandLine` API:

| Exercise | What it does |
|----------|-------------|
| `TestCommandAttribute()` | Placeholder — confirms `[Command]`-decorated classes compile |
| `TestOptionAttribute()` | Placeholder — confirms `[Option]`-decorated parameters compile |
| `TestConsoleSpinner()` | Creates `ConsoleSpinner` instances in all built-in styles (`ASCII`, `Braille`, `Circles`, `Arrows`, `UnicodeStars`), a custom character-sequence spinner, and a positionally-placed spinner |
| `TestProgressSpinner()` | Exercises `ProgressSpinner` with a short simulated delay |
| `TestGlobalOption()` | Creates three `GlobalOption` instances: a flag option (`--verbose`), a value option (`--config`), and a value option with aliases (`--debug-level`) |
| `TestRootCommandBuilder()` | Builds a `RootCommandBuilder` with help, typo correction, parameter explanation, a banner, and a global verbose option |
| `TestCompleteApplication(args)` | Builds a more complete `RootCommandBuilder` with a banner and global options; does not invoke the parser with real args |

The file also defines a set of sample command classes that illustrate the CLI attribute model:

| Class | Commands | Notable features |
|-------|----------|-----------------|
| `GreetCommands` | `greet hello` | `[Option]` with alias `"n"` and a default value |
| `ProcessCommands` | `process start` | `[Command]` with aliases (`"proc"`, `"run"`) |
| `DataCommands` | `import` | Multiple options including `int` and `bool` typed parameters |
| `ConfigCommands` | `setup` | Enum-typed options (`LogLevel`, `OutputFormat`) |
| `BackupCommands` | `backup` | `ConsoleSpinner` usage inside a command method |
| `DownloadCommands` | `download` | Async command returning `Task` |
| `DeployCommands` | `deploy` | Async command returning `Task<int>`, `ProgressSpinner` with `UpdateMessage` |
| `FileCommands` | `file copy`, `file list` | Nested command group, async copy with `ProgressSpinner` |
| `UtilityCommands` | `version`, `help` | `[Command]` with multiple aliases |

### ConfigurationTests.cs

Static helper methods demonstrating `ByteForge.Toolkit.Configuration` usage patterns. All calls to the library are commented out; the methods print status messages only. Intended as a scratchpad for uncommenting and running specific configuration scenarios:

- `TestBasicConfiguration` — reads `string`, `int`, and `bool` values from INI sections
- `TestStronglyTypedConfiguration` — maps a section to a typed class and saves changes
- `TestArrayConfiguration` — reads array-valued configuration sections

### DataTests.cs

Static helper methods demonstrating `ByteForge.Toolkit.Data` (database) usage patterns. Calls are commented out:

- `TestBasicDatabaseOperations` — `GetValue<int>`, `GetRecords` with parameters
- `TestTransactionHandling` — `BeginTransaction`, `Commit`, `Rollback`

### SecurityTests.cs

Static helper methods demonstrating `ByteForge.Toolkit.Security` usage patterns. Calls are commented out:

- `TestAESEncryption` — encrypt and decrypt a string, verify round-trip
- `TestCustomKey` — encrypt multiple strings with a custom key/IV pair

## Relationship to Automated Tests

TestBed is complementary to `Toolkit.Modern.Tests`. It provides:

- A quick way to visually inspect spinner and CLI output in a real terminal
- A scratchpad for trying out new API usage before writing formal tests
- Sample code showing how to wire up `RootCommandBuilder` end-to-end

It does **not** replace the automated test suite. Regressions should be caught by `Toolkit.Modern.Tests`, not by running TestBed manually.

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Root** | Solution overview | [../readme.md](../readme.md) |
| **Toolkit.Modern** | Core library | [../Toolkit.Modern/readme.md](../Toolkit.Modern/readme.md) |
| **Automated tests** | MSTest unit test suite | [../Toolkit.Modern.Tests/README.md](../Toolkit.Modern.Tests/README.md) |
| **CLI source** | CommandLine module | [../Toolkit.Modern/CommandLine/readme.md](../Toolkit.Modern/CommandLine/readme.md) |
