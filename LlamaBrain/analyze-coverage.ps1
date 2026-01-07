# Parse Cobertura coverage XML and identify files needing tests
param(
    [string]$CoverageFile = "TestResults\afd2b2a0-db91-4d89-bb62-32ec8dd62f7f\coverage.cobertura.xml"
)

[xml]$coverage = Get-Content $CoverageFile

$fileData = @{}

foreach ($class in $coverage.coverage.packages.package.classes.class) {
    $lineRate = [double]$class.'line-rate'
    $branchRate = [double]$class.'branch-rate'
    $complexity = [int]$class.complexity
    $filename = $class.filename
    
    # Count total lines and covered lines
    $totalLines = 0
    $coveredLines = 0
    $totalBranches = 0
    $coveredBranches = 0
    
    foreach ($method in $class.methods.method) {
        foreach ($line in $method.lines.line) {
            $totalLines++
            if ([int]$line.hits -gt 0) {
                $coveredLines++
            }
            if ($line.branch -eq "True") {
                $totalBranches++
                if ([int]$line.hits -gt 0) {
                    $coveredBranches++
                }
            }
        }
    }
    
    # Aggregate by filename (handle multiple classes per file)
    if (-not $fileData.ContainsKey($filename)) {
        $fileData[$filename] = @{
            TotalLines = 0
            CoveredLines = 0
            TotalComplexity = 0
            TotalBranches = 0
            CoveredBranches = 0
            Classes = 0
        }
    }
    
    $fileData[$filename].TotalLines += $totalLines
    $fileData[$filename].CoveredLines += $coveredLines
    $fileData[$filename].TotalComplexity += $complexity
    $fileData[$filename].TotalBranches += $totalBranches
    $fileData[$filename].CoveredBranches += $coveredBranches
    $fileData[$filename].Classes++
}

# Calculate coverage percentages for each file
$files = @()
foreach ($file in $fileData.Keys) {
    $data = $fileData[$file]
    $lineCoverage = if ($data.TotalLines -gt 0) { 
        [math]::Round(($data.CoveredLines / $data.TotalLines) * 100, 2) 
    } else { 0 }
    $branchCoverage = if ($data.TotalBranches -gt 0) { 
        [math]::Round(($data.CoveredBranches / $data.TotalBranches) * 100, 2) 
    } else { 0 }
    
    $files += [PSCustomObject]@{
        File = $file
        LineCoverage = $lineCoverage
        BranchCoverage = $branchCoverage
        Complexity = $data.TotalComplexity
        TotalLines = $data.TotalLines
        CoveredLines = $data.CoveredLines
        Classes = $data.Classes
    }
}

# Sort by line coverage (lowest first)
$files = $files | Sort-Object LineCoverage

Write-Host "`n=== FILES WITH NO COVERAGE (0%) ===" -ForegroundColor Red
$noCoverage = $files | Where-Object { $_.LineCoverage -eq 0 }
if ($noCoverage) {
    $noCoverage | Format-Table -AutoSize
    Write-Host "Total: $($noCoverage.Count) files" -ForegroundColor Red
} else {
    Write-Host "None" -ForegroundColor Green
}

Write-Host "`n=== FILES WITH LOW COVERAGE (< 50%) ===" -ForegroundColor Yellow
$lowCoverage = $files | Where-Object { $_.LineCoverage -gt 0 -and $_.LineCoverage -lt 50 }
if ($lowCoverage) {
    $lowCoverage | Format-Table -AutoSize
    Write-Host "Total: $($lowCoverage.Count) files" -ForegroundColor Yellow
} else {
    Write-Host "None" -ForegroundColor Green
}

Write-Host "`n=== FILES WITH MEDIUM COVERAGE (50-80%) ===" -ForegroundColor Cyan
$mediumCoverage = $files | Where-Object { $_.LineCoverage -ge 50 -and $_.LineCoverage -lt 80 }
if ($mediumCoverage) {
    $mediumCoverage | Format-Table -AutoSize
    Write-Host "Total: $($mediumCoverage.Count) files" -ForegroundColor Cyan
} else {
    Write-Host "None" -ForegroundColor Green
}

Write-Host "`n=== SUMMARY ===" -ForegroundColor White
Write-Host "Total files analyzed: $($files.Count)"
Write-Host "Files with 0% coverage: $($noCoverage.Count)"
Write-Host "Files with < 50% coverage: $($lowCoverage.Count)"
Write-Host "Files with 50-80% coverage: $($mediumCoverage.Count)"
$highCoverage = $files | Where-Object { $_.LineCoverage -ge 80 }
Write-Host "Files with >= 80% coverage: $($highCoverage.Count)"

# Export to CSV for further analysis
$outputFile = "coverage-analysis.csv"
$files | Export-Csv -Path $outputFile -NoTypeInformation
Write-Host "`nDetailed analysis exported to: $outputFile" -ForegroundColor Green

# Generate a prioritized list of files needing tests
Write-Host "`n=== PRIORITY LIST: FILES NEEDING TESTS ===" -ForegroundColor Magenta
$needsTests = $files | Where-Object { $_.LineCoverage -lt 80 } | Sort-Object LineCoverage, Complexity -Descending
$needsTests | Select-Object File, LineCoverage, BranchCoverage, Complexity, TotalLines | Format-Table -AutoSize
