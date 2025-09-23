# Net Module

## ByteForge.Toolkit.Network File Transfer Suite

Cross-protocol (FTP / FTPS Explicit & Implicit / SFTP) high?level client built on the embedded WinSCP engine with a clean, task?based API, safe concurrency, rich progress reporting and batch orchestration utilities.

### ?? Key Features
- Unified API over FTP, FTPS (Explicit/Implicit) and SFTP
- Automatic embedded WinSCP extraction via `Core.EnsureWinSCP()` (implicit)
- Sync & Async methods for all operations
- High?level batching with bounded concurrency (`maxConcurrent`)
- Fine?grained progress reporting (`IProgress<FileTransferProgress>` / `IProgress<FileOperationProgress>`)
- Graceful cancellation (honours `CancellationToken` for all async ops)
- Per-item result objects with success + error aggregation
- Safe overwrite / resume semantics
- Remote directory inspection (`ListDirectory`) mapped to strong `RemoteFileInfo`
- Existence checks and bulk deletion helpers
- SFTP private key or password authentication
- Optional relaxed host key / certificate validation toggles for controlled environments
- Deterministic configuration factories for each protocol (`CreateFtpConfig`, `CreateFtpsConfig`, `CreateSftpConfig`)

### ?? Core Types
| Type | Purpose |
|------|---------|
| `FileTransferClient` | Primary facade for all transfer operations |
| `FileTransferConfig` | Connection + security settings (protocol, host, creds, timeouts) |
| `TransferProtocol` | Enum (FTP, FTPS_Explicit, FTPS_Implicit, SFTP) |
| `FileTransferItem` | Describes a single upload/download intent |
| `FileTransferResult` | Result for a single transfer (success / error) |
| `FileOperationResult` | Result for non?transfer ops (delete etc.) |
| `FileTransferProgress` | Progress snapshot for multi-file transfers |
| `FileOperationProgress` | Progress snapshot for bulk operations |
| `RemoteFileInfo` | Metadata for remote listing entries |
| `FileTransferException` | Domain exception wrapping underlying failures |

### ?? Security Notes
- FTPS certificate & SFTP host key trust relaxing is opt?in (`AcceptAnyCertificate`, `AcceptAnyHostKey`) – keep false in production.
- Private key auth supported for SFTP (`SshPrivateKeyPath`). If provided, password is ignored.
- Timeouts configurable (default 30s).

### ?? Configuration Examples
```csharp
// SFTP (password)
var cfg = FileTransferConfig.CreateSftpConfig("sftp.example.com", "deploy", "s3cret");

// SFTP (key based)
var keyCfg = new FileTransferConfig {
    HostName = "sftp.example.com",
    UserName = "deploy",
    SshPrivateKeyPath = "C:/keys/deploy.ppk",
    Protocol = TransferProtocol.SFTP,
    AcceptAnyHostKey = true // only for non?prod
};

// Explicit FTPS
var ftps = FileTransferConfig.CreateFtpsConfig("ftps.example.com", "user", "pass", useImplicitTls:false);

// Plain FTP (legacy)
var ftp = FileTransferConfig.CreateFtpConfig("legacy.host.local", "u", "p");
```

### ?? Upload Single File
```csharp
using var client = new FileTransferClient(cfg);
client.Connect();
client.UploadFile(@"C:\data\report.csv", "/incoming/report.csv", overwrite:true);
```

### ?? Download Single File (Async + Cancellation)
```csharp
using var client = new FileTransferClient(cfg);
await client.ConnectAsync(token);
await client.DownloadFileAsync("/exports/daily.zip", @"C:\pull\daily.zip", overwrite:true, cancellationToken: token);
```

### ?? Batch Upload with Progress & Concurrency
```csharp
var items = Directory.GetFiles(@"C:\batch\out")
    .Select(f => new FileTransferItem { LocalPath = f, RemotePath = "/in/" + Path.GetFileName(f), Overwrite = true });

var progressList = new List<int>();
var progress = new Progress<FileTransferProgress>(p => progressList.Add(p.PercentComplete));

using var client = new FileTransferClient(cfg);
await client.ConnectAsync();
var results = await client.UploadFilesAsync(items, maxConcurrent:4, progress:progress);

foreach (var r in results)
    Console.WriteLine($"{r.RemotePath}: {(r.Success ? "OK" : r.ErrorMessage)}");
```

### ?? Remote Directory Listing
```csharp
using var client = new FileTransferClient(cfg);
client.Connect();
var entries = client.ListDirectory("/incoming");
foreach (var e in entries)
    Console.WriteLine($"{(e.IsDirectory?"<DIR>":"     ")} {e.Name} {e.Size} bytes");
```

### ?? Bulk Delete
```csharp
var targets = new [] { "/incoming/old1.zip", "/incoming/old2.zip", "/incoming/old3.zip" };
var deleteProgress = new Progress<FileOperationProgress>(p => Console.WriteLine($"Delete {p.PercentComplete}%"));

using var client = new FileTransferClient(cfg);
await client.ConnectAsync();
var deleteResults = await client.DeleteFilesAsync(targets, maxConcurrent:3, progress:deleteProgress);
```

### ? Best Practices
- Prefer SFTP or FTPS over plain FTP.
- Keep `AcceptAnyHostKey/AcceptAnyCertificate` = false in production.
- Reuse a single `FileTransferClient` per logical batch to avoid handshake overhead.
- Use bounded concurrency (3–6) to balance throughput and server load.
- Always honour cancellation tokens for long batch jobs.
- Log per-item failures but continue remaining transfers (already supported by result aggregation).
- Dispose the client (or use `using`) to ensure session closure.

### ?? Testing Focus
Recommended scenarios:
- Connection failure (bad host / credentials / port)
- SFTP key vs password priority
- Large batch with partial failures
- Cancellation mid-transfer (token triggered)
- Overwrite=false behaviour when remote file exists
- Listing large directories

### ?? Related Modules
| Module | Description |
|--------|-------------|
| [?? Home](../readme.md) | Main toolkit documentation |
| [Core](../Core/readme.md) | Embedded resource extraction (WinSCP) |
| [Configuration](../Configuration/readme.md) | Store encrypted credentials / endpoints |
| [Security](../Security/readme.md) | Encrypt stored passwords before loading config |
| [Logging](../Logging/readme.md) | Capture transfer diagnostics |
| [Utils](../Utils/readme.md) | Timing, path helpers & formatting |

### ?? Core Integration
The Net module depends on embedded WinSCP binaries shipped as resources. If not already extracted you may explicitly call:
```csharp
Core.EnsureWinSCP(); // Usually automatic when first session opens
```

---
*High-throughput, protocol?agnostic transfers with clean diagnostics and safe concurrency.*
