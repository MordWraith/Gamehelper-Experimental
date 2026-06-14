# Markiert einen Build-Ordner als GameHelper Experimental (eigenes GitHub-Release, getrennt von stabil).
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDir,
    [string]$Repository = "MordWraith/Gamehelper-Experimental"
)

$ErrorActionPreference = "Stop"
$ScriptsRoot = $PSScriptRoot
$RepoRoot = Split-Path $ScriptsRoot -Parent

if (-not (Test-Path $PublishDir)) {
    throw "Publish-Ordner fehlt: $PublishDir"
}

$configTemplate = Join-Path $ScriptsRoot "github.config.experimental.json"
if (Test-Path $configTemplate) {
    Copy-Item $configTemplate (Join-Path $PublishDir "github.config.json") -Force
}
else {
    (@{ repository = $Repository } | ConvertTo-Json) | Set-Content (Join-Path $PublishDir "github.config.json") -Encoding UTF8
}

$versionPath = Join-Path $PublishDir "VERSION.txt"
$versionLine = "GameHelper (unbekannt)"
if (Test-Path $versionPath) {
    $firstLine = (Get-Content $versionPath -TotalCount 1 -ErrorAction SilentlyContinue)
    if ($firstLine) { $versionLine = $firstLine.Trim() }
}

$builtAt = (Get-Date).ToUniversalTime().ToString("o")
$versionTxt = @"
$versionLine [Experimental]
Kanal: Experimental (getrennt von stabilem GameHelper)
GitHub: https://github.com/$Repository/releases
Gebaut am: $builtAt (UTC)

Pruefen: Rechtsklick auf GameHelper.App.exe -> Details -> Dateiversion
"@

$versionTxt | Set-Content $versionPath -Encoding UTF8

$distributionTxt = @"
=== GameHelper Experimental ===

$versionLine
Kanal:  Experimental (nicht der stabile Release)
GitHub: https://github.com/$Repository/releases
Gebaut: $builtAt (UTC)

WICHTIG:
1. In einen EIGENEN, LEEREN Ordner installieren (nicht ueber stabiles GameHelper legen).
2. Stabiles GameHelper und Experimental duerfen parallel laufen, wenn sie in getrennten Ordnern liegen.
3. Auto-Update laedt nur von $Repository (github.config.json im Installationsordner).
4. Fuer Tester: GameHelperDownloader-Experimental.exe von den Experimental-Releases verwenden.

Nach dem Entpacken: VERSION.txt oeffnen und Version ablesen.
"@

$distributionTxt | Set-Content (Join-Path $PublishDir "VERTEILUNG-HINWEIS.txt") -Encoding UTF8

Write-Host "  Experimental-Kanal angewendet: $PublishDir" -ForegroundColor DarkCyan
Write-Host "  Update-Repo: $Repository" -ForegroundColor DarkCyan
