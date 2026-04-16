# WinSCP

`WinSCPnet` is a managed .NET wrapper around the WinSCP executable (`winscp.exe`) that enables .NET applications to perform file transfers and remote file operations over SFTP, SCP, FTP, FTPS, WebDAV, and S3.

The library works by launching `winscp.exe` as a child process and communicating with it through a custom shared-memory IPC protocol. It parses the structured XML session log produced by WinSCP to surface results, events, and error information back to the caller as strongly-typed .NET objects.

**Target frameworks:** .NET Framework 4.8, .NET 8.0, .NET 9.0 (Windows only)

---

## Architecture Overview

```
Your Application
      |
  Session  (Core/)
      |
  ExeSessionProcess  (Console/)
      |  shared-memory IPC (ConsoleCommStruct)
  winscp.exe
      |  XML log
  SessionLogReader  (Logging/)
```

The `Session` class is the primary public API surface. It manages the lifecycle of a `winscp.exe` child process via `ExeSessionProcess`, which handles the low-level IPC. Results are read back from the WinSCP XML log via the log-reader pipeline in `Logging/`. Windows Job objects (`Jobs/`) optionally guard the child process so it is killed if the host process exits.

---

## Folder Structure

| Folder | Purpose |
|--------|---------|
| [`Console/`](Console/readme.md) | Shared-memory IPC structs and the `ExeSessionProcess` that drives `winscp.exe` |
| [`Core/`](Core/readme.md) | `Session` and `SessionOptions` — the primary public API |
| [`Enums/`](Enums/readme.md) | All enumerations and exception types |
| [`Events/`](Events/readme.md) | Event argument classes and handler delegates for session callbacks |
| [`Jobs/`](Jobs/readme.md) | Windows Job object wrapper for child-process lifetime management |
| [`Logging/`](Logging/readme.md) | Incremental XML log readers that parse WinSCP's structured output |
| [`Native/`](Native/readme.md) | P/Invoke declarations for Windows kernel and user APIs |
| [`Remote/`](Remote/readme.md) | Models for remote file and directory information |
| [`Results/`](Results/readme.md) | Strongly-typed result objects returned from session operations |
| [`Security/`](Security/readme.md) | Unix file permissions model and Windows security ACL helpers |
| [`Transfer/`](Transfer/readme.md) | `TransferOptions` — configures how file transfers are performed |
| [`Utilities/`](Utilities/readme.md) | Internal helpers: logging, locking, streaming, path manipulation |

---

## Quick-Start Usage

```csharp
var options = new SessionOptions
{
    Protocol = Protocol.Sftp,
    HostName = "example.com",
    UserName = "user",
    Password = "secret",
    SshHostKeyFingerprint = "ssh-rsa 2048 xx:xx:...",
};

using (var session = new Session())
{
    session.Open(options);

    // Upload a file
    session.PutFiles(@"C:\local\file.txt", "/remote/dir/").Check();

    // Download a file
    session.GetFiles("/remote/dir/file.txt", @"C:\local\").Check();

    // List a directory
    RemoteDirectoryInfo dir = session.ListDirectory("/remote/dir/");
    foreach (RemoteFileInfo file in dir.Files)
        Console.WriteLine(file.Name);
}
```

---

## Key Types

| Type | Location | Description |
|------|----------|-------------|
| `Session` | `Core/Session.cs` | Main API: open, transfer, synchronize, execute commands |
| `SessionOptions` | `Core/SessionOptions.cs` | Connection parameters (host, credentials, protocol, keys) |
| `TransferOptions` | `Transfer/TransferOptions.cs` | Per-transfer settings (mode, mask, resume, permissions) |
| `RemoteFileInfo` | `Remote/RemoteFileInfo.cs` | Metadata for a single remote file or directory entry |
| `TransferOperationResult` | `Results/TransferOperationResult.cs` | Outcome of upload/download operations |
| `SynchronizationResult` | `Results/SynchronizationResult.cs` | Outcome of directory synchronization |
| `FilePermissions` | `Security/FilePermissions.cs` | Unix permission model (numeric, text, octal) |
| `RemotePath` | `Utilities/RemotePath.cs` | Static helpers for remote path manipulation |

---

## Related Modules

| Module | Description |
|--------|-------------|
| **[CLI](../Toolkit.Modern/CommandLine/readme.md)** | Command-line parsing |
| **[Configuration](../Toolkit.Modern/Configuration/readme.md)** | INI-based configuration |
| **[Core](../Toolkit.Modern/Core/readme.md)** | Core utilities and WinSCP management |
| **[Data](../Toolkit.Modern/Data/readme.md)** | Database and file processing |
| **[Net](../Toolkit.Modern/Net/readme.md)** | Network file transfers |
| **[Security](../Toolkit.Modern/Security/readme.md)** | Encryption and security |
