param(
    [Parameter(Mandatory = $true)]
    [string]$ContainerName,

    [Parameter(Mandatory = $true)]
    [string]$SaPassword,

    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

function Get-SqlCmdPath {
    $candidates = @(
        "/opt/mssql-tools18/bin/sqlcmd",
        "/opt/mssql-tools/bin/sqlcmd"
    )

    foreach ($candidate in $candidates) {
        docker exec $ContainerName bash -lc "test -x $candidate" *> $null
        if ($LASTEXITCODE -eq 0) {
            return $candidate
        }
    }

    throw "Unable to locate sqlcmd inside container '$ContainerName'."
}

function Invoke-ContainerSqlCmd {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $sqlcmdPath = Get-SqlCmdPath
    $joinedArguments = ($Arguments | ForEach-Object { "'$_'" }) -join " "
    docker exec $ContainerName bash -lc "$sqlcmdPath -S localhost -U sa -P '$SaPassword' -C $joinedArguments"
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForSqlServer {
    $maxAttempts = 60
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            Invoke-ContainerSqlCmd -Arguments @("-Q", "SELECT 1")
            Write-Host "SQL Server is ready."
            return
        }
        catch {
            Write-Host "Waiting for SQL Server to become ready (attempt $attempt/$maxAttempts)..."
            Start-Sleep -Seconds 5
        }
    }

    throw "SQL Server container '$ContainerName' did not become ready in time."
}

$createScript = Join-Path $RepoRoot "Toolkit.Modern.Tests\TODO\01_CreateTestUnitDB.sql"
$seedScript = Join-Path $RepoRoot "Toolkit.Modern.Tests\TODO\02_SeedTestData.sql"

if (!(Test-Path $createScript)) {
    throw "Create script not found: $createScript"
}

if (!(Test-Path $seedScript)) {
    throw "Seed script not found: $seedScript"
}

Wait-ForSqlServer

docker cp $createScript "${ContainerName}:/tmp/01_CreateTestUnitDB.sql"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to copy create script into SQL Server container."
}

docker cp $seedScript "${ContainerName}:/tmp/02_SeedTestData.sql"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to copy seed script into SQL Server container."
}

Invoke-ContainerSqlCmd -Arguments @("-b", "-i", "/tmp/01_CreateTestUnitDB.sql")
Invoke-ContainerSqlCmd -Arguments @("-b", "-i", "/tmp/02_SeedTestData.sql")

Write-Host "TestUnitDB schema and seed data created successfully."
