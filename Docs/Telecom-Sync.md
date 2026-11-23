# Telecom fork sync

Date: 2026-11-22

Baseline: `ByteForgeBiz/Toolkit`, branch `master`, commit `04c3ab7`.
Source: `C:\Users\pauls\source\TelecomInc\MainProjects\ByteForge.Toolkit`.

## Scope

Mapped Telecom library code into `Toolkit.Modern`, shared HTML assets into root
`HTML`, and applicable new tests into `Toolkit.Modern.Tests`. Existing modernized
tests were retained rather than replaced with Telecom's older suite. Source-only
module documentation was added alongside the corresponding modules.

Integrated configuration reload, raw reads, in-memory collection assignment, time
zone options, database application names, command timing, local connection
transactions, execution-plan and deadlock diagnostics, SQL script safety, shared
TELCO RBAC/account/invitation/handoff services, request probe defenses, mail helper
overloads, and updated shared/application HTML assets. The imported application
assets include Praxis, Walgreens, AMCH, and Global RBAC surfaces already maintained
by the Telecom fork; they are source assets, not independently deployed apps.

Preserved modern project and assembly identities, metadata, dependency versions,
the sibling WinSCP source project, existing module namespaces, current modern tests,
and existing root documentation. Did not import Telecom root build scripts,
repository policies, changelogs, IDE state, binaries, or obsolete DBAccess2 fixtures.
No changes were made to Telecom or the sibling Config/Controls projects.

## Reconciliation

- Embedded shared account CSS, JavaScript, invitation and portal pages from root
  HTML using the imported services' expected logical resource names. Added net48
  System.Web references required by the imported account code.
- Configuration reload retains the existing configuration root and settings
  object identities. Imported tests establish that callers holding settings
  references observe refreshed scalar and collection values.
- Template newline-to-HTML conversion is available through `ReplaceNewLines`,
  but remains opt-in to preserve the modern repository's plaintext behavior.
- Adapted new fixtures to modern namespaces, the transaction assembly reference,
  timing helper signature, and friend-assembly override visibility. Updated
  connection-string assertions to account for canonicalization and application
  naming without dropping checks for the other connection settings.
- Reconciled an obsolete ClientAdmin policy fixture with the imported role-based
  policy. Full Admin roles remain prohibited; ordinary roles stay bounded by the
  actor's permission ceiling.
- Did not retain the Telecom Client Admin SQL integration fixture: it calls the
  retired per-user `SetClientAdministration` API, which the current source rejects.
  The new disposable-database security fixture described below supersedes that
  retired fixture.

## Verification

Build command (all three library targets):

```powershell
dotnet build Toolkit.Modern/ByteForge.Toolkit.Modern.csproj -c Release --disable-build-servers -p:ImportByteForgeTasks=false -p:CopyFiles=false -p:UseSharedCompilation=false -p:NuGetAudit=false
```

Unit test command (net48 and net9.0):

```powershell
dotnet test Toolkit.Modern.Tests/ByteForge.Toolkit.Modern.Tests.csproj -c Release --disable-build-servers -p:ImportByteForgeTasks=false -p:CopyFiles=false -p:UseSharedCompilation=false -p:NuGetAudit=false --filter "TestCategory=Unit&FullyQualifiedName!~DatabaseLoggerSqlServerLiveTests&TestCategory!=ODBC"
```

Legacy bidirectional build synchronization was explicitly disabled during
verification. Package vulnerability auditing was disabled for the local offline
build; this is not a security audit. The normal repository build infrastructure
was retained.

The library built for net48, net8.0, and net9.0 with zero warnings and errors.
The selected unit suite passed 616 tests on each test target. Initial broader
runs exposed SQL logging fixture setup failures; SQL logging live tests and Access
ODBC fixtures were excluded from the final run. This is not a claim that every
integration test passed. Test compilation also emits analyzer recommendations.

Four shared UI fixtures passed: company entry, date ranges (eight checks), page
loading (60 browser checks), and version footers. Playwright and headless Edge were
used for the browser fixtures. Other application-specific browser fixtures,
deployed host integrations, live transfers, and production database behavior were
not verified.

