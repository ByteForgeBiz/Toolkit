# Results

This folder contains the strongly-typed result objects returned by `Session` operations, the exception type used to represent remote errors, and collection base classes used throughout the library.

---

## Result Type Hierarchy

```
OperationResultBase
  TransferOperationResult     (GetFiles / PutFiles)
  RemovalOperationResult      (RemoveFiles)
  SynchronizationResult       (SynchronizeDirectories)
  CommandExecutionResult      (ExecuteCommand)
```

All result types are `ComVisible` and carry a `Failures` collection of `SessionRemoteException`. After any operation, call `.Check()` to throw on the first failure, or inspect `.IsSuccess` and `.Failures` manually.

---

## Types

### `OperationResultBase`

Base class for all operation results.

| Member | Description |
|--------|-------------|
| `Failures` | `SessionRemoteExceptionCollection` — errors reported by WinSCP during the operation. |
| `IsSuccess` | `true` when `Failures.Count == 0`. |
| `Check()` | Throws `Failures[0]` (a `SessionRemoteException`) if any failure was recorded. |

---

### `TransferOperationResult`

Returned by `Session.GetFiles` and `Session.PutFiles`.

| Member | Description |
|--------|-------------|
| `Transfers` | `TransferEventArgsCollection` — one entry per transferred file, including metadata such as size, destination, and side. |

---

### `RemovalOperationResult`

Returned by `Session.RemoveFiles`.

| Member | Description |
|--------|-------------|
| `Removals` | `RemovalEventArgsCollection` — one entry per removed file. |

---

### `SynchronizationResult`

Returned by `Session.SynchronizeDirectories`.

| Member | Description |
|--------|-------------|
| `Uploads` | `TransferEventArgsCollection` — files uploaded to the remote server. |
| `Downloads` | `TransferEventArgsCollection` — files downloaded from the remote server. |
| `Removals` | `RemovalEventArgsCollection` — files deleted during synchronization. |

---

### `CommandExecutionResult`

Returned by `Session.ExecuteCommand`.

| Member | Description |
|--------|-------------|
| `Output` | Standard output from the remote command. |
| `ErrorOutput` | Standard error output from the remote command. |
| `ExitCode` | Process exit code of the remote command. |

---

### `ComparisonDifference`

Represents a single difference found by `Session.CompareDirectories`. Not a result type but closely related; it appears in `ComparisonDifferenceCollection`.

| Member | Description |
|--------|-------------|
| `Action` | `SynchronizationAction` — what would need to happen to bring the two sides into sync. |
| `IsDirectory` | Whether the difference concerns a directory entry. |
| `Local` | `ComparisonFileInfo` for the local side. |
| `Remote` | `ComparisonFileInfo` for the remote side. |
| `Resolve(Session, TransferOptions)` | Executes the required action immediately, returning a `FileOperationEventArgs` for files or `null` for directories. |
| `Reverse()` | Flips the action (e.g., `UploadNew` becomes `DeleteLocal`). |

---

### `ComparisonFileInfo`

Lightweight model holding the file name and last-write time for one side of a `ComparisonDifference`.

---

### Exception Types

| Type | Description |
|------|-------------|
| `SessionRemoteException` | Represents an error reported by WinSCP in its XML log. Contains the error message and is collected into `OperationResultBase.Failures`. Can be re-thrown from `Check()`. |
| `SessionRemoteExceptionCollection` | Typed, COM-visible read-only collection of `SessionRemoteException`. |

---

### Collection Infrastructure

| Type | Description |
|------|-------------|
| `ReadOnlyInteropCollection<T>` | Generic base class for COM-visible read-only collections. Exposes a public read-only interface while providing an `InternalAdd` method for use within the library. Used by `TransferEventArgsCollection`, `RemovalEventArgsCollection`, `RemoteFileInfoCollection`, etc. |
| `ImplicitEnumerable<T>` | Internal helper enabling implicit conversion of a single item to an enumerable. |
| `StringCollection` | Typed, COM-visible collection of strings. |
| `OperationResultGuard` | Internal helper that validates operation result state. |
