# Wraedar (experimental port)

Wraedar is ported from [diesal/Wraedar](https://github.com/diesal/Wraedar) v1.0.4 for the MordWraith GameHelper experimental fork (`net10.0-windows`).

## Notes

- **Experimental only** — not part of the stable GameHelper release channel.
- **Runs alongside Radar** — both plugins can be enabled; they use separate overlay layers and settings.
- **DieselExileTools** — the `Plugins/Wraedar/DieselExileTools` project is a compatibility shim that recreates the original DieselExileTools API on top of GameHelper. Wraedar source is otherwise kept close to upstream.

## Build

```bash
dotnet build "Plugins/Wraedar/Wraedar.csproj" -c Release
```

Output is copied to `GameHelper/bin/Release/net10.0-windows/Plugins/Wraedar` (DLL, PDB, `PinFiles/`, `Media/MapIcons.png`).

## Map alignment

Use **Map Fix** (LargeMapCenterFix) in Wraedar settings if icons drift on the large map. Map scale uses the same baseline as Radar (`MapScaleModifier` default `0.187812`).
