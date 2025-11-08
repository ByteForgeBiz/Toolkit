# Core Module

## Overview

The **Core** module provides fundamental toolkit utilities and resource management functions for the ByteForge Toolkit. It handles embedded resource extraction and platform-specific initialization.

---

## Purpose

The Core module enables:
1. **Resource extraction** - Deploy embedded files to disk
2. **WinSCP management** - Handle WinSCP executable and libraries
3. **Checksum validation** - Verify file integrity
4. **Platform detection** - Handle platform-specific code

---

## Key Classes

### `Core`
**Purpose:** Static entry point for core functionality.

**Static Methods:**
```csharp
#if NETFRAMEWORK
    static void EnsureWinSCP();
#endif
```

**Purpose:**
- Ensures WinSCP executable and library are extracted
- Validates checksums against embedded resources
- Only re-extracts if missing or corrupted
- Platform-specific for .NET Framework

**Usage:**
```csharp
// In application startup (.NET Framework only)
#if NETFRAMEWORK
Core.EnsureWinSCP();  // Extract WinSCP files if needed
#endif

// Later, use WinSCP functionality
using (var session = new Session())
{
    session.Open(options);
}
```

**Why This Pattern?**
- Embeds WinSCP.exe and WinSCPnet.dll in assembly
- Extracts on first run
- Verifies integrity through checksums
- Only .NET Framework has embedded resources

### `WinScpResourceManager`
**Purpose:** Manages embedded WinSCP resource extraction (internal class).

**Responsibilities:**
- Extract embedded WinSCP.exe
- Extract WinSCPnet.dll wrapper
- Validate checksums
- Place files in correct locations
- Handle extraction errors

**Key Methods (Internal):**
```csharp
void EnsureWinScpFilesAvailable();
string GetWinScpPath();
string GetWinScpNetPath();
bool VerifyChecksum(string filePath, string embeddedResource);
void ExtractResource(string resourceName, string outputPath);
```

**How It Works:**
1. Check if WinSCP.exe exists in bin directory
2. Check if WinSCPnet.dll exists in bin directory
3. If missing or corrupted:
   - Extract embedded files
   - Verify checksums
   - Place in bin directory
4. On subsequent runs, use extracted files

---

## Resource Embedding

### Build-Time Process

Resources are embedded during build through:

1. **File Selection**
   - WinSCP.exe (from WinSCP installation)
   - WinSCPnet.dll (from WinSCP .NET wrapper)
   - Embedded in project as resources

2. **Checksum Generation**
 - SHA-256 hash computed for each file
   - Stored in project
   - Used for validation

3. **Assembly Embedding**
   - Files added to assembly resources
   - Available as stream at runtime
   - No additional deployment needed

### Resource Discovery

At runtime:
```csharp
// Get embedded resource stream
var assembly = Assembly.GetExecutingAssembly();
var resourceStream = assembly.GetManifestResourceStream("WinSCP.exe");

// Extract to disk
using (var fileStream = File.Create(outputPath))
{
resourceStream.CopyTo(fileStream);
}

// Verify checksum
var checksum = ComputeSha256(outputPath);
if (checksum != expectedChecksum)
    throw new InvalidOperationException("File corrupted");
```

---

## Platform-Specific Code

The Core module uses conditional compilation:

```csharp
#if NETFRAMEWORK
    // Only for .NET Framework
    public static void EnsureWinSCP()
    {
  var mgr = new WinScpResourceManager();
        mgr.EnsureWinScpFilesAvailable();
    }
#endif

#if NETCORE || NET5_OR_GREATER
    // .NET Core/.NET 5+ don't embed WinSCP
    // Users must install separately
#endif
```

**Why?**
- WinSCP is Windows-specific executable
- .NET Framework applications typically Windows-deployed
- .NET Core/.NET 5+ may run on Linux/macOS
- Embedded resources add assembly size
- Modern deployments use separate installer

