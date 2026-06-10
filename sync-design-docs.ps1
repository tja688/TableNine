param(
    [string]$SourcePath = 'C:\Users\jinji\Desktop\文档\MyNote\游戏开发项目\九宫牌局',
    [string]$TargetPath = (Join-Path $PSScriptRoot 'Assets\Docs'),
    [string]$ReportPath = (Join-Path $PSScriptRoot 'docs\design-sync\latest-sync-report.md'),
    [string]$RunReportDirectory = (Join-Path $PSScriptRoot 'docs\design-sync\runs')
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[Console]::OutputEncoding = $utf8NoBom
$OutputEncoding = $utf8NoBom

function Get-NormalizedRelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [System.IO.Path]::GetRelativePath($Root, $Path).Replace('/', '\')
}

function Get-ContentHash {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Add-ReportSection {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [AllowEmptyCollection()][string[]]$Items = @()
    )

    $script:reportLines.Add("## $Title")
    $script:reportLines.Add('')

    if ($Items.Count -eq 0) {
        $script:reportLines.Add('- None')
    }
    else {
        foreach ($item in ($Items | Sort-Object)) {
            $script:reportLines.Add("- $item")
        }
    }

    $script:reportLines.Add('')
}

if (-not (Test-Path -LiteralPath $SourcePath -PathType Container)) {
    throw "Source design directory does not exist: $SourcePath"
}

if (-not (Test-Path -LiteralPath $TargetPath -PathType Container)) {
    New-Item -ItemType Directory -Path $TargetPath | Out-Null
}

$sourceRoot = (Resolve-Path -LiteralPath $SourcePath).Path
$targetRoot = (Resolve-Path -LiteralPath $TargetPath).Path

$sourceFiles = @{}
$sourceDirectories = @{}
Get-ChildItem -LiteralPath $sourceRoot -Directory -Recurse |
    ForEach-Object {
        $relativePath = Get-NormalizedRelativePath -Root $sourceRoot -Path $_.FullName
        $sourceDirectories[$relativePath] = $_.FullName
    }

Get-ChildItem -LiteralPath $sourceRoot -File -Recurse |
    Where-Object { $_.Name -notlike '*.meta' } |
    ForEach-Object {
        $relativePath = Get-NormalizedRelativePath -Root $sourceRoot -Path $_.FullName
        $sourceFiles[$relativePath] = $_.FullName
    }

$targetFiles = @{}
Get-ChildItem -LiteralPath $targetRoot -File -Recurse |
    Where-Object { $_.Name -notlike '*.meta' } |
    ForEach-Object {
        $relativePath = Get-NormalizedRelativePath -Root $targetRoot -Path $_.FullName
        $targetFiles[$relativePath] = $_.FullName
    }

$added = New-Object System.Collections.Generic.List[string]
$updated = New-Object System.Collections.Generic.List[string]
$deleted = New-Object System.Collections.Generic.List[string]
$unchanged = New-Object System.Collections.Generic.List[string]
$deletedMeta = New-Object System.Collections.Generic.List[string]

foreach ($relativePath in ($sourceDirectories.Keys | Sort-Object)) {
    $targetDirectory = Join-Path $targetRoot $relativePath
    if (-not (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $targetDirectory | Out-Null
    }
}

foreach ($relativePath in ($sourceFiles.Keys | Sort-Object)) {
    $sourceFile = $sourceFiles[$relativePath]
    $targetFile = Join-Path $targetRoot $relativePath
    $targetDirectory = Split-Path -Parent $targetFile

    if (-not (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $targetDirectory | Out-Null
    }

    if (-not (Test-Path -LiteralPath $targetFile -PathType Leaf)) {
        Copy-Item -LiteralPath $sourceFile -Destination $targetFile -Force
        $added.Add($relativePath)
        continue
    }

    if ((Get-ContentHash -Path $sourceFile) -ne (Get-ContentHash -Path $targetFile)) {
        Copy-Item -LiteralPath $sourceFile -Destination $targetFile -Force
        $updated.Add($relativePath)
    }
    else {
        $unchanged.Add($relativePath)
    }
}

foreach ($relativePath in ($targetFiles.Keys | Sort-Object)) {
    if ($sourceFiles.ContainsKey($relativePath)) {
        continue
    }

    $targetFile = $targetFiles[$relativePath]
    Remove-Item -LiteralPath $targetFile -Force
    $deleted.Add($relativePath)

    $metaPath = "$targetFile.meta"
    if (Test-Path -LiteralPath $metaPath -PathType Leaf) {
        Remove-Item -LiteralPath $metaPath -Force
        $deletedMeta.Add("$relativePath.meta")
    }
}

Get-ChildItem -LiteralPath $targetRoot -Directory -Recurse |
    Sort-Object FullName -Descending |
    ForEach-Object {
        if (-not (Get-ChildItem -LiteralPath $_.FullName -Force)) {
            Remove-Item -LiteralPath $_.FullName -Force
        }
    }

Get-ChildItem -LiteralPath $targetRoot -Filter '*.meta' -File -Recurse | ForEach-Object {
    $pairedPath = $_.FullName.Substring(0, $_.FullName.Length - '.meta'.Length)

    if (-not (Test-Path -LiteralPath $pairedPath)) {
        $relativeMetaPath = Get-NormalizedRelativePath -Root $targetRoot -Path $_.FullName
        Remove-Item -LiteralPath $_.FullName -Force
        if (-not $deletedMeta.Contains($relativeMetaPath)) {
            $deletedMeta.Add($relativeMetaPath)
        }
    }
}

$reportDirectory = Split-Path -Parent $ReportPath
if (-not (Test-Path -LiteralPath $reportDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $reportDirectory | Out-Null
}

if (-not (Test-Path -LiteralPath $RunReportDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $RunReportDirectory | Out-Null
}

$runReportStamp = (Get-Date).ToString('yyyyMMdd-HHmmss-fff')
$runReportPath = Join-Path $RunReportDirectory "$runReportStamp-sync-report.md"

$reportLines = New-Object System.Collections.Generic.List[string]
$reportLines.Add('# Design Docs Sync Report')
$reportLines.Add('')
$reportLines.Add("- Timestamp: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))")
$reportLines.Add("- Source: $sourceRoot")
$reportLines.Add("- Target: $targetRoot")
$reportLines.Add('')
$reportLines.Add('## Summary')
$reportLines.Add('')
$reportLines.Add("- Added: $($added.Count)")
$reportLines.Add("- Updated: $($updated.Count)")
$reportLines.Add("- Deleted: $($deleted.Count)")
$reportLines.Add("- Deleted meta: $($deletedMeta.Count)")
$reportLines.Add("- Unchanged: $($unchanged.Count)")
$reportLines.Add('')

Add-ReportSection -Title 'Added' -Items $added.ToArray()
Add-ReportSection -Title 'Updated' -Items $updated.ToArray()
Add-ReportSection -Title 'Deleted' -Items $deleted.ToArray()
Add-ReportSection -Title 'Deleted Meta' -Items $deletedMeta.ToArray()
Add-ReportSection -Title 'Unchanged' -Items $unchanged.ToArray()

$gitStatus = @()
if (Get-Command git -ErrorAction SilentlyContinue) {
    $gitStatus = git -C $PSScriptRoot -c core.quotePath=false status --short -- 'Assets/Docs' 'docs/design-sync/latest-sync-report.md' 2>$null
}

$reportLines.Add('## Git Status')
$reportLines.Add('')
if ($gitStatus.Count -eq 0) {
    $reportLines.Add('No git status changes reported for synced docs.')
}
else {
    $reportLines.Add('```text')
    foreach ($line in $gitStatus) {
        $reportLines.Add($line)
    }
    $reportLines.Add('```')
}
$reportLines.Add('')

Set-Content -LiteralPath $runReportPath -Value $reportLines -Encoding UTF8

$latestLines = New-Object System.Collections.Generic.List[string]
$latestLines.Add('# Design Docs Sync Latest')
$latestLines.Add('')
$latestLines.Add("- Latest run report: $([System.IO.Path]::GetRelativePath($reportDirectory, $runReportPath).Replace('\\', '/'))")
$latestLines.Add("- Timestamp: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))")
$latestLines.Add("- Source: $sourceRoot")
$latestLines.Add("- Target: $targetRoot")
$latestLines.Add('')
$latestLines.Add('## Summary')
$latestLines.Add('')
$latestLines.Add("- Added: $($added.Count)")
$latestLines.Add("- Updated: $($updated.Count)")
$latestLines.Add("- Deleted: $($deleted.Count)")
$latestLines.Add("- Deleted meta: $($deletedMeta.Count)")
$latestLines.Add("- Unchanged: $($unchanged.Count)")
$latestLines.Add('')
$latestLines.Add('Full change details are in the latest run report above.')
$latestLines.Add('')

Set-Content -LiteralPath $ReportPath -Value $latestLines -Encoding UTF8

Write-Host 'Design docs sync complete.'
Write-Host "Added: $($added.Count), Updated: $($updated.Count), Deleted: $($deleted.Count), Deleted meta: $($deletedMeta.Count), Unchanged: $($unchanged.Count)"
Write-Host "Latest report: $ReportPath"
Write-Host "Run report: $runReportPath"
