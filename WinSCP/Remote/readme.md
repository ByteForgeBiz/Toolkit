# Remote

This folder contains the model types that represent remote file system entries returned by WinSCP directory listing operations.

---

## Types

### `RemoteFileInfo`

Represents a single file or directory entry on the remote server. Returned from `Session.ListDirectory`, `Session.GetFileInfo`, and `Session.EnumerateRemoteFiles`.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | File or directory name (without path). |
| `FullName` | `string` | Full remote path. |
| `FileType` | `char` | Unix file-type character: `'d'` for directory, `'-'` for regular file, `'l'` for symlink, etc. |
| `Length` | `long` | File size in bytes. |
| `Length32` | `int` | File size clamped to a 32-bit integer (for COM interop). |
| `LastWriteTime` | `DateTime` | Last modification time. |
| `FilePermissions` | `FilePermissions` | Unix permission bits (see `Security/`). |
| `Owner` | `string` | File owner name. |
| `Group` | `string` | File group name. |
| `IsDirectory` | `bool` | `true` when `FileType` is `'D'` (case-insensitive). |
| `IsThisDirectory` | `bool` | `true` when the entry represents `.` (current directory). |
| `IsParentDirectory` | `bool` | `true` when the entry represents `..` (parent directory). |

`RemoteFileInfo` is `ComVisible` with a registered GUID and `ClassInterfaceType.AutoDispatch`. Instances are created internally by `Session` when parsing the XML log.

---

### `RemoteDirectoryInfo`

Represents the contents of a remote directory. Returned directly from `Session.ListDirectory`.

| Property | Type | Description |
|----------|------|-------------|
| `Files` | `RemoteFileInfoCollection` | All file and directory entries in the listing, including `.` and `..`. |

`RemoteDirectoryInfo` is `ComVisible`. Entries are added internally via `AddFile(RemoteFileInfo)`.

---

### `RemoteFileInfoCollection`

A read-only, COM-visible, enumerable collection of `RemoteFileInfo` objects. Inherits from `ReadOnlyInteropCollection<RemoteFileInfo>` (defined in `Results/`), which provides safe internal mutation methods while exposing a read-only interface externally.

---

## Notes

- All properties on `RemoteFileInfo` use `internal set`, so they can only be set during XML log parsing within the library.
- The `IsDirectory` check is case-insensitive (`'D'` or `'d'` both match) to handle edge cases in some server implementations.
- When listing a directory, WinSCP typically includes `.` and `..` entries. Callers should use `IsThisDirectory` and `IsParentDirectory` to filter these out when iterating.
