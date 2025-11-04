# Utils Module

## ByteForge.Toolkit Utility Collection

A comprehensive collection of utility classes providing essential functionality for string manipulation, file operations, type parsing, template processing, and system diagnostics. Designed for enterprise .NET Framework 4.8 applications with focus on performance, reliability, and ease of use.

### 🚀 Key Features

#### Core Utilities
- **Console Operations**: Advanced progress bars with width detection and dynamic sizing
- **Type Parsing**: Flexible string-to-type conversion supporting .NET primitives and custom types
- **Template Processing**: Powerful placeholder replacement with escape sequences and validation
- **File Operations**: Network-aware path resolution with UNC support and cross-platform compatibility
- **Timing & Performance**: Execution time measurement and performance monitoring utilities

#### Advanced Parsing & Conversion
- **DateTime Intelligence**: Multi-format date/time parsing with culture awareness and timezone support
- **Boolean Extensions**: Domain-specific true/false parsing with customizable value registration
- **Enum Descriptions**: Seamless access to enum descriptions via `DescriptionAttribute`
- **Value Converters**: Extensible converter registry for database operations and custom transformations
- **String Utilities**: Comprehensive string manipulation, formatting, and validation helpers

#### Enterprise Integration
- **Async-to-Sync Bridges**: Safe async operation wrappers for synchronous contexts
- **Configuration Integration**: Seamless integration with ByteForge Configuration system
- **Database Support**: Value converters and type parsing for database operations
- **Performance Monitoring**: Built-in timing utilities for performance analysis

### 🧱 Architecture Overview

#### Utility Categories

##### System & Console Utilities
- **`ConsoleUtil`**: Progress bar rendering with dynamic width detection
- **`IOUtils`**: Universal path resolution and file system operations
- **`TimingUtil`**: High-precision execution timing and performance measurement

##### Parsing & Type Conversion
- **`Parser`**: Extensible type conversion from strings to .NET types
- **`DateTimeParser`**: Intelligent date/time parsing with format inference
- **`BooleanParser`**: Advanced boolean parsing with custom value support
- **`EnumExtensions`**: Description attribute access for enum values

##### Text Processing & Templates
- **`TemplateProcessor`**: Placeholder replacement with escape sequence support
- **`StringUtil`**: String manipulation, cleaning, and formatting utilities
- **`Utils`**: General-purpose utility methods and helpers

##### Database & Configuration Support
- **`ValueConverterRegistry`**: Custom converter registration for database operations
- **`IParser`**: Interface for custom parser implementations

### 🧪 Comprehensive Usage Examples

#### Console Operations & Progress Tracking
```csharp
// Basic progress bar
ConsoleUtil.DrawProgressBar(75, "Uploading file...");

// Dynamic progress in a loop
for (int i = 0; i <= 100; i += 5)
{
    ConsoleUtil.DrawProgressBar(i, $"Processing item {i}/100");
    Thread.Sleep(100);
}

// Check console availability for console-based apps
if (ConsoleUtil.IsConsoleAvailable)
{
    ConsoleUtil.DrawProgressBar(50, "Safe console operation");
}
```

#### Advanced Type Parsing & Conversion
```csharp
// Basic parser usage
var parser = new Parser();
int number = parser.Parse<int>("42");
DateTime date = parser.Parse<DateTime>("2024-01-15");
bool flag = parser.Parse<bool>("true");

// Static parsing with default instance
decimal amount = Parser.Parse<decimal>("123.45");
Guid id = Parser.Parse<Guid>("550e8400-e29b-41d4-a716-446655440000");

// Register custom type converters
parser.RegisterType(typeof(TimeSpan),
    value => TimeSpan.ParseExact(value, @"hh\:mm\:ss", null),
    value => ((TimeSpan)value).ToString(@"hh\:mm\:ss"));

// Parse with custom type
TimeSpan duration = parser.Parse<TimeSpan>("02:30:45");

// Handle enum parsing
public enum Priority { Low, Normal, High }
parser.RegisterType(typeof(Priority),
    value => Enum.Parse(typeof(Priority), value, true),
    value => value.ToString());

Priority taskPriority = parser.Parse<Priority>("High");
```

#### DateTime Intelligence & Culture Support
```csharp
// Automatic format detection
DateTime dt1 = DateTimeParser.Parse("2024-02-11 15:30:00");
DateTime dt2 = DateTimeParser.Parse("Feb 11, 2024 3:30 PM");
DateTime dt3 = DateTimeParser.Parse("11/02/2024 15:30:00");

// Culture-specific parsing
var usParser = new DateTimeParser(CultureInfo.GetCultureInfo("en-US"));
var ukParser = new DateTimeParser(CultureInfo.GetCultureInfo("en-GB"));

DateTime usDate = usParser.Parse("02/11/2024");  // February 11, 2024
DateTime ukDate = ukParser.Parse("02/11/2024");  // November 2, 2024

// Timezone-aware parsing
DateTime utcTime = DateTimeParser.Parse("2024-02-11T15:30:00Z");
DateTime estTime = DateTimeParser.Parse("2024-02-11 15:30:00 EST");
```

