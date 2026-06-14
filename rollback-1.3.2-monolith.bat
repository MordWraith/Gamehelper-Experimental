@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\restore-rollback-1.3.2-monolith.ps1" %*
pause
