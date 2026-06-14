# Stellt den Stand v1.3.2-monolith wieder her (vor Komponenten-Versionierung).
param(
    [switch]$HardReset,
    [switch]$SkipRebuild
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot "set-version.ps1")

Write-Host "=== Rollback v1.3.2-monolith ===" -ForegroundColor Cyan

$gitDir = Join-Path $Root ".git"
if (Test-Path $gitDir) {
    Push-Location $Root
    $tagExists = git tag -l "v1.3.2-monolith"
    if ($tagExists) {
        if ($HardReset) {
            Write-Host "Git: hard reset auf Tag v1.3.2-monolith ..." -ForegroundColor Yellow
            git reset --hard v1.3.2-monolith
        }
        else {
            Write-Host "Git: checkout Tag v1.3.2-monolith (Arbeitskopie) ..." -ForegroundColor Yellow
            git checkout v1.3.2-monolith -- .
        }

        Pop-Location
        if (-not $SkipRebuild) {
            Write-Host "Starte rebuild-experimental.ps1 ..." -ForegroundColor DarkGray
            & (Join-Path $Root "rebuild-experimental.ps1")
        }

        Write-Host "Rollback abgeschlossen (Git)." -ForegroundColor Green
        exit 0
    }

    Pop-Location
}

Write-Host "Kein Git-Tag v1.3.2-monolith — stelle nur Versionsnummern aus Snapshot wieder her." -ForegroundColor Yellow
Restore-MonolithVersionsFromSnapshot -Root $Root

if (-not $SkipRebuild) {
    & (Join-Path $Root "rebuild-experimental.ps1")
}

Write-Host "Rollback abgeschlossen (Snapshot)." -ForegroundColor Green
Write-Host "Hinweis: Ohne Git-Tag werden nur .csproj-Versionen zurueckgesetzt, nicht der gesamte Quellcode." -ForegroundColor DarkYellow
