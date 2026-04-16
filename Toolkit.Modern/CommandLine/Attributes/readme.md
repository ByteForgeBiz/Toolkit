# CommandLine / Attributes

This folder contains the two attributes that drive automatic command-line parsing in the CommandLine module.

---

## Attributes

### `CommandAttribute`

**Target:** `Class` or `Method`

Marks a class as a command group, or a method as a sub-command within a group (or as a top-level command when applied to a static method).

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | The token used on the command line to invoke this command. |
| `Description` | `string` | Help text shown by `--help`. |
| `Aliases` | `string[]` | Optional alternative tokens for the same command. |

```csharp
// Command group (class-level)
[Command("db", "Database operations", "database")]
public class DbCommands
{
    // Sub-command (method-level)
    [Command("ping", "Test database connectivity")]
    public void Ping([Option("Server name")] string server) { ... }
}

// Top-level command (static method)
[Command("version", "Show version")]
public static void ShowVersion() { ... }
```

---

### `OptionAttribute`

**Target:** `Parameter`

Decorates a method parameter to customise its name, description, and aliases as a command-line option. When absent, the parameter name and type are used with auto-generated aliases.

| Property | Type | Description |
|---|---|---|
| `Description` | `string` | Help text for the option (required). |
| `Name` | `string?` | Override the option name (without dashes). Uses parameter name if not set. |
| `Aliases` | `string[]` | Additional aliases. Combined with the auto-generated short forms. |

Auto-generated aliases follow this pattern for a parameter or option named `filename`:
- `/f`, `-f`, `--f`
- `/fil`, `-fil`, `--fil`
- `--filename`

```csharp
[Command("import", "Import data")]
public void Import(
    // Minimal: description only; name comes from parameter
    [Option("Path to the input file")] string inputFile,

    // Override name and add extra alias
    [Option("Output directory", Name = "out-dir", "--output")] string outputDirectory,

    // Nullable makes the option non-required automatically
    [Option("Batch size")] int? batchSize)
{ ... }
```

---

## Notes

- Both attributes live in the `ByteForge.Toolkit.CommandLine` namespace.
- `CommandBuilder` processes these attributes via reflection at startup; they have no runtime overhead at parse time.
- `[Option]` is only meaningful on method parameters processed by `CommandBuilder`. It has no effect on class properties.
