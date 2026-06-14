# Component-based versioning (versions.json -> .csproj, manifest, publish/)

function Get-VersionsFilePath {
    param([string]$Root)
    Join-Path $Root "versions.json"
}

function Get-ComponentVersionsDocument {
    param([string]$Root)

    $path = Get-VersionsFilePath -Root $Root
    if (-not (Test-Path $path)) {
        throw "versions.json fehlt: $path"
    }

    return Get-Content $path -Raw | ConvertFrom-Json
}

function Get-CorePluginIds {
    param([string]$Root)

    $doc = Get-ComponentVersionsDocument -Root $Root
    $ids = [System.Collections.Generic.List[string]]::new()
    foreach ($prop in $doc.plugins.PSObject.Properties) {
        if ($prop.Value.core -eq $true) {
            $ids.Add([string]$prop.Name)
        }
    }

    if ($ids.Count -gt 0) {
        return @($ids)
    }

    $fallback = Join-Path $Root "scripts\core-plugins.json"
    if (Test-Path $fallback) {
        $cfg = Get-Content $fallback -Raw | ConvertFrom-Json
        return @($cfg.corePluginIds)
    }

    return @()
}

function Get-ProjectVersion {
    param([string]$Root)

    $path = Get-VersionsFilePath -Root $Root
    if (Test-Path $path) {
        $doc = Get-ComponentVersionsDocument -Root $Root
        return [string]$doc.components.core.version
    }

    $csproj = Join-Path $Root "GameHelper\GameHelper.csproj"
    if (-not (Test-Path $csproj)) {
        return "1.0.0"
    }

    $xml = Get-Content $csproj -Raw
    if ($xml -match '<Version>([^<]+)</Version>') {
        return $Matches[1].Trim()
    }

    return "1.0.0"
}

function Set-CsprojVersion {
    param(
        [string]$CsprojPath,
        [string]$Version
    )

    if (-not (Test-Path $CsprojPath)) {
        Write-Warning "Ueberspringe: $CsprojPath"
        return
    }

    $parts = $Version.Split('.')
    $assemblyVersion = "$($parts[0]).$($parts[1]).$($parts[2]).0"
    $fileVersion = $assemblyVersion

    $xml = Get-Content $CsprojPath -Raw
    if ($xml -match '<Version>') {
        $xml = $xml -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
    }
    else {
        $xml = $xml -replace '(<PropertyGroup>\s*)', "`$1`r`n    <Version>$Version</Version>`r`n"
    }

    if ($xml -match '<AssemblyVersion>') {
        $xml = $xml -replace '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$assemblyVersion</AssemblyVersion>"
    }
    else {
        $xml = $xml -replace '(<Version>[^<]+</Version>)', "`$1`r`n    <AssemblyVersion>$assemblyVersion</AssemblyVersion>"
    }

    if ($xml -match '<FileVersion>') {
        $xml = $xml -replace '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$fileVersion</FileVersion>"
    }
    else {
        $xml = $xml -replace '(<AssemblyVersion>[^<]+</AssemblyVersion>)', "`$1`r`n    <FileVersion>$fileVersion</FileVersion>"
    }

    Set-Content $CsprojPath $xml -Encoding UTF8
    Write-Host "  Version $Version -> $(Split-Path $CsprojPath -Leaf)" -ForegroundColor DarkGray
}

