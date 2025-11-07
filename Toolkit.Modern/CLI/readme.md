# CLI (Command-Line Interface) Module

## Overview

The **CLI** module provides attribute-based command-line argument parsing and execution. It enables developers to define command structures declaratively using attributes, with automatic help generation and type-safe argument handling.

---

## Purpose & Design Philosophy

### Why This Module?

Command-line applications need:
1. **Easy parsing** - Parse arguments without manual string handling
2. **Type safety** - Convert arguments to target types automatically
3. **Help generation** - Display usage without manual work
4. **Validation** - Enforce required parameters
5. **Subcommands** - Support hierarchical command structures

The CLI module provides:
- **Attribute-based definitions** - `[Command]` and `[Option]` attributes
- **Automatic parsing** - Extract and type-convert arguments
- **Help text** - Generated from attributes
- **Validation** - Required parameters, type checking
- **Subcommand support** - Nested command hierarchies

### Architecture

```
Command-line args: ["upload", "--file", "data.csv", "--destination", "\\server\share"]
   ↓
[Parse with CommandParser]
   ├─→ Identify command: "upload"
   ├─→ Extract options: file="data.csv", destination="\\server\share"
   ├─→ Validate required parameters
   ↓
[Map to command class attributes]
   ├─→ [Command("upload")] class: UploadCommand
   ├─→ [Option] properties: File, Destination
   ↓
[Invoke command with parameters]
   ↓
Return ParseResult with results
```

---

## Key Classes

### `CommandParser`
**Purpose:** Main parser for command-line arguments.

**Responsibilities:**
- Parse argument arrays into structured commands
- Extract and type-convert options
- Validate arguments
- Generate parse results

**Key Methods:**
```csharp
// Parse arguments
ParseResult Parse(string[] args);
ParseResult Parse(params string[] args);

// Access results
ParseResult.Command  // Parsed command
ParseResult.Options  // Parsed options
ParseResult.RawArgs  // Unparsed arguments
```

**Usage:**
```csharp
var parser = new CommandParser();
var result = parser.Parse(args);

if (result.Command != null)
{
switch (result.Command.Name)
    {
        case "upload":
            HandleUpload(result);
   break;
        case "download":
        HandleDownload(result);
  break;
    }
}
```

### `CommandBuilder` / `RootCommandBuilder`
**Purpose:** Fluent API for building command definitions.

**Features:**
- Define command structure
- Add subcommands
- Set descriptions
- Configure options

**Usage:**
```csharp
var rootBuilder = new RootCommandBuilder();

var uploadCmd = rootBuilder.AddCommand("upload", "Upload files to server")
    .AddOption("file", "f", "File path", required: true)
    .AddOption("destination", "d", "Destination path", required: true);

var deleteCmd = rootBuilder.AddCommand("delete", "Delete files from server")
  .AddOption("path", "p", "File path", required: true)
    .AddOption("force", "Force deletion without confirmation");

var root = rootBuilder.Build();
```

### `[CommandAttribute]`
**Purpose:** Marks a class as a CLI command.

**Targets:** Classes

**Parameters:**
- `CommandName` (string) - Name used to invoke command
- `Description` (string) - Help text

**Usage:**
```csharp
[Command("upload", "Upload files to remote server")]
public class UploadCommand
{
    [Option("file", "f")]
    public string FilePath { get; set; }
    
    [Option("destination", "d")]
    public string DestinationPath { get; set; }
    
    public void Execute()
    {
        // Upload logic
    }
}

[Command("delete", "Delete files from remote server")]
public class DeleteCommand
{
    [Option("path", "p")]
    public string Path { get; set; }
    
    [Option("force", "Skip confirmation")]
    public bool Force { get; set; }
    
    public void Execute()
    {
    // Delete logic
    }
}
```

### `[OptionAttribute]`
**Purpose:** Marks a property as a command-line option.

**Targets:** Properties

