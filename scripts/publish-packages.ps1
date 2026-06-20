# Core / full / optional plugin ZIP packages for publish.ps1

function Get-OptionalPluginDefinitions {
    param([string]$Root)

    $doc = Get-ComponentVersionsDocument -Root $Root
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($prop in $doc.plugins.PSObject.Properties) {
        if ($prop.Value.core -eq $true) {
            continue
        }

        $items.Add([ordered]@{
            Id      = [string]$prop.Name
            Folder  = [string]$prop.Value.folder
            Version = [string]$prop.Value.version
        })
    }

    return @($items)
}

function Get-CorePluginFolderNames {
    param([string]$Root)

    $map = Get-CorePluginFoldersMap -Root $Root
    return @($map.Values | Sort-Object -Unique)
}

function Get-CorePluginFoldersMap {
    param([string]$Root)

    $doc = Get-ComponentVersionsDocument -Root $Root
    $map = @{}
    foreach ($prop in $doc.plugins.PSObject.Properties) {
        if ($prop.Value.core -eq $true) {
            $map[[string]$prop.Name] = [string]$prop.Value.folder
        }
    }

    if ($map.Count -eq 0) {
        $fallback = Join-Path $Root "scripts\core-plugins.json"
        if (Test-Path $fallback) {
            $cfg = Get-Content $fallback -Raw | ConvertFrom-Json
            foreach ($id in @($cfg.corePluginIds)) {
                $plugin = $doc.plugins.$id
                if ($plugin) {
                    $map[[string]$id] = [string]$plugin.folder
                }
            }
        }
    }

    return $map
}

function Test-IsCorePublishPath {
    param(
        [string]$RelativePath,
        [string[]]$CorePluginFolders
    )

    if ($RelativePath -notmatch '^Plugins/([^/]+)/') {
        return $true
    }

    return $CorePluginFolders -contains $Matches[1]
}

function Select-CorePublishFiles {
    param(
        [array]$PublishFiles,
        [string]$Root
    )

    $coreFolders = @(Get-CorePluginFolderNames -Root $Root)
    return @($PublishFiles | Where-Object { Test-IsCorePublishPath -RelativePath $_.path -CorePluginFolders $coreFolders })
}

function Select-PluginPublishFiles {
    param(
        [array]$PublishFiles,
        [string]$PluginFolder
    )

    $prefix = "Plugins/$PluginFolder/"
    return @($PublishFiles | Where-Object {
        $path = [string]$_.path
        $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
    })
}

function New-PluginPackageZipName {
    param(
        [string]$PluginId,
        [string]$PluginVersion
    )

    return "GameHelper-Plugin-$PluginId-$PluginVersion.zip"
}

function New-PluginPackageManifestFiles {
    param([array]$FileEntries)

    return @($FileEntries | ForEach-Object {
        [ordered]@{ path = $_.path; hash = $_.hash }
    })
}

function Get-PluginUpstreamUrl {
    param([string]$PluginId)

    $upstream = @{
        Atlas             = 'https://github.com/yokkenUA/Atlas/releases/tag/v0.1.3'
        AutoHotKeyTrigger = 'https://github.com/Gordin/GameHelper2'
        AuraTracker         = 'https://github.com/MordWraith/AuraTracker'
        AmanamuVoidAlert    = 'https://github.com/MordWraith/AmanamuVoidAlert'
        MapKillCounter      = 'https://github.com/MordWraith/MapKillCounter'
        RitualHelper        = 'https://github.com/Queuete/GameHelper'
        RuneforgeHelper     = 'https://github.com/yokkenUA/RunecraftHelper'
        RunecraftHelper     = 'https://github.com/yokkenUA/RunecraftHelper'
        SekhemaHelper       = 'https://github.com/yokkenUA/SekhemaHelper'
        FarmTracker         = 'https://github.com/MordWraith/FarmTracker'
        Hiveblood           = 'https://github.com/MordWraith/Hiveblood'
        SimpleBars          = 'https://github.com/MordWraith/SimpleBars'
        Wraedar             = 'https://github.com/diesal/Wraedar'
    }

    if ($upstream.ContainsKey($PluginId)) {
        return [string]$upstream[$PluginId]
    }

    return ''
}

