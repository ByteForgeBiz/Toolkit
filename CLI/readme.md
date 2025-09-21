# CLI Module

## ByteForge.Toolkit.CommandLine

A sophisticated command-line interface (CLI) parsing framework built on System.CommandLine. It provides a declarative, attribute-driven approach to building robust command-line applications with advanced features like auto-aliasing, typo correction, and plugin discovery.

### 🚀 Key Features

- **Attribute-Based Commands**: Define commands and options using declarative attributes
- **Nested Command Structures**: Support for hierarchical command organization and grouping
- **Intelligent Auto-Aliasing**: Automatic alias generation with conflict resolution
- **Typo Correction**: Built-in suggestions for mistyped commands and options
- **Plugin Discovery System**: Dynamic loading and discovery of command plugins
- **Comprehensive Help System**: Auto-generated help documentation with formatting
- **Advanced Progress Indicators**: Thread-safe console spinners with Unicode support
- **Parse Error Handling**: Robust error handling with user-friendly messages
- **Cancellation Support**: Built-in support for operation cancellation

### 🧱 Core Components

#### CommandBuilder
- **Assembly Scanning**: Automatically discovers command classes from assemblies
- **Reflection-Based**: Uses reflection to build command trees from attributed methods
- **Type Safety**: Validates parameter types and ensures compile-time safety
- **Token Management**: Provides token management for complex parsing scenarios

#### RootCommandBuilder
- **Fluent Configuration**: Chainable API for CLI application setup
- **Feature Integration**: Seamlessly integrates help, typo correction, and plugins
- **Customization**: Extensive customization options for CLI behavior
- **Build Pipeline**: Structured build process for command hierarchies

#### Attribute System
- **CommandAttribute**: Defines commands with descriptions and aliases
- **OptionAttribute**: Configures command options with validation rules
- **Parameter Binding**: Automatic parameter binding with type conversion
- **Validation Support**: Built-in validation for common parameter types

#### ConsoleSpinner
- **Thread-Safe Animation**: Multi-threaded spinner with position control
- **Unicode Support**: Rich Unicode characters with ASCII fallback
- **Color Customization**: Full color support with console compatibility checks
- **Position Management**: Precise cursor positioning and restoration
- **Performance Optimized**: Minimal CPU overhead during animation

### 📋 Supported Parameter Types

The CLI system supports automatic type conversion for:
- **Primitives**: `string`, `int`, `long`, `float`, `double`, `decimal`, `bool`
- **Date/Time**: `DateTime` with flexible parsing
- **Identifiers**: `Guid` with validation
- **Collections**: Arrays and lists of supported types
- **Nullable Types**: All nullable variants of supported types

### 🧪 Implementation Examples

#### Basic Command Definition
```csharp
[Command("Creates a new project", "create", "new")]
public class CreateCommand
{
    [Command("Creates a new console project", "console")]
    public void CreateConsole(
        [Option("The name of the project")] string name,
        [Option("The output directory")] string output = null,
        [Option("Include unit tests")] bool includeTests = false)
    {
        Console.WriteLine($"Creating console project: {name}");
        if (!string.IsNullOrEmpty(output))
            Console.WriteLine($"Output directory: {output}");
        if (includeTests)
            Console.WriteLine("Including unit test project");
    }

    [Command("Creates a new web API project", "api", "web")]
    public void CreateWebApi(
        [Option("The name of the API project")] string name,
        [Option("API version")] string version = "v1")
    {
        Console.WriteLine($"Creating Web API project: {name} ({version})");
    }
}
```

#### Advanced CLI Application Setup
```csharp
class Program
{
    static async Task<int> Main(string[] args)
    {
        var parser = new RootCommandBuilder("MyTool")
            .WithDescription("A comprehensive development toolkit")
            .AddAssembly(typeof(Program).Assembly)
            .SearchPlugins("plugins/*.dll")
            .UseHelp()
            .UseTypoCorrections()
            .UseEnvironmentVariables()
            .ConfigureLogging(LogLevel.Info)
            .Build();

        return await parser.InvokeAsync(args);
    }
}
```

