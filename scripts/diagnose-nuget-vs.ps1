# Writes NDJSON diagnostics for VS "找不到類型 System.Object" / long-path NuGet issues.
# Run in a normal PowerShell (outside Cursor agent sandbox), from any cwd.
$ErrorActionPreference = 'Continue'
$logPath = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'debug-78badc.log'
# Prefer repo root relative to this script: scripts/ -> repo
$repoRoot = Split-Path $PSScriptRoot -Parent
$logPath = Join-Path $repoRoot 'debug-78badc.log'

function Write-DebugLog {
    param([string]$HypothesisId, [string]$Message, [hashtable]$Data = @{})
    $payload = [ordered]@{
        sessionId    = '78badc'
        timestamp    = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        hypothesisId = $HypothesisId
        location     = 'scripts/diagnose-nuget-vs.ps1'
        message      = $Message
        data         = $Data
        runId        = 'vs-diag'
    }
    ($payload | ConvertTo-Json -Compress -Depth 6) | Add-Content -Path $logPath -Encoding utf8
}

$nugetProc = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
$nugetUser = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'User')
$nugetMachine = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Machine')

Write-DebugLog -HypothesisId 'B' -Message 'NUGET_PACKAGES env scopes' -Data @{
    process = "$nugetProc"
    user    = "$nugetUser"
    machine = "$nugetMachine"
}

$appObjProps = Join-Path $repoRoot 'src\Soraeru.App\obj\Soraeru.App.csproj.nuget.g.props'
$sandboxInObj = $false
$nugetRootInObj = $null
if (Test-Path $appObjProps) {
    $text = Get-Content $appObjProps -Raw
    $sandboxInObj = $text -match 'cursor-sandbox'
    if ($text -match 'NuGetPackageRoot[^>]*>\s*([^<\r\n]+)') { $nugetRootInObj = $Matches[1].Trim() }
}
Write-DebugLog -HypothesisId 'A' -Message 'App nuget.g.props probe' -Data @{
    exists        = (Test-Path $appObjProps)
    sandboxInObj  = $sandboxInObj
    nugetRootInObj = "$nugetRootInObj"
}

$env:NUGET_PACKAGES = 'D:\.nuget\packages'
$cliProject = Join-Path $repoRoot 'src\Soraeru.ClientLogic\Soraeru.ClientLogic.csproj'
$cliOut = & dotnet build $cliProject -v:q 2>&1 | Out-String
$cliOk = $LASTEXITCODE -eq 0
$hasObjectErr = $cliOut -match 'System\.Object'
Write-DebugLog -HypothesisId 'C' -Message 'ClientLogic CLI build' -Data @{
    exitCode     = $LASTEXITCODE
    ok           = $cliOk
    hasObjectErr = $hasObjectErr
    tail         = ($cliOut.Trim() -split "`n" | Select-Object -Last 8) -join ' | '
}

$appProject = Join-Path $repoRoot 'src\Soraeru.App\Soraeru.App.csproj'
$appOut = & dotnet build $appProject -f net10.0-windows10.0.19041.0 -v:q 2>&1 | Out-String
$appOk = $LASTEXITCODE -eq 0
$appObjectErr = $appOut -match 'System\.Object'
Write-DebugLog -HypothesisId 'D' -Message 'App Windows CLI build' -Data @{
    exitCode     = $LASTEXITCODE
    ok           = $appOk
    hasObjectErr = $appObjectErr
    tail         = ($appOut.Trim() -split "`n" | Select-Object -Last 12) -join ' | '
}

Write-Host "Wrote diagnostics to $logPath"
Write-Host "NUGET_PACKAGES User='$nugetUser' Machine='$nugetMachine' Process(was)='$nugetProc'"
Write-Host "ClientLogic ok=$cliOk | App Windows ok=$appOk"