function Get-PluginForkBrowseUrl {
    param([string]$PluginId)

    $fork = @{
        FarmTracker        = 'https://github.com/MordWraith/FarmTracker'
        Hiveblood          = 'https://github.com/MordWraith/Hiveblood'
        AmanamuVoidAlert   = 'https://github.com/MordWraith/AmanamuVoidAlert'
        AuraTracker        = 'https://github.com/MordWraith/AuraTracker'
        MapKillCounter     = 'https://github.com/MordWraith/MapKillCounter'
        SimpleBars         = 'https://github.com/MordWraith/SimpleBars'
    }

    if ($fork.ContainsKey($PluginId)) {
        return [string]$fork[$PluginId]
    }

    return "https://github.com/MordWraith/Gamehelper/tree/main/Plugins/$PluginId"
}

function Get-PluginCatalogUiMetadata {
    param([string]$PluginId)

    $map = @{
        Atlas = @{
            Author = 'Nekkoy'
            DescriptionEn = 'Endgame atlas overlay (yokkenUA v0.1.3): search, chevron routes, hide available, expedition targets, content icons.'
            DescriptionDe = 'Endgame-Atlas-Overlay (yokkenUA v0.1.3): Suche, Chevron-Routen, Verfuegbare ausblenden, Expedition-Ziele, Content-Icons.'
        }
        AutoHotKeyTrigger = @{
            Author = 'GameHelper2 upstream'
            DescriptionEn = 'Configurable hotkey triggers for in-game actions.'
            DescriptionDe = 'Konfigurierbare Hotkey-Ausloeser fuer Spielaktionen.'
        }
        AuraTracker = @{
            Author = 'Skrip'
            DescriptionEn = 'Tracks player auras and reservation.'
            DescriptionDe = 'Zeigt Spieler-Auren und Reservierung an.'
        }
        MapKillCounter = @{
            Author = 'MordWraith'
            DescriptionEn = 'Kill counter for the current map.'
            DescriptionDe = 'Kill-Zaehler fuer die aktuelle Map.'
        }
        FarmTracker = @{
            Author = 'Senbry'
            DescriptionEn = 'Farm session tracker: loot value, profit/h, kills, maps, timers (poe2scout/ninja).'
            DescriptionDe = 'Farm-Session-Tracker: Loot-Wert, Profit/h, Kills, Maps, Timer (poe2scout/ninja).'
        }
        AmanamuVoidAlert = @{
            Author = '1k4ru5g3'
            DescriptionEn = 'Alerts for Amanamu void mechanics.'
            DescriptionDe = 'Warnungen bei Amanamu-Void-Mechaniken.'
        }
        PlayerBuffBar = @{
            Author = 'MordWraith'
            DescriptionEn = 'Compact player buff display.'
            DescriptionDe = 'Kompakte Anzeige der Spieler-Buffs.'
        }
        RitualHelper = @{
            Author = 'caio'
            DescriptionEn = 'Ritual reward prices in the Ritual panel.'
            DescriptionDe = 'Ritual-Belohnungspreise im Ritual-Panel.'
        }
        RuneforgeHelper = @{
            Author = 'Nekkoy'
            DescriptionEn = 'Runeshape prices (fork: poe2scout, display options). Use only RuneforgeHelper OR RunecraftHelper — not both.'
            DescriptionDe = 'Runeshape-Preise (Fork: poe2scout, Anzeige-Optionen). Nur RuneforgeHelper ODER RunecraftHelper — nicht beide.'
        }
        RunecraftHelper = @{
            Author = 'Nekkoy'
            DescriptionEn = 'Runeshape prices (yokkenUA upstream 1:1, poe.ninja). Use only RunecraftHelper OR RuneforgeHelper — not both.'
            DescriptionDe = 'Runeshape-Preise (yokkenUA upstream 1:1, poe.ninja). Nur RunecraftHelper ODER RuneforgeHelper — nicht beide.'
        }
        SekhemaHelper = @{
            Author = 'Nekkoy'
            DescriptionEn = 'Sekhema trial map pathing assistance.'
            DescriptionDe = 'Hilfe fuer Sekhema-Trial-Map-Pfade.'
        }
        Hiveblood = @{
            Author = 'MordWraith'
            DescriptionEn = 'Hiveblood estimate for PoE2: Genesis Tree sync + Breach gain popups; overlay at inventory.'
            DescriptionDe = 'Hiveblood-Schaetzung (PoE2): Genesis-Tree-Sync + Breach-Popups; Anzeige am Inventar.'
        }
        SimpleBars = @{
            Author = 'Reynbow'
            DescriptionEn = 'Lightweight on-screen health bars.'
            DescriptionDe = 'Leichte Lebensbalken auf dem Bildschirm.'
        }
        Wraedar = @{
            Author = 'Wraedar upstream'
            DescriptionEn = 'Map pins, tiles, and navigation helpers.'
            DescriptionDe = 'Map-Pins, Kacheln und Navigations-Hilfen.'
        }
    }

    if ($map.ContainsKey($PluginId)) {
        $item = $map[$PluginId]
        return [ordered]@{
            Author        = [string]$item.Author
            DescriptionEn = [string]$item.DescriptionEn
            DescriptionDe = [string]$item.DescriptionDe
            UpstreamUrl   = Get-PluginUpstreamUrl -PluginId $PluginId
        }
    }

    return [ordered]@{
        Author        = ''
        DescriptionEn = "Optional plugin package for $PluginId."
        DescriptionDe = "Optionales Plugin-Paket fuer $PluginId."
        UpstreamUrl   = Get-PluginUpstreamUrl -PluginId $PluginId
    }
}

function New-PluginsCatalogDocument {
    param(
        [string]$Root,
        [string]$CoreVersion,
        [string]$Published,
        [array]$PluginPackages
    )

    $doc = Get-ComponentVersionsDocument -Root $Root
    $plugins = [System.Collections.Generic.List[object]]::new()

    foreach ($pkg in $PluginPackages) {
        $pluginDef = $doc.plugins.($pkg.Id)
        $meta = Get-PluginCatalogUiMetadata -PluginId $pkg.Id
        $pluginVersion = [string]$pluginDef.version
        $plugins.Add([ordered]@{
            id               = $pkg.Id
            folder           = [string]$pluginDef.folder
            version          = $pluginVersion
            author           = $meta.Author
            displayName      = [ordered]@{ en = $pkg.Id; de = $pkg.Id }
            description      = [ordered]@{
                en = $meta.DescriptionEn
                de = $meta.DescriptionDe
            }
            upstreamUrl      = $meta.UpstreamUrl
            sourceUrl        = Get-PluginForkBrowseUrl -PluginId $pkg.Id
            package          = [ordered]@{
                name = $pkg.Name
                hash = $pkg.Hash
                size = $pkg.Size
            }
            minCoreVersion   = $CoreVersion
            files            = $pkg.Files
            defaultAutoStart = $false
        })
    }

    return [ordered]@{
        version   = $CoreVersion
        published = $Published
        plugins   = @($plugins)
    }
}

function Sign-PublishJsonDocument {
    param(
        [string]$JsonPath,
        [string]$Root
    )

    $null = & (Join-Path $PSScriptRoot "ensure-update-signing-key.ps1")
    $null = & (Join-Path $PSScriptRoot "sign-manifest.ps1") -ManifestPath $JsonPath -Root $Root

    $dir = Split-Path $JsonPath -Parent
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($JsonPath)
    $defaultSig = Join-Path $dir "manifest.sig"
    $targetSig = Join-Path $dir "$baseName.sig"
    if (Test-Path $defaultSig) {
        Move-Item $defaultSig $targetSig -Force
    }

    if (-not (Test-Path $targetSig)) {
        throw "Signatur fehlt fuer $JsonPath"
    }

    return , $targetSig
}