#### Console Spinner with Progress Indication
```csharp
public async Task ProcessLargeDataset()
{
    Console.WriteLine("Processing dataset...");
    
    // Spinner at current cursor position
    using (var spinner = new ConsoleSpinner())
    {
        spinner.Color = ConsoleColor.Green;
        await ProcessDataAsync();
    }
    
    Console.WriteLine(" ✓ Dataset processing complete!");
}

// Advanced positioned spinner
public async Task BackupDatabase()
{
    Console.WriteLine("Database backup in progress:");
    
    // Spinner at specific position with custom timing
    using (var spinner = new ConsoleSpinner(left: 2, top: Console.CursorTop, 
           style: SpinnerStyle.Unicode, delay: 80))
    {
        spinner.Color = ConsoleColor.Cyan;
        await CreateDatabaseBackupAsync();
    }
    
    Console.WriteLine("\n✓ Backup completed successfully!");
}
```

#### Plugin-Based Command System
```csharp
// In a separate plugin assembly
[Command("Database operations", "db")]
public class DatabaseCommands
{
    [Command("Migrate database", "migrate")]
    public async Task Migrate(
        [Option("Connection string")] string connectionString,
        [Option("Target version")] int? targetVersion = null)
    {
        using var spinner = new ConsoleSpinner();
        spinner.Color = ConsoleColor.Yellow;
        
        await RunMigrationsAsync(connectionString, targetVersion);
    }
}
```

### 🧭 Usage Patterns

#### Simple CLI Application
```csharp
var parser = new RootCommandBuilder("calculator")
    .AddAssembly(typeof(CalculatorCommands).Assembly)
    .UseHelp()
    .Build();

return await parser.InvokeAsync(args);
```

#### Enterprise CLI with Full Features
```csharp
var parser = new RootCommandBuilder("enterprise-tool")
    .WithDescription("Enterprise development and deployment toolkit")
    .WithVersion("2.1.0")
    .AddAssembly(typeof(CoreCommands).Assembly)
    .AddAssembly(typeof(DeploymentCommands).Assembly)
    .SearchPlugins("plugins/")
    .UseHelp()
    .UseTypoCorrections()
    .UseEnvironmentVariables("ETOOL_")
    .ConfigureGlobalOptions(builder =>
    {
        builder.AddVerbosityOption();
        builder.AddConfigFileOption();
    })
    .HandleExceptions(ex => LogException(ex))
    .Build();

return await parser.InvokeAsync(args);
```

### ✅ Best Practices

#### Command Organization
- **Logical Grouping**: Organize commands by functional domains
- **Consistent Naming**: Use clear, consistent naming conventions
- **Alias Strategy**: Provide meaningful aliases for frequently used commands
- **Help Documentation**: Always include comprehensive help text

#### Performance Considerations
- **Lazy Loading**: Commands are loaded on-demand for better startup performance
- **Resource Management**: Properly dispose of spinners and other resources
- **Memory Efficiency**: Minimize memory footprint in command implementations
- **Async Operations**: Use async/await for I/O-bound operations

#### Error Handling
- **Graceful Degradation**: Handle missing dependencies gracefully
- **User-Friendly Messages**: Provide clear, actionable error messages
- **Validation**: Validate inputs early and provide specific feedback
- **Exception Handling**: Wrap exceptions in meaningful context

#### Visual Feedback
- **Progress Indication**: Use spinners for operations taking >500ms
- **Color Coding**: Use colors consistently to indicate status
- **Position Management**: Manage cursor position carefully with spinners
- **Cleanup**: Always dispose spinners properly using `using` statements

### 🔧 Configuration Options

#### Spinner Styles
- **ASCII**: `|/-\\` (universal compatibility)
- **Unicode**: `⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏` (modern terminals)
- **Custom**: Define your own character sequences

#### Command Parsing Options
- **Case Sensitivity**: Configure case-sensitive or case-insensitive parsing
- **Delimiter Options**: Customize option delimiters and separators
- **Response Files**: Support for response file arguments
- **Environment Variables**: Automatic environment variable binding

#### Help System Configuration
- **Custom Templates**: Define custom help templates
- **Section Ordering**: Control help section display order
- **Example Integration**: Include usage examples in help output
- **Localization**: Support for localized help text

### 📊 Performance Characteristics

- **Startup Time**: Optimized for fast CLI application startup
- **Memory Usage**: Minimal memory footprint with efficient object pooling
- **Animation Performance**: Spinner animations use <1% CPU
- **Reflection Overhead**: Command discovery happens once at startup
- **Thread Safety**: All components are thread-safe for concurrent usage

---

## 📚 Related Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [Configuration](../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |