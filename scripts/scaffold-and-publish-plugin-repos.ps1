# Scaffolds optional plugin standalone repos and pushes to github.com/MordWraith/{PluginId}
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
$SrcPlugins = Join-Path $Root 'Plugins'
$OutBase = 'D:\ZusatzProgramme'
$License = Join-Path $OutBase 'FarmTracker\LICENSE'
$GitIgnore = Join-Path $OutBase 'FarmTracker\.gitignore'
$GitName = 'MordWraith'
$GitEmail = 'mordwraith@users.noreply.github.com'

$versions = Get-Content (Join-Path $Root 'versions.json') -Raw | ConvertFrom-Json

function Get-OptionalPluginIds {
    $ids = @()
    foreach ($p in $versions.plugins.PSObject.Properties) {
        if ($p.Value.core -eq $false) { $ids += [string]$p.Name }
    }
    return $ids | Sort-Object
}

function Get-PluginMeta {
    param([string]$Id)
    $v = [string]$versions.plugins.$Id.version
    $folder = [string]$versions.plugins.$Id.folder
    $map = @{
        Atlas = @{
            AuthorLine = 'Nekkoy (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/yokkenUA/Atlas'
            Bullets = @(
                'Endgame **atlas overlay**: search, pathing, map content badges'
                'Biome / content JSON data shipped with the plugin'
                'Fork integration for community GameHelper2 builds'
            )
            ConfigNote = 'config/settings.txt — overlay and search options (JSON)'
            CreditOriginal = '**Nekkoy** — [yokkenUA/Atlas](https://github.com/yokkenUA/Atlas)'
            CreditPort = '**MordWraith**'
        }
        AutoHotKeyTrigger = @{
            AuthorLine = 'GameHelper2 upstream (fork: MordWraith)'
            Upstream = 'https://github.com/Gordin/GameHelper2'
            Bullets = @(
                'Configurable **hotkey triggers** (flasks, skills, dynamic conditions)'
                'Profiles, rules, cooldowns, minion-skill templates'
                'Uses fork-specific `AhkKeySender` (WM_KEYUP) for stable PoE key injection'
            )
            ConfigNote = 'config/settings.txt — profiles and rules (JSON)'
            CreditOriginal = '**GameHelper2 / Gordin** upstream AHK plugin'
            CreditPort = '**MordWraith** (fork maintenance, AhkKeySender, pause-menu guards)'
        }
        AuraTracker = @{
            AuthorLine = 'Skrip (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/derekShaheen/AuraTracker'
            Bullets = @(
                '**Nearby enemy panel**: HP/ES bars, buff icons, per-target **DPS**'
                'Optional total DPS header, filters, draggable layout'
                'German/English settings UI in this fork'
            )
            ConfigNote = 'config/AuraTracker.settings.json'
            CreditOriginal = '**Skrip** — [derekShaheen/AuraTracker](https://github.com/derekShaheen/AuraTracker)'
            CreditPort = '**MordWraith**'
        }
        MapKillCounter = @{
            AuthorLine = 'MordWraith'
            Upstream = ''
            Bullets = @(
                'Per-**map** and per-**session** kills by rarity (**N/M/R/U**)'
                'Map timer + optional session overlay (pauses in town, ESC, background)'
                'Horizontal/vertical layout, resizable overlay, custom colors'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**MordWraith**'
            CreditPort = ''
        }
        FarmTracker = @{
            AuthorLine = 'Senbry (GameHelper2 port: MordWraith)'
            Upstream = ''
            Bullets = @(
                'Session farm tracker: timers, **N/M/R/U** kills, **loot profit** (div equivalent)'
                'Prices from poe2scout / poe.ninja + custom_prices.txt'
                'Map/session overlays, archive, map resume; **sub-areas** (Abyss, etc.) count as same run'
            )
            ConfigNote = 'config/settings.txt, custom_prices.txt, sessions/'
            CreditOriginal = '**Senbry**'
            CreditPort = '**MordWraith**'
        }
        Hiveblood = @{
            AuthorLine = 'MordWraith'
            Upstream = ''
            Bullets = @(
                '**Hiveblood** tracker for PoE2 (Genesis Tree sync)'
                '**+N** gains from Breach popups; cap warning near 100k'
                'Draggable overlay + position dummy; DE/EN settings'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**MordWraith**'
            CreditPort = ''
        }
        AmanamuVoidAlert = @{
            AuthorLine = '1k4ru5g3 (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/1k4ru5g3/AmanamuVoidAlertPlugin'
            Bullets = @(
                'Tracks **Abyss / Amanamu void** Lightless monsters'
                'On-screen labels + off-screen arrows (inside/outside cloud colors)'
                'Rare/unique filter, distance timers, optional debug window'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**1k4ru5g3** — [AmanamuVoidAlertPlugin](https://github.com/1k4ru5g3/AmanamuVoidAlertPlugin)'
            CreditPort = '**MordWraith**'
        }
        PlayerBuffBar = @{
            AuthorLine = 'MordWraith'
            Upstream = ''
            Bullets = @(
                'Up to **4 independent buff bars** — each with own watchlist, position, icon size'
                '**Resource row**: power / frenzy / endurance charges + rage (poe2db icons)'
                'Buff icons from **poe2db.tw** (auto-download), durations, stacks, inactive preview'
                'Skill aliases (e.g. **refutation** → `runic_fortress`) for Kalguuran buffs'
            )
            ConfigNote = 'config/settings.txt, icons/ (auto-cached)'
            CreditOriginal = '**MordWraith**'
            CreditPort = ''
        }
        RitualHelper = @{
            AuthorLine = 'caio (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/Queuete/GameHelper'
            Bullets = @(
                '**Ritual reward prices** from poe.ninja in the Ritual UI'
                'Green/red item boxes; **Ctrl+C** auto-mapping for unmapped uniques'
                'Currency icons and alert sounds (configurable)'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**caio** — AutoRitualPricer / [Queuete/GameHelper](https://github.com/Queuete/GameHelper) lineage'
            CreditPort = '**MordWraith**'
        }
        RuneforgeHelper = @{
            AuthorLine = 'Nekkoy (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/yokkenUA/RunecraftHelper'
            Bullets = @(
                '**Runeshape** reward prices in the Runeforge UI (poe2scout)'
                'DE/EN display options — fork variant of RunecraftHelper'
                'Use **only RuneforgeHelper OR RunecraftHelper**, not both'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**Nekkoy** — [yokkenUA/RunecraftHelper](https://github.com/yokkenUA/RunecraftHelper)'
            CreditPort = '**MordWraith**'
        }
        RunecraftHelper = @{
            AuthorLine = 'Nekkoy (upstream 1:1 port: MordWraith)'
            Upstream = 'https://github.com/yokkenUA/RunecraftHelper'
            Bullets = @(
                '**Runeshape** reward prices (poe.ninja) in the Runecraft UI'
                'Upstream-aligned fork build for GameHelper2'
                'Use **only RunecraftHelper OR RuneforgeHelper**, not both'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**Nekkoy** — [yokkenUA/RunecraftHelper](https://github.com/yokkenUA/RunecraftHelper)'
            CreditPort = '**MordWraith**'
        }
        SekhemaHelper = @{
            AuthorLine = 'Nekkoy (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/yokkenUA/SekhemaHelper'
            Bullets = @(
                '**Sekhema Trial** map pathing / option helper overlay'
                'Highlights trial choices and paths for GameHelper2'
            )
            ConfigNote = 'config/settings.txt'
            CreditOriginal = '**Nekkoy** — [yokkenUA/SekhemaHelper](https://github.com/yokkenUA/SekhemaHelper)'
            CreditPort = '**MordWraith**'
        }
        SimpleBars = @{
            AuthorLine = 'Reynbow (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/Reynbow/simplebars'
            Bullets = @(
                'Lightweight on-screen **health bars** (gradient / circle-dot)'
                'POI monsters, town/hideout toggles — disable built-in **HealthBars**'
                'Requires `Textures/` PNGs (copy from Reynbow/simplebars if missing)'
            )
            ConfigNote = 'config/settings.txt, Textures/'
            CreditOriginal = '**Reynbow** — [simplebars](https://github.com/Reynbow/simplebars)'
            CreditPort = '**MordWraith**'
        }
        Wraedar = @{
            AuthorLine = 'Wraedar upstream (GameHelper2 port: MordWraith)'
            Upstream = 'https://github.com/diesal/Wraedar'
            Bullets = @(
                '**Map pins**, tiles, and navigation helpers for GameHelper2'
                'Fork port of Wraedar map tools'
            )
            ConfigNote = 'config/settings.txt (if used by build)'
            CreditOriginal = '**Wraedar** — [diesal/Wraedar](https://github.com/diesal/Wraedar)'
            CreditPort = '**MordWraith**'
        }
    }
    if (-not $map.ContainsKey($Id)) { throw "No metadata for $Id" }
    $m = $map[$Id]
    $m.Version = $v
    $m.Folder = $folder
    $m.RepoUrl = "https://github.com/MordWraith/$Id"
    return $m
}

function New-Readme {
    param($Id, $Meta)
    $upstreamLine = if ($Meta.Upstream) { "Upstream: $($Meta.Upstream)`n" } else { '' }
    $bullets = ($Meta.Bullets | ForEach-Object { "- $_" }) -join "`n"
    $creditPort = if ($Meta.CreditPort) { "`n- GameHelper2 port: $($Meta.CreditPort)" } else { '' }
    @"
# $Id

Community plugin for [GameHelper2](https://github.com/Gordin/GameHelper2) (Path of Exile 2).

$($Meta.Bullets[0].Replace('**','').Split(':')[0]) — maintained for community GameHelper2 forks. **Build from source.**

## Features

$bullets

Read-only overlay — no input automation (except AutoHotKeyTrigger, which sends configured keys).

## Requirements

- [GameHelper2](https://github.com/Gordin/GameHelper2) source tree
- **.NET 10 SDK** (`net10.0-windows`, x64)

## Build & install

``````bash
git clone https://github.com/Gordin/GameHelper2.git
cd GameHelper2
git clone $($Meta.RepoUrl).git Plugins/$Id
dotnet build GameHelper/GameHelper.csproj -c Release
dotnet build Plugins/$Id/$Id.csproj -c Release
``````

Enable **$Id** in GameHelper → Plugins.

### Config

- $($Meta.ConfigNote)

## Credits

- $($Meta.CreditOriginal)$creditPort
- Host: [GameHelper2](https://github.com/Gordin/GameHelper2)

## Disclaimer

Third-party plugin — use at your own risk. Comply with GGG terms of service.

## Version history

| Version | Notes |
|---------|--------|
| **$($Meta.Version)** | Community GameHelper2 release |
"@
}

function New-Credits {
    param($Id, $Meta)
    $portRow = if ($Meta.CreditPort) { "| GameHelper2 port | $($Meta.CreditPort) |`n" } else { '' }
    @"
# Credits

| Role | Name / link |
|------|-------------|
| Original / author | $($Meta.CreditOriginal) |
$portRow| Host platform | [Gordin/GameHelper2](https://github.com/Gordin/GameHelper2) |

Standalone repo: $($Meta.RepoUrl)
"@
}

function New-DiscordPost {
    param($Id, $Meta)
    $upstreamLine = if ($Meta.Upstream) { "**Upstream:** $($Meta.Upstream)  `n" } else { '' }
    $bullets = ($Meta.Bullets | ForEach-Object { "- $_" }) -join "`n"
    $extraInstall = ''
    if ($Id -eq 'SimpleBars') {
        $extraInstall = @"

3. If ``Textures/`` is empty, copy bar PNGs from [Reynbow/simplebars](https://github.com/Reynbow/simplebars/tree/main/Textures)
4. Build:
"@
    }
    else {
        $extraInstall = '3. Build:'
    }
    @"
# Discord forum — first post (copy into ``#plugins``)

**Title:** ``$Id``

**Tags:** ``community``

---

## $Id

**Author:** $($Meta.AuthorLine)  
**Source:** $($Meta.RepoUrl)  
$upstreamLine**Compatible with:** [Gordin/GameHelper2](https://github.com/Gordin/GameHelper2) ``main``, **.NET 10** (``net10.0-windows``, x64). Works on maintained community forks on the same API/target.

### What it does (PoE2)

$bullets

Read-only overlay — no input automation$(if ($Id -eq 'AutoHotKeyTrigger') { ' except configured key sends from rules' } else { '' }).

### Install (source only — no prebuilt DLL)

1. Clone [GameHelper2](https://github.com/Gordin/GameHelper2)
2. Clone this plugin into ``Plugins/$Id``:
   ``````
   git clone $($Meta.RepoUrl).git Plugins/$Id
   ``````
$extraInstall
   ``````
   dotnet build GameHelper/GameHelper.csproj -c Release
   dotnet build Plugins/$Id/$Id.csproj -c Release
   ``````
$(if ($Id -ne 'SimpleBars') { '4' } else { '5' }). Enable **$Id** in GameHelper → Plugins

Full README: $($Meta.RepoUrl)

### Config

- $($Meta.ConfigNote)

### v$($Meta.Version)

- Community GameHelper2 release

### Support

- Feedback / feature requests: **this thread**
- Crashes / GameHelper install issues: ``#help`` with tag **``plugin``**

### Disclaimer

Community third-party plugin. Use at your own risk. Not affiliated with GGG. Only install from trusted sources.
"@
}

function Ensure-ConfigTemplate {
    param([string]$Dest, [string]$Id)
    $cfg = Join-Path $Dest 'config'
    if (-not (Test-Path $cfg)) { New-Item -ItemType Directory -Path $cfg -Force | Out-Null }
    switch ($Id) {
        'AuraTracker' {
            $f = Join-Path $cfg 'AuraTracker.settings.json'
            if (-not (Test-Path $f)) { '{}' | Set-Content $f -Encoding UTF8 }
        }
        'FarmTracker' {
            $f = Join-Path $cfg 'settings.txt'
            if (-not (Test-Path $f)) { '{}' | Set-Content $f -Encoding UTF8 }
            $cp = Join-Path $Dest 'custom_prices.txt'
            if (-not (Test-Path $cp)) { '# ItemName=divine' | Set-Content $cp -Encoding UTF8 }
        }
        default {
            $f = Join-Path $cfg 'settings.txt'
            if (-not (Test-Path $f)) { '{}' | Set-Content $f -Encoding UTF8 }
        }
    }
}

function Publish-PluginRepo {
    param([string]$Id)
    $meta = Get-PluginMeta -Id $Id
    $src = Join-Path $SrcPlugins $meta.Folder
    if (-not (Test-Path $src)) { throw "Source missing: $src" }

    $dest = Join-Path $OutBase $Id
    $repoExists = $false
    gh repo view "MordWraith/$Id" 1>$null 2>$null
    if ($LASTEXITCODE -eq 0) { $repoExists = $true }

    if ($repoExists -and (Test-Path (Join-Path $dest '.git'))) {
        Write-Host "Refresh $Id (keep git history) ..." -ForegroundColor DarkGray
        robocopy $src $dest /E /XD bin obj .git /XF *.user /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    }
    else {
        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
        robocopy $src $dest /E /XD bin obj .git /XF *.user /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        git -C $dest init -q
        git -C $dest branch -M master 2>$null
    }

    Copy-Item $License $dest -Force
    Copy-Item $GitIgnore $dest -Force
    Ensure-ConfigTemplate -Dest $dest -Id $Id

    New-Readme -Id $Id -Meta $meta | Set-Content (Join-Path $dest 'README.md') -Encoding UTF8
    New-Credits -Id $Id -Meta $meta | Set-Content (Join-Path $dest 'CREDITS.md') -Encoding UTF8
    New-DiscordPost -Id $Id -Meta $meta | Set-Content (Join-Path $dest 'DISCORD_FORUM_POST.md') -Encoding UTF8

    git -C $dest add -A
    $status = git -C $dest status --porcelain
    if ($status) {
        git -C $dest -c user.name=$GitName -c user.email=$GitEmail commit -q -m "Community GameHelper2 release (v$($meta.Version))"
    }

    if (-not $repoExists) {
        gh repo create "MordWraith/$Id" --public --description "GameHelper2 plugin: $Id" --source $dest --remote origin --push
    }
    else {
        if (-not (git -C $dest remote get-url origin 2>$null)) {
            git -C $dest remote add origin "https://github.com/MordWraith/$Id.git"
        }
        git -C $dest push -u origin master
    }
    Write-Host "OK $Id -> $($meta.RepoUrl)" -ForegroundColor Green
}

$ids = Get-OptionalPluginIds
foreach ($id in $ids) {
    Publish-PluginRepo -Id $id
}

Write-Host "Done. $($ids.Count) optional plugin repos." -ForegroundColor Cyan
