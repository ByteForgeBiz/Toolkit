# Net Module

The Net module provides file transfer operations over FTP, FTPS, and SFTP, built on top of `ByteForge.WinSCP.Session`. It exposes a unified `FileTransferClient` with sync and async methods for uploading, downloading, listing, deleting, and checking files, plus batch async operations with concurrency control and progress reporting.

---

## Key Types

| Type | Description |
|------|-------------|
| `FileTransferClient` | Main client — connects, transfers files, and manages the WinSCP session |
| `FileTransferConfig` | Connection parameters (host, credentials, protocol, port, timeout) |
| `TransferProtocol` | Enum of supported protocols |
| `FileTransferResult` | Result of a single upload or download |
| `FileTransferItem` | A local+remote path pair for batch operations |
| `FileTransferProgress` | Progress snapshot for batch transfers |
| `FileOperationProgress` | Progress snapshot for batch delete/other operations |
| `FileOperationResult` | Result of a single file operation (delete, mkdir, etc.) |
| `RemoteFileInfo` | Metadata for a file or directory on the remote server |
| `FileTransferException` | Custom exception thrown on connection or transfer failures |

---

## `TransferProtocol`

```csharp
public enum TransferProtocol
{
    FTP,
    FTPS_Explicit,
    FTPS_Implicit,
    SFTP
}
```

Use `FileTransferConfig.CreateFtpsConfig(useImplicitTls: false)` for FTPS Explicit, or `useImplicitTls: true` for FTPS Implicit.

---

## `FileTransferConfig`

Connection configuration. Use the static factory methods rather than constructing manually.

| Property | Default | Description |
|----------|---------|-------------|
| `HostName` | — | Remote server hostname or IP |
| `UserName` | — | Login username |
| `Password` | — | Login password |
| `SshPrivateKeyPath` | `null` | Path to SSH private key (SFTP only) |
| `Protocol` | `SFTP` | Transfer protocol |
| `Port` | 22 | Remote port |
| `TimeoutSeconds` | 30 | Connection and command timeout |
| `AcceptAnyHostKey` | `false` | Skip SSH host key validation (use with care) |
| `AcceptAnyCertificate` | `false` | Skip TLS certificate validation (use with care) |

### Factory methods

```csharp
FileTransferConfig.CreateFtpConfig(host, user, pass, port = 21)
FileTransferConfig.CreateFtpsConfig(host, user, pass, useImplicitTls = false, port = 0)
FileTransferConfig.CreateSftpConfig(host, user, pass, port = 22)
```

---

## `FileTransferClient`

Wraps `ByteForge.WinSCP.Session`. A `SemaphoreSlim` guards the underlying session to ensure thread-safe access.

**Constructor:**
```csharp
var client = new FileTransferClient(config);
```

### Connection

```csharp
bool Connect();       // Returns true on success
void Disconnect();
```

### Single-file operations

| Method | Description |
|--------|-------------|
| `UploadFile(localPath, remotePath, overwrite)` | Upload one file |
| `DownloadFile(remotePath, localPath, overwrite)` | Download one file |
| `FileExists(remotePath)` | Check whether a remote file exists |
| `DeleteFile(remotePath)` | Delete a remote file |
| `CreateDirectory(remotePath)` | Create a remote directory |
| `ListDirectory(remotePath)` | List files and directories at a remote path |

All methods have async equivalents (`UploadFileAsync`, `DownloadFileAsync`, etc.).

Resume behavior: `OverwriteMode.Resume` is used by default for uploads, enabling interrupted transfer resumption.

### Batch async operations

```csharp
Task<IReadOnlyList<FileTransferResult>> UploadFilesAsync(
    IEnumerable<FileTransferItem> items,
    int maxConcurrent = 4,
    IProgress<FileTransferProgress>? progress = null,
    CancellationToken ct = default);

Task<IReadOnlyList<FileTransferResult>> DownloadFilesAsync(
    IEnumerable<FileTransferItem> items,
    int maxConcurrent = 4,
    IProgress<FileTransferProgress>? progress = null,
    CancellationToken ct = default);

Task<IReadOnlyList<FileOperationResult>> DeleteFilesAsync(
    IEnumerable<string> remotePaths,
    int maxConcurrent = 4,
    IProgress<FileOperationProgress>? progress = null,
    CancellationToken ct = default);
```

`maxConcurrent` controls a `SemaphoreSlim` that limits how many transfers run in parallel.

