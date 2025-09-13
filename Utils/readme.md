# Utils.md

## ByteForge Toolkit Utility Modules

A collection of versatile helper classes that streamline string manipulation, IO operations, parsing, logging, and diagnostics in .NET Framework 4.8 applications.

### 🚀 Features
- Console progress bar rendering
- Flexible date and time parsing across cultures and formats
- File path resolution with UNC support
- Synchronous wrapper for async methods
- Enum extension methods for `DescriptionAttribute`
- Rich type parser supporting .NET primitives and custom types
- Advanced boolean parser supporting domain-specific values
- Email formatting helpers, string cleansing, and general utilities

### 🧱 Core Components
- `ConsoleUtil`: Console progress bars with width detection and dynamic sizing
- `DateTimeParser`: Intelligent format-inferred timestamp parser with culture support
- `IOUtils`: Network-aware universal path resolution (UNC/local conversion)
- `TimingUtil`: Execution time measurement and performance logging
- `Utils`: Phone formatting, string filters, async-to-sync wrappers, general utilities
- `Parser`: Flexible type conversion from string to .NET types
- `BooleanParser`: Extended true/false parsing with custom domain values
- `EnumExtensions`: Access enum descriptions via `DescriptionAttribute`
- `TemplateProcessor`: Placeholder replacement in text templates with escape sequences
- `ValueConverterRegistry`: Custom value converter registration for database operations

### 🧪 Examples
#### Progress Bar
```csharp
ConsoleUtil.DrawProgressBar(75, "Uploading file...");
```

#### DateTime Parsing
```csharp
var dt = DateTimeParser.Parse("2024-02-11 15:30:00 EST");
```

#### Run Async Sync
```csharp
string result = Utils.RunSync(async token => await SomeAsyncCall(token));
```

#### Universal Path
```csharp
string uncPath = IOUtils.GetUniversalPath("Z:\Shared\Project.docx");
```

#### Enum Description
```csharp
string label = MyEnum.ValueX.GetDescription();
```

#### Boolean Parsing
```csharp
bool flag = BooleanParser.Parse("enabled");
BooleanParser.RegisterFalseValue("nah");
```

#### General Utility
```csharp
string formatted = Utils.FormatUSPhoneNumber("+1 (202) 555-0183");
string clean = "dirty value".Remove(" ");
```

#### Template Processing
```csharp
var processor = new TemplateProcessor();
processor["Name"] = "John Doe";
processor["Department"] = "Engineering";
processor["Date"] = DateTime.Now.ToString("yyyy-MM-dd");

string template = "Hello <Name>, welcome to <Department>. Today is <Date>.";
string result = processor.Process(template);
// Result: "Hello John Doe, welcome to Engineering. Today is 2024-01-15."

// With escape sequences
processor.UseEscapeSequences = true;
processor["Newline"] = "\\n";
string template2 = "Line 1<Newline>Line 2";
string result2 = processor.Process(template2);
// Result: "Line 1\nLine 2" (with actual newline)
```

#### Value Converter Registry
```csharp
// Register custom converter for database operations
ValueConverterRegistry.RegisterConverter("UpperCase", value => value?.ToString()?.ToUpper());

// Register converter for special formatting
ValueConverterRegistry.RegisterConverter("CurrencyFormat", value => 
{
    if (decimal.TryParse(value?.ToString(), out decimal amount))
        return amount.ToString("C");
    return value;
});

// Use in database column attributes
// [DBColumn("name", ValueConverter = "UpperCase")]
// public string Name { get; set; }
```

### ✅ Best Practices

#### Parsing and Conversion
- Register custom boolean/enum string values for localization support
- Use `TimingUtil` for performance instrumentation and optimization
- Clean user inputs with `NullIfEmpty` or `.Remove()` extension helpers
- Extend `Parser` class with domain-specific converters when needed

#### Template Processing
- Use meaningful placeholder names in templates (`<UserName>` not `<x>`)
- Validate template keys don't contain `<` or `>` characters
- Consider escape sequence usage for complex formatting requirements
- Cache `TemplateProcessor` instances for frequently used templates

#### Value Converters
- Register converters early in application lifecycle
- Use descriptive converter names that indicate their function
- Handle null values gracefully in converter functions
- Test converters with edge cases and invalid inputs

#### Performance Considerations
- Use `ConsoleUtil.DrawProgressBar` for long-running operations
- Prefer `Utils.RunSync` over `Task.Result` for async-to-sync conversion
- Cache parsed values when possible rather than re-parsing repeatedly

### 🔗 Related Modules
- [CSV](../Data/CSV/readme.md)
- [Logging](../Logging/readme.md)
- [Configuration](../Configuration/readme.md)
