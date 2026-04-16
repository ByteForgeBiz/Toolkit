# Utilities

This folder contains internal helper types that support the rest of the WinSCP wrapper: diagnostic logging, thread synchronization, stream abstractions, and remote path utilities. Most types here are `internal`; `RemotePath` and `Lock` are the only `public` types.

---

## Types

### `Logger`

Internal diagnostic logger for the WinSCP wrapper itself (separate from WinSCP's own session log). Writes timestamped, thread-indented messages to a text file when `Session.DebugLogPath` is set.

| Feature | Description |
|---------|-------------|
| `LogPath` | Path to the debug log file. When set, logging is enabled; when cleared, the file is closed. |
| `LogLevel` | Verbosity level (-1 to 2). Higher levels include more detail, such as GC stats and memory counters. |
| `Logging` | `true` when a log file is open. |
| `WriteLine(...)` | Writes a formatted message at level 0. |
| `WriteLineLevel(int, ...)` | Writes a message only when `LogLevel >= level`. |
| `WriteException(Exception)` | Logs an exception with its stack trace and returns it for convenient `throw`. |
| `CreateCallstack(object token)` | Returns a `Callstack` that writes "entering" and "leaving" messages for the calling method. |
| `WriteCounters()` | Logs performance counter values (CPU, memory). |
| `GetAssemblyFilePath()` | Returns the path of the WinSCP .NET wrapper assembly. |

---

### `Callstack`

Implements call-flow tracing via the `IDisposable` pattern. On construction it resolves the calling method name from the stack and writes "entering <method>" to the log. On `Dispose` it writes "leaving <method>". Used throughout the library with `using (_logger.CreateCallstack()) { ... }`.

---

### `CallstackAndLock`

Extends `Callstack` to also hold a `Lock`. Enters the lock on construction and releases it on `Dispose`. Supports `DisarmLock()` to transfer lock ownership away from the `using` block.

---

### `Lock`

A non-reentrant mutual-exclusion lock that wraps `Monitor`. Throws `InvalidOperationException` if the same thread attempts to enter twice, making recursive locking bugs detectable at runtime. Public type.

| Method | Description |
|--------|-------------|
| `Enter()` | Acquires the lock; throws if already held by the current thread. |
| `Exit()` | Releases the lock. |

---

### `RemotePath`

Static helper class for manipulating remote (POSIX-style) file paths. Public and `ComVisible`.

| Method | Description |
|--------|-------------|
| `Combine(path1, path2)` | Joins two remote paths with `/`. If `path2` is absolute, it is returned as-is. |
| `GetDirectoryName(path)` | Returns the parent directory portion of a remote path. |
| `GetFileName(path)` | Returns the file-name portion after the last `/`. |
| `AddDirectorySeparator(path)` | Appends `/` if not already present. |
| `EscapeFileMask(fileMask)` | Escapes WinSCP file-mask special characters (`[`, `*`, `?`, `<`, `>`). |
| `EscapeOperationMask(mask)` | Escapes operation-mask special characters (`\`, `*`, `?`). |
| `TranslateRemotePathToLocal(remotePath, remoteRoot, localRoot)` | Maps a remote path to a local path by substituting root prefixes and replacing `/` with `\`. |
| `TranslateLocalPathToRemote(localPath, localRoot, remoteRoot)` | Inverse of the above. |

Also implements `IReflect` to support COM late-binding clients.

---

### `PipeStream`

An in-memory, thread-safe pipe stream with one writer and one reader. Used to connect the WinSCP stdout byte stream to the consumer when transferring file data. The writer calls `WriteInternal` to enqueue bytes; the reader calls `Read` which blocks until data arrives. `CloseWrite` signals end-of-stream to the reader.

---

### `PatientFileStream`

A read-only `Stream` wrapper over a `FileStream` that blocks (with `Session.DispatchEvents`) when no data is available, rather than returning 0 immediately. Used by `SessionLogReader` to read the WinSCP XML log file while WinSCP is still writing to it. Retries with exponential back-off until data arrives or the session times out.

---

### `ProgressHandler`

A small `IDisposable` scope guard. On `Dispose` it calls `Session.DisableProgressHandling()`, ensuring that file-transfer progress callbacks are cleanly unregistered when the operation completes or throws.

---

### `Tools`

A collection of static internal utility methods.

| Method | Description |
|--------|-------------|
| `TimeSpanToMilliseconds(TimeSpan)` | Converts a `TimeSpan` to an `int` (milliseconds); throws on overflow. |
| `MillisecondsToTimeSpan(int)` | Converts an integer millisecond count to a `TimeSpan`. |
| `ArgumentEscape(string)` | Doubles any `"` characters in a string for safe embedding in WinSCP command arguments. |
| `AddRawParameters(ref string, Dictionary, string, bool)` | Appends a dictionary of name/value pairs to a command-line argument string using a named switch (e.g., `-rawtransfersettings`). |
| `LengthTo32Bit(long)` | Clamps a 64-bit file length to an `int` for COM interop via `RemoteFileInfo.Length32`. |

---

### `GenericSecurity` (also in Security/)

Provides `NativeObjectSecurity` operations for raw kernel object handles. Defined in `Utilities/` but related to the Security concerns described in `Security/readme.md`.
