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

---

## 📚 Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../../readme.md) | ByteForge.Toolkit main documentation |
| [CommandLine](../../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](../../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |
| [Zip](../../Zip/readme.md) | Advanced ZIP library with multi-part archives, self-extracting executables, and AES encryption |

