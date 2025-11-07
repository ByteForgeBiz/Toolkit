# Net (Network) Module

## Overview

The **Net** module provides network file transfer operations, abstracting the complexity of different protocols (FTP, SFTP, SCP) into a unified interface. It works with the WinSCPnet wrapper library to provide reliable file transfer capabilities.

---

## Purpose

Network operations need:
1. **Protocol abstraction** - Single API for multiple protocols
2. **Progress tracking** - Real-time transfer feedback
3. **Error recovery** - Resumable transfers and retry logic
4. **Configuration** - Flexible transport options
5. **Result reporting** - Detailed operation outcomes

---

## Key Classes

### `FileTransferClient`
**Purpose:** Main client for file transfer operations.

**Responsibilities:**
- Establish remote connections
- Execute file transfers
- Track progress
- Handle errors

**Key Methods:**
```csharp
// Connection
void Connect(FileTransferConfig config);
void Disconnect();
bool IsConnected { get; }

// Operations
Task<FileTransferResult> UploadAsync(string localPath, string remotePath);
Task<FileTransferResult> DownloadAsync(string remotePath, string localPath);
Task<FileTransferResult> UploadBatchAsync(IEnumerable<FileTransferItem> items);
Task<FileTransferResult> DownloadBatchAsync(IEnumerable<FileTransferItem> items);

// Events
event EventHandler<FileTransferProgress>? ProgressChanged;
event EventHandler<FileTransferResult>? TransferCompleted;
```

### `FileTransferConfig`
**Purpose:** Configuration for file transfers.

**Properties:**
```csharp
TransferProtocol Protocol { get; set; }  // FTP, SFTP, SCP
string HostName { get; set; }
int Port { get; set; }
string UserName { get; set; }
string Password { get; set; }
TimeSpan Timeout { get; set; }
bool EnableCompression { get; set; }
TransferMode TransferMode { get; set; }
```

### `FileTransferItem`
**Purpose:** Represents a single file transfer operation.

**Properties:**
```csharp
string SourcePath { get; set; }    // Local or remote
string DestinationPath { get; set; }  // Remote or local
string FileName { get; set; }
long FileSize { get; set; }
DateTime LastModified { get; set; }
bool Overwrite { get; set; }
```

### `FileTransferProgress`
**Purpose:** Progress information for ongoing transfer.

**Properties:**
```csharp
string FileName { get; }
long TotalBytes { get; }
long TransferredBytes { get; }
double PercentComplete { get; }
TimeSpan ElapsedTime { get; }
TimeSpan EstimatedTimeRemaining { get; }
TransferSpeed Speed { get; }  // bytes per second
```

### `FileTransferResult`
**Purpose:** Result of a transfer operation.

**Properties:**
```csharp
bool IsSuccess { get; }
FileTransferStatus Status { get; }  // Completed, Failed, Cancelled
IList<FileTransferItem> TransferedFiles { get; }
IList<FileTransferItem> FailedFiles { get; }
string ErrorMessage { get; }
DateTime StartTime { get; }
DateTime EndTime { get; }
TimeSpan Duration { get; }
```

### Enums

**`TransferProtocol`**
```csharp
public enum TransferProtocol
{
    FTP,      // Standard FTP
    FTPS,     // FTP over SSL/TLS
    SFTP,     // SSH File Transfer (secure)
    SCP,    // Secure Copy
    WebDAV    // WebDAV over HTTP
}
```

**`TransferMode`**
```csharp
public enum TransferMode
{
    ASCII,  // Text mode
    Binary,     // Binary mode
    Auto        // Detect automatically
}
```

**`FileTransferStatus`**
```csharp
public enum FileTransferStatus
{
    Pending,     // Not yet started
    InProgress,  // Actively transferring
    Completed,   // Successfully finished
    Failed,      // Transfer failed
    Cancelled,   // User cancelled
    Paused       // Transfer paused
}
```

### Remote File Information

**`RemoteFileInfo`**
```csharp
public class RemoteFileInfo
{
    string FileName { get; }
    string FullPath { get; }
    long FileSize { get; }
    DateTime LastModified { get; }
    bool IsDirectory { get; }
    bool IsSymLink { get; }
    string Permissions { get; }
}
```

**`RemoteDirectoryInfo`**
```csharp
public class RemoteDirectoryInfo
{
    string DirectoryName { get; }
    string FullPath { get; }
    DateTime LastModified { get; }
    IList<RemoteFileInfo> Files { get; }
    IList<RemoteDirectoryInfo> SubDirectories { get; }
}
```

---

## Usage Patterns

### Basic Upload