function Sync-ComponentVersions {
    param([string]$Root)

    $doc = Get-ComponentVersionsDocument -Root $Root
    Write-Host "Synchronisiere versions.json -> .csproj ..." -ForegroundColor Cyan

    foreach ($component in $doc.components.PSObject.Properties) {
        $version = [string]$component.Value.version
        foreach ($projectRel in @($component.Value.projects)) {
            $csproj = Join-Path $Root ($projectRel -replace '/', '\')
            Set-CsprojVersion -CsprojPath $csproj -Version $version
        }
    }

    foreach ($plugin in $doc.plugins.PSObject.Properties) {
        $version = [string]$plugin.Value.version
        $csproj = Join-Path $Root ([string]$plugin.Value.project -replace '/', '\')
        Set-CsprojVersion -CsprojPath $csproj -Version $version
    }
}

function Set-ComponentVersion {
    param(
        [string]$Root,
        [string]$Component,
        [string]$Version,
        [string]$PluginId
    )

    if (-not (Test-VersionFormat $Version)) {
        throw "Ungueltige Version '$Version'. Format: z.B. 1.0.1"
    }

    $path = Get-VersionsFilePath -Root $Root
    $doc = Get-ComponentVersionsDocument -Root $Root

    switch ($Component.ToLowerInvariant()) {
        'core' {
            $doc.components.core.version = $Version
            $doc.components.gameOffsets.version = $Version
            break
        }
        'launcher' { $doc.components.launcher.version = $Version; break }
        'downloader' { $doc.components.downloader.version = $Version; break }
        'gameoffsets' { $doc.components.gameOffsets.version = $Version; break }
        'plugin' {
            if ([string]::IsNullOrWhiteSpace($PluginId)) {
                throw "Fuer -Component plugin ist -PluginId erforderlich."
            }

            if (-not $doc.plugins.PSObject.Properties.Name.Contains($PluginId)) {
                throw "Unbekanntes Plugin '$PluginId' in versions.json."
            }

            $doc.plugins.$PluginId.version = $Version
            break
        }
        default { throw "Unbekannte Komponente '$Component'. Erlaubt: core, launcher, downloader, gameOffsets, plugin." }
    }

    ($doc | ConvertTo-Json -Depth 8) | Set-Content $path -Encoding UTF8
    Sync-ComponentVersions -Root $Root
    Write-Host "Komponente '$Component' auf $Version gesetzt." -ForegroundColor Green
}

function Get-ComponentVersionsSummary {
    param([string]$Root)

    $doc = Get-ComponentVersionsDocument -Root $Root
    $summary = [ordered]@{
        versionScheme = [int]$doc.versionScheme
        legacyMonolithVersion = [string]$doc.legacyMonolithVersion
        core = [string]$doc.components.core.version
        launcher = [string]$doc.components.launcher.version
        downloader = [string]$doc.components.downloader.version
        gameOffsets = [string]$doc.components.gameOffsets.version
        corePlugins = @(Get-CorePluginIds -Root $Root)
        plugins = [ordered]@{}
    }

    foreach ($plugin in $doc.plugins.PSObject.Properties) {
        $summary.plugins[[string]$plugin.Name] = [ordered]@{
            version = [string]$plugin.Value.version
            folder = [string]$plugin.Value.folder
            core = [bool]$plugin.Value.core
        }
    }

    return $summary
}

function Write-ComponentVersionsFile {
    param(
        [string]$PublishDir,
        [string]$Root
    )

    $summary = Get-ComponentVersionsSummary -Root $Root
    $jsonPath = Join-Path $PublishDir "components.json"
    ($summary | ConvertTo-Json -Depth 6) | Set-Content $jsonPath -Encoding UTF8
}

function Get-PublishManifestVersion {
    param([string]$Root)
    Get-ProjectVersion -Root $Root
}

function Build-ManifestComponentsBlock {
    param([string]$Root)

    $doc = Get-ComponentVersionsDocument -Root $Root
    $block = [ordered]@{
        versionScheme = [int]$doc.versionScheme
        legacyMonolithVersion = [string]$doc.legacyMonolithVersion
        core = [string]$doc.components.core.version
        launcher = [string]$doc.components.launcher.version
        downloader = [string]$doc.components.downloader.version
        gameOffsets = [string]$doc.components.gameOffsets.version
        plugins = [ordered]@{}
    }

    foreach ($plugin in $doc.plugins.PSObject.Properties) {
        $block.plugins[[string]$plugin.Name] = [ordered]@{
            version = [string]$plugin.Value.version
            folder = [string]$plugin.Value.folder
            core = [bool]$plugin.Value.core
        }
    }

    return $block
}

function Restore-MonolithVersionsFromSnapshot {
    param([string]$Root)

    $snapshot = Join-Path $Root "_rollback\v1.3.2-monolith\component-versions.snapshot.json"
    if (-not (Test-Path $snapshot)) {
        throw "Rollback-Snapshot fehlt: $snapshot"
    }

    $data = Get-Content $snapshot -Raw | ConvertFrom-Json
    Set-CsprojVersion -CsprojPath (Join-Path $Root "GameHelper\GameHelper.csproj") -Version $data.components.core
    Set-CsprojVersion -CsprojPath (Join-Path $Root "Launcher\Launcher.csproj") -Version $data.components.launcher
    Set-CsprojVersion -CsprojPath (Join-Path $Root "Downloader\Downloader.csproj") -Version $data.components.downloader
    if ($data.components.gameOffsets) {
        Set-CsprojVersion -CsprojPath (Join-Path $Root "GameOffsets\GameOffsets.csproj") -Version $data.components.gameOffsets
    }

    if (Test-Path (Join-Path $Root "versions.json")) {
        Remove-Item (Join-Path $Root "versions.json") -Force
    }
}
