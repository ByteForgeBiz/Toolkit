param(
    [Parameter(Mandatory = $true)]
    [string]$InstanceName,

    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

function Get-SqlCmdPath {
    $command = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw "Unable to locate sqlcmd on the GitHub Actions Windows runner."
}

function Invoke-LocalDbSqlCmd {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $sqlcmdPath = Get-SqlCmdPath
    & $sqlcmdPath -S "(localdb)\$InstanceName" -E -I @Arguments
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE." }
}

function Ensure-LocalDbStarted {
    $sqlLocalDb = Get-Command sqllocaldb -ErrorAction SilentlyContinue
    if ($null -eq $sqlLocalDb) {
        throw "Unable to locate sqllocaldb on the GitHub Actions Windows runner."
    }

    & $sqlLocalDb.Source info $InstanceName *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "LocalDB instance '$InstanceName' was not found."
    }

    & $sqlLocalDb.Source start $InstanceName
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start LocalDB instance '$InstanceName'."
    }
}

function Wait-ForSqlServer {
    $maxAttempts = 60
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            Invoke-LocalDbSqlCmd -Arguments @("-Q", "SELECT 1")
            Write-Host "LocalDB is ready."
            return
        }
        catch {
            Write-Host "Waiting for LocalDB to become ready (attempt $attempt/$maxAttempts)..."
            Start-Sleep -Seconds 2
        }
    }

    throw "LocalDB instance '$InstanceName' did not become ready in time."
}

$createScript = Join-Path $RepoRoot "Toolkit.Modern.Tests\TODO\01_CreateTestUnitDB.sql"
$seedScript = Join-Path $RepoRoot "Toolkit.Modern.Tests\TODO\02_SeedTestData.sql"

if (!(Test-Path $createScript)) {
    throw "Create script not found: $createScript"
}

if (!(Test-Path $seedScript)) {
    throw "Seed script not found: $seedScript"
}

Ensure-LocalDbStarted
Wait-ForSqlServer

Invoke-LocalDbSqlCmd -Arguments @("-b", "-i", $createScript)
Invoke-LocalDbSqlCmd -Arguments @("-b", "-i", $seedScript)

Write-Host "TestUnitDB schema and seed data created successfully in LocalDB."
