# Namespace Analysis Script for ByteForge Toolkit
# Analyzes all C# files to verify namespace matches folder structure (one level deep)

param(
    [string]$BasePath = "C:\Users\pauls\source\ByteForge\Toolkit\Toolkit.Modern"
)

# Rule: If file is in folder 'xpto/subfolder/file.cs', namespace should be 'ByteForge.Toolkit.xpto'
# Base folder is Toolkit.Modern (so files directly in Toolkit.Modern should be 'ByteForge.Toolkit')

$CorrectFiles = @()
$IncorrectFiles = @()

# Get all C# source files, excluding generated/obj files
$CsFiles = Get-ChildItem -Path $BasePath -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notmatch "\\obj\\" -and 
    $_.FullName -notmatch "AssemblyInfo\.cs$" -and
    $_.FullName -notmatch "AssemblyAttributes\.cs$" -and
    $_.FullName -notmatch "GlobalUsings\.g\.cs$"
}

Write-Host "Analyzing $($CsFiles.Count) C# source files..." -ForegroundColor Green
Write-Host "Base path: $BasePath" -ForegroundColor Yellow
Write-Host ""

foreach ($file in $CsFiles) {
    # Get relative path from Toolkit.Modern
    $relativePath = $file.FullName.Substring($BasePath.Length + 1)
    
    # Determine expected namespace based on folder structure (one level deep)
    $pathParts = $relativePath.Split('\')
    
    if ($pathParts.Length -eq 1) {
        # File directly in Toolkit.Modern
        $expectedNamespace = "ByteForge.Toolkit"
    } else {
        # File in subfolder - use first folder level only
        $firstFolder = $pathParts[0]
        $expectedNamespace = "ByteForge.Toolkit.$firstFolder"
    }
    
    # Read file and extract namespace
    try {
        $content = Get-Content -Path $file.FullName -Raw
        
        # Find namespace declaration
        if ($content -match "namespace\s+([\w\.]+)") {
            $actualNamespace = $matches[1]
            
            # Check if namespace matches expected
            if ($actualNamespace -eq $expectedNamespace) {
                $CorrectFiles += [PSCustomObject]@{
                    File = $relativePath
                    ActualNamespace = $actualNamespace
                    ExpectedNamespace = $expectedNamespace
                    Status = "Correct"
                }
            } else {
                $IncorrectFiles += [PSCustomObject]@{
                    File = $relativePath
                    ActualNamespace = $actualNamespace
                    ExpectedNamespace = $expectedNamespace
                    Status = "Incorrect"
                }
            }
        } else {
            # No namespace found
            $IncorrectFiles += [PSCustomObject]@{
                File = $relativePath
                ActualNamespace = "No namespace found"
                ExpectedNamespace = $expectedNamespace
                Status = "Missing Namespace"
            }
        }
    } catch {
        Write-Warning "Error reading file: $($file.FullName) - $($_.Exception.Message)"
    }
}

# Generate report
Write-Host "=== NAMESPACE ANALYSIS REPORT ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total files analyzed: $($CsFiles.Count)" -ForegroundColor White
Write-Host "Files with correct namespaces: $($CorrectFiles.Count)" -ForegroundColor Green
Write-Host "Files with incorrect namespaces: $($IncorrectFiles.Count)" -ForegroundColor Red
Write-Host ""

if ($CorrectFiles.Count -gt 0) {
    Write-Host "=== FILES WITH CORRECT NAMESPACES ===" -ForegroundColor Green
    $CorrectFiles | Sort-Object File | Format-Table -Property File, ActualNamespace -AutoSize
    Write-Host ""
}

if ($IncorrectFiles.Count -gt 0) {
    Write-Host "=== FILES WITH INCORRECT NAMESPACES ===" -ForegroundColor Red
    $IncorrectFiles | Sort-Object File | Format-Table -Property File, ActualNamespace, ExpectedNamespace, Status -AutoSize
    Write-Host ""
    
    Write-Host "=== NAMESPACE CORRECTIONS NEEDED ===" -ForegroundColor Yellow
    foreach ($incorrectFile in $IncorrectFiles | Sort-Object File) {
        Write-Host "File: $($incorrectFile.File)" -ForegroundColor White
        Write-Host "  Current:  $($incorrectFile.ActualNamespace)" -ForegroundColor Red
        Write-Host "  Expected: $($incorrectFile.ExpectedNamespace)" -ForegroundColor Green
        Write-Host ""
    }
}

# Summary by folder
Write-Host "=== SUMMARY BY FOLDER ===" -ForegroundColor Cyan
$folderSummary = @{}

foreach ($file in $CorrectFiles + $IncorrectFiles) {
    $pathParts = $file.File.Split('\')
    $folder = if ($pathParts.Length -eq 1) { "Root" } else { $pathParts[0] }
    
    if (-not $folderSummary.ContainsKey($folder)) {
        $folderSummary[$folder] = @{ Correct = 0; Incorrect = 0; Total = 0 }
    }
    
    $folderSummary[$folder].Total++
    if ($file.Status -eq "Correct") {
        $folderSummary[$folder].Correct++
    } else {
        $folderSummary[$folder].Incorrect++
    }
}

$folderSummary.GetEnumerator() | Sort-Object Name | ForEach-Object {
    $folder = $_.Key
    $stats = $_.Value
    $percentage = [math]::Round(($stats.Correct / $stats.Total) * 100, 1)
    
    Write-Host "$folder`: $($stats.Correct)/$($stats.Total) correct ($percentage%)" -ForegroundColor $(if ($percentage -eq 100) { "Green" } else { "Yellow" })
}