**Parameters:**
- `LongName` (string) - Long form (e.g., `--file`)
- `ShortName` (string, optional) - Short form (e.g., `-f`)
- `Description` (string, optional) - Help text
- `Required` (bool) - Whether parameter is required
- `DefaultValue` (object, optional) - Value if not provided

**Usage:**
```csharp
[Command("process")]
public class ProcessCommand
{
    // Required option
    [Option("input", "i", "Input file path", required: true)]
    public string InputFile { get; set; }
    
    // Optional with default
 [Option("output", "o", "Output file path", defaultValue: "output.txt")]
    public string OutputFile { get; set; }
    
    // Boolean flag
    [Option("verbose", "v", "Enable verbose output")]
    public bool Verbose { get; set; }
    
// With type conversion
    [Option("threads", "t", "Number of threads", defaultValue: 4)]
    public int ThreadCount { get; set; }
}
```

### `GlobalOption`
**Purpose:** Defines options available to all commands.

**Usage:**
```csharp
public class GlobalOption
{
    [Option("help", "h", "Show help")]
    public bool Help { get; set; }
    
    [Option("version", "v", "Show version")]
    public bool Version { get; set; }
    
    [Option("config", "c", "Configuration file path")]
    public string ConfigFile { get; set; }
}
```

### `ParseResult`
**Purpose:** Result of parsing command-line arguments.

**Properties:**
```csharp
// Parsed command
Command ParsedCommand { get; }

// Parsed options (Dictionary<string, object>)
IDictionary<string, object> Options { get; }

// Unparsed remaining arguments
string[] RemainingArgs { get; }

// Whether parsing succeeded
bool IsValid { get; }

// Errors
IList<string> Errors { get; }
```

**Usage:**
```csharp
var result = parser.Parse(args);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
Console.WriteLine($"Error: {error}");
    return;
}

// Access options
string file = (string)result.Options["file"];
int threads = (int)result.Options.GetValueOrDefault("threads", 4);
```

### `ConsoleSpinner` / `ProgressSpinner`
**Purpose:** Display animated progress indicators.

**Usage:**
```csharp
// Simple spinner
var spinner = new ConsoleSpinner("Processing");
spinner.Start();

DoLongOperation();

spinner.Stop();

// Progress spinner with percentage
var progress = new ProgressSpinner();
for (int i = 0; i <= 100; i++)
{
    progress.Update(i, $"Processing {i}%");
    System.Threading.Thread.Sleep(50);
}
progress.Complete();
```

---

## Usage Patterns

### Basic Command Definition

```csharp
[Command("greet", "Greet a person")]
public class GreetCommand
{
    [Option("name", "n", "Person's name", required: true)]
    public string Name { get; set; }
    
    [Option("greeting", "g", "Custom greeting", defaultValue: "Hello")]
    public string Greeting { get; set; }
    
    public void Execute()
    {
        Console.WriteLine($"{Greeting}, {Name}!");
    }
}

// Usage:
// app.exe greet --name John
// app.exe greet -n John -g "Hi"
// app.exe greet --name "Jane Doe" --greeting "Welcome"
```

### Multiple Subcommands

```csharp
[Command("files", "File operations")]
public class FilesCommand
{
    [Option("operation", "op", "Operation: list, copy, move, delete", required: true)]
    public string Operation { get; set; }
    
    [Option("source", "s", "Source file")]
    public string SourceFile { get; set; }
    
    [Option("destination", "d", "Destination")]
    public string Destination { get; set; }
    
    public void Execute()
    {
   switch (Operation.ToLower())
  {
            case "list":
      ListFiles();
      break;
        case "copy":
                CopyFile(SourceFile, Destination);
 break;
            case "move":
 MoveFile(SourceFile, Destination);
          break;
       case "delete":
            DeleteFile(SourceFile);
    break;
        }
}
}
```

### Type Conversion

