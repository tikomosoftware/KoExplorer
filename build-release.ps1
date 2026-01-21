# KoExplorer デュアルリリースビルドスクリプト
# 2つのビルドを作成: フレームワーク依存版（軽量）と自己完結型版（単一EXE）

param(
    [string]$Version = "0.1.0-alpha",
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Green
Write-Host "  KoExplorer Dual Release Build" -ForegroundColor Green
Write-Host "  Version: $Version" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# 変数定義
$ProjectFile = "KoExplorer.csproj"
$DistDir = "dist"
$TempFrameworkDir = "$DistDir\temp_framework"
$TempStandaloneDir = "$DistDir\temp_standalone"
$FrameworkZipFile = "$DistDir\KoExplorer-v$Version-framework-dependent-release.zip"
$StandaloneZipFile = "$DistDir\KoExplorer-v$Version-standalone-release.zip"

# ビルド開始時刻を記録
$BuildStartTime = Get-Date

# Create dist folder
if (-not (Test-Path $DistDir)) {
    Write-Host "Creating dist folder..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $DistDir | Out-Null
}

# Clean build option
if ($Clean) {
    Write-Host "Clean build..." -ForegroundColor Cyan
    dotnet clean -c Release
    if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
    if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
}

# 既存のZIPファイルを削除
if (Test-Path $FrameworkZipFile) {
    Remove-Item -Path $FrameworkZipFile -Force
}
if (Test-Path $StandaloneZipFile) {
    Remove-Item -Path $StandaloneZipFile -Force
}

# 一時ディレクトリを削除
if (Test-Path $TempFrameworkDir) {
    Remove-Item -Path $TempFrameworkDir -Recurse -Force
}
if (Test-Path $TempStandaloneDir) {
    Remove-Item -Path $TempStandaloneDir -Recurse -Force
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

# ========================================
# フレームワーク依存ビルド（軽量版）
# ========================================
Write-Host "Building Framework-Dependent (Lightweight)..." -ForegroundColor Cyan
$frameworkBuildSuccess = $false
try {
    Write-Host "  (.NET Runtime excluded - framework-dependent)" -ForegroundColor Gray
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -p:PublishSingleFile=false `
        -o $TempFrameworkDir
    
    if ($LASTEXITCODE -eq 0) {
        # Remove unnecessary files
        $filesToRemove = @("*.pdb", "*.xml", "*.deps.json")
        foreach ($pattern in $filesToRemove) {
            $files = Get-ChildItem -Path $TempFrameworkDir -Filter $pattern -Recurse -ErrorAction SilentlyContinue
            foreach ($file in $files) {
                Remove-Item $file.FullName -Force
            }
        }
        
        # Copy README
        if (Test-Path "README.md") {
            Copy-Item "README.md" "$TempFrameworkDir\README.md" -Force
        }
        
        # Create ZIP
        Compress-Archive -Path "$TempFrameworkDir\*" -DestinationPath $FrameworkZipFile -CompressionLevel Optimal
        Write-Host "  ✓ Framework-dependent build completed" -ForegroundColor Green
        $frameworkBuildSuccess = $true
    } else {
        throw "dotnet publish failed"
    }
} catch {
    Write-Host "  ✗ Framework-dependent build failed: $($_.Exception.Message)" -ForegroundColor Red
}

# ========================================
# 自己完結型ビルド（単一EXE版）
# ========================================
Write-Host ""
Write-Host "Building Self-Contained (Single EXE)..." -ForegroundColor Cyan
$standaloneBuildSuccess = $false
try {
    Write-Host "  (.NET Runtime included - standalone)" -ForegroundColor Gray
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $TempStandaloneDir
    
    if ($LASTEXITCODE -eq 0) {
        # Copy README
        if (Test-Path "README.md") {
            Copy-Item "README.md" "$TempStandaloneDir\README.md" -Force
        }
        
        # Create ZIP
        Compress-Archive -Path "$TempStandaloneDir\*" -DestinationPath $StandaloneZipFile -CompressionLevel Optimal
        Write-Host "  ✓ Self-contained build completed" -ForegroundColor Green
        $standaloneBuildSuccess = $true
    } else {
        throw "dotnet publish failed"
    }
} catch {
    Write-Host "  ✗ Self-contained build failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 両方のビルドが失敗した場合はエラー終了
if (-not $frameworkBuildSuccess -and -not $standaloneBuildSuccess) {
    Write-Error "Both builds failed"
    exit 1
}

# Cleanup temporary directories
Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
if (Test-Path $TempFrameworkDir) {
    Remove-Item -Path $TempFrameworkDir -Recurse -Force
}
if (Test-Path $TempStandaloneDir) {
    Remove-Item -Path $TempStandaloneDir -Recurse -Force
}
Write-Host "Cleanup completed" -ForegroundColor Green
Write-Host ""

# ビルド結果のサマリー表示
$BuildEndTime = Get-Date
$BuildDuration = $BuildEndTime - $BuildStartTime
$BuildTimeSeconds = [math]::Round($BuildDuration.TotalSeconds, 1)

Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Release build completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# フレームワーク依存ビルドの情報
if ($frameworkBuildSuccess -and (Test-Path $FrameworkZipFile)) {
    $frameworkZipInfo = Get-Item $FrameworkZipFile
    $frameworkZipHash = Get-FileHash $FrameworkZipFile -Algorithm SHA256
    
    Write-Host "📦 Framework-Dependent Build (Lightweight):" -ForegroundColor Cyan
    Write-Host "   File: $($frameworkZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($frameworkZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $($frameworkZipHash.Hash)" -ForegroundColor Gray
    Write-Host "   ⚠ Requires .NET 9.0 Desktop Runtime" -ForegroundColor Yellow
    Write-Host ""
}

# 自己完結型ビルドの情報
if ($standaloneBuildSuccess -and (Test-Path $StandaloneZipFile)) {
    $standaloneZipInfo = Get-Item $StandaloneZipFile
    $standaloneZipHash = Get-FileHash $StandaloneZipFile -Algorithm SHA256
    
    Write-Host "📦 Self-Contained Build (Single EXE):" -ForegroundColor Cyan
    Write-Host "   File: $($standaloneZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($standaloneZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $($standaloneZipHash.Hash)" -ForegroundColor Gray
    Write-Host "   ✓ No .NET Runtime installation required" -ForegroundColor Green
    Write-Host ""
}

Write-Host "⏱ Total build time: $BuildTimeSeconds seconds" -ForegroundColor White
Write-Host ""
Write-Host "Distribution files:" -ForegroundColor Cyan
Write-Host "  $DistDir\" -ForegroundColor White
Write-Host ""
