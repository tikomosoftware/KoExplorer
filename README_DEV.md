# KoExplorer - 開発者向けドキュメント

このドキュメントは、KoExplorerの開発に参加する開発者向けの技術情報を提供します。

## 目次
- [プロジェクト構成](#プロジェクト構成)
- [技術スタック](#技術スタック)
- [アーキテクチャ](#アーキテクチャ)
- [開発環境のセットアップ](#開発環境のセットアップ)
- [コーディング規約](#コーディング規約)
- [テスト](#テスト)
- [貢献ガイドライン](#貢献ガイドライン)

## プロジェクト構成

```
KoExplorer/
├── App.xaml                    # アプリケーションエントリポイント
├── App.xaml.cs                 # DIコンテナ設定
├── MainWindow.xaml             # メインウィンドウUI
├── MainWindow.xaml.cs          # メインウィンドウコードビハインド
├── KoExplorer.csproj           # プロジェクトファイル
├── Directory.Build.props       # ビルド設定（デフォルトReleaseビルド）
│
├── Models/                     # データモデル
│   ├── AppSettings.cs          # アプリケーション設定
│   ├── FolderTreeNode.cs       # フォルダツリーノード
│   ├── ImageFileInfo.cs        # 画像ファイル情報
│   └── ThumbnailItem.cs        # サムネイルアイテム
│
├── ViewModels/                 # ビューモデル
│   └── MainViewModel.cs        # メインビューモデル（MVVM）
│
├── Services/                   # ビジネスロジック・サービス
│   ├── FileSystemService.cs    # ファイルシステム操作
│   ├── ImageLoaderService.cs   # 画像読み込み
│   ├── ThumbnailService.cs     # サムネイル生成
│   ├── SettingsService.cs      # 設定の保存/読み込み
│   └── LogService.cs           # ログ出力
│
├── Converters/                 # 値コンバーター
│   └── FileInfoConverters.cs   # ファイル情報表示用コンバーター
│
├── Styles/                     # UIスタイル定義
│   ├── Colors.xaml             # カラーパレット
│   ├── Buttons.xaml            # ボタンスタイル
│   └── Controls.xaml           # コントロールスタイル
│
└── Resources/                  # リソースファイル
    └── app-icon.ico            # アプリケーションアイコン
```

## 技術スタック

### フレームワーク・言語
- **UI フレームワーク**: WPF (Windows Presentation Foundation)
- **.NET バージョン**: .NET 9.0
- **言語**: C# 12
- **プラットフォーム**: Windows 10/11 (x64)

### アーキテクチャ・パターン
- **設計パターン**: MVVM (Model-View-ViewModel)
- **MVVM ライブラリ**: [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.4.0
  - `ObservableObject`: プロパティ変更通知の基底クラス
  - `RelayCommand`: コマンドパターンの実装
  - `[ObservableProperty]`: ソースジェネレーターによる自動プロパティ生成
  - `[RelayCommand]`: ソースジェネレーターによる自動コマンド生成
- **依存性注入**: [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) 10.0.2
  - サービスのライフタイム管理（Singleton, Transient）
  - 疎結合な設計によるテスタビリティ向上

### 画像処理ライブラリ
- **汎用画像処理**: [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) 3.1.12
  - JPG, PNG, WebP, BMP, GIF, TIFF などの読み込み
  - サムネイル生成（リサイズ）
  - 画像メタデータ（EXIF）の取得
  - 高性能なメモリ管理
- **SVG レンダリング**: [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) 2.0.0.1
  - ベクター画像 (SVG) のレンダリング
  - WPFへの統合
- **2D グラフィックス**: [SkiaSharp](https://github.com/mono/SkiaSharp) 2.88.8
  - 高性能な 2D グラフィックスエンジン
  - クロスプラットフォーム対応
  - WPF 統合 (SkiaSharp.Views.WPF 2.88.8)

### ユーティリティ
- **ファイル操作**: [Microsoft.VisualBasic](https://www.nuget.org/packages/Microsoft.VisualBasic) 10.3.0
  - ゴミ箱への移動機能 (`FileSystem.DeleteFile`)
  - Windows統合機能

## アーキテクチャ

### MVVM パターン

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│    View     │ ◄─────► │  ViewModel   │ ◄─────► │    Model    │
│  (XAML)     │ Binding │   (Logic)    │  Uses   │   (Data)    │
└─────────────┘         └──────────────┘         └─────────────┘
                               │
                               ▼
                        ┌──────────────┐
                        │   Services   │
                        │  (Business)  │
                        └──────────────┘
```

### 依存性注入

`App.xaml.cs`でDIコンテナを構成：

```csharp
services.AddSingleton<IImageLoaderService, ImageLoaderService>();
services.AddSingleton<IThumbnailService, ThumbnailService>();
services.AddSingleton<IFileSystemService, FileSystemService>();
services.AddSingleton<ISettingsService, SettingsService>();
services.AddSingleton<MainViewModel>();
services.AddSingleton<MainWindow>();
```

### データフロー

1. **ユーザー操作** → View (XAML)
2. **コマンド実行** → ViewModel (RelayCommand)
3. **ビジネスロジック** → Services
4. **データ更新** → Model
5. **プロパティ変更通知** → View (INotifyPropertyChanged)

## 開発環境のセットアップ

詳細は [BUILD.md](BUILD.md) を参照してください。

### 必要なツール
- Visual Studio 2022 (17.8以降) または Visual Studio Code
- .NET 9.0 SDK
- Git

### クイックスタート
```bash
# リポジトリのクローン
git clone https://github.com/your-org/KoExplorer.git
cd KoExplorer

# ビルド（Releaseモード）
dotnet build

# デバッグビルド
dotnet build -c Debug

# 実行
dotnet run
```

## コーディング規約

### C# コーディングスタイル
- **命名規則**: 
  - クラス・メソッド: PascalCase
  - プライベートフィールド: _camelCase
  - パブリックプロパティ: PascalCase
- **インデント**: スペース4つ
- **改行**: LF (Unix形式)
- **Nullable**: 有効化（`<Nullable>enable</Nullable>`）

### MVVM ベストプラクティス
- ViewModelにUI要素（Control）への参照を持たせない
- コードビハインドは最小限に（イベントハンドラーのみ）
- ビジネスロジックはServicesに分離
- CommunityToolkit.Mvvmのソースジェネレーターを活用

### 非同期処理
- I/O操作は必ず非同期（`async`/`await`）
- UIスレッドのブロックを避ける
- `Task.Run`で重い処理をバックグラウンド実行

## デバッグ機能

### デバッグモード
- **Debugビルド**: テストボタンとデバッグログエリアが表示
- **Releaseビルド**: デバッグ機能は完全に非表示

デバッグログは`MainWindow.xaml.cs`の`AddDebugLog()`メソッドで出力：
```csharp
#if DEBUG
AddDebugLog("デバッグメッセージ");
#endif
```

### ログファイル
- 場所: `%LOCALAPPDATA%\KoExplorer\logs\app.log`
- ログレベル: Info, Warning, Error

## テスト

### 現在の状況
- ユニットテストは未実装
- 手動テストで品質保証

### 今後の計画
- xUnit によるユニットテスト
- Moq によるモックテスト
- WPF UI テスト（FlaUI）

## パフォーマンス最適化

### 実装済み
- サムネイルの遅延読み込み
- 画像キャッシュ
- 非同期I/O
- フォルダツリーの遅延展開

### 今後の改善
- サムネイルキャッシュの永続化
- 画像プリロード
- メモリ使用量の最適化

## 既知の問題・制限事項

1. **フォルダツリー同期**: フォルダ選択ダイアログからの選択時、左ペインのツリーが自動展開されない場合がある
2. **大容量画像**: 非常に大きな画像（100MB以上）の読み込みに時間がかかる
3. **アニメーションGIF**: 静止画として表示（アニメーション再生非対応）

## 貢献ガイドライン

### プルリクエストの流れ
1. Issueを作成して機能・修正内容を議論
2. フォークしてブランチを作成（`feature/xxx`, `fix/xxx`）
3. コードを実装
4. コミットメッセージは明確に（日本語OK）
5. プルリクエストを作成

### コミットメッセージ
```
[種別] 簡潔な説明

詳細な説明（必要に応じて）

例:
[機能追加] EXIF情報表示機能を実装
[バグ修正] サムネイル生成時のメモリリークを修正
[リファクタリング] ImageLoaderServiceを整理
```

## リリースプロセス

1. バージョン番号を更新（`KoExplorer.csproj`, `MainWindow.xaml`, `MainViewModel.cs`）
2. `CHANGELOG.md`を更新
3. Releaseビルド: `dotnet build -c Release`
4. 動作確認
5. GitHubでリリースタグを作成
6. リリースノートを記載
7. ビルド成果物をアップロード

## 参考リンク

- [WPF ドキュメント](https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/ja-jp/dotnet/communitytoolkit/mvvm/)
- [SixLabors.ImageSharp](https://docs.sixlabors.com/articles/imagesharp/)
- [.NET 9.0](https://docs.microsoft.com/ja-jp/dotnet/core/whats-new/dotnet-9)

## ライセンス

MIT License - 詳細は [LICENSE](LICENSE) を参照

---

開発に関する質問は [Discussions](../../discussions) または [Issues](../../issues) でお気軽にどうぞ。
