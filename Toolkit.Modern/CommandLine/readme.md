# CommandLine Module

## Overview

The **CommandLine** module provides attribute-based and reflection-driven command-line parsing for console applications. It builds on top of `System.CommandLine` to offer automatic command discovery from assemblies, token replacement pre-processing, optional banner display, and animated progress indicators.

---

## Purpose

The module solves four problems in one cohesive API:

1. **Command discovery** — scan an assembly (or a `plugins/` directory) and build a fully configured `System.CommandLine` parser from methods decorated with `[Command]` and parameters decorated with `[Option]`.
2. **Token normalization** — map case-variant or abbreviated tokens to their canonical forms before the parser sees them.
3. **Global options** — intercept cross-cutting flags (e.g., `--console`, `--quiet`) before sub-command routing, without polluting individual command definitions.
4. **Progress feedback** — `ConsoleSpinner` and `ProgressSpinner` provide animated spinners for long-running operations.

---

## Key Classes

### `RootCommandBuilder`

Fluent builder that configures the `System.CommandLine` pipeline and produces a `CommandParser`.

| Method | Description |
|---|---|
| `AddAssembly(string path)` | Load commands from an assembly file. |
| `AddAssembly(Assembly)` | Load commands from a loaded assembly. |
| `SearchPlugins()` | Scan the `plugins/` directory next to the entry assembly. |
| `SearchPlugins(string path)` | Scan a custom directory for plugin DLLs. |
| `AddCommand(Command)` | Add a pre-built `System.CommandLine.Command` directly. |
| `WithBanner(Action)` | Register an action printed before every `Parse` call. |
| `UseParameterExplanation(bool)` | Print a summary of parsed values to the console after parsing. |
| `AddGlobalOption(name, desc, Action, aliases)` | Register a flag-style global option (no value). |
| `AddGlobalOption(name, desc, Action<string>, aliases)` | Register a value-taking global option. |
| `AddConsoleLoggingOptions(enable, disable)` | Shortcut to add `--console`/`--quiet` global logging flags. |
| `UseHelp / UseEnvironmentVariables / UseParseDirective / UseSuggestDirective / UseTypoCorrections / UseParseErrorReporting / UseExceptionHandler / UseCancellation / UseCaseSensitivity` | Toggle individual `System.CommandLine` middleware features. All default to enabled except `UseCaseSensitivity` (defaults disabled). |
| `Build()` | Finalise alias generation and return a configured `CommandParser`. |

**Usage:**

```csharp
var parser = new RootCommandBuilder("My tool description")
    .AddAssembly(Assembly.GetExecutingAssembly())
    .SearchPlugins()
    .WithBanner(() => Console.WriteLine("MyTool v1.0"))
    .UseParameterExplanation()
    .AddConsoleLoggingOptions(
        enableConsoleAction: () => Log.EnableConsole(),
        disableConsoleAction: () => Log.DisableConsole())
    .Build();

var result = parser.Parse(args);
result.Invoke();
```

---

### `CommandBuilder` (static)

Scans an assembly and produces a collection of `System.CommandLine.Command` objects. Called internally by `RootCommandBuilder`; can also be used standalone.

**Key behaviours:**

- Classes decorated with `[Command]` become command groups. Instance methods on those classes decorated with `[Command]` become sub-commands.
- Static methods decorated with `[Command]` in any class become top-level commands.
- Method parameters are mapped to `Option<T>` objects. Parameter names and `[Option]` attributes drive names, descriptions, and aliases.
- Options receive automatically generated aliases: single-char (`-f`, `/f`), three-char prefix (`-fil`, `/fil`, `--fil`), and full name (`--filename`).
- Enum parameters are supported; their value names are added to the token normalisation table.
- Unsupported parameter types (arrays) and name conflicts are logged as warnings and skipped rather than thrown.
- All registered names are stored in `CommandBuilder.TokenList` for use during token replacement in `CommandParser`.

**Supported parameter types:** `string`, `int`, `long`, `float`, `double`, `decimal`, `bool`, `DateTime`, `Guid`, and any `enum`.

---

### `CommandParser`

The parser produced by `RootCommandBuilder.Build()`. Wraps `System.CommandLine.Parsing.Parser`.

| Member | Description |
|---|---|
| `Parse(params string[] args)` | Apply token replacement, process global options, invoke the optional banner, parse, and optionally print a parameter summary. Returns a `ParseResult`. |
| `Configuration` | Exposes the underlying `CommandLineConfiguration`. |

---

### `ParseResult`

A thin wrapper around `System.CommandLine.Parsing.ParseResult`.

| Member | Description |
|---|---|
| `Invoke()` | Execute the matched command handler and return its exit code. |
| `CommandResult` | The `System.CommandLine.CommandResult` for the parsed command. |
| `Errors` | Read-only list of `ParseError` objects. |
| `FindResultFor(Symbol)` | Look up the parse result for a specific symbol. |
| `GetValueForOption<T>(Option<T>)` | Retrieve the typed value of an option. |
| `GetValueForArgument<T>(Argument<T>)` | Retrieve the typed value of an argument. |