```csharp
var config = new FileTransferConfig
{
    Protocol = TransferProtocol.SFTP,
    HostName = "sftp.example.com",
    UserName = "user",
    Password = "password",
    Timeout = TimeSpan.FromSeconds(30)
};

var client = new FileTransferClient();

try
{
    client.Connect(config);
    
    var result = await client.UploadAsync(
     localPath: @"C:\data\file.csv",
    remotePath: "/uploads/"
    );
    
    if (result.IsSuccess)
        Console.WriteLine("Upload completed");
    else
 Console.WriteLine($"Upload failed: {result.ErrorMessage}");
}
finally
{
    client.Disconnect();
}
```

### Download with Progress

```csharp
client.ProgressChanged += (sender, progress) =>
{
    Console.WriteLine($"{progress.FileName}: {progress.PercentComplete:P}");
    Console.WriteLine($"Speed: {progress.Speed} B/s");
    Console.WriteLine($"ETA: {progress.EstimatedTimeRemaining}");
};

var result = await client.DownloadAsync(
    remotePath: "/exports/data.zip",
    localPath: @"C:\downloads\"
);
```

### Batch Operations

```csharp
var items = new[]
{
    new FileTransferItem 
    { 
        SourcePath = @"C:\data1.csv", 
        DestinationPath = "/uploads/" 
    },
    new FileTransferItem 
    { 
        SourcePath = @"C:\data2.csv", 
        DestinationPath = "/uploads/" 
    },
    new FileTransferItem 
    { 
  SourcePath = @"C:\data3.csv", 
      DestinationPath = "/uploads/" 
    }
};

var result = await client.UploadBatchAsync(items);

foreach (var failed in result.FailedFiles)
    Log.Warning($"Failed to upload: {failed.FileName}");
```

### Remote Directory Listing

```csharp
var remoteDir = await client.ListDirectoryAsync("/data/");

Console.WriteLine($"Directory: {remoteDir.DirectoryName}");
Console.WriteLine("Files:");
foreach (var file in remoteDir.Files)
{
  Console.WriteLine($"  {file.FileName} ({file.FileSize} bytes)");
    Console.WriteLine($"  Modified: {file.LastModified}");
    Console.WriteLine($"  Permissions: {file.Permissions}");
}
```

---

## Error Handling

### Common Transfer Errors

| Error | Cause | Recovery |
|-------|-------|----------|
| Connection timeout | Network delay | Retry with longer timeout |
| Authentication failed | Wrong credentials | Verify username/password |
| File not found | Path incorrect | Check remote path |
| Permission denied | Insufficient rights | Request access or use different account |
| Disk full | No space | Free up space or use compression |

### Exception Handling

```csharp
try
{
    await client.UploadAsync(localPath, remotePath);
}
catch (FileTransferException ex) when (ex.ErrorCode == 530)
{
    // 530 = User not logged in / Access denied
    Log.Error("Access denied - check credentials");
}
catch (FileTransferException ex) when (ex.ErrorCode == 550)
{
// 550 = File not found
    Log.Error("Remote file not found");
}
catch (TimeoutException ex)
{
    Log.Error("Transfer timeout - network issue");
}
catch (Exception ex)
{
    Log.Error($"Unexpected error: {ex.Message}");
}
```

---

## Performance Optimization

### Compression

```csharp
var config = new FileTransferConfig
{
    // ... other settings ...
    EnableCompression = true  // Reduce bandwidth usage
};
```

### Batch Transfers

```csharp
// Efficient - single connection for multiple files
var items = GetAllFilesToUpload();
var result = await client.UploadBatchAsync(items);

// Less efficient - new connection per file
foreach (var file in files)
{
    client.Connect(config);
    await client.UploadAsync(file.LocalPath, file.RemotePath);
    client.Disconnect();
}
```

### Large File Resumption

```csharp
var config = new FileTransferConfig
{
    // ... other settings ...
    EnableResume = true  // Resume interrupted transfers
};
```

---

## File Organization

### `FileTransferClient.cs`
Main client implementation.

### `FileTransferConfig.cs`
Configuration model.

### `FileTransferItem.cs`
Individual transfer item.

### `FileTransferProgress.cs`
Progress tracking model.

### `FileTransferResult.cs`
Operation result model.

### `FileTransferException.cs`
Custom exception for transfer errors.

### `TransferProtocol.cs`
Supported protocol enumeration.

### `Remote/`
Remote file information classes.

---

## Summary

The Net module provides:

**Key Strengths:**
- ? Multiple protocol support
- ? Unified transfer interface
- ? Real-time progress reporting
- ? Batch operation support
- ? Comprehensive error handling
- ? Large file support
- ? Resume capability

**Best For:**
- File synchronization
- Automated backups
- Cross-server deployments
- Data migration
- Remote monitoring

**Not Ideal For:**
- Real-time streaming
- Extremely high-frequency operations
- Direct socket-level control

