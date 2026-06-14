# Rollback: v1.3.2 monolith

Snapshot of Experimental **before** component-based versioning and core/optional plugin split.

## Restore (recommended)

From the repository root:

```powershell
.\scripts\restore-rollback-1.3.2-monolith.ps1
```

Or double-click `rollback-1.3.2-monolith.bat`.

## Restore via Git tag

If this folder is a Git repository with tag `v1.3.2-monolith`:

```powershell
git checkout v1.3.2-monolith -- .
# or full reset (discards all local changes):
git reset --hard v1.3.2-monolith
```

## What this snapshot is

- Monolith release version **1.3.2** (`GameHelper.App` + `GameHelper.exe`)
- Single version number for the whole distribution
- Full ZIP contains all plugins (no core-only bundle yet)

## After rollback

Rebuild with `rebuild-experimental.bat` and republish if needed.
