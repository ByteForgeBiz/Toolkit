# Console

This folder implements the low-level inter-process communication (IPC) layer that connects the .NET wrapper to the `winscp.exe` child process.

WinSCP communicates with the .NET wrapper through a **shared-memory channel**: a named Win32 file-mapping object is created, and the two processes exchange event records through it. The .NET side launches WinSCP with `--console` flags, then runs a background thread that polls for incoming console events and dispatches them.

---

## How the IPC Works

1. `ExeSessionProcess` creates a set of named kernel objects:
   - A **file-mapping** (`WinSCPConsoleMapping{id}`) — the shared-memory region.
   - A **request event** (`WinSCPConsoleEventRequest{id}`) — WinSCP signals this when it has placed an event in shared memory.
   - A **response event** (`WinSCPConsoleEventResponse{id}`) — the wrapper signals this after it processes the event.
   - A **cancel event** (`WinSCPConsoleEventCancel{id}`) — used to abort a pending operation.

2. A background thread in `ExeSessionProcess` loops on the request event, reads the `ConsoleCommStruct` from shared memory, dispatches to the appropriate handler, then signals the response event.

3. The shared memory layout is: `ConsoleCommHeader` (12 bytes) followed by the largest possible payload struct.

---

## Key Types

| Type | Description |
|------|-------------|
| `ExeSessionProcess` | Manages the `winscp.exe` child process lifecycle; resolves the executable path, validates its version, creates IPC objects, pumps console events, and routes input/output. Implements `IDisposable`. |
| `ConsoleCommStruct` | Managed view over the shared-memory region. Maps the file into the current process on construction, provides typed access to the current event payload via strongly-typed properties, and writes changes back to shared memory on disposal. |
| `ConsoleCommHeader` | The 12-byte fixed header at the start of shared memory: size, protocol version, and current event type. |
| `ConsoleEvent` | Enum identifying the event stored in shared memory: `Print`, `Input`, `Choice`, `Title`, `Init`, `Progress`, `TransferOut`, `TransferIn`. |
| `ConsolePrintEventStruct` | Payload for a print (output text) event. |
| `ConsoleInputEventStruct` | Payload for an input (read from stdin) event. |
| `ConsoleChoiceEventStruct` | Payload for a user-choice prompt event. |
| `ConsoleInitEventStruct` | Payload for session initialization/capability negotiation. |
| `ConsoleTitleEventStruct` | Payload for a console-title-change event (not applied; logged only). |
| `ConsoleProgressEventStruct` | Payload for file-transfer progress updates. |
| `ConsoleTransferEventStruct` | Payload for raw data transfer (stdin/stdout pipe) events. |
| `SessionLogReader` | Incrementally reads the WinSCP XML session log from disk, blocking with exponential back-off until new XML nodes appear. Handles timeouts, failure elements, and file reopening. |

---

## Executable Discovery

`ExeSessionProcess.FindExecutable` searches for `winscp.exe` in the following locations, in order:

1. Directory of the WinSCP .NET wrapper assembly.
2. Directory of the application entry-point assembly.
3. Directory of the current process executable.
4. WinSCP installation path from `HKCU\Software\...\winscp3_is1`.
5. WinSCP installation path from `HKLM\Software\...\winscp3_is1`.
6. Default installation path (`%ProgramFiles(x86)%\WinSCP` on 64-bit systems).

The located executable's product version is checked against the wrapper assembly's version; a mismatch throws `SessionLocalException` unless `Session.DisableVersionCheck` is set.

---

## Security Context

When `Session.ExecutableProcessUserName` is configured, `ExeSessionProcess` grants that user account access to the current window station and desktop before launching `winscp.exe` under that account. Named kernel objects for IPC also receive an explicit DACL for the target user.
