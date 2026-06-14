# Setzt die Version einer einzelnen Komponente in versions.json und synchronisiert .csproj.
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("core", "launcher", "downloader", "gameOffsets", "plugin")]
    [string]$Component,
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$PluginId
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot "set-version.ps1")

Set-ComponentVersion -Root $Root -Component $Component -Version $Version -PluginId $PluginId
