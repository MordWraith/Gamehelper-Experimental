# Experimental-Quellprojekt: Build -> Deploy-Ordner (ohne GitHub).
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Repository = "MordWraith/Gamehelper-Experimental"
)

$ErrorActionPreference = "Stop"
$SourceRoot = $PSScriptRoot
. (Join-Path $SourceRoot "scripts\project-paths.ps1")
$DeployDir = $GameHelperExperimentalDeployRoot
$BuildScript = Join-Path $SourceRoot "scripts\build.ps1"
$ApplyScript = Join-Path $SourceRoot "scripts\apply-experimental-channel.ps1"

if (-not (Test-Path $BuildScript)) {
    Write-Error "build.ps1 nicht gefunden in $SourceRoot"
}

$started = Get-Date
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host " Experimental Quellcode -> Deploy" -ForegroundColor Magenta
Write-Host " $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Quellcode:  $SourceRoot" -ForegroundColor DarkGray
Write-Host "Deploy:     $DeployDir" -ForegroundColor Yellow
Write-Host ""

& $BuildScript -Configuration $Configuration -OutputDir $DeployDir

$exe = Join-Path $DeployDir "GameHelper.exe"
if (-not (Test-Path $exe)) { $exe = Join-Path $DeployDir "GameHelper.App.exe" }
if (-not (Test-Path $exe)) { throw "GameHelper.exe fehlt in $DeployDir" }

& $ApplyScript -PublishDir $DeployDir -Repository $Repository

Write-Host ""
Write-Host "Fertig in $([math]::Round(((Get-Date) - $started).TotalSeconds, 1))s -> $DeployDir" -ForegroundColor Green
Write-Host ""
