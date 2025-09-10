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
- `ConsoleUtil`: Console progress bars with width detection
- `DateTimeParser`: Intelligent format-inferred timestamp parser
- `IOUtils`: Get network-aware universal paths
- `TimingUtil`: Measure execution time and log performance
- `Utils`: Phone formatting, string filters, sync wrappers
- `Parser`: Type conversion from string to object
- `BooleanParser`: Extended true/false parsing with custom values
- `EnumExtensions`: Access enum descriptions

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

### ✅ Best Practices
- Register custom boolean/enum string values for localization
- Use `TimingUtil` for performance instrumentation
- Clean user inputs with `NullIfEmpty` or `.Remove()` helpers
- Extend `Parser` with domain-specific converters when needed

### 🔗 Related Modules
- [CSV](../Data/CSV/readme.md)
- [Logging](../Logging/readme.md)
- [Configuration](../Configuration/readme.md)
