# Enums

This folder contains all enumerations, constants, and exception types used throughout the WinSCP wrapper.

---

## Enumerations

| Type | Description |
|------|-------------|
| `Protocol` | Transfer protocol selection: `Sftp`, `Scp`, `Ftp`, `Webdav`, `S3`. |
| `FtpMode` | FTP connection mode: `Passive` (default) or `Active`. |
| `FtpSecure` | FTP security mode: `None`, `Implicit` (FTPS), `Explicit` (FTPES). |
| `SshHostKeyPolicy` | How to handle the remote SSH host key: `Check` (verify against fingerprint) or `GiveUpSecurityAndAcceptAny`. |
| `TransferMode` | File transfer encoding: `Binary`, `Ascii`, `Automatic`. |
| `OverwriteMode` | Behavior when the destination file exists: `Overwrite`, `Resume`, `Append`. |
| `SynchronizationMode` | Direction for `SynchronizeDirectories`: `Local`, `Remote`, `Both`. |
| `SynchronizationCriteria` | How files are compared during synchronization: `None`, `Time`, `Size`, `Either`. |
| `SynchronizationAction` | Action identified during directory comparison: `UploadNew`, `DownloadNew`, `UploadUpdate`, `DownloadUpdate`, `DeleteRemote`, `DeleteLocal`. |
| `EnumerationOptions` | Flags controlling `EnumerateRemoteFiles`: `None`, `AllDirectories`, `MatchDirectories`, `EnumerateDirectories`. |
| `ProgressOperation` | The type of operation reported in a progress event: `Transfer`. |
| `ProgressSide` | Which side (local or remote) a progress event refers to. |
| `TransferResumeSupportState` | Resume support state: `Default`, `On`, `Off`, `Smart`. |
| `LogReadFlags` | Flags for XML log readers: `None`, `ThrowFailures`. |
| `JobObjectInfoType` | Win32 job object information class (used internally for `SetInformationJobObject`). |
| `RegistryFlags` | Flags for `RegGetValue` P/Invoke calls. |
| `RegistryType` | Registry value type identifiers for `RegGetValue` P/Invoke calls. |
| `FileMapAccess` | Access flags for `MapViewOfFile`: `FileMapCopy`, `FileMapWrite`, `FileMapRead`, `FileMapAllAccess`. |
| `FileMapProtection` | Protection flags for `CreateFileMapping`: `PageReadWrite`, etc. |
| `FileMappingRights` | Rights for file-mapping named objects used in security descriptors. |
| `DesktopRights` | Win32 desktop access-rights flags used when granting cross-user process launch access. |
| `WindowStationRights` | Win32 window-station access-rights flags. |
| `StandardRights` | Standard Win32 object access rights (Delete, ReadControl, etc.). |

---

## Exception Types

| Type | Base | Description |
|------|------|-------------|
| `SessionException` | `Exception` | Base class for all WinSCP session exceptions. |
| `SessionLocalException` | `SessionException` | Thrown for errors that originate locally in the .NET wrapper (e.g., executable not found, version mismatch, IPC failure). Carries a reference to the `Session` that produced the error. |
| `SessionRemoteException` | `SessionException` | Thrown for errors reported by WinSCP in its XML log (e.g., remote file not found, permission denied). Contains the error message parsed from the log. |
| `StdOutException` | `SessionException` | Thrown when unexpected output is received on stdout from the WinSCP process. |

---

## Constants

| Type | Description |
|------|-------------|
| `Constants` | Internal numeric constants used throughout the library (e.g., default timeouts, buffer sizes). |
| `AssemblyConstants` | Assembly-level string constants (e.g., version, product name). |

---

## `TransferResumeSupport`

`TransferResumeSupport` is a companion class (not a pure enum) that configures resume behavior for file transfers. It is referenced from `TransferOptions`.

| Property | Description |
|----------|-------------|
| `State` | `TransferResumeSupportState` value controlling when resume is attempted. |
| `Threshold` | File size threshold (in bytes) above which resume is attempted when `State` is `Smart`. |
