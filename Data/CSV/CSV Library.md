# CSV.md

## ByteForge CSV Processing Library

A high-performance CSV parsing utility with automatic format detection and streaming support, built for .NET Framework 4.8.

### 🚀 Features
- Stream-based processing for large files
- Auto-detect delimiter (comma, tab, semicolon, pipe)
- Progress tracking via events
- Quote character and multiline field handling
- Customizable processors

### 🧱 Core Components
- `CSVReader`: Streaming parser with progress event
- `CSVFormat`: Format detection and custom delimiter support
- `BufferedReader`: Efficient sample and line buffering (internal)

### 🧪 Usage Example
```csharp
CSVReader.ReadFile("data.csv", (headers, values, rawLine) => {
    Console.WriteLine(string.Join(", ", values));
});
```

#### With Progress
```csharp
var reader = new CSVReader();
reader.Progress += (s, e) => Console.WriteLine($"Progress: {e.Progress}%");
reader.DataProcessor = (headers, values, raw) => { /* handle row */ };
reader.ReadFile("data.csv");
```

### ✅ Best Practices
- Handle file-not-found and malformed formats
- Use streaming methods for large datasets
- Implement `DataProcessor` for custom logic

### 🔗 Related Modules
- [Utils](../../Utils/readme.md)
- [Logging](../../Logging/readme.md)
- [Configuration](../../Configuration/readme.md)


