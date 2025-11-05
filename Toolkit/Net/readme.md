# Net Module

## ByteForge.Toolkit.Network File Transfer Suite

Cross-protocol (FTP / FTPS Explicit & Implicit / SFTP) high?level client built on the embedded WinSCP engine with a clean, task?based API, safe concurrency, rich progress reporting and batch orchestration utilities.

### 🚀 Key Features
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

### 🧱 Core Types
| Type                     | Purpose                                                          |
|--------------------------|------------------------------------------------------------------|
| `FileTransferClient`     | Primary facade for all transfer operations                       |
| `FileTransferConfig`     | Connection + security settings (protocol, host, creds, timeouts) |
| `TransferProtocol`       | Enum (FTP, FTPS_Explicit, FTPS_Implicit, SFTP)                   |
| `FileTransferItem`       | Describes a single upload/download intent                        |
| `FileTransferResult`     | Result for a single transfer (success / error)                   |
| `FileOperationResult`    | Result for non?transfer ops (delete etc.)                        |
| `FileTransferProgress`   | Progress snapshot for multi-file transfers                       |
| `FileOperationProgress`  | Progress snapshot for bulk operations                            |
| `RemoteFileInfo`         | Metadata for remote listing entries                              |
| `FileTransferException`  | Domain exception wrapping underlying failures                    |

### 📚 Related Modules
| Module                                      | Description                                                                   |
|---------------------------------------------|-------------------------------------------------------------------------------|
| [🏠 Home](../readme.md)                     | Main toolkit documentation                                                    |
| [Core](../Core/readme.md)                   | Embedded resource extraction (WinSCP)                                         |
| [Configuration](../Configuration/readme.md) | Store encrypted credentials / endpoints                                       |
| [Security](../Security/readme.md)           | Encrypt stored passwords before loading config                                |
| [Logging](../Logging/readme.md)             | Capture transfer diagnostics                                                  |
| [Utils](../Utils/readme.md)                 | Timing, path helpers & formatting                                             |
