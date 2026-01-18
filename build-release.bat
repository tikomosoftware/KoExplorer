@echo off
REM KoExplorer Release Build
REM Double-click this file to create release build

echo ==========================================
echo   KoExplorer Release Build
echo ==========================================
echo.

REM Execute PowerShell script
powershell -ExecutionPolicy Bypass -File "%~dp0build-release.ps1"

echo.
echo Build completed.
pause
