# README.md

**ByteForge.Toolkit** is a modular .NET Framework 4.8 library designed to streamline development with high-performance utilities, structured configuration management, secure encryption, logging, networking/file transfer and more. This project is organized into cohesive components, each documented individually.

## 📚 Modules

| Module | Description |
|--------|-------------|
| [CommandLine](./CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](./Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](./Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](./DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](./Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](./Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Net](./Net/readme.md) | FTP / FTPS / SFTP client with batching, progress & concurrency |
| [Security](./Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](./Utils/readme.md) | Miscellaneous helpers: parsing, timing, IO, console, templates |
| [HTML](./HTML/readme.md) | NPD UI framework (modals, calendar, utilities, theming) |
| [Core](./Core/readme.md) | Core services & embedded WinSCP resource deployment |

---

Each module can be used independently or integrated as part of a larger application. Designed with maintainability, performance, and security in mind.

---

## 🔧 Requirements

- .NET Framework 4.8
- Windows OS (some features are Windows-specific)
- Microsoft.Extensions.Configuration (for Configuration module)
- System.CommandLine NuGet package (for CLI module)
- WinSCP (embedded deployment handled automatically by Core / Net modules)

## 🧑‍💻 Author

Developed by **Paulo Santos**

---

For more information, see each module's documentation.