---

## Usage Patterns

### .NET Framework Applications

```csharp
class Program
{
    static void Main()
    {
        // Ensure WinSCP files are available
        try
        {
            Core.EnsureWinSCP();
        }
        catch (Exception ex)
   {
 Console.WriteLine($"Failed to extract WinSCP: {ex.Message}");
       return;
        }
     
        // Now WinSCP functionality is available
        TransferFiles();
    }
    
    static void TransferFiles()
    {
        var options = new SessionOptions { /* ... */ };
        using (var session = new Session())
        {
      session.Open(options);
            // File transfer operations...
      }
    }
}
```

### .NET Core/.NET 5+ Applications

```csharp
class Program
{
    static void Main()
    {
        // No need for EnsureWinSCP() - not available in this target
      
#if NETFRAMEWORK
     Core.EnsureWinSCP();
#else
     Console.WriteLine("WinSCP must be installed separately on this platform");
#endif
        
        TransferFiles();
    }
}
```

---

## Error Handling

### Common Issues

| Issue                     | Cause                      | Solution                              |
|---------------------------|----------------------------|---------------------------------------|
| Resource not found        | Embedded file missing      | Rebuild project, check resource names |
| Checksum mismatch         | File corrupted/modified    | Delete extracted file, re-extract     |
| Extract permission denied | No write permission        | Run with appropriate permissions      |
| WinSCP not found          | Resource extraction failed | Check .NET Framework target           |

### Exception Handling

```csharp
try
{
    Core.EnsureWinSCP();
}
catch (FileNotFoundException ex)
{
    Log.Error("Embedded WinSCP resource not found", ex);
    throw;
}
catch (InvalidOperationException ex) when (ex.Message.Contains("checksum"))
{
    Log.Error("WinSCP file checksum verification failed", ex);
    throw;
}
catch (UnauthorizedAccessException ex)
{
    Log.Error("Insufficient permissions to extract WinSCP", ex);
throw;
}
catch (Exception ex)
{
    Log.Error("Unexpected error during WinSCP extraction", ex);
    throw;
}
```

---

## File Organization

### `Core.cs`
Main static class with public API.

**Contains:**
- `EnsureWinSCP()` method
- Partial class definition
- XML documentation

### `Core.WinScpResourceManager.cs`
WinSCP resource management (partial class).

**Contains:**
- `WinScpResourceManager` class
- Resource extraction logic
- Checksum validation
- Error handling

---

## Integration with Deployment

### Windows Deployment

**Option 1: Embedded (Net Framework)**
```
ByteForge.Toolkit.Modern.exe <- Contains WinSCP.exe
    ↓
First run calls Core.EnsureWinSCP()
    ↓
bin/WinSCP.exe <- Extracted
bin/WinSCPnet.dll <- Extracted
```

**Option 2: External (all platforms)**
```
ByteForge.Toolkit.Modern.exe <- No embedded files
WinSCP/
  ├── WinSCP.exe
    ├── WinSCPnet.dll
    ↓
Application locates WinSCP in PATH or fixed location
```

### Build Configuration

```xml
<!-- .csproj example -->
<ItemGroup>
    <EmbeddedResource Include="Resources\WinSCP.exe" />
    <EmbeddedResource Include="Resources\WinSCPnet.dll" />
</ItemGroup>

<Target Name="GenerateResourceChecksums" BeforeTargets="Build">
    <!-- Compute checksums for verification -->
</Target>
```

---

## Performance Considerations

### Extraction Performance

```csharp
// First run - extraction occurs
Core.EnsureWinSCP();  // ~1-2 seconds (extract + verify)

// Subsequent runs - quick check
Core.EnsureWinSCP();  // ~50ms (files exist, checksums valid)
```

**Optimization:**
- Only extract on demand
- Cache checksum results
- Check file exists before extraction
- Parallel extraction if multiple files

### Assembly Size Impact

