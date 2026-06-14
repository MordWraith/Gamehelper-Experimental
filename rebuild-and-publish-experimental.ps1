# Experimental-Quellprojekt: bauen + GitHub Releases (MordWraith/Gamehelper-Experimental).
param(
    [string]$Version,
    [string[]]$Changelog,
    [switch]$SkipUpload,
    [switch]$FullUpload,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Repository = "MordWraith/Gamehelper-Experimental"
)

$ErrorActionPreference = "Stop"
$SourceRoot = $PSScriptRoot
. (Join-Path $SourceRoot "scripts\project-paths.ps1")
. (Join-Path $SourceRoot "scripts\set-version.ps1")
$DeployDir = $GameHelperExperimentalDeployRoot

$BuildScript = Join-Path $SourceRoot "scripts\build.ps1"
$PublishScript = Join-Path $SourceRoot "scripts\publish-experimental.ps1"
$ApplyScript = Join-Path $SourceRoot "scripts\apply-experimental-channel.ps1"

if (-not (Test-Path $BuildScript)) {
    Write-Error "Quellcode nicht gefunden: $SourceRoot"
}

$started = Get-Date
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host " Experimental Quellcode Rebuild + Publish" -ForegroundColor Magenta
Write-Host " $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Prompt-VersionInput -Root $SourceRoot
}
elseif (-not (Test-VersionFormat $Version)) {
    throw "Ungueltige Version '$Version'. Format: x.y.z"
}

$current = Get-ProjectVersion -Root $SourceRoot
if ((Compare-ProjectVersion $Version $current) -lt 0) {
    throw "Version $Version ist niedriger als $current im Experimental-Quellprojekt."
}

Write-Host ""
Write-Host "Setze Experimental-Version auf $Version ..." -ForegroundColor Cyan
Set-ProjectVersion -Root $SourceRoot -Version $Version

if (-not $Changelog -or $Changelog.Count -eq 0) {
    $fromFile = Get-ReleaseNotesLines -Root $SourceRoot
    if ($fromFile.Count -gt 0) {
        $Changelog = $fromFile
    }
    else {
        $Changelog = Prompt-ChangelogInput
    }
}

if ($Changelog -and $Changelog.Count -gt 0) {
    $Changelog | Set-Content (Join-Path $SourceRoot "release-notes-experimental.txt") -Encoding UTF8
}

if ($SkipUpload) {
    & $BuildScript -Configuration $Configuration -Version $Version -OutputDir $DeployDir
    & $ApplyScript -PublishDir $DeployDir -Repository $Repository
}
else {
    $publishArgs = @{
        Version          = $Version
        Repository       = $Repository
        PublishDirectory = $DeployDir
        Configuration    = $Configuration
    }
    if ($Changelog -and $Changelog.Count -gt 0) { $publishArgs.Changelog = $Changelog }
    if ($FullUpload) { $publishArgs.FullUpload = $true }
    & $PublishScript @publishArgs
}

$exe = Join-Path $DeployDir "GameHelper.exe"
if (-not (Test-Path $exe)) { $exe = Join-Path $DeployDir "GameHelper.App.exe" }
if (-not (Test-Path $exe)) { throw "GameHelper.exe fehlt in $DeployDir" }

$elapsed = (Get-Date) - $started
Write-Host ""
Write-Host "Fertig: Experimental v$Version in $([math]::Round($elapsed.TotalSeconds, 1))s" -ForegroundColor Green
Write-Host "Deploy: $DeployDir" -ForegroundColor Green
if (-not $SkipUpload) {
    Write-Host "Releases: https://github.com/$Repository/releases" -ForegroundColor Green
}
Write-Host "Stabil-Quellcode ($GameHelperStableRoot) wurde NICHT geaendert." -ForegroundColor DarkGray
Write-Host ""
