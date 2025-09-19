# Zip.md

## ByteForge.Toolkit.Zip

A comprehensive ZIP archive library based on DotNetZip, providing advanced ZIP functionality far beyond .NET's built-in ZipArchive capabilities. Supports encryption, multi-part archives, self-extracting executables, and multiple compression algorithms.

### 🚀 Features
- **Multi-part ZIP support** - Create and read spanned/segmented archives
- **Self-extracting archives** - Generate Windows Forms and command-line SFX files
- **Advanced encryption** - WinZip AES 128/256-bit encryption and traditional ZIP encryption
- **Multiple compression algorithms** - Deflate, BZip2, and raw storage methods
- **ZIP64 support** - Handle archives larger than 4GB with unlimited entries
- **File selection** - Advanced file filtering with expression-based criteria
- **Unicode support** - Proper handling of international filenames
- **Parallel compression** - Multi-threaded compression for improved performance
- **Stream-based operations** - Work with ZIP data without files
- **Progress reporting** - Track compression/extraction progress
- **Password protection** - Secure archives with encryption

### 🧱 Core Components
- `ZipFile`: Main class for creating, reading, and updating ZIP archives
- `ZipEntry`: Represents individual files within a ZIP archive
- `ZipOutputStream`/`ZipInputStream`: Stream-based ZIP operations
- `FileSelector`: Expression-based file filtering and selection
- `BZip2Compressor`: BZip2 compression algorithm implementation
- `WinZipAesCrypto`: WinZip AES encryption implementation
- `ZlibStream`: Zlib compression/decompression streams

### 🧪 Basic Examples

#### Creating a ZIP file
```csharp
using (var zip = new ZipFile())
{
    zip.AddFile(@"C:\MyFile.txt", "docs");
    zip.AddDirectory(@"C:\MyFolder", "data");
    zip.Save(@"C:\MyArchive.zip");
}
```

#### Extracting files
```csharp
using (var zip = ZipFile.Read(@"C:\MyArchive.zip"))
{
    foreach (ZipEntry entry in zip)
    {
        entry.Extract(@"C:\ExtractTo", ExtractExistingFileAction.OverwriteSilently);
    }
}
```

#### Creating password-protected archive
```csharp
using (var zip = new ZipFile())
{
    zip.Password = "SecretPassword";
    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
    zip.AddFile(@"C:\Sensitive.txt");
    zip.Save(@"C:\Encrypted.zip");
}
```

### 🔧 Advanced Features

#### Self-extracting archives
```csharp
using (var zip = new ZipFile())
{
    zip.AddDirectory(@"C:\MyApp");
    
    var options = new SelfExtractorSaveOptions
    {
        Flavor = SelfExtractorFlavor.WinFormsApplication,
        DefaultExtractDirectory = "%TEMP%\\MyApp",
        PostExtractCommandLine = "setup.exe"
    };
    
    zip.SaveSelfExtractor(@"C:\MyInstaller.exe", options);
}
```

#### File selection with expressions
```csharp
using (var zip = new ZipFile())
{
    // Add all .txt files modified after 2023-01-01
    zip.AddSelectedFiles("name = *.txt AND mtime > 2023-01-01", @"C:\Documents", "docs");
    
    // Add files larger than 1MB
    zip.AddSelectedFiles("size > 1mb", @"C:\Data", "data");
    
    zip.Save(@"C:\Filtered.zip");
}
```

#### Multi-part archives
```csharp
using (var zip = new ZipFile())
{
    zip.MaxOutputSegmentSize = 1024 * 1024; // 1MB segments
    zip.AddDirectory(@"C:\LargeData");
    zip.Save(@"C:\Archive.z01"); // Creates .z01, .z02, etc.
}
```

### 🔒 Encryption Support

#### Available encryption methods
- `EncryptionAlgorithm.None` - No encryption
- `EncryptionAlgorithm.PkzipWeak` - Traditional ZIP encryption (compatible but weak)
- `EncryptionAlgorithm.WinZipAes128` - WinZip AES 128-bit encryption
- `EncryptionAlgorithm.WinZipAes256` - WinZip AES 256-bit encryption (recommended)

### 📊 Compression Algorithms

#### Built-in compression methods
- **Deflate** - Standard ZIP compression (default)
- **BZip2** - Higher compression ratio, slower speed
- **Store** - No compression (fastest)

```csharp
using (var zip = new ZipFile())
{
    var entry = zip.AddFile(@"C:\Document.txt");
    entry.CompressionMethod = CompressionMethod.BZip2;
    entry.CompressionLevel = CompressionLevel.BestCompression;
    zip.Save(@"C:\Compressed.zip");
}
```

### 🎯 Key Advantages over .NET ZipArchive

- **Self-extracting archives** - Create executable installers
- **Multi-part archives** - Split large archives across multiple files
- **Advanced encryption** - WinZip AES encryption support
- **BZip2 compression** - Better compression ratios
- **File selection** - Expression-based filtering
- **ZIP64 support** - No size limitations
- **Parallel processing** - Multi-threaded operations
- **Progress tracking** - Real-time operation feedback
- **Unicode handling** - Better international filename support
- **Legacy compatibility** - Works with older ZIP formats

### 🔗 Dependencies
- .NET Framework 4.8
- System.Security.Cryptography (for AES encryption)
- System.Windows.Forms (for WinForms self-extractors)

### ✅ Best Practices
- Use AES 256-bit encryption for sensitive data
- Consider BZip2 for maximum compression when speed isn't critical
- Use file selection expressions to filter large directory trees
- Set appropriate segment sizes for multi-part archives
- Always dispose of ZipFile objects properly
- Test self-extracting archives on target systems

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

### 📝 Notes
- Based on the proven DotNetZip library by Dino Chiesa
- Licensed under Microsoft Public License
- Includes managed implementations of Zlib and BZip2
- Self-extractor functionality requires compilation at runtime
- WinZip AES encryption provides industry-standard security