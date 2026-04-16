# Core Module

## Overview

The **Core** module provides a single entry point for ensuring that embedded third-party binaries shipped inside the assembly are extracted to disk and kept up to date. Currently the only managed dependency is **WinSCP.exe**, required by the WinSCP integration in the Net module.

---

## Purpose

Embedding `WinSCP.exe` inside the assembly simplifies deployment — there are no separate installer steps and no dependency on a machine-wide WinSCP installation. The Core module handles:

1. **Presence check** — verify the file exists next to the entry assembly.
2. **Integrity check** — compare the on-disk file's SHA-256 hash to the embedded resource's hash.
3. **Extraction** — write the embedded resource to disk using a safe temp-file-then-move pattern if the file is absent or outdated.
4. **Cleanup** — optionally delete extracted files (used in tests and deployment scenarios that manage their own lifecycle).

---

## Key Types

### `Core` (public static partial class)

The public API surface of the module.

| Member | Description |
|---|---|
| `EnsureWinSCP()` | Ensures `WinSCP.exe` is present and current next to the entry assembly. Extracts from embedded resources if the file is missing or its checksum does not match the embedded version. |

**Usage:**

```csharp
// Call once at application startup, before any WinSCP session is opened
Core.EnsureWinSCP();

// WinSCP is now available alongside the executing assembly
using var session = new WinSCP.Session();
session.Open(options);
```

`EnsureWinSCP()` is idempotent: on subsequent calls when the file is already present and up to date it returns almost immediately (only the checksum comparison is performed).

---

### `WinScpResourceManager` (private nested class)

Internal to `Core`. Manages all filesystem and reflection operations.

| Method | Description |
|---|---|
| `EnsureWinScpFilesAvailable()` | Entry point called by `Core.EnsureWinSCP()`. Delegates to `EnsureFileAvailable`. |
| `AreWinScpFilesAvailable()` | Returns `true` if `WinSCP.exe` exists and its checksum matches the embedded resource. Used in testing. |
| `CleanupWinScpFiles()` | Deletes `WinSCP.exe` from the execution directory, ignoring any errors. Used in cleanup scenarios. |

Extraction uses a write-then-move pattern to avoid leaving a partially written binary if the process is interrupted:

1. Stream the embedded resource to `WinSCP.exe.tmp`.
2. Delete any existing `WinSCP.exe`.
3. Move `WinSCP.exe.tmp` to `WinSCP.exe`.

---

## Embedded Resources

The assembly embeds `WinSCP.exe` as the resource `ByteForge.Toolkit.Dependencies.WinSCP.exe`.

The target extraction directory is the folder containing the entry assembly (`Assembly.GetEntryAssembly()`, falling back to `GetCallingAssembly()` and `GetExecutingAssembly()`).

---

## Checksum Validation

SHA-256 is used to compare the on-disk file against the embedded resource. If either checksum cannot be computed (e.g., the resource is missing or an I/O error occurs), the comparison returns `false` and extraction is performed.

---

## Error Handling

| Scenario | Behaviour |
|---|---|
| Embedded resource not found | `FileNotFoundException` thrown from `CalculateResourceChecksum` or `ExtractResourceToFile`. |
| Extraction fails (I/O, permissions) | `InvalidOperationException` wrapping the original exception, thrown from `EnsureFileAvailable`. |
| Cleanup fails | Errors are silently ignored in `CleanupWinScpFiles`. |
| Checksum comparison fails | Treated as "file out of date"; extraction is attempted. |

---

## File Organisation

| File | Contents |
|---|---|
| `Core.cs` | `Core` partial class with the public `EnsureWinSCP()` method. |
| `Core.WinScpResourceManager.cs` | `Core` partial class with the private `WinScpResourceManager` nested class. |

---

## Related Modules

| Module | Description |
|---|---|
| **[CommandLine](../CommandLine/readme.md)** | Command-line parsing |
| **[Configuration](../Configuration/readme.md)** | INI-based configuration |
| **[Net](../Net/readme.md)** | WinSCP-based file transfer (consumes `EnsureWinSCP`) |
