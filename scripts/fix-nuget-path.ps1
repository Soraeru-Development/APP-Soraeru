#Requires -Version 5.1
<#
.SYNOPSIS
  Clear polluted NuGet obj artifacts and restore using the short global packages folder.

.DESCRIPTION
  Cursor Agent sandbox may inject NUGET_PACKAGES under %TEMP%\cursor-sandbox-cache\...
  That can override Directory.Build.props RestorePackagesPath and write long paths into
  obj\*.nuget.g.props / *.nuget.g.targets / project.assets.json, breaking VS (MAX_PATH).

  This repo hardens via Directory.Build.props on Windows (RestorePackagesPath / NuGetPackageRoot).
  nuget.config no longer sets globalPackagesFolder (Linux Docker restore must not see D:\).
  This script still clears polluted bin/obj, forces process env away from
  sandbox, restores, optionally builds, and verifies no sandbox path remains.

.EXAMPLE
  # Prefer a normal PowerShell outside Cursor agent sandbox; then reopen VS.
  .\scripts\fix-nuget-path.ps1
  .\scripts\fix-nuget-path.ps1 -Build
  .\scripts\fix-nuget-path.ps1 -AllProjects -Build
#>
[CmdletBinding()]
param(
    [switch]$AllProjects,
    [switch]$Build,
    [string]$PackagesFolder = 'D:\.nuget\packages'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

function Get-EnvLevel {
    param([string]$Name, [string]$Level)
    [Environment]::GetEnvironmentVariable($Name, $Level)
}

Write-Host '=== NuGet packages path hardening ==='
Write-Host "Target packages folder: $PackagesFolder"

$userNuGet = Get-EnvLevel -Name 'NUGET_PACKAGES' -Level 'User'
$machineNuGet = Get-EnvLevel -Name 'NUGET_PACKAGES' -Level 'Machine'
$processBefore = $env:NUGET_PACKAGES
Write-Host "NUGET_PACKAGES (User)    = $(if ($userNuGet) { $userNuGet } else { '(not set)' })"
Write-Host "NUGET_PACKAGES (Machine) = $(if ($machineNuGet) { $machineNuGet } else { '(not set)' })"
Write-Host "NUGET_PACKAGES (Process before) = $(if ($processBefore) { $processBefore } else { '(not set)' })"

foreach ($levelName in @('User', 'Machine')) {
    $val = Get-EnvLevel -Name 'NUGET_PACKAGES' -Level $levelName
    if ($val -and ($val -match 'cursor-sandbox' -or $val -match '[\\/]Temp[\\/]')) {
        Write-Warning @"
Persistent $levelName environment variable NUGET_PACKAGES looks wrong:
  $val

This script cannot safely change User/Machine env permanently.
Remove it via: Settings → System → About → Advanced system settings → Environment Variables
or PowerShell (User):
  [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', `$null, 'User')
Then open a NEW terminal / restart VS.
"@
    }
}

# Prefer short path for this process; remove sandbox override entirely then set correct value.
if ($env:NUGET_PACKAGES -and ($env:NUGET_PACKAGES -match 'cursor-sandbox' -or $env:NUGET_PACKAGES -ne $PackagesFolder)) {
    Write-Host "Unsetting process NUGET_PACKAGES (was: $env:NUGET_PACKAGES)"
    Remove-Item Env:\NUGET_PACKAGES -ErrorAction SilentlyContinue
}
$env:NUGET_PACKAGES = $PackagesFolder
Write-Host "NUGET_PACKAGES (Process now) = $env:NUGET_PACKAGES"

if (-not (Test-Path $PackagesFolder)) {
    New-Item -ItemType Directory -Path $PackagesFolder -Force | Out-Null
    Write-Host "Created $PackagesFolder"
}

$targets = @(
    (Join-Path $repoRoot 'src\Soraeru.App')
)
if ($AllProjects) {
    $targets = Get-ChildItem (Join-Path $repoRoot 'src') -Directory |
        ForEach-Object { $_.FullName }
    $testRoot = Join-Path $repoRoot 'tests'
    if (Test-Path $testRoot) {
        $targets += Get-ChildItem $testRoot -Directory | ForEach-Object { $_.FullName }
    }
}

function Remove-TreeRobust {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    # Prefer cmd rmdir — more reliable on Windows when some files are briefly locked.
    cmd /c "rmdir /s /q `"$Path`"" | Out-Null
    if (Test-Path $Path) {
        Get-ChildItem $Path -Recurse -Force -ErrorAction SilentlyContinue |
            Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
        Remove-Item $Path -Force -Recurse -ErrorAction SilentlyContinue
    }
    if (Test-Path $Path) {
        Write-Warning "Could not fully delete (file lock?): $Path — close Visual Studio and retry."
    } else {
        Write-Host "Removed $Path"
    }
}

foreach ($dir in $targets) {
    # obj first: nuget.g.* / project.assets.json are what poison VS Imports.
    foreach ($leaf in @('obj', 'bin')) {
        Remove-TreeRobust (Join-Path $dir $leaf)
    }
}

$appObj = Join-Path $repoRoot 'src\Soraeru.App\obj'
if (Test-Path $appObj) {
    throw "src\Soraeru.App\obj is still locked. Close Visual Studio (and any MSBuild), then re-run this script."
}

Write-Host 'dotnet nuget locals global-packages -l'
& dotnet nuget locals global-packages -l
if ($LASTEXITCODE -ne 0) { throw "dotnet nuget locals failed ($LASTEXITCODE)" }

$sln = Join-Path $repoRoot 'Soraeru.slnx'
# Explicit MSBuild property as belt-and-suspenders with Directory.Build.props.
$restoreArgs = @(
    'restore', $sln,
    "-p:RestorePackagesPath=$PackagesFolder",
    "-p:NuGetPackageRoot=$PackagesFolder\"
)
Write-Host ("dotnet " + ($restoreArgs -join ' '))
& dotnet @restoreArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed ($LASTEXITCODE)" }

$probeFiles = @(
    (Join-Path $repoRoot 'src\Soraeru.App\obj\Soraeru.App.csproj.nuget.g.props'),
    (Join-Path $repoRoot 'src\Soraeru.App\obj\Soraeru.App.csproj.nuget.g.targets'),
    (Join-Path $repoRoot 'src\Soraeru.App\obj\project.assets.json')
)

# Also scan any TFM-specific generated files under App obj.
$appObjProbe = Join-Path $repoRoot 'src\Soraeru.App\obj'
if (Test-Path $appObjProbe) {
    $probeFiles += Get-ChildItem $appObjProbe -Recurse -File -Include '*.nuget.g.props','*.nuget.g.targets','project.assets.json' |
        ForEach-Object { $_.FullName }
    $probeFiles = $probeFiles | Select-Object -Unique
}

$bad = @()
$missing = @()
foreach ($f in $probeFiles) {
    if (-not (Test-Path $f)) {
        if ($f -like '*Soraeru.App.csproj.nuget.g.*' -or $f -like '*\project.assets.json') {
            $missing += $f
        }
        continue
    }
    if (Select-String -Path $f -Pattern 'cursor-sandbox' -Quiet) {
        $bad += $f
    }
}

if ($missing.Count -gt 0) {
    Write-Warning ("Missing after restore:`n" + ($missing -join "`n"))
}

if ($bad.Count -gt 0) {
    Write-Error @"
Restore still references cursor-sandbox-cache in:
$($bad -join "`n")

NUGET_PACKAGES process env may still be forced by the host. Run this script in a
normal PowerShell (outside Cursor agent sandbox), remove User/Machine NUGET_PACKAGES
if it points at sandbox, then retry. Directory.Build.props should force
RestorePackagesPath=D:\.nuget\packages — if this still fails, report the probe files.
"@
    exit 1
}

$props = Join-Path $repoRoot 'src\Soraeru.App\obj\Soraeru.App.csproj.nuget.g.props'
if (Test-Path $props) {
    $rootLine = Select-String -Path $props -Pattern 'NuGetPackageRoot' | Select-Object -First 1
    Write-Host "OK: no cursor-sandbox in App NuGet artifacts"
    if ($rootLine) { Write-Host $rootLine.Line.Trim() }
}

if ($Build) {
    $appProj = Join-Path $repoRoot 'src\Soraeru.App\Soraeru.App.csproj'
    Write-Host 'dotnet build (windows TFM)...'
    & dotnet build $appProj -f net10.0-windows10.0.19041.0 -c Debug --no-restore `
        "-p:RestorePackagesPath=$PackagesFolder" `
        "-p:NuGetPackageRoot=$PackagesFolder\"
    if ($LASTEXITCODE -ne 0) { throw "windows build failed ($LASTEXITCODE)" }
    Write-Host 'Build succeeded.'
}

Write-Host @'

Done. Close Visual Studio (if open), reopen Soraeru.slnx, then build.
If Agent restore polluted obj again, re-run this script (MSBuild props should already force short path).
Details: docs/dev-setup-build.md
'@