Local logs are under `Logs/telecom-merge-build.log`,
`Logs/telecom-merge-tests.log`, and `Logs/telecom-merge-browser.log`.
No commit, push, deployment, or production database changes were performed.

## Local SQL database rebuild and verification

After confirming that `(local)` had no `TestUnitDB`, rebuilt and seeded it at the
user's request with the repository's `01_CreateTestUnitDB.sql` and
`02_SeedTestData.sql`. Used `sqlcmd -E -C -I -b`; `-I` enables QUOTED_IDENTIFIER,
which the schema requires and the CI bootstrap already supplies.

Verified the initial seed: 20 TestEntities, 50 BulkTestEntities, five TestDataTypes,
and 5,000 PerformanceTestTable rows. SQL tests then ran sequentially against this
local disposable test database, avoiding cross-framework interference.

The SQL selection was
`(FullyQualifiedName~Unit.Data.Database|FullyQualifiedName~DatabaseLoggerSqlServerLiveTests)&TestCategory!=ODBC`.
All 190 selected tests passed on net48 and all 190 passed on net9.0, with no skips.
This includes CRUD, bulk processing, transactions, execution plans, schema/extended
properties, parameter parsing, and the previously failing live SQL logging tests.
Access ODBC tests and separate TELCO application-database integration are outside
this selection.

The first run revealed one remaining case-sensitive assertion that expected
`Integrated Security=true` literally. Updated it to parse the connection string
and check the server, database, and authentication properties; no production
library change was needed. Final results are in `Logs/sql-tests-net48.log`,
`Logs/sql-tests-net9.log`, and `Logs/SqlTests/sql-net48.trx` / `sql-net9.trx`.

## Priority update regression coverage

Added 22 integration cases in `DBAccessConnectionTransactionTests` and
`TelcoSecurityIntegrationTests`. Each case creates and drops its own
`ByteForgeRegression_<guid>` database on the configured test server, leaving
`TestUnitDB` and application identity databases out of the fixture's setup/cleanup.
These tests require database creation rights and do not silently skip absent SQL
infrastructure.

Connection tests verify one-session visibility, commit, disposal/exception
rollback, owner reuse, rejection of nested/mixed transaction models, a single
attempt for a timed-out command despite retry settings, and asynchronous bulk
inserts participating in the owning transaction. A nontransactional SQL sequence
counts timeout attempts so the assertion does not depend on timing estimates.

Security tests verify active project/role/privilege boundaries, privilege unions,
delegation authority and permission ceilings, protected actors/administrators,
preservation of shared identity and foreign memberships, rollback of partial role
replacement, current-password validation, exact self-service password characters,
name-only changes, atomic profile/password rollback on token invalidation failure,
invitation/reset eligibility/expiry/replacement/cooldown, and exactly one successful
password change when two requests race to consume a reset token.

The tests found a connection transaction bug: a command failure followed by a
successful read cleared `LastException`, allowing partial work to commit. The
transaction now remembers command failure independently until disposal and rolls
back even when `Complete` was requested after a later successful query. The same
test confirms a subsequent new transaction can commit normally.

The RBAC fixture intentionally models the store's required shared-schema contract;
it is not a production migration test. It uses a reduced PBKDF2 work factor only
for fixture seed identities; real password-changing APIs retain their normal
production hash settings. No email delivery, HTTP authorization, cookies, or host
session revocation is exercised. See [the RBAC overview](RBAC.md) for that boundary.

Final full-suite logs and TRX results are under `Logs/updated-suite-net48.log`,
`Logs/updated-suite-net9.log`, and `Logs/SqlTests/updated-suite-*.trx`. Runs use
`--filter "TestCategory!=ODBC"`, execute net48 and net9.0 sequentially, and disable
legacy build synchronization. Access ODBC coverage remains outside this run.

Final result: 1,019 tests passed on net48 and 1,019 on net9.0, with no failures or
skips within the non-ODBC selection, including all 22 new integration cases on
each framework. The remaining net8.0 library target also compiled with zero
warnings/errors after the rollback fix.
