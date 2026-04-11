[CmdletBinding()]
param(
    [ValidateSet('merge', 'rebase')]
    [string]$Mode = 'rebase',

    [switch]$KeepTempFile
)

$ErrorActionPreference = 'Stop'

function Assert-GitSuccess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

function Test-GitStateFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return Test-Path (Join-Path $gitDir $Name)
}

$repoRoot = (Resolve-Path $PSScriptRoot).Path

git -C $repoRoot rev-parse --show-toplevel | Out-Null
Assert-GitSuccess "This script must be run from inside a git repository."

$currentBranch = (git -C $repoRoot branch --show-current).Trim()
Assert-GitSuccess "Unable to determine the current branch."

if ([string]::IsNullOrWhiteSpace($currentBranch)) {
    throw "Detached HEAD is not supported. Check out a branch first."
}

if ($currentBranch -eq 'master') {
    throw "You are already on 'master'. Switch to a feature branch before running this script."
}

$gitDir = (git -C $repoRoot rev-parse --git-dir).Trim()
Assert-GitSuccess "Unable to locate the .git directory."

if (-not [System.IO.Path]::IsPathRooted($gitDir)) {
    $gitDir = Join-Path $repoRoot $gitDir
}

$statusOutput = @(git -C $repoRoot status --porcelain --untracked-files=normal)
Assert-GitSuccess "Unable to inspect the worktree state."

if ($statusOutput.Count -gt 0) {
    throw "Working tree is not clean. Commit, stash, or remove local changes before switching branches."
}

$stateFiles = @(
    'MERGE_HEAD',
    'CHERRY_PICK_HEAD',
    'REVERT_HEAD',
    'BISECT_LOG',
    'rebase-apply',
    'rebase-merge'
)

foreach ($stateFile in $stateFiles) {
    if (Test-GitStateFile $stateFile) {
        throw "Repository has an in-progress git operation ('$stateFile'). Finish or abort it before running this script."
    }
}

$tempBat = Join-Path ([System.IO.Path]::GetTempPath()) ("update-from-master-{0}.bat" -f ([guid]::NewGuid().ToString('N')))
$tempBatEscaped = $tempBat.Replace('"', '""')
$repoRootEscaped = $repoRoot.Replace('"', '""')
$currentBranchEscaped = $currentBranch.Replace('"', '""')

$batchContent = @"
@echo off
setlocal

set "REPO_ROOT=$repoRootEscaped"
set "ORIGINAL_BRANCH=$currentBranchEscaped"
set "MODE=$Mode"

cd /d "%REPO_ROOT%"
if errorlevel 1 goto :fail

call :run git checkout master
call :run git pull --ff-only origin master
call :run git checkout "%ORIGINAL_BRANCH%"

if /i "%MODE%"=="rebase" (
    call :run git rebase master
) else (
    call :run git merge master
)

call :run git push --force-with-lease origin "%ORIGINAL_BRANCH%"

echo.
echo Done.
set "RESULT=0"
goto :cleanup

:run
echo.
echo ^> %*
call %*
if errorlevel 1 goto :fail
exit /b 0

:fail
echo.
echo Failed while running git commands.
set "RESULT=1"

:cleanup
"@

if (-not $KeepTempFile) {
    $batchContent += @"
del "%tempBatEscaped%" >nul 2>&1
"@
}

$batchContent += @"
exit /b %RESULT%
"@

Set-Content -LiteralPath $tempBat -Value $batchContent -Encoding ASCII

Write-Host "Original branch: $currentBranch"
Write-Host "Integration mode: $Mode"
Write-Host "Temporary runner: $tempBat"

& $tempBat
$exitCode = $LASTEXITCODE

if ($KeepTempFile) {
    Write-Host "Temporary batch file kept at: $tempBat"
}

exit $exitCode
