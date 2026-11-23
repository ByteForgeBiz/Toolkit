# SQL Server deadlock diagnostics

Set `bEnableDeadlockTrace=true` in a database configuration section, or set
`DatabaseOptions.EnableDeadlockTrace = true`. The default is false.

After opening a SQL client connection, DBAccess checks global trace flags 1204
and 1222. Existing global tracing allows capture even for a non-sysadmin login.
Otherwise a sysadmin login enables global 1204 and verifies it. Unsupported
providers, insufficient permissions or initialization failures log a warning
and disable the option in memory; the INI file is not written. Checks are cached
per DBAccess instance. Server tracing is never disabled on disposal. Global
status is checked on a fresh, unpooled connection outside ambient transactions.
This supports SQL Server 2000's two-column `TraceFlag`/`Status` result without
confusing a session-only trace flag on an application connection with global
tracing. Modern results still require `Global=1`.
Global 1204 affects the entire SQL instance and may add overhead; use it deliberately.
Azure SQL Database may reject server tracing and error-log access; it fails open.

On SQL error 1205 (including a wrapped exception), normal command and script
execution reads the current SQL error log using a separate connection outside
the failed transaction. It uses a three-second connection/command timeout,
retains a bounded log tail and formats at most 32,768 characters plus a truncation marker. Diagnostic failures do
not replace the original exception, retry application commands or recurse into
DBAccess. This feature does not change the existing retry policy.

The report uses the original session SPID and server-local clock. SQL Server
2000 textual 1204 reports and modern 1204/1222 log rows are supported. Reports
are labeled candidates because time and SPID do not guarantee unique matching.
The current log may not contain a report yet, may have rolled over, or may
exceed the read budget. No matching report is an explicit diagnostic outcome.
Extended Events XML capture is not implemented by this option.

Formatted output is masked by LogSecretMasker, logged and attached to the
original exception under `DBAccess.DeadlockTraceDataKey`. Error reporters must
look through inner exceptions and explicitly include that value, HTML-encoding
it in HTML messages. Walgreens' error handler does this. SQL text can still
contain business data; the existing masker is not a general SQL data anonymizer.

Focused offline tests: `dotnet test Verification/DeadlockTrace/DeadlockTrace.Tests.csproj`.
These tests never enable server tracing or create production deadlocks.
