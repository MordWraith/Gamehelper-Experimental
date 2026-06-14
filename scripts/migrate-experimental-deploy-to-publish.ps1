# Migrate settings from legacy "Gamehelper Experimental" folder into publish\
param(
    [string]$OldDeploy = "D:\ZusatzProgramme\Gamehelper Experimental",
    [string]$NewDeploy = (Join-Path (Split-Path $PSScriptRoot -Parent) "publish")
)

$ErrorActionPreference = "Stop"

function Invoke-RobocopyMerge {
    param([string]$Source, [string]$Target)
    if (-not (Test-Path $Source)) { return }
    New-Item -ItemType Directory -Path $Target -Force | Out-Null
    & robocopy $Source $Target /E /XO /NFL /NDL /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed ($Source -> $Target), exit $LASTEXITCODE"
    }
}

if (-not (Test-Path $OldDeploy)) {
    Write-Host "Old deploy folder not found: $OldDeploy" -ForegroundColor DarkYellow
    exit 0
}

New-Item -ItemType Directory -Path $NewDeploy -Force | Out-Null

Invoke-RobocopyMerge (Join-Path $OldDeploy "configs") (Join-Path $NewDeploy "configs")

$pluginsRoot = Join-Path $OldDeploy "Plugins"
if (Test-Path $pluginsRoot) {
    foreach ($plugin in Get-ChildItem $pluginsRoot -Directory) {
        $srcConfig = Join-Path $plugin.FullName "config"
        if (Test-Path $srcConfig) {
            Invoke-RobocopyMerge $srcConfig (Join-Path $NewDeploy "Plugins\$($plugin.Name)\config")
        }
    }
}

foreach ($extra in @("imgui.ini", "migration-notice.dismissed", "github.config.json", "update.state.json")) {
    $src = Join-Path $OldDeploy $extra
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $NewDeploy $extra) -Force
        Write-Host "  copied: $extra" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Done: $OldDeploy -> $NewDeploy" -ForegroundColor Green
Write-Host "Next: run rebuild-experimental.bat" -ForegroundColor Green
