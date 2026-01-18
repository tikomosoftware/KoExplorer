# KoExplorer Release Build Script
# Creates a distribution ZIP file

param(
    [string]$Version = "0.1.0-alpha",
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Green
Write-Host "  KoExplorer Release Build" -ForegroundColor Green
Write-Host "  Version: $Version" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Project paths
$projectRoot = $PSScriptRoot
$distFolder = Join-Path $projectRoot "dist"
$publishFolder = Join-Path $projectRoot "bin\Release\net9.0-windows\win-x64\publish"

# Create dist folder
if (-not (Test-Path $distFolder)) {
    Write-Host "Creating dist folder..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $distFolder | Out-Null
}

# Clean build option
if ($Clean) {
    Write-Host "Clean build..." -ForegroundColor Cyan
    dotnet clean -c Release
    if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
    if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
}

# Step 1: Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore packages" -ForegroundColor Red
    exit 1
}
Write-Host "Package restore completed" -ForegroundColor Green
Write-Host ""

# Step 2: Build Release
Write-Host "Building Release..." -ForegroundColor Cyan
dotnet build -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed" -ForegroundColor Green
Write-Host ""

# Step 3: Publish (framework-dependent)
Write-Host "Publishing application..." -ForegroundColor Cyan
Write-Host "  (.NET Runtime excluded - framework-dependent)" -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "Publish completed" -ForegroundColor Green
Write-Host ""

# Step 4: Remove unnecessary files
Write-Host "Removing unnecessary files..." -ForegroundColor Cyan
$filesToRemove = @("*.pdb", "*.xml", "*.deps.json")

foreach ($pattern in $filesToRemove) {
    $files = Get-ChildItem -Path $publishFolder -Filter $pattern -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        Remove-Item $file.FullName -Force
        Write-Host "  Removed: $($file.Name)" -ForegroundColor DarkGray
    }
}
Write-Host "Cleanup completed" -ForegroundColor Green
Write-Host ""

# Step 5: Create ZIP file
$zipFileName = "KoExplorer-v$Version-win-x64-release.zip"
$zipFilePath = Join-Path $distFolder $zipFileName

Write-Host "Creating ZIP file..." -ForegroundColor Cyan
Write-Host "  Output: $zipFilePath" -ForegroundColor Yellow

# Remove existing ZIP file
if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath -Force
}

# Create ZIP file
Compress-Archive -Path "$publishFolder\*" -DestinationPath $zipFilePath -CompressionLevel Optimal

if (Test-Path $zipFilePath) {
    $zipSize = (Get-Item $zipFilePath).Length / 1MB
    $zipSizeRounded = [math]::Round($zipSize, 2)
    Write-Host "ZIP file created successfully" -ForegroundColor Green
    Write-Host "  File name: $zipFileName" -ForegroundColor Green
    Write-Host "  Size: $zipSizeRounded MB" -ForegroundColor Green
}
else {
    Write-Host "Failed to create ZIP file" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 6: Display distribution file contents
Write-Host "Distribution file contents:" -ForegroundColor Cyan
$publishFiles = Get-ChildItem -Path $publishFolder -Recurse -File
$totalSize = ($publishFiles | Measure-Object -Property Length -Sum).Sum / 1MB
$totalSizeRounded = [math]::Round($totalSize, 2)
Write-Host "  File count: $($publishFiles.Count)" -ForegroundColor Yellow
Write-Host "  Total size: $totalSizeRounded MB" -ForegroundColor Yellow
Write-Host ""

# Display main files
Write-Host "Main files:" -ForegroundColor Cyan
$mainFiles = Get-ChildItem -Path $publishFolder -File | Where-Object { 
    $_.Extension -in @('.exe', '.dll', '.json', '.ico') 
} | Sort-Object Length -Descending | Select-Object -First 10

foreach ($file in $mainFiles) {
    $size = $file.Length / 1KB
    $sizeRounded = [math]::Round($size, 2)
    $fileName = $file.Name.PadRight(40)
    Write-Host "  $fileName $sizeRounded KB" -ForegroundColor DarkGray
}
Write-Host ""

# Completion message
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Release build completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Distribution file:" -ForegroundColor Cyan
Write-Host "  $zipFilePath" -ForegroundColor White
Write-Host ""
Write-Host "Note:" -ForegroundColor Yellow
Write-Host "  This build requires .NET 9.0 Runtime" -ForegroundColor Yellow
Write-Host "  Users can install it from:" -ForegroundColor Yellow
Write-Host "  https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor White
Write-Host ""

# Open dist folder
Write-Host "Open dist folder? (Y/N)" -ForegroundColor Cyan
$response = Read-Host
if ($response -eq 'Y' -or $response -eq 'y') {
    Start-Process explorer.exe -ArgumentList $distFolder
}
