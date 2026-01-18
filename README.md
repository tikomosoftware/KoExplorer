# KoExplorer - エクスプローラー風イメージビューアー

Windows向けエクスプローラータイプの高速イメージビューアーアプリケーションです。

## 特徴

- **エクスプローラー風UI**: 使い慣れたインターフェースで快適な画像閲覧
- **高速表示**: 大量の画像を素早く閲覧
- **多様な画像フォーマット対応**: JPG, PNG, WebP, BMP, GIF, TIFF, ICO, SVG
- **豊富なキーボードショートカット**: 効率的な操作
- **サムネイルキャッシュ**: 高速なサムネイル表示

## 対応フォーマット

- **JPG/JPEG** (.jpg, .jpeg)
- **PNG** (.png)
- **WebP** (.webp)
- **BMP** (.bmp)
- **GIF** (.gif)
- **TIFF** (.tiff, .tif)
- **ICO** (.ico)
- **SVG** (.svg)

## システム要件

- **OS**: Windows 10 (Version 1809以降) / Windows 11
- **.NET**: .NET 9.0 Runtime
- **推奨メモリ**: 4GB以上

## ビルド方法

### 前提条件

- Visual Studio 2022以降（.NET 9.0 SDK含む）
- または .NET 9.0 SDK

### ビルド手順

```powershell
# リポジトリをクローン
git clone https://github.com/yourusername/KoExplorer.git
cd KoExplorer

# 依存パッケージの復元
dotnet restore

# ビルド
dotnet build

# 実行
dotnet run
```

### Visual Studioでのビルド

1. `KoExplorer.sln` を Visual Studio で開く
2. ソリューションをビルド (Ctrl+Shift+B)
3. デバッグ実行 (F5)

## キーボードショートカット

### ナビゲーション
- **→** / **N**: 次の画像
- **←** / **P**: 前の画像
- **Home**: 最初の画像
- **End**: 最後の画像

### ズーム
- **Ctrl + +**: 拡大
- **Ctrl + -**: 縮小
- **Ctrl + 0** / **1**: 実寸表示
- **F** / **Ctrl + F**: ウィンドウにフィット
- **W**: 幅に合わせる
- **H**: 高さに合わせる

### 表示
- **F11**: フルスクリーン切り替え
- **F3**: サムネイルパネル表示切り替え

### ファイル操作
- **Ctrl + C**: 画像をクリップボードにコピー
- **Ctrl + Shift + C**: パスをコピー
- **Delete**: ゴミ箱に移動
- **Ctrl + E**: エクスプローラーで開く
- **Ctrl + O**: 既定のアプリで開く

## プロジェクト構造

```
KoExplorer/
├── Models/              # データモデル
│   ├── ImageFileInfo.cs
│   ├── FolderTreeNode.cs
│   ├── ThumbnailItem.cs
│   └── AppSettings.cs
├── ViewModels/          # ビューモデル (MVVM)
│   └── MainViewModel.cs
├── Services/            # ビジネスロジック
│   ├── ImageLoaderService.cs
│   ├── ThumbnailService.cs
│   ├── FileSystemService.cs
│   ├── SettingsService.cs
│   └── LogService.cs
├── Converters/          # 値コンバーター
│   └── FileInfoConverters.cs
├── Styles/              # UIスタイル
│   ├── Colors.xaml
│   ├── Buttons.xaml
│   └── Controls.xaml
├── App.xaml            # アプリケーション定義
├── App.xaml.cs         # アプリケーションロジック
├── MainWindow.xaml     # メインウィンドウUI
└── MainWindow.xaml.cs  # メインウィンドウロジック
```

## 技術スタック

- **フレームワーク**: WPF (.NET 9.0)
- **言語**: C# 12
- **アーキテクチャ**: MVVM パターン
- **画像処理**:
  - SixLabors.ImageSharp: 汎用画像処理
  - Svg.Skia / SkiaSharp: SVG対応
- **依存性注入**: Microsoft.Extensions.DependencyInjection
- **MVVM支援**: CommunityToolkit.Mvvm

## 開発状況

### 実装済み機能
- ✅ エクスプローラー風UI（フォルダツリー、プレビュー、サムネイル）
- ✅ 画像の読み込みと表示
- ✅ サムネイル生成とキャッシュ
- ✅ 画像ナビゲーション（次/前/最初/最後）
- ✅ ズーム機能（拡大/縮小/実寸/フィット）
- ✅ ファイル操作（コピー/削除/エクスプローラーで開く）
- ✅ 並び替え（名前/日時/サイズ）
- ✅ 設定の保存/読み込み
- ✅ 高速起動（遅延読み込み）

### 今後の改善予定
- 🔲 フォルダツリーの自動同期
- 🔲 EXIF情報表示
- 🔲 画像比較モード
- 🔲 フィルタ・検索機能
- 🔲 スライドショー機能

## ライセンス

MIT License

## 作者

KoExplorer Team

## 貢献

プルリクエストを歓迎します。大きな変更の場合は、まずissueを開いて変更内容を議論してください。