#### Template Processing & String Manipulation
```csharp
// Basic template processing
var processor = new TemplateProcessor();
processor.Add("UserName", "John Doe");
processor.Add("Department", "Engineering");
processor.Add("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

string template = "Welcome <UserName> from <Department>! Last login: <LoginTime>";
string result = processor.Process(template);
// Result: "Welcome John Doe from Engineering! Last login: 2024-01-15 14:30"

// Advanced template with escape sequences
processor.UseEscapeSequences = true;
processor.Add("Tab", "\\t");
processor.Add("NewLine", "\\n");
processor.Add("Quote", "\\\"");

string advancedTemplate = "Name:<Tab><UserName><NewLine>Quote: <Quote>Hello World<Quote>";
string formatted = processor.Process(advancedTemplate);
// Result: "Name:	John Doe\nQuote: \"Hello World\""

// Template with indexer syntax
processor["CompanyName"] = "ByteForge Technologies";
processor["Year"] = "2024";
string copyright = processor.Process("© <Year> <CompanyName>. All rights reserved.");

// Bulk template processing
var templates = new Dictionary<string, string>
{
    ["welcome"] = "Hello <Name>, welcome to <Company>!",
    ["goodbye"] = "Goodbye <Name>, see you later!",
    ["reminder"] = "Don't forget your appointment at <Time>, <Name>."
};

processor.Add("Name", "Alice");
processor.Add("Company", "TechCorp");
processor.Add("Time", "3:00 PM");

foreach (var template in templates)
{
    Console.WriteLine($"{template.Key}: {processor.Process(template.Value)}");
}
```

#### File Operations & Path Resolution
```csharp
// Universal path conversion (network drives to UNC)
string uncPath = IOUtils.GetUniversalPath(@"Z:\Shared\Documents\Project.docx");
// Result: "\\server\shared\Documents\Project.docx"

// Safe path operations
string safePath = IOUtils.GetSafePath(@"C:\Program Files\My App\config.ini");
bool pathExists = IOUtils.PathExists(@"\\server\share\file.txt");

// Cross-platform path handling
string normalizedPath = IOUtils.NormalizePath("/var/log/application.log");
```

#### Performance Timing & Monitoring
```csharp
// Basic timing
using (var timer = new TimingUtil())
{
    // Perform operations
    DoSomeWork();
    
    Console.WriteLine($"Operation took: {timer.ElapsedMilliseconds} ms");
}

// Advanced timing with logging
var timing = TimingUtil.Measure(() =>
{
    // Complex operation
    ProcessLargeDataset();
});

Log.Info($"Dataset processing completed in {timing.TotalMilliseconds:F2} ms");

// Multiple timing measurements
var timings = new List<TimeSpan>();
for (int i = 0; i < 10; i++)
{
    var elapsed = TimingUtil.Measure(() => SingleOperation());
    timings.Add(elapsed);
}

var average = TimeSpan.FromTicks((long)timings.Average(t => t.Ticks));
Log.Info($"Average operation time: {average.TotalMilliseconds:F2} ms");
```

#### Boolean Parsing with Custom Values
```csharp
// Basic boolean parsing
bool result1 = BooleanParser.Parse("true");
bool result2 = BooleanParser.Parse("yes");
bool result3 = BooleanParser.Parse("1");
bool result4 = BooleanParser.Parse("enabled");

// Register custom domain values
BooleanParser.RegisterTrueValue("activate");
BooleanParser.RegisterTrueValue("confirmed");
BooleanParser.RegisterFalseValue("deactivate");
BooleanParser.RegisterFalseValue("denied");

// Use custom values
bool activated = BooleanParser.Parse("activate");   // true
bool confirmed = BooleanParser.Parse("confirmed");  // true
bool denied = BooleanParser.Parse("denied");        // false

// Bulk registration for localization
var trueValues = new[] { "sí", "verdadero", "activado" };
var falseValues = new[] { "no", "falso", "desactivado" };

foreach (var value in trueValues)
    BooleanParser.RegisterTrueValue(value);
foreach (var value in falseValues)
    BooleanParser.RegisterFalseValue(value);

// Parse Spanish boolean values
bool spanish1 = BooleanParser.Parse("sí");        // true
bool spanish2 = BooleanParser.Parse("activado");  // true
```