---

## Result Types

### `FileTransferResult`

| Property | Type | Description |
|----------|------|-------------|
| `LocalPath` | `string?` | Local file path involved in the transfer |
| `RemotePath` | `string?` | Remote file path involved in the transfer |
| `Success` | `bool` | Whether the transfer succeeded |
| `ErrorMessage` | `string?` | Error description if `Success` is `false` |

### `FileOperationResult`

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string?` | Path involved in the operation |
| `Success` | `bool` | Whether the operation succeeded |
| `ErrorMessage` | `string?` | Error description if failed |

### `RemoteFileInfo`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | File or directory name |
| `FullPath` | `string?` | Full remote path |
| `Size` | `long` | File size in bytes |
| `LastModified` | `DateTime` | Last modification time |
| `IsDirectory` | `bool` | Whether the entry is a directory |
| `Permissions` | `string?` | UNIX permission string (e.g., `rwxr-xr-x`) |

### Progress Types

**`FileTransferProgress`** (for upload/download batches):

| Property | Description |
|----------|-------------|
| `ProcessedCount` | Number of files completed so far |
| `TotalCount` | Total files in the batch |
| `PercentComplete` | Integer 0–100 |
| `CurrentItem` | The `FileTransferItem` currently being processed |

**`FileOperationProgress`** (for delete batches):

| Property | Description |
|----------|-------------|
| `ProcessedCount` | Files processed so far |
| `TotalCount` | Total in batch |
| `PercentComplete` | Integer 0–100 |
| `CurrentPath` | Remote path currently being processed |

---

## Usage

### Upload a file via SFTP

```csharp
var config = FileTransferConfig.CreateSftpConfig("sftp.example.com", "user", "secret");
using var client = new FileTransferClient(config);

client.Connect();
var result = client.UploadFile(@"C:\reports\daily.csv", "/uploads/daily.csv", overwrite: true);

if (!result.Success)
    Log.Error($"Upload failed: {result.ErrorMessage}");
```

### Upload via FTPS (Explicit TLS)

```csharp
var config = FileTransferConfig.CreateFtpsConfig("ftp.example.com", "user", "secret",
                                                  useImplicitTls: false);
```

### Batch upload with progress

```csharp
var items = new[]
{
    new FileTransferItem { LocalPath = @"C:\data\a.csv", RemotePath = "/in/a.csv" },
    new FileTransferItem { LocalPath = @"C:\data\b.csv", RemotePath = "/in/b.csv" },
};

var progress = new Progress<FileTransferProgress>(p =>
    Console.WriteLine($"{p.PercentComplete}% — {p.CurrentItem.LocalPath}"));

var results = await client.UploadFilesAsync(items, maxConcurrent: 3, progress: progress);

foreach (var r in results.Where(r => !r.Success))
    Log.Warning($"Failed: {r.LocalPath} — {r.ErrorMessage}");
```

### SFTP with private key authentication

```csharp
var config = new FileTransferConfig
{
    Protocol           = TransferProtocol.SFTP,
    HostName           = "sftp.example.com",
    UserName           = "deploy",
    SshPrivateKeyPath  = @"C:\keys\id_rsa.ppk",
    Port               = 22,
    AcceptAnyHostKey   = false
};
```

### List a remote directory

```csharp
var entries = client.ListDirectory("/uploads/");
foreach (var entry in entries)
    Console.WriteLine($"{(entry.IsDirectory ? "DIR" : "   ")} {entry.Name} ({entry.Size} bytes)");
```

### Download and check existence

```csharp
if (client.FileExists("/exports/report.zip"))
{
    var result = await client.DownloadFileAsync("/exports/report.zip", @"C:\downloads\", overwrite: true);
    if (result.Success)
        Log.Info($"Downloaded to {result.LocalPath}");
}
```

---

## Error Handling

`FileTransferException` is thrown on connection or session failures. Individual file failures in batch operations are captured in `FileTransferResult.ErrorMessage` rather than thrown.

```csharp
try
{
    client.Connect();
}
catch (FileTransferException ex)
{
    Log.Error("Could not connect to remote server", ex);
}
```

---

## Related Modules

| Module | Description |
|--------|-------------|
| [WinSCP](../../WinSCP/readme.md) | Underlying WinSCP session wrapper |
| [Logging](../Logging/readme.md) | Logging used throughout the client |