function Build-DistributionPackages {
    param(
        [string]$Root,
        [string]$CoreVersion,
        [string]$PublishDirectory,
        [array]$PublishFiles,
        [string]$PublishedAt
    )

    $coreZipName = "GameHelper-$CoreVersion-core.zip"
    $fullZipName = "GameHelper-$CoreVersion-full.zip"
    $coreZipPath = Join-Path $env:TEMP $coreZipName
    $fullZipPath = Join-Path $env:TEMP $fullZipName

    $coreFiles = @(Select-CorePublishFiles -PublishFiles $PublishFiles -Root $Root)

    Write-Host "  Erstelle Core-ZIP ($coreZipName, $($coreFiles.Count) Dateien) ..." -ForegroundColor DarkGray
    New-PublishZip -Version $CoreVersion -PublishDirectory $PublishDirectory -FileEntries $coreFiles -OutputPath $coreZipPath
    $coreHash = (Get-FileHash $coreZipPath -Algorithm SHA256).Hash
    $coreSize = (Get-Item $coreZipPath).Length

    Write-Host "  Erstelle Full-ZIP ($fullZipName, $($PublishFiles.Count) Dateien) ..." -ForegroundColor DarkGray
    New-PublishZip -Version $CoreVersion -PublishDirectory $PublishDirectory -FileEntries $PublishFiles -OutputPath $fullZipPath
    $fullHash = (Get-FileHash $fullZipPath -Algorithm SHA256).Hash
    $fullSize = (Get-Item $fullZipPath).Length

    $pluginPackages = [System.Collections.Generic.List[object]]::new()
    foreach ($plugin in @(Get-OptionalPluginDefinitions -Root $Root)) {
        $pluginFiles = @(Select-PluginPublishFiles -PublishFiles $PublishFiles -PluginFolder $plugin.Folder)
        if ($pluginFiles.Count -eq 0) {
            Write-Host "  Warnung: Keine Publish-Dateien fuer Plugin $($plugin.Id) ($($plugin.Folder))" -ForegroundColor Yellow
            continue
        }

        $zipName = New-PluginPackageZipName -PluginId $plugin.Id -PluginVersion $plugin.Version
        $zipPath = Join-Path $env:TEMP $zipName
        Write-Host "  Erstelle Plugin-ZIP $zipName ($($pluginFiles.Count) Dateien) ..." -ForegroundColor DarkGray
        New-PublishZip -Version $plugin.Version -PublishDirectory $PublishDirectory -FileEntries $pluginFiles -OutputPath $zipPath
        $pluginPackages.Add([ordered]@{
            Id     = $plugin.Id
            Name   = $zipName
            Path   = $zipPath
            Hash   = (Get-FileHash $zipPath -Algorithm SHA256).Hash
            Size   = (Get-Item $zipPath).Length
            Files  = @(New-PluginPackageManifestFiles -FileEntries $pluginFiles)
        })
    }

    $catalogPath = Join-Path $env:TEMP "plugins-catalog.json"
    $catalogDoc = New-PluginsCatalogDocument -Root $Root -CoreVersion $CoreVersion -Published $PublishedAt -PluginPackages @($pluginPackages)
    ($catalogDoc | ConvertTo-Json -Depth 8) | Set-Content $catalogPath -Encoding UTF8
    $catalogSigPath = Sign-PublishJsonDocument -JsonPath $catalogPath -Root $Root
    $catalogHash = (Get-FileHash $catalogPath -Algorithm SHA256).Hash
    $catalogSize = (Get-Item $catalogPath).Length

    return [ordered]@{
        CoreZipName      = $coreZipName
        CoreZipPath      = $coreZipPath
        CoreHash         = $coreHash
        CoreSize         = $coreSize
        CoreFiles        = $coreFiles
        FullZipName      = $fullZipName
        FullZipPath      = $fullZipPath
        FullHash         = $fullHash
        FullSize         = $fullSize
        PluginPackages   = @($pluginPackages)
        CatalogPath      = $catalogPath
        CatalogSigPath   = $catalogSigPath
        CatalogHash      = $catalogHash
        CatalogSize      = $catalogSize
    }
}

function Test-UseComponentDistributionPackages {
    param(
        [string]$Root,
        [switch]$LegacyManifest
    )

    if ($LegacyManifest) {
        return $false
    }

    return Test-Path (Get-VersionsFilePath -Root $Root)
}
