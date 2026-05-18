# DBAccess Legacy Parity Audit

Date: 2026-05-18

## Sources Compared

- Legacy source of truth: `C:\Users\pauls\source\TelecomInc\ByteForge.Toolkit\Data\Database`
- MainProjects mirror: `C:\Users\pauls\source\TelecomInc\MainProjects\ByteForge.Toolkit\Data\Database`
- Modern source: `Toolkit.Modern\Data\Database`

## Findings

- `DBAccess.DataBaseType.AzureSQL` existed in the older legacy toolkit but was missing from the modern toolkit.
- The older legacy toolkit treats `AzureSQL` as a SQL client database through `IsSqlClientDatabase`.
- `CreateConnection()` and `CreateDataAdapter()` should route `SQLServer` and `AzureSQL` through `SqlConnection` / `SqlDataAdapter`.
- Parameter parsing should treat `AzureSQL` like `SQLServer`, allowing named parameter reuse and stored procedure assignment syntax.
- Extended-property APIs should allow `AzureSQL` because Azure SQL supports the SQL Server extended-property procedures used by `DBAccess`.
- `BulkDbProcessor<T>` should allow `AzureSQL` because it uses the SQL client provider and `SqlBulkCopy`.
- `DatabaseOptions.GetConnectionString()` should support `AzureSQL` directly when no explicit `sConnectionString` override is provided.
- `net48` should use `System.Data.SqlClient`; `net8.0` and `net9.0` should use `Microsoft.Data.SqlClient`.

## Intentional Differences

- `DBAccess2` remains intentionally excluded from the modern toolkit.
- Namespace and layout modernization remains intentional: modern code uses `ByteForge.Toolkit.Data` under `Toolkit.Modern\Data\Database`.
- Modern nullable annotations, collection expressions, and XML documentation improvements remain intentional where they do not change the public compatibility surface.

## Verification Checklist

- Search for direct `DataBaseType.SQLServer` branches and decide whether they should use `IsSqlClientDatabase`.
- Search for `Microsoft.Data.SqlClient` in shared database code and verify it is behind `#if !NET48`.
- Search for `sType` documentation and examples to ensure `AzureSQL` is listed with `SQLServer` and `ODBC`.
- Run both `net48` and `net9.0` database test slices after DBAccess changes.