#### Enum Extensions & Descriptions
```csharp
// Define enum with descriptions
public enum OrderStatus
{
    [Description("Order is pending approval")]
    Pending,
    
    [Description("Order has been approved and is being processed")]
    Approved,
    
    [Description("Order has been shipped to customer")]
    Shipped,
    
    [Description("Order has been delivered successfully")]
    Delivered,
    
    [Description("Order was cancelled by customer or system")]
    Cancelled
}

// Use description extensions
OrderStatus status = OrderStatus.Approved;
string description = status.GetDescription();
// Result: "Order has been approved and is being processed"

// Bulk processing with descriptions
foreach (OrderStatus value in Enum.GetValues(typeof(OrderStatus)))
{
    Console.WriteLine($"{value}: {value.GetDescription()}");
}

// UI binding with descriptions
var statusList = Enum.GetValues(typeof(OrderStatus))
    .Cast<OrderStatus>()
    .Select(s => new { Value = s, Text = s.GetDescription() })
    .ToList();
```

#### Value Converters for Database Operations
```csharp
// Register standard converters
ValueConverterRegistry.RegisterConverter("UpperCase", 
    value => value?.ToString()?.ToUpperInvariant());

ValueConverterRegistry.RegisterConverter("LowerCase", 
    value => value?.ToString()?.ToLowerInvariant());

ValueConverterRegistry.RegisterConverter("TrimWhitespace", 
    value => value?.ToString()?.Trim());

// Register formatting converters
ValueConverterRegistry.RegisterConverter("CurrencyFormat", value =>
{
    if (decimal.TryParse(value?.ToString(), out decimal amount))
        return amount.ToString("C", CultureInfo.CurrentCulture);
    return value;
});

ValueConverterRegistry.RegisterConverter("DateFormat", value =>
{
    if (DateTime.TryParse(value?.ToString(), out DateTime date))
        return date.ToString("yyyy-MM-dd");
    return value;
});

// Register validation converters
ValueConverterRegistry.RegisterConverter("EmailValidation", value =>
{
    var email = value?.ToString();
    if (string.IsNullOrEmpty(email))
        return email;
    
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address;
    }
    catch
    {
        throw new ArgumentException($"Invalid email format: {email}");
    }
});

// Use in entity definitions
public class Customer
{
    [DBColumn("customer_name", ValueConverter = "UpperCase")]
    public string Name { get; set; }
    
    [DBColumn("email", ValueConverter = "EmailValidation")]
    public string Email { get; set; }
    
    [DBColumn("created_date", ValueConverter = "DateFormat")]
    public DateTime CreatedDate { get; set; }
}
```

#### Async-to-Sync Operations
```csharp
// Safe async-to-sync conversion
string result = Utils.RunSync(async cancellationToken =>
{
    using (var client = new HttpClient())
    {
        var response = await client.GetStringAsync("https://api.example.com/data");
        return response;
    }
});

// With timeout handling
try
{
    var data = Utils.RunSync(async cancellationToken =>
    {
        await Task.Delay(5000, cancellationToken); // Simulate work
        return "Completed";
    }, TimeSpan.FromSeconds(3));
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out");
}

// Database operations
var users = Utils.RunSync(async cancellationToken =>
{
    return await dbContext.Users
        .Where(u => u.IsActive)
        .ToListAsync(cancellationToken);
});
```

#### String Utilities & Formatting
```csharp
// Phone number formatting
string formatted1 = Utils.FormatUSPhoneNumber("2025550183");
// Result: "(202) 555-0183"

string formatted2 = Utils.FormatUSPhoneNumber("+1-202-555-0183");
// Result: "(202) 555-0183"

// String cleaning and manipulation
string cleaned = "  dirty   value  ".Clean();  // "dirty value"
string trimmed = "test string".NullIfEmpty();
string safe = null.NullIfEmpty();  // returns null

// Remove unwanted characters
string alphanumeric = "abc123!@#".Remove("!@#");  // "abc123"
string digitsOnly = "abc123def".Remove(c => !char.IsDigit(c));  // "123"

// Email validation and formatting
bool isValid = Utils.IsValidEmail("user@example.com");
string normalized = Utils.NormalizeEmail("User@Example.Com");  // "user@example.com"
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

---

## 📚 Modules

| Module                                        | Description                                                                     |
|-----------------------------------------------|---------------------------------------------------------------------------------|
| [🏠 Home](../readme.md)                       | ByteForge.Toolkit main documentation                                            |
| [CommandLine](../CLI/readme.md)               | Attribute-based CLI parsing with aliasing, typo correction, and plugin support  |
| [Configuration](../Configuration/readme.md)   | INI-based configuration system with typed section support                       |
| [Data](../Data/readme.md)                     | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes                                                |
| [Logging](../Logging/readme.md)               | Thread-safe logging system with async file/console output                       |
| [Mail](../Mail/readme.md)                     | Email utility with HTML support and attachment handling                         |
| [Net](../Net/readme.md)                       | FTP/FTPS/SFTP high-level transfer client                                        |
| [Security](../Security/readme.md)             | AES-based string encryption with key generation and Galois Field logic          |
| [Utils](../Utils/readme.md)                   | Miscellaneous helpers: timing, path utilities, progress bar                     |
| [Core](../Core/readme.md)                     | Embedded resource deployment (WinSCP)                                           |
| [HTML](../HTML/readme.md)                     | Web UI framework components                                                     |

