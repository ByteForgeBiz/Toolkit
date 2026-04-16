# Transfer

This folder contains `TransferOptions`, the configuration type passed to file transfer and synchronization operations on `Session`.

---

## `TransferOptions`

`TransferOptions` controls how individual file transfers are performed. Pass an instance to methods such as `Session.GetFiles`, `Session.PutFiles`, `Session.SynchronizeDirectories`, and `ComparisonDifference.Resolve`. When `null` is passed, WinSCP uses its defaults (binary mode, preserve timestamp, overwrite).

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TransferMode` | `TransferMode` | `Binary` | `Binary` (byte-for-byte copy), `Ascii` (newline conversion), or `Automatic` (let WinSCP decide based on file extension). |
| `PreserveTimestamp` | `bool` | `true` | When `true`, the modification time of the source file is applied to the transferred file. |
| `FilePermissions` | `FilePermissions` | `null` | Unix permissions to set on the transferred file. `null` means use the server default (typically inherited). |
| `FileMask` | `string` | `null` | A WinSCP file mask (e.g., `*.txt`, `*.txt; *.csv`) to filter which files are included in the transfer. |
| `ResumeSupport` | `TransferResumeSupport` | Default state | Controls whether interrupted transfers can be resumed. See `TransferResumeSupportState` in `Enums/`. |
| `SpeedLimit` | `int` | `0` | Maximum transfer speed in KB/s. `0` means unlimited. |
| `OverwriteMode` | `OverwriteMode` | `Overwrite` | What to do when the destination file already exists: `Overwrite`, `Resume` (continue from where it stopped), or `Append`. |
| `RawSettings` | `Dictionary<string, string>` | (empty) | Advanced transfer settings passed directly to WinSCP via `-rawtransfersettings`. |

### Methods

| Method | Description |
|--------|-------------|
| `AddRawSettings(string, string)` | Adds an entry to `RawSettings` for low-level WinSCP parameter overrides. |

### `ToSwitches()` (internal)

Converts the configured options into a WinSCP scripting command-line switch string. For example:

```
-permissions=755 -preservetime -transfer=binary -filemask="*.txt"
```

This string is appended to `get`, `put`, `synchronize`, and related scripting commands before they are sent to `winscp.exe`.

---

## `TransferResumeSupport`

A companion class (defined in `Enums/`) that pairs with `TransferOptions.ResumeSupport`.

| Property | Description |
|----------|-------------|
| `State` | `TransferResumeSupportState`: `Default` (use WinSCP setting), `On` (always resume), `Off` (never resume), `Smart` (resume only for files above `Threshold`). |
| `Threshold` | File size in bytes above which `Smart` resume applies. |

`ToString()` serializes the state into the format expected by WinSCP's `-resumesupport` switch.
