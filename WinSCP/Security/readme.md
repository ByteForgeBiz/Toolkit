# Security

This folder contains two distinct groups of types:

1. **Unix file permission model** — `FilePermissions`, used to represent and manipulate remote file permissions.
2. **Windows security helpers** — ACL and access-rights types used internally when launching `winscp.exe` under a different user account.

---

## Unix File Permissions

### `FilePermissions`

Represents a Unix permission set for a remote file. Exposed as a property of `RemoteFileInfo` and as a parameter to `TransferOptions`. Is `ComVisible`.

**Representations:**

| Property | Example | Description |
|----------|---------|-------------|
| `Numeric` | `493` | Integer (0–4095) covering the standard 9 permission bits plus `SetUid`, `SetGid`, and `Sticky`. |
| `Octal` | `"755"` | 3- or 4-digit octal string; validated on assignment. |
| `Text` | `"rwxr-xr-x"` | 9-character symbolic string; handles `s`, `S`, `t`, `T` for special bits. |

**Individual bit properties:** `UserRead`, `UserWrite`, `UserExecute`, `GroupRead`, `GroupWrite`, `GroupExecute`, `OtherRead`, `OtherWrite`, `OtherExecute`, `SetUid`, `SetGid`, `Sticky`.

Setting any bit property updates `Numeric`. Permissions obtained from the server are marked read-only (via `CreateReadOnlyFromText`); attempting to modify them throws `InvalidOperationException`.

---

## Windows Security Helpers

These types are used internally by `ExeSessionProcess` when `Session.ExecutableProcessUserName` is set to run `winscp.exe` under a different Windows account. The wrapper must grant that account access to the current process's window station and desktop before launching the child process.

### `GenericAccessRule`

Extends `System.Security.AccessControl.AccessRule`. Constructs an access rule with an explicit `accessMask` integer (rather than a typed `AccessRights` enum), allowing raw Win32 access-mask values to be used for window-station and desktop objects that have no corresponding managed enum.

### `GenericSecurity`

Extends `NativeObjectSecurity`. Provides a way to read and modify the DACL of a native kernel object (window station or desktop) identified by a raw `SafeHandle`. Used with `GenericAccessRule` to add an allow-ACE for the target user before spawning the child process.

---

## Supplementary Types

### `IOCounters`

Mirrors the `IO_COUNTERS` Win32 structure. Contains read/write operation counts and byte counts for a process or job. Embedded inside `JobObjectExtendedLimitInformation` (in `Jobs/`).

### Flag Enumerations

The following flag enumerations in this folder describe Win32 access-rights values; they are consumed by `ExeSessionProcess` and `UnsafeNativeMethods`:

| Type | Description |
|------|-------------|
| `DesktopRights` | Win32 desktop access rights (e.g., `CreateDesktop`, `ReadObjects`, `WriteObjects`). |
| `WindowStationRights` | Win32 window-station access rights (e.g., `CreateDesktop`, `EnumDesktops`). |
| `StandardRights` | Standard Win32 object rights (`Delete`, `ReadControl`, `WriteDac`, `WriteOwner`, `Synchronize`). |
| `FileMapAccess` | Access flags for `MapViewOfFile` (`FileMapCopy`, `FileMapWrite`, `FileMapRead`, `FileMapAllAccess`). |
| `FileMapProtection` | Protection flags for `CreateFileMapping`. |
| `FileMappingRights` | Combined rights used when creating named security descriptors for file-mapping objects. |
