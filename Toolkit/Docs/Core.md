# Core Module

## ByteForge.Toolkit Core Services

Foundational helpers for the toolkit – currently focused on embedded dependency deployment (WinSCP runtime) used by the `Net` module for cross?protocol file transfer.

### 🛠️ Purpose
Some modules (notably `Net`) rely on the WinSCP engine (`WinSCP.exe`, `WinSCPnet.dll`). To simplify distribution, these binaries are embedded as managed resources and extracted on demand to the application directory only when required or when the on-disk version differs (hash mismatch).

### 🧱 Key Components
| Type                               | Purpose                                                             |
|------------------------------------|---------------------------------------------------------------------|
| `Core` (static partial)            | Public fa?ade exposing helper methods                               |
| `WinScpResourceManager` (internal) | Handles extraction, verification, cleanup of embedded WinSCP assets |

### 🔒 Integrity & Update Strategy
- SHA-256 checksum comparison before writing – prevents unnecessary I/O
- Safe write pattern: resource ? temp file ? atomic replace
- Silent fallback: any checksum/comparison failure triggers re?extract
- Explicit cleanup helper available (internal currently)

### 🔌 Public API
```csharp
// Ensure WinSCP assets exist (idempotent)
Core.EnsureWinSCP();
```
This is automatically invoked by higher-level components (e.g., the first use inside `FileTransferClient`). Call it early at startup if you need deterministic availability.

### 🔄 Internal Flow
1. Resolve execution directory (entry assembly preferred)
2. For each embedded asset (`WinSCP.exe`, `WinSCPnet.dll`):
   - If file exists ? compute SHA-256 ? compare with embedded checksum ? skip if equal
   - Else extract resource to `*.tmp`, then move/replace
3. Continue silently if a stale file cannot be deleted (best-effort deployment)

### 🧪 Suggested Tests
| Scenario           | Expectation                          |
|--------------------|--------------------------------------|
| First extraction   | Both files created successfully      |
| Re-run extraction  | No overwrite if hashes match         |
| Corrupt local file | Replaced with valid embedded version |
| Missing one asset  | Only missing file extracted          |

### 📝 Operational Notes
- Works in single-instance and multi-process scenarios (atomic move pattern reduces race risk)
- Not intended for trimming/publishing scenarios that remove embedded resources
- If you relocate binaries post-startup, call `EnsureWinSCP()` again

### 📚 Related Modules
| Module                    | Usage                                                                   |
|---------------------------|-------------------------------------------------------------------------|
| [Net](./Net.md)           | Consumes extracted WinSCP binaries                                      |
| [Security](./Security.md) | Can encrypt credentials used by Net module                              |
| [Logging](./Logging.md)   | Log extraction lifecycle (if wrapped externally)                        |

### 🚀 Roadmap Ideas
- Optional signature verification (Authenticode)
- Pluggable extraction target path
- Versioned resource naming to support side-by-side upgrades
- Extraction diagnostics hook (event callbacks)

---
*Minimal, deterministic dependency deployment powering higher-level transfer operations.*