```csharp
[Command("import", "Import data from file")]
public class ImportCommand
{
    [Option("file", "f", "File path", required: true)]
    public string FilePath { get; set; }
    
    [Option("format", "Format: csv, json, xml", defaultValue: "csv")]
    public string Format { get; set; }
    
    [Option("batch-size", "Records per batch", defaultValue: 1000)]
    public int BatchSize { get; set; }
    
    [Option("skip-errors", "Continue on errors")]
    public bool SkipErrors { get; set; }
    
    [Option("timeout", "Operation timeout (seconds)", defaultValue: 300)]
    public int TimeoutSeconds { get; set; }
    
    public void Execute()
    {
        // BatchSize is int, parsed from "1000"
        // TimeoutSeconds is int, parsed from "300"
        // SkipErrors is bool, true if flag provided
    }
}
```

### Argument Validation

```csharp
[Command("connect", "Connect to database")]
public class ConnectCommand
{
    [Option("server", "s", "Server address", required: true)]
    public string Server { get; set; }
    
    [Option("port", "p", "Port number", defaultValue: 1433)]
    public int Port { get; set; }
    
  [Option("database", "d", "Database name", required: true)]
    public string Database { get; set; }
    
    [Option("username", "u", "Username")]
    public string Username { get; set; }
    
    [Option("password", "pwd", "Password")]
    public string Password { get; set; }
    
    public void Execute()
{
        // Validate
    if (Port < 1 || Port > 65535)
            throw new ArgumentException("Port must be 1-65535");
        
        if (string.IsNullOrEmpty(Username))
            Console.WriteLine("Warning: No username provided");
        
      // Connect
        ConnectToDatabase();
    }
}
```

---

## Main Program Integration

```csharp
class Program
{
    static int Main(string[] args)
    {
        try
        {
     // Parse arguments
     var parser = new CommandParser();
            var result = parser.Parse(args);
   
            // Handle global options
       if (HasGlobalOption(result, "help"))
  {
    DisplayHelp();
             return 0;
        }

   if (HasGlobalOption(result, "version"))
            {
                Console.WriteLine($"Version {GetVersion()}");
         return 0;
 }
  
            // Get command from attributes
        var commands = LoadCommandsFromAttributes();
         
            var command = FindCommand(commands, result.ParsedCommand?.Name);
       if (command == null)
  {
   Console.WriteLine($"Unknown command: {result.ParsedCommand?.Name}");
       DisplayHelp();
        return 1;
  }
            
            // Set command options from parse result
            SetCommandProperties(command, result.Options);

            // Execute command
     command.Execute();
            
            return 0;
        }
        catch (Exception ex)
        {
   Console.WriteLine($"Error: {ex.Message}");
            return 1;
      }
    }
    
    private static void DisplayHelp()
    {
        Console.WriteLine("Usage: app.exe <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
    // List available commands
 }
}
```

---

## Advanced Patterns

### Dynamic Command Loading

```csharp
public static IList<object> LoadCommandsFromAssembly()
{
    var commands = new List<object>();
    
    foreach (var type in typeof(Program).Assembly.GetTypes())
  {
        var attr = type.GetCustomAttribute<CommandAttribute>();
        if (attr != null)
        {
    commands.Add(Activator.CreateInstance(type));
     }
    }
    
    return commands;
}
```

### Dependency Injection

```csharp
[Command("process", "Process data")]
public class ProcessCommand
{
    private readonly IDataProcessor _processor;
    private readonly ILogger _logger;
    
    public ProcessCommand(IDataProcessor processor, ILogger logger)
    {
     _processor = processor;
        _logger = logger;
    }
    
    [Option("input", "i", required: true)]
    public string InputFile { get; set; }
    
    public void Execute()
    {
  _logger.Info($"Processing {InputFile}");
        _processor.Process(InputFile);
    }
}

// Usage
var processor = container.Resolve<IDataProcessor>();
var logger = container.Resolve<ILogger>();
var command = new ProcessCommand(processor, logger);
```

