# GameHelper Experimental

**Early-access / test builds** of [GameHelper](https://github.com/MordWraith/Gamehelper-Experimental) for Path of Exile 2.

This is **not** the stable release. Use it only if you want to test the component-based Core + in-app plugin system before it ships to everyone. Bugs and breaking changes are expected.

| | **Stable** | **Experimental (this repo)** |
|---|------------|------------------------------|
| **For** | Normal players | Testers, early feedback |
| **GitHub** | [MordWraith/Gamehelper](https://github.com/MordWraith/Gamehelper-Experimental) | [MordWraith/Gamehelper-Experimental](https://github.com/MordWraith/Gamehelper-Experimental) |
| **Installer** | [GameHelperDownloader.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader.exe) | [GameHelperDownloader-Experimental.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader-Experimental.exe) |
| **First install** | Full package (all bundled plugins) | **Core package only** (4 core plugins) |
| **Optional plugins** | Included in download | **Download on demand** from GitHub (in-app catalog) |
| **Auto-update** | Stable releases only | Experimental releases only |

**Do not mix installers.** The stable downloader installs stable builds. This repo has its own downloader and update channel (`github.config.json` in the install folder).

---

## Download

Requires [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (same as stable).

| What | Link |
|------|------|
| **Installer (recommended)** | [GameHelperDownloader-Experimental.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader-Experimental.exe) |
| **Core ZIP** | [GameHelper-*-core.zip](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) on [Releases](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) |
| **Full ZIP (all plugins)** | [GameHelper-*-full.zip](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) — or installer with `--full` |
| **Plugin catalog** | [plugins-catalog.json](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) (+ signature) on [Releases](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) |
| **All releases** | [Releases](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) |

### Install (default = Core only)

1. Create an **empty folder** (do not install over stable GameHelper in the same folder).
2. Run **[GameHelperDownloader-Experimental.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader-Experimental.exe)** in that folder.
   - Downloads **core ZIP**: launcher, overlay, and **4 core plugins** (AutoPot, HealthBars, Radar, PreloadAlert).
   - Also downloads **plugins-catalog.json** (signed list of optional plugins).
3. Start **`GameHelper.exe`**.

Optional plugins (Atlas, RitualHelper, Wraedar, …) are **not** included in the default install.

### Install optional plugins (in-app)

1. Open **Plugins → Plugin catalog**.
2. Click **Download** on the plugins you want.
3. Enable them under **Plugins → Installed plugins**.

Plugins are fetched as signed ZIPs from this repo’s [GitHub Releases](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest) (`GameHelper-Plugin-<Name>-<Version>.zip`).

### CLI (optional)

```text
GameHelperDownloader-Experimental.exe <folder> --install-plugin Atlas
GameHelperDownloader-Experimental.exe <folder> --remove-plugin Atlas
GameHelperDownloader-Experimental.exe <folder> --full
```

`--full` installs the complete package with all plugins (like stable), instead of Core-only.

You can keep stable and experimental side by side in **separate folders**.

### Windows Defender / helper won't start

Auto-update, plugin downloads, and unsigned DLLs often trigger **false positives** (`Wacatac`, `PowhidSubExec`, etc.). See [SECURITY.md](../SECURITY.md#windows-defender-and-antivirus-false-positives): allow in **Protection history**, or install from the **core/full ZIP** into a new folder. Reporting blocks is helpful — no need to audit source code yourself.

---

## Report issues

Experimental feedback: contact the maintainer or your test group (Discord, etc.).  
For stable bugs, use the main [Gamehelper](https://github.com/MordWraith/Gamehelper-Experimental) repo.

---

## Deutsch

**Experimental** = Testversion mit **Core + optionalem Plugin-Katalog**, **nicht** fuer den normalen Spielbetrieb.

| | **Stabil** | **Experimental** |
|---|------------|------------------|
| Repo | [Gamehelper](https://github.com/MordWraith/Gamehelper-Experimental) | [Gamehelper-Experimental](https://github.com/MordWraith/Gamehelper-Experimental) |
| Installer | [GameHelperDownloader.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader.exe) | [GameHelperDownloader-Experimental.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader-Experimental.exe) |
| Erstinstallation | Alles in einem Paket | **Nur Core** (4 Plugins) |
| Optionale Plugins | Im Download enthalten | **In der App herunterladen** (Plugin-Katalog) |

**Download:** [GameHelperDownloader-Experimental.exe](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest/download/GameHelperDownloader-Experimental.exe)

In einen **leeren Ordner** installieren. Standard = Core-Paket. Optionale Plugins unter **Plugins → Plugin-Katalog → Herunterladen**.  
Vollstaendiges Paket: `--full` am Downloader oder [Full-ZIP auf Releases](https://github.com/MordWraith/Gamehelper-Experimental/releases/latest).
