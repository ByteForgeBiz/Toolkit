# Migrating From The Legacy .NET Framework Toolkit

This repository is the modernized ByteForge Toolkit line. It is being brought
back to feature parity with the legacy `.NET Framework 4.8` toolkit at:

`C:\Users\pauls\source\TelecomInc\MainProjects\ByteForge.Toolkit`

The goal is drop-in compatibility where practical, while preserving the modern
project's existing folder and namespace style.

## Layout And Namespace Differences

Some legacy folders were renamed during the modernization. Keep using the modern
namespaces in new code.

| Legacy location | Modern location | Modern namespace |
| --- | --- | --- |
| `CLI` | `Toolkit.Modern/CommandLine` | `ByteForge.Toolkit.CommandLine` |
| `Utils` | `Toolkit.Modern/Utilities` | `ByteForge.Toolkit.Utilities` |
| `Converters` | `Toolkit.Modern/Utilities` or `Toolkit.Modern/Data/Database` | Existing modern namespace for the type |
| `Attributes` | `Toolkit.Modern/Data/Attributes` for data mapping attributes | `ByteForge.Toolkit.Data` |
| `Configuration` | `Toolkit.Modern/Configuration` | `ByteForge.Toolkit.Configuration` |
| `HTML` | `HTML` | Static CSS and JavaScript assets |

Legacy assembly metadata attributes are an exception: they remain in
`ByteForge.Toolkit` so assembly info files can continue to use the short
attribute names with `using ByteForge.Toolkit`.

## DBAccess2

`DBAccess2` was started in the legacy toolkit but was abandoned before becoming
functional. It is intentionally not ported to the modern toolkit.

Compatibility notes:

- `DBAccess` remains the supported database access implementation.
- `DBAccess.DataBaseType.AzureSQL` remains source-compatible with the legacy
  toolkit and is treated as part of the SQL client provider family, not ODBC.
- `DatabaseAccessFactory` exists for callers that used the legacy factory shape.
- The `useDBAccess2` factory flag is accepted for source compatibility but is
  ignored.
- `CreateModern(DatabaseOptions)` remains only as an obsolete compatibility shim
  and returns `DBAccess`.
- Do not migrate callers to `DBAccess2`; migrate them to `DBAccess` or
  `DatabaseAccessFactory.Create(...)`.

Provider notes:

- `net48` builds use `System.Data.SqlClient`, matching the legacy production
  surface and avoiding unnecessary `Microsoft.Data.SqlClient` dependency
  deployment for .NET Framework web applications.
- `net8.0` and `net9.0` builds use `Microsoft.Data.SqlClient`.
- `AzureSQL` uses the SQL client path for connection creation, data adapters,
  parameter reuse, extended properties, and bulk operations.

## Current Compatibility Additions

The modern toolkit includes these legacy-parity additions:

- Configuration documentation support types such as
  `ConfigDocumentationCatalog`, `ConfigSectionSchema`, and
  `ConfigOptionsProviderAttribute`.
- Logging context helpers such as `LogContext`, `LogSecretMasker`, and
  `LogRoutingContext`.
- `IDatabaseAccess` plus cancellation-token overloads on `DBAccess`.
- `DBAccess.DataBaseType.AzureSQL` parity with the legacy toolkit.
- `ReportTimestampFormatter` under `ByteForge.Toolkit.Utilities`.
- Assembly metadata attributes under `ByteForge.Toolkit`.
- Static HTML support assets under the repo-level `HTML` folder, with the
  companion overview in `Docs/HTML.md`.