### Custom Option Parsing

```csharp
// Extend to support environment variable fallback
[Option("api-key", "key", "API key", required: true)]
public string ApiKey { get; set; } = 
    Environment.GetEnvironmentVariable("APP_API_KEY") ?? "";

// Or custom parsing
[Option("credentials", "c", "user:password format")]
public (string user, string password) Credentials
{
    get
    {
      var parts = _credentials.Split(':');
        return (parts[0], parts[1]);
    }
    set
    {
        _credentials = $"{value.user}:{value.password}";
    }
}
private string _credentials;
```

---

## File Organization

### `CommandParser.cs`
Main argument parser.

### `CommandBuilder.cs` / `RootCommandBuilder.cs`
Fluent builders for command definitions.

### `Attributes/`
- `CommandAttribute.cs`
- `OptionAttribute.cs`

### `GlobalOption.cs`
Global command options.

### `ParseResult.cs`
Parsing results.

### `ConsoleSpinner.cs` / `ProgressSpinner.cs`
Progress indicators.

---

## Best Practices

### 1. **Clear Command Names**
```csharp
// Good
[Command("upload-file", "Upload file to server")]
[Command("list-files", "List files on server")]
[Command("delete-file", "Delete file from server")]

// Bad
[Command("cmd1")]
[Command("cmd2")]
[Command("cmd3")]
```

### 2. **Descriptive Options**
```csharp
// Good
[Option("input-file", "i", "Path to input CSV file", required: true)]
public string InputFile { get; set; }

// Bad
[Option("f")]
public string File { get; set; }
```

### 3. **Sensible Defaults**
```csharp
// Good
[Option("threads", "Number of processing threads", defaultValue: Environment.ProcessorCount)]
public int ThreadCount { get; set; }

// Bad
[Option("threads", required: true)]
public int ThreadCount { get; set; }
```

### 4. **Type Safety**
```csharp
// Good
[Option("port", "p", defaultValue: 8080)]
public int Port { get; set; }

// Avoid
[Option("port", "p")]
public string Port { get; set; }  // Force parsing from string
```

---

## Error Handling

```csharp
var result = parser.Parse(args);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
        Console.WriteLine($"Error: {error}");
    
  DisplayUsage();
    return 1;
}

try
{
    var command = GetCommand(result.ParsedCommand.Name);
    command.Execute();
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled");
    return 130;
}
catch (Exception ex)
{
 Console.WriteLine($"Unexpected error: {ex.Message}");
    return 1;
}
```

---

## Testing

```csharp
[TestMethod]
public void Parser_ParseUploadCommand_WithRequiredOptions()
{
    var parser = new CommandParser();
    var result = parser.Parse("upload", "--file", "data.csv", "--destination", "\\server\share");
    
    Assert.IsTrue(result.IsValid);
    Assert.AreEqual("upload", result.ParsedCommand.Name);
    Assert.AreEqual("data.csv", result.Options["file"]);
    Assert.AreEqual("\\server\share", result.Options["destination"]);
}

[TestMethod]
public void Parser_MissingRequiredOption_ReturnsError()
{
    var parser = new CommandParser();
    var result = parser.Parse("upload", "--file", "data.csv");
    
    Assert.IsFalse(result.IsValid);
    Assert.IsTrue(result.Errors.Any(e => e.Contains("destination")));
}
```

---

## Summary

The CLI module provides:

**Key Strengths:**
- ✓ Attribute-based command definitions
- ✓ Automatic type conversion
- ✓ Fluent API for building
- ✓ Help text generation
- ✓ Validation of required parameters
- ✓ Support for subcommands

**Best For:**
- Console applications
- Command-line tools
- Administrative utilities
- Batch processing scripts

**Not Ideal For:**
- Complex interactive CLIs (consider Spectre.Console)
- Real-time user interfaces
- Web applications