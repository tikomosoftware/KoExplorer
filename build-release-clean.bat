@echo off
REM KoExplorer Clean Release Build
REM Build from completely clean state

echo ==========================================
echo   KoExplorer Clean Release Build
echo ==========================================
echo.

REM Execute PowerShell script with Clean option
powershell -ExecutionPolicy Bypass -File "%~dp0build-release.ps1" -Clean

echo.
echo Build completed.
pause
