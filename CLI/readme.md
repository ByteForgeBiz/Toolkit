# CommandLine.md

## ByteForge.Toolkit.CommandLine

A flexible and extensible command-line parsing toolkit built on top of System.CommandLine. It enables declarative command definition using attributes and supports advanced CLI capabilities.

### 🚀 Features
- Attribute-based command and option definitions
- Nested commands and command groups
- Auto-alias generation and typo correction
- Built-in help documentation
- Plugin discovery system
- Parse error handling and cancellation

### 🧱 Core Components
- `CommandBuilder`: Parses assemblies and builds command trees
- `RootCommandBuilder`: Fluent configuration for CLI applications
- `CommandAttribute` & `OptionAttribute`: Define commands and options
- `ConsoleSpinner`: Thread-safe console spinner animation with Unicode support

### 🧪 Example
```csharp
[Command("Creates a new project", "create", "new")]
public class CreateCommand
{
    [Command("Creates a new console project", "console")]
    public void CreateConsole(
        [Option("The name of the project")] string name,
        [Option("The output directory")] string output = null)
    {
        // Implementation
    }
}
```

### 🧭 Usage
```csharp
var parser = new RootCommandBuilder("MyApp")
    .AddAssembly(typeof(Program).Assembly)
    .SearchPlugins()
    .UseHelp()
    .UseTypoCorrections()
    .Build();

return await parser.InvokeAsync(args);
```

### 🎨 Console Spinner
```csharp
// Create a spinner at current cursor position
using (var spinner = new ConsoleSpinner())
{
    // Long-running operation
    await SomeAsyncOperation();
}

// Custom positioned spinner with color
using (var spinner = new ConsoleSpinner(10, 5, SpinnerStyle.Unicode, 100))
{
    spinner.Color = ConsoleColor.Green;
    // Processing with visual feedback
}
```

### ✅ Best Practices
- Keep commands organized by logical groups
- Use descriptive aliases
- Always provide help text
- Use exception handling within command bodies
- Use `ConsoleSpinner` for long-running operations to provide visual feedback
- Always dispose spinners properly using `using` statements

---

## 📚 Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [CommandLine](../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |
| [Zip](../Zip/readme.md) | Advanced ZIP library with multi-part archives, self-extracting executables, and AES encryption |
