# README.md

**ByteForge.Toolkit** is a modular .NET Framework 4.8 library designed to streamline development with high-performance utilities, structured configuration management, secure encryption, logging, and more. This project is organized into cohesive components, each documented individually.

## 📚 Modules

| Module | Description |
|--------|-------------|
| [CommandLine](./CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](./Configuration/readme.md) | INI-based configuration system with typed section support |
| [Database](./Data/Database/readme.md) | Secure database access layer with SQL Server 2000 and ODBC support |
| [CSV](./Data/CSV/readme.md) | Stream-based CSV reader with format detection and progress tracking |
| [DataStructures](./DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](./Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](./Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](./Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](./Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |

---

Each module can be used independently or integrated as part of a larger application. Designed with maintainability, performance, and security in mind.

---

## 🔧 Requirements

- .NET Framework 4.8
- Windows OS (some features are Windows-specific)
- Microsoft.Extensions.Configuration (for Configuration module)
- System.CommandLine NuGet package (for CLI module)

## 🧑‍💻 Author

Developed by **Paulo Santos**

---

For more information, see each module's documentation.
