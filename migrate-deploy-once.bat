@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\migrate-experimental-deploy-to-publish.ps1"
pause
