# Veroeffentlicht GameHelper Experimental auf einem separaten GitHub-Repository.
param(
    [string]$Version,
    [string[]]$Changelog,
    [string]$Repository = "MordWraith/Gamehelper-Experimental",
    [string]$PublishDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) "publish"),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$SkipDownloader,
    [switch]$FullUpload
)

$ErrorActionPreference = "Stop"
$ScriptsRoot = $PSScriptRoot
$Root = Split-Path $ScriptsRoot -Parent

# Public key in UpdateSigningPublicKey.cs must match update-signing.key before compile.
& (Join-Path $ScriptsRoot "ensure-update-signing-key.ps1")

if (-not $SkipBuild) {
    $buildArgs = @{ Configuration = $Configuration; OutputDir = $PublishDirectory }
    if (-not [string]::IsNullOrEmpty($Version)) {
        $buildArgs.Version = $Version
    }
    & (Join-Path $ScriptsRoot "build.ps1") @buildArgs
}

& (Join-Path $ScriptsRoot "apply-experimental-channel.ps1") -PublishDir $PublishDirectory -Repository $Repository

$publishArgs = @{
    Version                      = $Version
    Changelog                    = $Changelog
    Repository                   = $Repository
    PublishDirectory             = $PublishDirectory
    DownloaderRemoteName         = "GameHelperDownloader-Experimental.exe"
    IncludeGithubConfigInPackage = $true
    UseRootGithubConfig          = $false
    ExperimentalDownloader       = $true
    Configuration                = $Configuration
    SkipBuild                    = $true
    SkipDownloader               = $SkipDownloader
    FullUpload                   = $FullUpload
}

& (Join-Path $ScriptsRoot "publish.ps1") @publishArgs
