# Core

This folder contains the two central public types of the WinSCP wrapper: `Session` and `SessionOptions`. Together they form the primary API that application code interacts with.

---

## Types

### `Session`

`Session` is the entry point for all WinSCP operations. It is `IDisposable` and `ComVisible`.

**Responsibilities:**

- Opens and closes a WinSCP session by spawning and managing an `ExeSessionProcess`.
- Sends scripting commands to `winscp.exe` and reads results back from the XML log via `SessionLogReader` / `ElementLogReader`.
- Exposes high-level methods for file transfer, directory listing, synchronization, and remote command execution.
- Fires .NET events (`FileTransferred`, `Failed`, `OutputDataReceived`, `FileTransferProgress`, `QueryReceived`) as WinSCP progress and status information arrive.
- Implements timeout detection and reconnection logic.
- Implements `IReflect` to support COM late-binding clients.

**Key configurable properties:**

| Property | Description |
|----------|-------------|
| `ExecutablePath` | Explicit path to `winscp.exe`; auto-discovered when omitted. |
| `SessionLogPath` | Path for WinSCP's human-readable session log. |
| `DefaultConfiguration` | When `true`, passes `/ini=nul` to prevent WinSCP from loading user settings. |
| `DisableVersionCheck` | Bypasses the version-match check between the wrapper assembly and the executable. |
| `ReconnectTime` | How long to attempt reconnection after a dropped connection. |
| `GuardProcessWithJob` | When `true`, a Windows Job object ensures the child process is killed if the host exits. |
| `ExecutableProcessUserName` | Runs `winscp.exe` under a different Windows user account. |

**Core operation methods (representative):**

| Method | Description |
|--------|-------------|
| `Open(SessionOptions)` | Establishes a connection to the remote server. |
| `GetFiles` / `PutFiles` | Download or upload files using glob masks. |
| `GetFileToDirectory` / `PutFileToDirectory` | Transfer a single file into a directory. |
| `ListDirectory` | Returns a `RemoteDirectoryInfo` for a remote path. |
| `SynchronizeDirectories` | Keeps local and remote directories in sync. |
| `CompareDirectories` | Returns a `ComparisonDifferenceCollection` without transferring. |
| `ExecuteCommand` | Runs a shell command on the remote server and returns `CommandExecutionResult`. |
| `RemoveFiles` / `RemoveEntry` | Deletes remote files or directories. |
| `MoveFile` | Renames or moves a remote file. |
| `FileExists` | Checks whether a remote path exists. |
| `GetFileInfo` | Returns `RemoteFileInfo` for a single remote path. |
| `Abort` | Cancels the current operation. |

---

### `SessionOptions`

`SessionOptions` holds all parameters needed to open a session. It is `ComVisible` and validates inputs on assignment.

| Property | Description |
|----------|-------------|
| `Protocol` | `Protocol` enum: `Sftp`, `Scp`, `Ftp`, `Webdav`, `S3`. Setting `S3` auto-sets the default hostname. |
| `HostName` | Hostname or IP of the remote server. |
| `PortNumber` | Port (0–65535); 0 uses the protocol default. |
| `UserName` | Authentication username. |
| `Password` / `SecurePassword` | Authentication password; stored internally as `SecureString`. |
| `NewPassword` / `SecureNewPassword` | Used for password-change authentication. |
| `Timeout` | Connection/operation timeout; must be a positive `TimeSpan` (default 15 s). |
| `SshHostKeyFingerprint` | Expected SSH host key fingerprint(s); validated by regex against known formats (MD5, SHA-256). |
| `SshHostKeyPolicy` | `SshHostKeyPolicy` enum: `Check`, `GiveUpSecurityAndAcceptAny`. |
| `SshPrivateKeyPath` / `SshPrivateKey` | SSH private key file path or inline key. |
| `PrivateKeyPassphrase` / `SecurePrivateKeyPassphrase` | Passphrase for the private key; stored as `SecureString`. |
| `FtpMode` | `FtpMode` enum: `Passive` or `Active`. |
| `FtpSecure` | `FtpSecure` enum: `None`, `Implicit`, `Explicit`. |
| `TlsHostCertificateFingerprint` | Expected TLS certificate fingerprint(s); validated by regex. |
| `GiveUpSecurityAndAcceptAnyTlsHostCertificate` | Disables TLS certificate verification. |
| `TlsClientCertificatePath` | Path to a TLS client certificate file. |
| `Secure` | Enables HTTPS/FTPS/DAVS/S3 for WebDAV and S3 protocols. |
| `RootPath` | Root path for WebDAV and S3 (must start with `/`). |
| `RawSettings` | Dictionary of advanced WinSCP settings passed via `/rawconfig`. |

**`ParseUrl(string url)`** populates all connection properties from a single URL in the format:

```
protocol://[user[:password]@]host[:port][/rootpath][;fingerprint=...][;x-name=...]
```

---

### `ISessionProcess`

Internal interface abstracting the child process (`ExeSessionProcess`). Defines `HasExited`, `ExitCode`, `Start()`, `ExecuteCommand()`, `Close()`, and the `OutputDataReceived` event. Used to enable unit testing with mock implementations.
