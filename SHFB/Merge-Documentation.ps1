param(
    [string]$BuildXmlPath = "..\Toolkit.Modern\bin\ByteForge.Toolkit.Modern.xml",
    [string]$ExamplesFolder = "xml-docs",
    [string]$OutputPath = "merged-docs\ByteForge.Toolkit.Modern.merged.xml"
)

Write-Host "=== ByteForge Documentation Merger ===" -ForegroundColor Green
Write-Host "Merging XML documentation with examples..." -ForegroundColor Yellow

# Ensure output directory exists
$outputDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "Created output directory: $outputDir" -ForegroundColor Cyan
}

# Load the main XML documentation file
if (-not (Test-Path $BuildXmlPath)) {
    Write-Error "Build XML file not found: $BuildXmlPath"
    exit 1
}

Write-Host "Loading main documentation: $BuildXmlPath" -ForegroundColor Cyan
[xml]$mainDoc = Get-Content $BuildXmlPath

# Get all example XML files
$exampleFiles = Get-ChildItem -Path $ExamplesFolder -Filter "*.Examples.xml"
Write-Host "Found $($exampleFiles.Count) example files:" -ForegroundColor Cyan
foreach ($file in $exampleFiles) {
    Write-Host "  - $($file.Name)" -ForegroundColor Gray
}

# Create a hashtable to store examples by member name
$examplesByMember = @{}

# Process each example file
foreach ($exampleFile in $exampleFiles) {
    Write-Host "Processing: $($exampleFile.Name)" -ForegroundColor Yellow
    
    try {
        [xml]$exampleDoc = Get-Content $exampleFile.FullName
        
        # Extract examples from each member
        foreach ($member in $exampleDoc.doc.members.member) {
            $memberName = $member.name
            
            if ($memberName) {
                if (-not $examplesByMember.ContainsKey($memberName)) {
                    $examplesByMember[$memberName] = @()
                }
                
                # Extract all example elements
                foreach ($example in $member.example) {
                    $examplesByMember[$memberName] += $example
                }
                
                Write-Host "    Added examples for: $memberName" -ForegroundColor Gray
            }
        }
    }
    catch {
        Write-Warning "Failed to process $($exampleFile.Name): $($_.Exception.Message)"
    }
}

Write-Host "Collected examples for $($examplesByMember.Keys.Count) members" -ForegroundColor Cyan

# Merge examples into the main documentation
$mergedCount = 0
foreach ($member in $mainDoc.doc.members.member) {
    $memberName = $member.name
    
    if ($examplesByMember.ContainsKey($memberName)) {
        Write-Host "Merging examples for: $memberName" -ForegroundColor Green
        
        # Add each example to the member
        foreach ($example in $examplesByMember[$memberName]) {
            # Import the example node into the main document
            $importedExample = $mainDoc.ImportNode($example, $true)
            $member.AppendChild($importedExample) | Out-Null
        }
        
        $mergedCount++
    }
}

# Add examples for members that don't exist in the main doc (namespace-level examples, etc.)
foreach ($memberName in $examplesByMember.Keys) {
    $existingMember = $mainDoc.doc.members.member | Where-Object { $_.name -eq $memberName }
    
    if (-not $existingMember) {
        Write-Host "Adding new member with examples: $memberName" -ForegroundColor Magenta
        
        # Create new member element
        $newMember = $mainDoc.CreateElement("member")
        $newMember.SetAttribute("name", $memberName)
        
        # Add all examples
        foreach ($example in $examplesByMember[$memberName]) {
            $importedExample = $mainDoc.ImportNode($example, $true)
            $newMember.AppendChild($importedExample) | Out-Null
        }
        
        # Add to members collection
        $mainDoc.doc.members.AppendChild($newMember) | Out-Null
        $mergedCount++
    }
}

Write-Host "Successfully merged examples for $mergedCount members" -ForegroundColor Green

# Save the merged documentation
Write-Host "Saving merged documentation to: $OutputPath" -ForegroundColor Cyan

# Create XML writer settings for proper formatting
$writerSettings = New-Object System.Xml.XmlWriterSettings
$writerSettings.Indent = $true
$writerSettings.IndentChars = "    "
$writerSettings.NewLineChars = "`r`n"
$writerSettings.Encoding = [System.Text.Encoding]::UTF8

# Write the merged document
$writer = [System.Xml.XmlWriter]::Create($OutputPath, $writerSettings)
try {
    $mainDoc.Save($writer)
}
finally {
    $writer.Close()
}

Write-Host "=== Merge Complete ===" -ForegroundColor Green
Write-Host "Output file: $OutputPath" -ForegroundColor Cyan
Write-Host "Merged documentation ready for SHFB!" -ForegroundColor Yellow

# Display statistics
$totalMembers = $mainDoc.doc.members.member.Count
$membersWithExamples = ($mainDoc.doc.members.member | Where-Object { $_.example }).Count

Write-Host "`nStatistics:" -ForegroundColor White
Write-Host "  Total members: $totalMembers" -ForegroundColor Gray
Write-Host "  Members with examples: $membersWithExamples" -ForegroundColor Gray
Write-Host "  Example coverage: $([math]::Round(($membersWithExamples / $totalMembers) * 100, 1))%" -ForegroundColor Gray

# Verification
if (Test-Path $OutputPath) {
    $outputSize = (Get-Item $OutputPath).Length
    Write-Host "  Output file size: $([math]::Round($outputSize / 1KB, 1)) KB" -ForegroundColor Gray
    
    # Quick validation - check if examples exist
    [xml]$mergedDoc = Get-Content $OutputPath
    $membersWithExamples = $mergedDoc.doc.members.member | Where-Object { $_.example }
    $exampleCount = ($membersWithExamples | Measure-Object).Count
    Write-Host "  Members with examples in merged file: $exampleCount" -ForegroundColor Gray
    
    Write-Host "`n✅ Merge completed successfully!" -ForegroundColor Green
    Write-Host "You can now use the merged file as input to SHFB instead of separate files." -ForegroundColor Yellow
} else {
    Write-Error "❌ Merge failed - output file not created"
    exit 1
}