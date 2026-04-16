# Events

This folder defines the event argument types, delegate types, and the COM-visible session events interface that `Session` exposes for callbacks during file transfer operations.

---

## Event Flow

When WinSCP reports progress or operation outcomes through its XML log or IPC channel, `Session` raises corresponding .NET events. Consumers subscribe to these events on the `Session` object before calling `Open`.

```
WinSCP XML log / IPC
        |
   Session parses
        |
   Raises event ──► Event handler delegate ──► Consumer code
        |                                       receives EventArgs
```

---

## Interface

| Type | Description |
|------|-------------|
| `ISessionEvents` | COM-visible dispatch interface (`IDispatch`) declaring all session events for COM clients. Dispatch IDs: `FileTransferred` (1), `Failed` (2), `OutputDataReceived` (3), `FileTransferProgress` (4), `QueryReceived` (5). |

---

## Delegate Types

| Type | Signature | Description |
|------|-----------|-------------|
| `FileTransferredEventHandler` | `(object, TransferEventArgs)` | Raised after each file transfer completes (or fails). |
| `FailedEventHandler` | `(object, FailedEventArgs)` | Raised when a session-level failure occurs. |
| `OutputDataReceivedEventHandler` | `(object, OutputDataReceivedEventArgs)` | Raised for each line of output (stdout/stderr) from WinSCP. |
| `FileTransferProgressEventHandler` | `(object, FileTransferProgressEventArgs)` | Raised periodically with transfer speed and progress information. |
| `QueryReceivedEventHandler` | `(object, QueryReceivedEventArgs)` | Raised when WinSCP presents a yes/no/cancel choice to the user. |

---

## Event Argument Types

### Hierarchy

```
EventArgs
  FileOperationEventArgs      (base for file operations)
    TransferEventArgs         (upload / download completed)
    ChmodEventArgs            (chmod applied to transferred file)
    RemovalEventArgs          (file removed as part of move/sync)
    TouchEventArgs            (timestamp set on transferred file)
  OperationEventArgs          (base for non-file operations)
  FailedEventArgs             (session failure)
  FileTransferProgressEventArgs  (in-progress transfer metrics)
  OutputDataReceivedEventArgs    (stdout/stderr line)
  QueryReceivedEventArgs         (interactive choice prompt)
```

### Type Details

| Type | Key Members | Description |
|------|-------------|-------------|
| `FileOperationEventArgs` | `FileName`, `Error` | Base class for events tied to a specific file. `Error` is non-null when the operation failed. |
| `TransferEventArgs` | `Side`, `Destination`, `Length`, `Touch`, `Chmod`, `Removal` | Raised when a file upload or download completes. `Side` indicates local or remote. Optionally contains nested `TouchEventArgs`, `ChmodEventArgs`, or `RemovalEventArgs`. |
| `ChmodEventArgs` | `FileName`, `FilePermissions` | Raised when file permissions are set on a transferred file. |
| `RemovalEventArgs` | `FileName`, `Error` | Raised when a file is removed as part of a move or synchronization delete. |
| `RemovalEventArgsCollection` | (collection of `RemovalEventArgs`) | Typed collection returned in `SynchronizationResult.Removals`. |
| `TouchEventArgs` | `FileName`, `LastWriteTime` | Raised when a file's timestamp is set on the remote server. |
| `TransferEventArgsCollection` | (collection of `TransferEventArgs`) | Typed collection returned in `TransferOperationResult.Transfers` and `SynchronizationResult.Uploads` / `.Downloads`. |
| `OperationEventArgs` | (base only) | Base class for non-file operation events. |
| `FailedEventArgs` | `Error` | Carries the `SessionRemoteException` from a session failure. |
| `FileTransferProgressEventArgs` | `Operation`, `Side`, `FileName`, `Directory`, `OverallProgress`, `FileProgress`, `CPS`, `Cancel` | Provides real-time transfer metrics. Set `Cancel = true` to abort the current transfer. `CPS` is characters (bytes) per second. |
| `OutputDataReceivedEventArgs` | `Data`, `Error` | A single line of output from WinSCP. `Error` is `true` for stderr lines. |
| `QueryReceivedEventArgs` | `Message`, `SelectedAction` | WinSCP is presenting a choice. Set `SelectedAction` to `Continue` or `Abort`; leave as `None` to use the timed default. |
