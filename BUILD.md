# KoExplorer - ビルド手順

このドキュメントでは、KoExplorerのビルド方法を詳しく説明します。

## 目次
- [前提条件](#前提条件)
- [開発環境のセットアップ](#開発環境のセットアップ)
- [ビルド方法](#ビルド方法)
- [実行方法](#実行方法)
- [配布パッケージの作成](#配布パッケージの作成)
- [トラブルシューティング](#トラブルシューティング)

## 前提条件

### 必須
- **OS**: Windows 10 (Version 1809以降) または Windows 11
- **.NET 9.0 SDK**: [ダウンロード](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Git**: [ダウンロード](https://git-scm.com/downloads)

### 推奨
- **Visual Studio 2022** (17.8以降)
  - ワークロード: ".NET デスクトップ開発"
  - または **Visual Studio Code** + C# 拡張機能

## 開発環境のセットアップ

### 1. リポジトリのクローン

```bash
git clone https://github.com/your-org/KoExplorer.git
cd KoExplorer
```

### 2. .NET SDKの確認

```bash
dotnet --version
```

9.0.x が表示されることを確認してください。

### 3. 依存パッケージの復元

```bash
dotnet restore
```

## ビルド方法

### コマンドラインでのビルド

#### Releaseビルド（デフォルト）
```bash
dotnet build
```

プロジェクトは`Directory.Build.props`でデフォルトがReleaseビルドに設定されています。

#### Debugビルド
```bash
dotnet build -c Debug
```

#### ビルド出力先
- **Release**: `bin/Release/net9.0-windows/`
- **Debug**: `bin/Debug/net9.0-windows/`

### Visual Studioでのビルド

1. `KoExplorer.csproj`をVisual Studioで開く
2. ソリューション構成を選択（Debug / Release）
3. メニュー: `ビルド` → `ソリューションのビルド` (Ctrl+Shift+B)

### ビルドのクリーン

```bash
# ビルド成果物を削除
dotnet clean

# 完全クリーン（obj, binフォルダも削除）
dotnet clean
rmdir /s /q bin obj
```

## 実行方法

### コマンドラインから実行

#### Releaseビルドを実行
```bash
dotnet run -c Release
```

#### Debugビルドを実行
```bash
dotnet run -c Debug
```

#### 直接exeを実行
```bash
# Release
.\bin\Release\net9.0-windows\KoExplorer.exe

# Debug
.\bin\Debug\net9.0-windows\KoExplorer.exe
```

### Visual Studioから実行

1. F5キーを押す（デバッグ実行）
2. または Ctrl+F5（デバッグなしで実行）

## 配布パッケージの作成

### 簡単な方法: ビルドスクリプトを使用（推奨）

プロジェクトルートに用意されているスクリプトを使用すると、簡単にリリースビルドを作成できます。

#### Windows バッチファイル（ダブルクリックで実行）

```batch
# 通常のリリースビルド
build-release.bat

# クリーンビルド（完全に再ビルド）
build-release-clean.bat
```

#### PowerShellスクリプト（詳細制御）

```powershell
# 基本的な使い方
.\build-release.ps1

# バージョン指定
.\build-release.ps1 -Version "1.0.1"

# クリーンビルド
.\build-release.ps1 -Clean

# バージョン指定 + クリーンビルド
.\build-release.ps1 -Version "1.0.1" -Clean
```

**スクリプトの動作:**
1. NuGetパッケージの復元
2. Releaseビルドの実行
3. フレームワーク依存型で発行（.NET Runtimeは除外）
4. 不要なファイル（.pdb, .xml）を削除
5. `dist/KoExplorer-v{Version}-win-x64.zip`を作成

**出力先**: `dist/KoExplorer-v1.0.0-win-x64.zip`

### 方法1: ���一フォルダ配布

#### Releaseビルド
```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
```

出力先: `bin/Release/net9.0-windows/win-x64/publish/`

#### 配布用ZIPの作成
```powershell
# PowerShellで実行
$version = "1.0.0"
$publishDir = "bin\Release\net9.0-windows\win-x64\publish"
$zipFile = "KoExplorer-v$version-win-x64.zip"

Compress-Archive -Path "$publishDir\*" -DestinationPath $zipFile -Force
```

### 方法2: 自己完結型（.NET Runtime同梱）

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

**メリット**: ユーザーが.NET Runtimeをインストール不要  
**デメリット**: ファイルサイズが大きい（約150MB）

### 方法3: 単一実行ファイル

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**メリット**: 1つのexeファイルで配布可能  
**デメリット**: 起動時に展開されるため初回起動が遅い

### 推奨配布方法

**フレームワーク依存型（推奨）**:
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

- ファイルサイズが小さい（約10MB）
- ユーザーは.NET 9.0 Runtimeをインストール必要
- Windowsユーザーには一般的

## ビルド構成の詳細

### プロジェクト設定

`KoExplorer.csproj`:
```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net9.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
  <ApplicationIcon>Resources\app-icon.ico</ApplicationIcon>
  <Version>1.0.0</Version>
</PropertyGroup>
```

### デフォルトビルド構成

`Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <Configuration Condition="'$(Configuration)' == ''">Release</Configuration>
  </PropertyGroup>
</Project>
```

これにより、`dotnet build`でReleaseビルドが実行されます。

### デバッグ機能の制御

- **Debugビルド**: テストボタンとデバッグログエリアが表示
- **Releaseビルド**: デバッグ機能は完全に非表示（`#if DEBUG`で制御）

## NuGetパッケージ

### 使用パッケージ一覧

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.2" />
<PackageReference Include="Microsoft.VisualBasic" Version="10.3.0" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
<PackageReference Include="Svg.Skia" Version="2.0.0.1" />
<PackageReference Include="SkiaSharp" Version="2.88.8" />
<PackageReference Include="SkiaSharp.Views.WPF" Version="2.88.8" />
```

### パッケージの更新

```bash
# すべてのパッケージを最新版に更新
dotnet list package --outdated
dotnet add package <PackageName>
```

## トラブルシューティング

### ビルドエラー: "SDK not found"

**原因**: .NET 9.0 SDKがインストールされていない

**解決策**:
```bash
# SDKのバージョン確認
dotnet --list-sdks

# .NET 9.0 SDKをインストール
# https://dotnet.microsoft.com/download/dotnet/9.0
```

### ビルドエラー: "The type or namespace name 'Mvvm' could not be found"

**原因**: NuGetパッケージが復元されていない

**解決策**:
```bash
dotnet restore
dotnet clean
dotnet build
```

### 実行エラー: "Application could not be started"

**原因**: .NET 9.0 Runtimeがインストールされていない

**解決策**:
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)をインストール
- または自己完結型ビルドを使用

### ビルドは成功するが実行できない

**原因**: 出力ディレクトリのファイルが不完全

**解決策**:
```bash
dotnet clean
dotnet build -c Release
```

### Visual Studioでビルドエラー

**原因**: キャッシュの問題

**解決策**:
1. Visual Studioを閉じる
2. `bin`と`obj`フォルダを削除
3. Visual Studioを再起動
4. ソリューションをリビルド

## CI/CD（参考）

### GitHub Actionsの例

`.github/workflows/build.yml`:
```yaml
name: Build

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build -c Release --no-restore
    
    - name: Publish
      run: dotnet publish -c Release -r win-x64 --self-contained false
    
    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: KoExplorer-win-x64
        path: bin/Release/net9.0-windows/win-x64/publish/
```

## バージョン管理

### バージョン番号の更新箇所

リリース時は以下の5箇所を更新：

1. **KoExplorer.csproj**
```xml
<Version>1.0.0</Version>
```

2. **MainWindow.xaml**
```xml
Title="KoExplorer v1.0.0"
```

3. **MainViewModel.cs**
```csharp
"KoExplorer v1.0.0\n\n..."
```

4. **build-release.ps1**
```powershell
[string]$Version = "1.0.0"
```

5. **README.md**
```markdown
# KoExplorer v1.0.0
```

### セマンティックバージョニング

- **メジャー**: 互換性のない変更（例: 2.0.0）
- **マイナー**: 後方互換性のある機能追加（例: 1.1.0）
- **パッチ**: バグ修正（例: 1.0.1）
- **プレリリース**: alpha, beta, rc（例: 1.1.0-alpha, 2.0.0-beta, 2.0.0-rc1）

## 参考リンク

- [.NET CLI リファレンス](https://docs.microsoft.com/ja-jp/dotnet/core/tools/)
- [dotnet build](https://docs.microsoft.com/ja-jp/dotnet/core/tools/dotnet-build)
- [dotnet publish](https://docs.microsoft.com/ja-jp/dotnet/core/tools/dotnet-publish)
- [WPF アプリの配置](https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/deployment/)

---

ビルドに関する質問は [Issues](../../issues) でお気軽にどうぞ。
