# Erstellt GameHelperDownloader.exe (einzelne Datei, laedt oeffentlich von GitHub Releases).

param(

    [ValidateSet("Debug", "Release")]

    [string]$Configuration = "Release",

    [switch]$SelfContained,

    [switch]$ExperimentalChannel

)



$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent

$Project = Join-Path $Root "Downloader\Downloader.csproj"

$OutDir = Join-Path $Root "publish-downloader"



Write-Host "=== GameHelperDownloader bauen ($Configuration) ===" -ForegroundColor Cyan

Write-Host "Download-Quelle: GitHub Releases (oeffentlich, kein Token)." -ForegroundColor DarkGray



$publishArgs = @(

    "publish", $Project,

    "-c", $Configuration,

    "-r", "win-x64",

    "-o", $OutDir,

    "-p:PublishSingleFile=true"

)

if ($ExperimentalChannel) {
    $publishArgs += "-p:ExperimentalChannel=true"
    Write-Host "Kanal: Experimental (MordWraith/Gamehelper-Experimental)" -ForegroundColor DarkCyan
}



if ($SelfContained) {

    $publishArgs += @(

        "-p:SelfContained=true",

        "-p:IncludeNativeLibrariesForSelfExtract=true",

        "-p:EnableCompressionInSingleFile=true"

    )

    Write-Host "Modus: self-contained (~50 MB, keine extra .NET-Installation)" -ForegroundColor DarkGray

}

else {

    $publishArgs += "-p:SelfContained=false"

    Write-Host "Modus: framework-dependent (~1 MB, .NET 10 Desktop Runtime noetig)" -ForegroundColor DarkGray

}



dotnet @publishArgs

if ($LASTEXITCODE -ne 0) { throw "dotnet publish fehlgeschlagen" }



$builtExe = Join-Path $OutDir "GameHelperDownloader.exe"

$rootExeName = if ($ExperimentalChannel) { "GameHelperDownloader-Experimental.exe" } else { "GameHelperDownloader.exe" }
$rootExe = Join-Path $Root $rootExeName

if (Test-Path $rootExe) {
    try {
        Copy-Item $builtExe $rootExe -Force -ErrorAction Stop
    }
    catch {
        Write-Warning "Konnte $rootExe nicht ueberschreiben (Datei in Verwendung?). Frisch gebaut: $builtExe"
    }
}
else {
    Copy-Item $builtExe $rootExe -Force
}

$scriptsRoot = $PSScriptRoot
if ($ExperimentalChannel) {
    $configTemplate = Join-Path $scriptsRoot "github.config.experimental.json"
    if (Test-Path $configTemplate) {
        Copy-Item $configTemplate (Join-Path $OutDir "github.config.json") -Force
        Copy-Item $configTemplate (Join-Path $Root "github.config.json") -Force
        Write-Host "  github.config.json -> Experimental-Repo" -ForegroundColor DarkGray
    }
}

$displayExe = if ((Test-Path $builtExe) -and (Test-Path $rootExe)) {
    try {
        if ((Get-Item $builtExe).LastWriteTime -gt (Get-Item $rootExe).LastWriteTime) { $builtExe } else { $rootExe }
    }
    catch { $builtExe }
}
elseif (Test-Path $builtExe) { $builtExe }
else { $rootExe }

$sizeKb = [math]::Round((Get-Item $displayExe).Length / 1KB)

Write-Host ""

Write-Host "Fertig ($sizeKb KB):" -ForegroundColor Green

Write-Host "  $displayExe"

Write-Host ""

Write-Host "Verteilen: nur diese eine EXE weitergeben." -ForegroundColor Cyan

if (-not $SelfContained) {

    Write-Host "Hinweis: Nutzer brauchen .NET 10 Desktop Runtime." -ForegroundColor DarkGray

}

Write-Host "Groessere Offline-EXE: build-downloader.bat -SelfContained" -ForegroundColor DarkGray