---

### `GlobalOption`

Represents a cross-cutting option processed before sub-command routing. Not passed through to `System.CommandLine`; handled directly by `CommandParser.Parse`.

| Property | Description |
|---|---|
| `Name` | Primary name (without prefix). |
| `Description` | Help text. |
| `ExpectsValue` | Whether the option consumes the next argument as its value. |
| `AllAliases` | Full set of auto-generated and custom aliases (populated by `RootCommandBuilder.Build()`). |
| `CustomAliases` | Developer-supplied aliases passed in the constructor. |
| `Matches(string arg)` | Returns `true` if `arg` is any of the option's aliases (case-insensitive). |

---

### `ConsoleSpinner`

Thread-safe animated spinner that renders at a fixed or dynamic console position. Implements `IDisposable`.

| Constructor overloads | Description |
|---|---|
| `new ConsoleSpinner()` | Default style at current cursor position. |
| `new ConsoleSpinner(string message)` | Display a message beside the spinner. |
| `new ConsoleSpinner(string message, SpinnerStyle style)` | Specific animation style. |
| `new ConsoleSpinner(string message, SpinnerStyle style, ConsoleColor color)` | With custom foreground color. |
| `new ConsoleSpinner(int positionX, int positionY, ...)` | Fixed console position. Pass `-1` for dynamic. |

Properties: `Message`, `Color`, `PositionX`, `PositionY`, `Delay`, `IsRunning`.

Methods: `Start()`, `Stop()`, `Dispose()`.

If Unicode is unavailable in the console, falls back to `SpinnerStyle.ASCII` automatically.

**Usage:**

```csharp
using (var spinner = new ConsoleSpinner("Processing...", SpinnerStyle.Braille, ConsoleColor.Cyan))
{
    DoLongWork();
} // Stop() called automatically
```

---

### `SpinnerStyle` (enum)

| Value | Characters |
|---|---|
| `ASCII` / `Default` | `\|/-\\` |
| `Braille` / `Dots` | `⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏` |
| `Circles` | `◐◓◑◒` |
| `Arrows` | `←↖↑↗→↘↓↙` |
| `SimpleStars` | `✶✸✹✺✹✷` |
| `UnicodeStars` | `✶✸✹✺✹✷❋❂❉✼✽✾` |

---

### `ProgressSpinner`

An `IDisposable` spinner that delegates rendering to `ConsoleUtil.DrawProgressBar` with an indefinite progress value (`-1`). Designed for `using` block usage.

| Member | Description |
|---|---|
| `Message` | Get or set the displayed message while running. |
| `UpdateMessage(string)` | Convenience wrapper for setting `Message`. |
| `Stop()` | Stop the spinner and clear the progress bar line. |
| `Dispose()` | Calls `Stop()`. |

```csharp
using (var spinner = new ProgressSpinner("Connecting to API..."))
{
    var result = await apiClient.FetchDataAsync();
    spinner.UpdateMessage("Parsing response...");
    Process(result);
}
```

---

## Attributes

See [`Attributes/readme.md`](Attributes/readme.md) for `[Command]` and `[Option]`.

---

## Command Discovery Pattern

```csharp
// Group: class-level [Command] + method-level [Command]
[Command("files", "File operations")]
public class FileCommands
{
    [Command("list", "List files in a directory")]
    public void List(
        [Option("Path to list")] string path,
        [Option("Include hidden files")] bool? hidden)
    {
        // implementation
    }

    [Command("copy", "Copy a file")]
    public void Copy(
        [Option("Source path", "--src")] string source,
        [Option("Destination path", "--dst")] string destination)
    {
        // implementation
    }
}

// Top-level: static method with [Command]
public static class Utilities
{
    [Command("version", "Show tool version")]
    public static void ShowVersion()
    {
        Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
    }
}
```

The above produces:
- `files list --path <value> [--hidden]`
- `files copy --source <value> --destination <value>`
- `version`

---

## File Organisation

| File | Contents |
|---|---|
| `CommandParser.cs` | `CommandParser` class |
| `CommandBuilder.cs` | `CommandBuilder` static class, `NameTracker` (private) |
| `RootCommandBuilder.cs` | `RootCommandBuilder` fluent builder |
| `GlobalOption.cs` | `GlobalOption` class |
| `ParseResult.cs` | `ParseResult` wrapper |
| `ConsoleSpinner.cs` | `ConsoleSpinner` class, `SpinnerStyle` enum |
| `ProgressSpinner.cs` | `ProgressSpinner` class |
| `Attributes/` | `[Command]` and `[Option]` attributes |

---

## Related Modules

| Module | Description |
|---|---|
| **[Configuration](../Configuration/readme.md)** | INI-based configuration |
| **[Core](../Core/readme.md)** | WinSCP resource management |
| **[Logging](../Logging/readme.md)** | Structured logging (used internally for warnings) |