```
Bare assembly: ~500 KB
+ WinSCP.exe embedded: +5 MB
+ WinSCPnet.dll embedded: +500 KB
Total with resources: ~6 MB
```

---

## Deployment Guide

### Step 1: Build for .NET Framework
```bash
dotnet publish -c Release -f net48
```

### Step 2: Verify Resources Embedded
```bash
# Check assembly contains resources
dir /s bin\Release\net48\
# Should see WinSCP resources in .csproj
```

### Step 3: Test Extraction
```csharp
Core.EnsureWinSCP();
bool exists = File.Exists("WinSCP.exe");
Assert.IsTrue(exists, "WinSCP.exe not extracted");
```

### Step 4: Deploy Application
```bash
# All files in bin\Release\net48 directory
# Deployment automatically extracts WinSCP files on first run
```

---

## Advanced Scenarios

### Custom Resource Location

```csharp
public class CustomResourceManager
{
    private readonly string _resourceDirectory;
    
    public CustomResourceManager(string resourceDir)
    {
        _resourceDirectory = resourceDir;
    }
    
    public void EnsureResourcesAvailable()
    {
        // Custom logic for non-standard installations
        if (!File.Exists(Path.Combine(_resourceDirectory, "WinSCP.exe")))
        {
     ExtractToCustomLocation();
        }
    }
    
    private void ExtractToCustomLocation()
    {
        // Extract embedded resources to custom path
    }
}
```

### Multiple Versions

```csharp
public class VersionedResourceManager
{
    // Handle different WinSCP versions
  private readonly Dictionary<string, string> _versionChecksums;
    
    public bool VerifyVersion(string filePath, string version)
    {
 string expectedChecksum = _versionChecksums[version];
     return ComputeChecksum(filePath) == expectedChecksum;
    }
}
```

---

## Troubleshooting

### Resources Not Found
```csharp
// Debug: List available resources
var assembly = Assembly.GetExecutingAssembly();
var resources = assembly.GetManifestResourceNames();
foreach (var res in resources)
    Console.WriteLine(res);
```

### Extraction Fails
```csharp
// Check permissions
var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
var directoryInfo = new DirectoryInfo(directory);
bool canWrite = IsWritable(directoryInfo);
```

### Checksum Validation

```csharp
// Manual checksum verification
string computed = ComputeSha256("WinSCP.exe");
string expected = "abc123...";
if (computed != expected)
    File.Delete("WinSCP.exe");  // Force re-extraction
```

---

## Summary

The Core module provides:

**Key Strengths:**
- ✓ Transparent resource extraction
- ✓ Checksum validation for integrity
- ✓ Platform-specific conditional compilation
- ✓ Automatic setup on first run
- ✓ Minimal deployment complexity
- ✓ Support for embedded dependencies

**Best For:**
- Simplifying WinSCP deployment
- .NET Framework applications
- Windows-only deployments
- Single-file deployment scenarios
- Reducing external dependencies

**Limitations:**
- Assembly size increase
- Windows-specific resources
- Limited to .NET Framework
- Not suitable for portable Linux deployments

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                            | Description                |
|---------------------------------------------------|----------------------------|
| **[CLI](../CommandLine/readme.md)**               | Command-line parsing       |
| **[Configuration](../Configuration/readme.md)**   | INI-based configuration    |
| **[Data](../Data/readme.md)**                     | Database & file processing |
| **[DataStructures](../DataStructures/readme.md)** | Collections & utilities    |
| **[JSON](../Json/readme.md)**                     | Delta serialization        |
| **[Logging](../Logging/readme.md)**               | Structured logging         |
| **[Mail](../Mail/readme.md)**                     | Email processing           |
| **[Net](../Net/readme.md)**                       | Network file transfers     |
| **[Security](../Security/readme.md)**             | Encryption & security      |
| **[Utils](../Utilities/readme.md)**               | General utilities          |
