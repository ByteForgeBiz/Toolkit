# ByteForge.Toolkit.CommandLine

A flexible and extensible command-line parsing toolkit for .NET 4.8 applications, built on top of System.CommandLine. This toolkit provides a declarative approach to defining commands and options using attributes, making it easy to create sophisticated command-line interfaces.

## Features

- Attribute-based command and option definitions
- Support for command groups and subcommands
- Automatic alias generation for commands and options
- Plugin system for extending functionality
- Built-in support for help documentation
- Environment variable integration
- Typo correction and suggestions
- Parse error reporting
- Exception handling
- Process termination handling

## Core Components

### CommandBuilder

The `CommandBuilder` class is responsible for creating commands from assemblies. It scans assemblies for types and methods marked with the `CommandAttribute` and builds a command hierarchy accordingly.

Key features:
- Supports a wide range of parameter types including primitives, strings, and their nullable variants
- Automatic validation of method parameters
- Intelligent alias generation
- Name conflict resolution
- Built-in duplicate detection

### RootCommandBuilder

A fluent builder class for creating command-line parsers. It provides a clean API for configuring various aspects of the command-line interface.

Available configurations:
- Help system (`UseHelp`)
- Environment variables (`UseEnvironmentVariables`)
- Parse directive (`UseParseDirective`)
- Suggestion system (`UseSuggestDirective`)
- Typo corrections (`UseTypoCorrections`)
- Error reporting (`UseParseErrorReporting`)
- Exception handling (`UseExceptionHandler`)
- Process termination handling (`UseCancellation`)

### Attributes

#### CommandAttribute

Used to mark classes or methods as commands. Applied to:
- Classes: Defines a command group
- Methods: Defines individual commands within a group

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

#### OptionAttribute

Used to define command parameters with custom names, descriptions, and aliases.

```csharp
public void ProcessFile(
    [Option("Input file path", "input", "i", "in")] string inputPath,
    [Option("Output format", "format", "f")] string format = "json")
{
    // Implementation
}
```

## Usage Example

```csharp
var parser = new RootCommandBuilder("My CLI Application")
    .AddAssembly(typeof(Program).Assembly)
    .SearchPlugins()
    .UseHelp()
    .UseEnvironmentVariables()
    .UseTypoCorrections()
    .Build();

return await parser.InvokeAsync(args);
```

## Plugin System

The toolkit includes a plugin system that allows you to extend functionality by loading additional command assemblies from a plugins directory:

```csharp
builder.SearchPlugins(); // Searches in default plugins directory
// or
builder.SearchPlugins("path/to/plugins");
```

## Parameter Type Support

Supported parameter types:
- `string`
- `int`
- `long`
- `float`
- `double`
- `decimal`
- `bool`
- `DateTime`
- `Guid`
- Nullable variants of all value types

## Best Practices

1. Keep command groups focused and logically organized
2. Provide clear, concise descriptions for commands and options
3. Use meaningful aliases that are easy to remember
4. Handle exceptions appropriately in command implementations
5. Include help text for all commands and options
6. Test commands with various input combinations
7. Validate user input thoroughly

## Requirements

- .NET Framework 4.8
- System.CommandLine NuGet package
