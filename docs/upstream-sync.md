# Upstream sync (Gordin GameHelper2)

Experimental keeps **Radar** and **AutoHotKeyTrigger** aligned with [Gordin/GameHelper2](https://github.com/Gordin/GameHelper2). Core, plugin store, theme, and optional plugins stay fork-specific.

## Reference points

| Item | Value |
|------|--------|
| Upstream repo | `https://github.com/Gordin/GameHelper2` |
| Upstream branch | `main` |
| Last full plugin sync | `618c3c5` (2026-06, Ritual/Runestone + Co-op baseline) |
| Pre-cleanup snapshot tag | `pre-upstream-cleanup` → commit before Radar/AHK reset |

To inspect upstream Radar/AHK history:

```bash
gh api repos/Gordin/GameHelper2/commits?path=Plugins/Radar&per_page=10
gh api repos/Gordin/GameHelper2/commits?path=Plugins/AutoHotKeyTrigger&per_page=10
```

Or clone shallow:

```bash
git clone --depth 1 https://github.com/Gordin/GameHelper2.git %TEMP%\gordin-gh2
```

## Quick sync workflow (~15–30 min)

Applies when Gordin changes `Plugins/Radar/` and/or `Plugins/AutoHotKeyTrigger/` only.

1. **Check upstream** — read commit messages / diff for the plugin folder.
2. **Copy** — replace plugin sources from upstream (`.cs`, `.csproj`, `.json`, `.txt`; exclude `bin/` and `obj/`).
3. **Re-apply fork patches** — see [Conscious fork patches](#conscious-fork-patches) (should be empty or minimal).
4. **Build**
   ```bash
   dotnet build GameOverlay.sln -c Release
   ```
5. **Smoke test in-game** — see [Test checklist](#test-checklist).
6. **Update this file** — set *Last full plugin sync* to the upstream commit hash and date.

### Copy example (PowerShell)

```powershell
$src = "$env:TEMP\gordin-gh2\Plugins"
$exp = "D:\ZusatzProgramme\Gamehelper-Experimental-Src\Plugins"

robocopy "$src\Radar" "$exp\Radar" /XD bin obj /NFL /NDL
robocopy "$src\AutoHotKeyTrigger" "$exp\AutoHotKeyTrigger" *.cs *.json *.csproj /S /XD bin obj /NFL /NDL
```

Restore plugin versions in `.csproj` if needed (`versions.json` is the store source of truth).

## Do not reset from upstream

These are Experimental fork value — merge Gordin core changes manually when needed:

| Area | Path / notes |
|------|----------------|
| Plugin store & updater | `GameHelper/PluginStore/`, `Shared/PluginPackageManager.cs`, pending updates |
| Theme & settings UI | `GameHelper/Ui/`, `GameHelper/Settings/SettingsWindow.cs` |
| Overlay localization shim | `GameHelper` (`OverlayLocalization`) — other plugins may still use `L()` |
| Game offsets & actor fixes | `GameOffsets/`, e.g. `DeployedEntityArray` minion fix |
| Optional plugins | Atlas, RitualHelper, Wraedar, AuraTracker, … |
| Version manifest | `versions.json`, publish scripts |

**Stable** (`D:\ZusatzProgramme\Gamehelper`) is a separate repo; sync Radar/AHK there only when explicitly planned (no plugin store).

## Conscious fork patches

After the 2026 upstream cleanup, Radar and AHK should match Gordin with **no standing patches**. If you must diverge, document each patch here:

| Plugin | File | Reason | Upstream issue/PR |
|--------|------|--------|-------------------|
| *(none)* | | | |

Prefer opening PRs to Gordin instead of long-lived fork diffs.

## Removed fork features (do not re-add without reason)

- **Radar:** `IconPixelPerfect`, boss-arena default restore (`boss_arena_tgt_files.default.txt`), DE/EN `L()` in plugin UI
- **AutoHotKey:** `AhkKeySender.cs`, `AutoHotKeyTriggerJson.cs`, `TemplateUi.cs`, DE/EN in templates

Minion command offset fix lives in **GameOffsets** (core), not in the AHK plugin.

## Test checklist

### Radar

- [ ] Minimap and large map icons render
- [ ] Runestone encounter icon, socket count, path line (cyan)
- [ ] Ritual icons and path line (purple)
- [ ] Path planning / hide reached paths
- [ ] Local co-op mode (`AutoDetectCoopMode`, `EnableCoopMode`)
- [ ] Settings save/load (`Plugins/Radar/config/settings.txt`)

After upstream icon default changes, delete local Radar settings once so defaults apply.

### AutoHotKeyTrigger

- [ ] Profiles load from existing `config/`
- [ ] Minion command condition (relies on core `DeployedEntityArray` offset)
- [ ] Nearby monsters, status effects, vitals, flask rules
- [ ] Key send / rule execution in game

## Rollback

To restore the pre-cleanup plugin state:

```bash
git checkout pre-upstream-cleanup -- Plugins/Radar Plugins/AutoHotKeyTrigger
dotnet build GameOverlay.sln -c Release
```

Tag `pre-upstream-cleanup` points at the last commit **before** the Gordin Radar/AHK reset.
