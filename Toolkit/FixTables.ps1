param(
    [string]$Path = "."
)

# Function to process a table block
function Process-Table {
    param([string[]]$tableLines)

    $rows = @()
    foreach ($line in $tableLines) {
        $parts = $line -split '\|'
        # Skip first and last empty parts
        $cells = $parts[1..($parts.Count - 2)] | ForEach-Object { $_.Trim() }
        $rows += ,$cells
    }

    if ($rows.Count -eq 0) { return $tableLines }

    $numCols = $rows[0].Count
    $maxLens = 0..($numCols - 1) | ForEach-Object {
        $col = $_
        ($rows | ForEach-Object { $_[$col].Length } | Measure-Object -Maximum).Maximum
    }

    $newLines = @()
    for ($r = 0; $r -lt $rows.Count; $r++) {
        if ($r -eq 1 -and $rows.Count -gt 1) {
            # Separator row
            $newCells = $maxLens | ForEach-Object { "-" * ($_ + 2) }
        } else {
            # Header or data rows
            $newCells = 0..($numCols - 1) | ForEach-Object {
                $col = $_
                " " + $rows[$r][$col].PadRight($maxLens[$col]) + " "
            }
        }
        $newLine = "|" + ($newCells -join "|") + "|"
        $newLines += $newLine
    }

    return $newLines
}

# Get all .md files recursively
$mdFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.md"

foreach ($file in $mdFiles) {
    Write-Host "Processing $($file.FullName)"
    $content = Get-Content $file.FullName -Raw
    $lines = $content -split "`n"

    $inTable = $false
    $tableLines = @()
    $newContent = @()

    foreach ($line in $lines) {
        if ($line -match '^\s*\|\s*.*\|\s*$') {
            if (!$inTable) { $inTable = $true }
            $tableLines += $line
        } else {
            if ($inTable) {
                # Process the table
                $processedTable = Process-Table $tableLines
                $newContent += $processedTable
                $tableLines = @()
                $inTable = $false
            }
            $newContent += $line
        }
    }

    if ($inTable) {
        $processedTable = Process-Table $tableLines
        $newContent += $processedTable
    }

    $newContent -join "`n" | Set-Content $file.FullName -Encoding UTF8
}

Write-Host "Table fixing completed."