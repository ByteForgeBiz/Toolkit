# ByteForge Toolkit

A modular, enterprise-grade .NET library providing utilities for configuration management, database access, CLI parsing, security, data processing, file transfer, logging, and more.

---

## Solution Overview

`ByteForge.Toolkit.sln` contains four C# projects targeting multiple .NET frameworks from a single codebase.

```
ByteForge.Toolkit.sln
├── Toolkit.Modern/          # ByteForge.Toolkit.Modern  — core library (net48; net8.0; net9.0)
├── WinSCP/                  # WinSCPnet                 — SFTP/FTP wrapper (net48-windows; net8.0-windows; net9.0-windows)
├── Toolkit.Modern.Tests/    # ByteForge.Toolkit.Modern.Tests — test suite (net48; net9.0)
└── TestBed/                 # TestBed                   — scratch console app (net9.0-windows)
```

### Project summaries

| Project | Assembly | Frameworks | Role |
|---------|----------|------------|------|
| `Toolkit.Modern` | `ByteForge.Toolkit.Modern` | net48, net8.0, net9.0 | Primary library — all modules live here |
| `WinSCP` | `WinSCPnet` | net48-windows, net8.0-windows, net9.0-windows | .NET wrapper for WinSCP (SFTP/FTP/FTPS/SCP) |
| `Toolkit.Modern.Tests` | `ByteForge.Toolkit.Modern.Tests` | net48, net9.0 | MSTest unit and integration tests |
| `TestBed` | `TestBed` | net9.0-windows | Ephemeral console app for manual experimentation |

`Toolkit.Modern` references `WinSCPnet` and also embeds `WinSCP.exe` as an `EmbeddedResource` for self-contained deployment.

---

## Build Requirements

- **Visual Studio 2022 or 2026** (MSBuild is resolved automatically by the build script)
- **.NET SDK 9.0+** (for net8.0/net9.0 targets)
- **.NET Framework 4.8** developer pack (for the net48 target)
- **PowerShell** (used internally by the build script)

---

## Building the Solution

Use `BuildSolution.bat` from the repo root. It auto-detects the Visual Studio installation via `vswhere`, runs NuGet restore, and builds all projects.

```bat
:: Debug (default)
BuildSolution.bat

:: Release
BuildSolution.bat Release
```

The script:
1. Cleans `obj\` and `bin\<cfg>\` directories before building.
2. Searches for MSBuild via `vswhere`, then falls back through VS 2026/2022/2019/2017 and the .NET Framework SDK.
3. Runs MSBuild restore followed by an AnyCPU build.
4. Writes a build log to `Logs\Build\ByteForge.Toolkit.log`.

If you need a manual build (no script):

```powershell
dotnet restore
dotnet build --configuration Debug
```

---

## Running Tests

The test project targets both `net48` and `net9.0`. Build the solution first, then:

```powershell
# Run all tests (no rebuild)
dotnet test "Toolkit.Modern.Tests\ByteForge.Toolkit.Modern.Tests.csproj" --no-build

# Run with code coverage (OpenCover is included as a NuGet package)
cd Toolkit.Modern.Tests
packages\OpenCover.4.7.1221\tools\OpenCover.Console.exe `
    -target:"dotnet.exe" `
    -targetargs:"test ByteForge.Toolkit.Modern.Tests.csproj --no-build --configuration Debug" `
    -output:coverage.xml -register:user `
    -filter:"+[ByteForge.Toolkit*]*"

# Generate HTML report
packages\ReportGenerator.5.4.18\tools\net47\ReportGenerator.exe `
    -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html
```

Test categories:

| Category | Location |
|----------|----------|
| CLI | `Unit\CLI\` |
| Configuration | `Unit\Configuration\` |
| Data (Audio, CSV, Database) | `Unit\Data\` |
| DataStructures | `Unit\DataStructures\` |
| Logging | `Unit\Logging\` |
| Mail | `Unit\Mail\` |
| Security | `Unit\Security\` |
| Utils | `Unit\Utils\` |

ODBC tests (Access `.mdb`/`.accdb`) require the Microsoft Access ODBC driver and are separated from the default test run.

---

## Solution Structure

```
.
├── Toolkit.Modern/               # Core library source (see Toolkit.Modern/readme.md)
│   ├── CommandLine/              # CLI parsing
│   ├── Configuration/            # INI configuration
│   ├── Core/                     # Core utilities and WinSCP resource manager
│   ├── Data/                     # CSV, database, audio, attributes
│   ├── DataStructures/           # BinarySearchTree, URL utilities
│   ├── Dependencies/             # Embedded WinSCP.exe
│   ├── Json/                     # Delta serialization
│   ├── Logging/                  # Logging system
│   ├── Mail/                     # Email/attachment processing
│   ├── Net/                      # File transfer client
│   ├── Properties/               # AssemblyInfo.cs
│   ├── Security/                 # AES encryption
│   ├── Utilities/                # General-purpose helpers
│   └── ByteForge.Toolkit.Modern.csproj
├── WinSCP/                       # WinSCPnet wrapper library
│   └── WinSCPnet.csproj
├── Toolkit.Modern.Tests/         # Test suite
│   ├── Unit/                     # Unit tests organised by module
│   ├── Helpers/                  # Shared test helpers
│   ├── Models/                   # Test data models
│   ├── TestData/                 # Access databases, large dummy files
│   └── ByteForge.Toolkit.Modern.Tests.csproj
├── TestBed/                      # Manual experimentation console app
│   └── TestBed.csproj
├── .github/
│   ├── scripts/                  # bootstrap-test-sql.ps1
│   └── workflows/                # ci.yml
├── BuildSolution.bat             # Primary build script
├── ByteForge.Tasks.targets       # Custom MSBuild targets (bidirectional sync, versioning)
└── ByteForge.Toolkit.sln
```

---

## Custom Build System — ByteForge.Tasks.targets

`ByteForge.Tasks.targets` is an optional MSBuild import that adds enterprise-grade build automation. It is only active when a `CentralLocationPath` property is set and the targets file is reachable from the project directory.

Key capabilities:

| Phase | Target | Description |
|-------|--------|-------------|
| 1 — Pre-build | `SyncFromCentral` | Copies files from a central shared location into the project (newer-wins). |
| 2 — Post-build | `SyncToCentral` | Pushes changed project files back to the central location. |
| 3 — Cleanup | `SafeCleanupCentral` | Removes obsolete files from the central location that are no longer in the project. |
| 4 — Versioning | `UpdateSmartVersion` | Increments `Minor` for new files, `Patch` for modified files, and writes the version to `version.txt` and `AssemblyInfo.cs`. |
| 5 — Housekeeping | `ForceCleanupFiles` | Deletes leftover artefacts from the previous ROBOCOPY-based build system. |

The bidirectional sync uses a custom inline Roslyn task (`BidirectionalSyncTask`) and only copies a file when the source timestamp is newer than the target. The versioning format is `Major.Minor.Patch.MMdd`.

---

## Dependencies

### Toolkit.Modern

| Package | Version | Purpose |
|---------|---------|---------|
| `Costura.Fody` / `Fody` | 6.0.0 / 6.9.3 | Embeds dependencies into the output assembly |
| `Microsoft.Extensions.Configuration` + extensions | 9.0.10 | INI configuration provider |
| `Newtonsoft.Json` | 13.0.4 | JSON serialization |
| `RestSharp` | 112.1.0 | HTTP client |
| `System.CommandLine` | 2.0.0-beta4 | CLI parsing (net8.0/net9.0 only) |
| `System.CommandLine.NamingConventionBinder` | 2.0.0-beta4 | CLI option binding |
| `System.Data.SqlClient` | 4.9.0 | SQL Server connectivity |
| `System.Data.Odbc` | 9.0.10 | ODBC connectivity |
| `System.Text.Json` | 9.0.10 | High-performance JSON |
| Various BCL back-ports | — | `System.Memory`, `System.Buffers`, `System.IO.Pipelines`, `System.ValueTuple`, etc. |

### WinSCPnet

| Package | Version | Purpose |
|---------|---------|---------|
| `System.Diagnostics.PerformanceCounter` | 9.0.10 | Performance monitoring |
| `System.Threading.AccessControl` | 9.0.10 | Thread synchronisation primitives |

### Toolkit.Modern.Tests

| Package | Version | Purpose |
|---------|---------|---------|
| `MSTest` | 4.0.1 | Test framework |
| `Microsoft.NET.Test.Sdk` | 18.0.0 | Test runner |
| `AwesomeAssertions` | 9.3.0 | Fluent assertion library |
| `OpenCover` | 4.7.1221 | Code coverage instrumentation |
| `ReportGenerator` | 5.4.18 | HTML coverage reports |
| `coverlet.collector` | 6.0.4 | Alternative coverage collector |

---

## Build Artefacts

| Configuration | Output location |
|---------------|----------------|
| Debug | `Toolkit.Modern\bin\Debug\net48\`, `\net8.0\`, `\net9.0\` |
| Release | `Toolkit.Modern\bin\Release\net48\`, `\net8.0\`, `\net9.0\` |
| API docs | `Toolkit.Modern\bin\ByteForge.Toolkit.Modern.xml` |
| Build logs | `Logs\Build\ByteForge.Toolkit.log` |
| Test results | `Toolkit.Modern.Tests\TestResults\` |

---

## Module Documentation

| Module | readme |
|--------|--------|
| Core library | [Toolkit.Modern/readme.md](Toolkit.Modern/readme.md) |
| WinSCP wrapper | [WinSCP/readme.md](WinSCP/readme.md) |
| CommandLine | [Toolkit.Modern/CommandLine/readme.md](Toolkit.Modern/CommandLine/readme.md) |
| Configuration | [Toolkit.Modern/Configuration/readme.md](Toolkit.Modern/Configuration/readme.md) |
| Core | [Toolkit.Modern/Core/readme.md](Toolkit.Modern/Core/readme.md) |
| Data | [Toolkit.Modern/Data/readme.md](Toolkit.Modern/Data/readme.md) |
| DataStructures | [Toolkit.Modern/DataStructures/readme.md](Toolkit.Modern/DataStructures/readme.md) |
| Json | [Toolkit.Modern/Json/readme.md](Toolkit.Modern/Json/readme.md) |
| Logging | [Toolkit.Modern/Logging/readme.md](Toolkit.Modern/Logging/readme.md) |
| Mail | [Toolkit.Modern/Mail/readme.md](Toolkit.Modern/Mail/readme.md) |
| Net | [Toolkit.Modern/Net/readme.md](Toolkit.Modern/Net/readme.md) |
| Security | [Toolkit.Modern/Security/readme.md](Toolkit.Modern/Security/readme.md) |
| Utilities | [Toolkit.Modern/Utilities/readme.md](Toolkit.Modern/Utilities/readme.md) |
