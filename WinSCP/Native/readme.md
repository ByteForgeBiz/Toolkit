# Native

This folder contains the `UnsafeNativeMethods` class, which centralises all P/Invoke declarations used by the WinSCP wrapper. Keeping native interop in one place limits the scope of `unsafe`/`extern` declarations and makes the dependency on Windows APIs explicit.

---

## `UnsafeNativeMethods`

A `static internal` class. All methods are thin wrappers around Win32 API functions; no business logic is present here.

### Shared-Memory / File Mapping (kernel32)

| Method | Win32 API | Purpose |
|--------|-----------|---------|
| `CreateFileMapping` | `CreateFileMappingW` | Creates or opens a named shared-memory region. Used to establish the IPC channel between the wrapper and `winscp.exe`. |
| `MapViewOfFile` | `MapViewOfFile` | Maps a view of the file-mapping into the current process's address space. Returns a raw pointer used by `ConsoleCommStruct`. |
| `UnmapViewOfFile` | `UnmapViewOfFile` | Unmaps (and flushes) a previously mapped view. Called on `ConsoleCommStruct.Dispose`. |

### Handle Management (kernel32)

| Method | Win32 API | Purpose |
|--------|-----------|---------|
| `CloseHandle` | `CloseHandle` | Closes a kernel object handle (job objects, etc.). |

### Job Objects (kernel32)

| Method | Win32 API | Purpose |
|--------|-----------|---------|
| `CreateJobObject` | `CreateJobObjectW` | Creates a named Windows Job object to group the child WinSCP process. |
| `SetInformationJobObject` | `SetInformationJobObject` | Configures job limits; used to set `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`. |

### Window Station / Desktop (user32)

| Method | Win32 API | Purpose |
|--------|-----------|---------|
| `GetProcessWindowStation` | `GetProcessWindowStation` | Retrieves a handle to the current process's window station, needed when granting cross-user access before spawning WinSCP under a different user. |
| `GetThreadDesktop` | `GetThreadDesktop` | Retrieves a handle to the current thread's desktop for the same reason. |
| `GetCurrentThreadId` | `GetCurrentThreadId` | Returns the calling thread's ID, passed to `GetThreadDesktop`. |

### Registry (advapi32)

| Method | Win32 API | Purpose |
|--------|-----------|---------|
| `RegGetValue` | `RegGetValue` | Reads a registry value directly. Used as an alternative to the managed `RegistryKey` API in some discovery paths. |

---

## Related Enums

The `Enums/` folder contains the enumerations used as parameters to several of these methods:

- `FileMapAccess` — `dwDesiredAccess` for `MapViewOfFile`.
- `FileMapProtection` — `fProtect` for `CreateFileMapping`.
- `JobObjectInfoType` — `JobObjectInformationClass` for `SetInformationJobObject`.
- `RegistryFlags` / `RegistryType` — flags and type codes for `RegGetValue`.
