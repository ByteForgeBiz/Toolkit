# WinSCP

This project is a .NET wrapper for WinSCP, providing a managed API for file transfer operations over SFTP, FTP, etc.

It targets .NET Framework 4.8, .NET 8.0, and .NET 9.0.

## Key Components

- `Enums`: Various enumerations for transfer modes, synchronization, etc.
- `Events`: Event argument classes and handlers for session and transfer events.
- `Jobs`: Classes related to job management.
- `Logging`: Custom log readers for WinSCP operations.
- `Native`: Unsafe native methods for low-level interactions.
- `Properties`: Assembly information.
- `Remote`: Classes for remote file and directory information.
- `Results`: Result classes for operations like transfers and synchronizations.
- `Security`: Security-related utilities and permissions.
- `Transfer`: Options for file transfers.
- `Utilities`: Helper classes for logging, paths, streams, etc.

This library enables .NET applications to integrate WinSCP's powerful file transfer capabilities.

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                                         | Description                        |
|----------------------------------------------------------------|------------------------------------|
| **[CLI](../Toolkit.Modern/CommandLine/readme.md)**             | Command-line parsing               |
| **[Configuration](../Toolkit.Modern/Configuration/readme.md)** | INI-based configuration            |
| **[Core](../Toolkit.Modern/Core/readme.md)**                   | Core utilities & WinSCP management |
| **[Data](../Toolkit.Modern/Data/readme.md)**                   | Database & file processing         |
| **[Net](../Toolkit.Modern/Net/readme.md)**                     | Network file transfers             |
| **[Security](../Toolkit.Modern/Security/readme.md)**           | Encryption & security              |
