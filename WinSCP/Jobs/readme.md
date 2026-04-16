# Jobs

This folder contains the Windows Job object wrapper used to guard the lifetime of the `winscp.exe` child process.

---

## Purpose

By default, `ExeSessionProcess` creates a named Windows Job object and assigns the WinSCP child process to it. The job is configured with the `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` flag, which means that when the .NET wrapper process exits (and the job handle is closed), the operating system automatically terminates all processes in the job — including `winscp.exe`. This prevents orphaned WinSCP processes if the host application crashes or is killed.

---

## Types

### `Job`

Wraps a Win32 Job object (`CreateJobObject` / `SetInformationJobObject` / `CloseHandle`).

| Member | Description |
|--------|-------------|
| `Job(Logger, string name)` | Creates a named job object and configures it with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` (`LimitFlags = 8192`). Logs a warning if creation fails but does not throw, so a missing job does not prevent the session from opening. |
| `Close()` | Closes the native job handle. Because the job was created with kill-on-close, all child processes in the job are terminated at this point. |
| `Dispose()` | Calls `Close()` and suppresses the finalizer. |

### `JobObjectBasicLimitInformation`

Mirrors the `JOBOBJECT_BASIC_LIMIT_INFORMATION` native structure. Contains per-process and per-job time limits, working set limits, active process count, CPU affinity, priority class, and scheduling class. Only `LimitFlags` is set by this library (to `8192` = kill-on-close).

### `JobObjectExtendedLimitInformation`

Mirrors the `JOBOBJECT_EXTENDED_LIMIT_INFORMATION` native structure. Extends `JobObjectBasicLimitInformation` with I/O counters (via `IOCounters` from `Security/`) and per-process and per-job memory limits. This is the structure passed to `SetInformationJobObject`.

---

## Relationship to Other Folders

- `Job` is instantiated by `ExeSessionProcess` (in `Console/`) when `Session.GuardProcessWithJobInternal` returns `true`.
- `UnsafeNativeMethods` (in `Native/`) provides the `CreateJobObject`, `SetInformationJobObject`, and `CloseHandle` P/Invoke declarations.
- `IOCounters` (in `Security/`) provides the I/O counter fields embedded in `JobObjectExtendedLimitInformation`.
