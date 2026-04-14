using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoExplorer.Models;
using KoExplorer.Services;

namespace KoExplorer.ViewModels;

/// <summary>
/// メインビューモデル
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IImageLoaderService _imageLoader;
    private readonly IThumbnailService _thumbnailService;
    private readonly IFileSystemService _fileSystemService;
    private readonly ISettingsService _settingsService;

    private List<string> _imageFiles = new();
    private int _internalImageIndex = -1; // 内部用インデックス（0始まり）
    private AppSettings _settings = new();
    private string _currentFolderPath = string.Empty; // 現在のフォルダパス
    private bool _isUpdatingSelection = false; // 選択更新中フラグ

    /// <summary>現在のフォルダパス（アドレスバー用）</summary>
    [ObservableProperty]
    private string _addressBarPath = string.Empty;
    
    // ビューポートサイズ（ScrollViewerのサイズ）
    private double _viewportWidth = 800.0;
    private double _viewportHeight = 600.0;
    
    // 現在のズームモード（Fit, Width, Height, Manual）
    private string _currentZoomMode = "Manual";
    
    public double ViewportWidth
    {
        get => _viewportWidth;
        set
        {
            if (SetProperty(ref _viewportWidth, value))
            {
                OnViewportSizeChanged();
            }
        }
    }
    
    public double ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            if (SetProperty(ref _viewportHeight, value))
            {
                OnViewportSizeChanged();
            }
        }
    }
    
    /// <summary>
    /// ビューポートサイズ変更時の処理
    /// </summary>
    private void OnViewportSizeChanged()
    {
        // ウィンドウにフィット系のモードの場合は自動的に再計算
        if (_currentZoomMode == "Fit")
        {
            FitToWindow();
        }
        else if (_currentZoomMode == "Width")
        {
            FitToWidth();
        }
        else if (_currentZoomMode == "Height")
        {
            FitToHeight();
        }
    }

    #region Properties

    [ObservableProperty]
    private ObservableCollection<FolderTreeNode> _folderTree = new();

    [ObservableProperty]
    private ObservableCollection<ThumbnailItem> _thumbnailItems = new();

    [ObservableProperty]
    private ThumbnailItem? _selectedThumbnail;

    // 複数選択されたアイテム
    private ObservableCollection<ThumbnailItem> _selectedItems = new();
    public ObservableCollection<ThumbnailItem> SelectedItems
    {
        get => _selectedItems;
        set => SetProperty(ref _selectedItems, value);
    }

    [ObservableProperty]
    private BitmapSource? _currentImageSource;

    [ObservableProperty]
    private ImageFileInfo? _currentImageInfo;

    [ObservableProperty]
    private string _statusText = "準備完了";

    [ObservableProperty]
    private int _currentImageIndex = 1; // 表示用インデックス（1始まり）

    [ObservableProperty]
    private int _totalImages = 0;

    [ObservableProperty]
    private double _zoomScale = 1.0;

    [ObservableProperty]
    private double _zoomPercentage = 100.0;

    [ObservableProperty]
    private double _imageDisplayWidth = double.NaN; // NaNで自動サイズ

    [ObservableProperty]
    private double _imageDisplayHeight = double.NaN; // NaNで自動サイズ

    [ObservableProperty]
    private int _thumbnailSize = 128;

    [ObservableProperty]
    private bool _isFolderTreeVisible = true;

    [ObservableProperty]
    private bool _isThumbnailPanelVisible = true;

    [ObservableProperty]
    private bool _isFullScreen = false;

    [ObservableProperty]
    private bool _showImageInfo = true;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private Brush _previewBackground = new SolidColorBrush(Color.FromRgb(128, 128, 128));

    // 背景色チェック用
    [ObservableProperty]
    private bool _isBackgroundWhite = false;

    [ObservableProperty]
    private bool _isBackgroundGray = true;

    [ObservableProperty]
    private bool _isBackgroundBlack = false;

    [ObservableProperty]
    private bool _isBackgroundChecker = false;

    // 表示モード
    [ObservableProperty]
    private bool _isDetailViewMode = true; // デフォルトは詳細表示

    [ObservableProperty]
    private bool _isThumbnailViewMode = false;

    [ObservableProperty]
    private bool _isPreviewVisible = true; // プレビューエリアの表示状態

    /// <summary>
    /// デバッグモードかどうか
    /// </summary>
    public bool IsDebugMode
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    #endregion

    public MainViewModel(
        IImageLoaderService imageLoader,
        IThumbnailService thumbnailService,
        IFileSystemService fileSystemService,
        ISettingsService settingsService)
    {
        _imageLoader = imageLoader;
        _thumbnailService = thumbnailService;
        _fileSystemService = fileSystemService;
        _settingsService = settingsService;

        InitializeAsync();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private async void InitializeAsync()
    {
        try
        {
            // 設定読み込み
            _settings = await _settingsService.LoadSettingsAsync();
            ApplySettings();

            // フォルダツリー構築（非同期で実行、UIをブロックしない）
            await Task.Run(async () =>
            {
                try
                {
                    var tree = _fileSystemService.GetFolderTree();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        FolderTree = tree;
                    });
                }
                catch
                {
                    // エラーは無視
                }
            });

            // 前回開いたフォルダを復元
            if (!string.IsNullOrEmpty(_settings.LastOpenedFolder) &&
                Directory.Exists(_settings.LastOpenedFolder))
            {
                await LoadFolderAsync(_settings.LastOpenedFolder);
                StatusText = $"前回のフォルダを開きました: {_settings.LastOpenedFolder}";

                // ツリーを該当フォルダまで展開（UIスレッドで実行）
                OnRestoreTreeRequested?.Invoke(_settings.LastOpenedFolder);
            }
            else
            {
                StatusText = "準備完了";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"初期化エラー: {ex.Message}";
        }
    }

    /// <summary>
    /// 起動時のツリー展開要求イベント（MainWindowが購読する）
    /// </summary>
    public event Action<string>? OnRestoreTreeRequested;

    /// <summary>
    /// 設定を適用
    /// </summary>
    private void ApplySettings()
    {
        ThumbnailSize = _settings.ThumbnailSize;
        IsFolderTreeVisible = _settings.ShowFolderTree;
        IsThumbnailPanelVisible = _settings.ShowThumbnailPanel;
        ShowImageInfo = _settings.ShowImageInfo;

        // 背景色を直接設定
        SetBackground(_settings.BackgroundColor);
    }

    /// <summary>
    /// フォルダを読み込む
    /// </summary>
    [RelayCommand]
    private async Task LoadFolderAsync(object? parameter)
    {
        string folderPath;

        if (parameter is FolderTreeNode node)
        {
            folderPath = node.FullPath;
        }
        else if (parameter is string path)
        {
            folderPath = path;
        }
        else if (parameter is ThumbnailItem item && item.IsFolder)
        {
            // サムネイルアイテムからフォルダを開く
            folderPath = item.FilePath;
        }
        else
        {
            // フォルダ選択ダイアログ
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "フォルダを選択",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "フォルダ選択"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            folderPath = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        _currentFolderPath = folderPath;
        AddressBarPath = folderPath;
        StatusText = "ファイルを読み込み中...";

        // 画像ファイル取得（画像表示用）
        _imageFiles = await _fileSystemService.GetImageFilesAsync(folderPath);
        TotalImages = _imageFiles.Count;

        // サブフォルダとファイルを取得
        var subFolders = await _fileSystemService.GetSubFoldersAsync(folderPath);
        var allFiles = await _fileSystemService.GetAllFilesAsync(folderPath);

        // サムネイルアイテムを生成（フォルダ + ファイル）
        await GenerateFolderAndFileThumbnailsAsync(subFolders, allFiles);

        // 画像がある場合は最初の画像を表示
        if (_imageFiles.Count > 0)
        {
            _internalImageIndex = 0;
            await LoadCurrentImageAsync();
            StatusText = $"{subFolders.Count}個のフォルダ、{_imageFiles.Count}個の画像、{allFiles.Count}個のファイル";
        }
        else
        {
            CurrentImageSource = null;
            CurrentImageInfo = null;
            StatusText = $"{subFolders.Count}個のフォルダ、{allFiles.Count}個のファイル（画像ファイルなし）";
        }

        // 設定を保存
        _settings.LastOpenedFolder = folderPath;
        await _settingsService.SaveSettingsAsync(_settings);
    }

    /// <summary>
    /// フォルダとファイルのサムネイル生成
    /// </summary>
    private async Task GenerateFolderAndFileThumbnailsAsync(List<string> folders, List<string> files)
    {
        ThumbnailItems.Clear();

        // 親フォルダへのナビゲーション（ルートでない場合）
        if (!string.IsNullOrEmpty(_currentFolderPath))
        {
            var parentPath = Directory.GetParent(_currentFolderPath)?.FullName;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentItem = new ThumbnailItem
                {
                    FilePath = parentPath,
                    FileName = "..",
                    IsFolder = true,
                    ModifiedDate = DateTime.MinValue
                };
                ThumbnailItems.Add(parentItem);
            }
        }

        // サブフォルダを追加
        foreach (var folder in folders)
        {
            try
            {
                var dirInfo = new DirectoryInfo(folder);
                var item = new ThumbnailItem
                {
                    FilePath = folder,
                    FileName = dirInfo.Name,
                    IsFolder = true,
                    ModifiedDate = dirInfo.LastWriteTime
                };
                ThumbnailItems.Add(item);
            }
            catch
            {
                // アクセス権限がない場合などはスキップ
            }
        }

        // ファイルを追加
        var tasks = files.Select(async (filePath, index) =>
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var item = new ThumbnailItem
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Index = index,
                    IsLoading = true,
                    IsFolder = false,
                    FileSize = fileInfo.Length,
                    ModifiedDate = fileInfo.LastWriteTime
                };

                Application.Current.Dispatcher.Invoke(() => ThumbnailItems.Add(item));

                var thumbnail = await _thumbnailService.GenerateThumbnailAsync(filePath, ThumbnailSize);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.ThumbnailSource = thumbnail;
                    item.IsLoading = false;
                    item.HasError = thumbnail == null;
                });
            }
            catch
            {
                // エラーは無視
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 現在の画像を読み込む
    /// </summary>
    private async Task LoadCurrentImageAsync()
    {
        if (_internalImageIndex < 0 || _internalImageIndex >= _imageFiles.Count)
            return;

        HasError = false;
        ErrorMessage = string.Empty;

        var filePath = _imageFiles[_internalImageIndex];

        try
        {
            // 画像読み込み
            var imageSource = await _imageLoader.LoadImageAsync(filePath);
            if (imageSource == null)
            {
                HasError = true;
                ErrorMessage = "画像の読み込みに失敗しました";
                return;
            }

            CurrentImageSource = imageSource;

            // 画像情報取得
            CurrentImageInfo = await _imageLoader.GetImageInfoAsync(filePath);

            // インデックス更新（表示用は1始まり）
            CurrentImageIndex = _internalImageIndex + 1;

            // サムネイル選択（ファイルパスで検索）
            var thumbnailItem = ThumbnailItems.FirstOrDefault(t => t.FilePath == filePath);
            if (thumbnailItem != null)
            {
                _isUpdatingSelection = true;
                SelectedThumbnail = thumbnailItem;
                _isUpdatingSelection = false;
            }

            // デフォルトズームモード適用
            if (_settings.DefaultZoomMode == "Fit")
            {
                FitToWindowCommand.Execute(null);
            }
            else
            {
                ActualSizeCommand.Execute(null);
            }

            StatusText = $"{Path.GetFileName(filePath)} を表示中";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"エラー: {ex.Message}";
        }
    }

    #region Navigation Commands

    [RelayCommand]
    private async Task NextImageAsync()
    {
        if (_internalImageIndex < _imageFiles.Count - 1)
        {
            _internalImageIndex++;
            await LoadCurrentImageAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousImageAsync()
    {
        if (_internalImageIndex > 0)
        {
            _internalImageIndex--;
            await LoadCurrentImageAsync();
        }
    }

    [RelayCommand]
    private async Task FirstImageAsync()
    {
        if (_imageFiles.Count > 0)
        {
            _internalImageIndex = 0;
            await LoadCurrentImageAsync();
        }
    }

    [RelayCommand]
    private async Task LastImageAsync()
    {
        if (_imageFiles.Count > 0)
        {
            _internalImageIndex = _imageFiles.Count - 1;
            await LoadCurrentImageAsync();
        }
    }

    #endregion

    #region Zoom Commands

    [RelayCommand]
    private void ZoomIn()
    {
        _currentZoomMode = "Manual";
        ZoomScale = Math.Min(ZoomScale * 1.25, 16.0);
        ZoomPercentage = ZoomScale * 100;
        UpdateImageDisplaySize();
    }

    [RelayCommand]
    private void ZoomOut()
    {
        _currentZoomMode = "Manual";
        ZoomScale = Math.Max(ZoomScale / 1.25, 0.1);
        ZoomPercentage = ZoomScale * 100;
        UpdateImageDisplaySize();
    }

    [RelayCommand]
    private void ActualSize()
    {
        _currentZoomMode = "Manual";
        ZoomScale = 1.0;
        ZoomPercentage = 100.0;
        UpdateImageDisplaySize();
        StatusText = "実寸表示 (100%)";
    }

    [RelayCommand]
    private void FitToWindow()
    {
        _currentZoomMode = "Fit";
        
        // ウィンドウにフィット（スクロールバーが表示されないように）
        if (CurrentImageSource == null)
        {
            StatusText = "画像が読み込まれていません";
            return;
        }

        var imageWidth = CurrentImageSource.PixelWidth;
        var imageHeight = CurrentImageSource.PixelHeight;
        
        if (imageWidth > 0 && imageHeight > 0 && ViewportWidth > 0 && ViewportHeight > 0)
        {
            var scaleX = ViewportWidth / imageWidth;
            var scaleY = ViewportHeight / imageHeight;
            var scale = Math.Min(scaleX, scaleY);
            
            ZoomScale = Math.Max(scale, 0.1);
            ZoomPercentage = ZoomScale * 100;
            UpdateImageDisplaySize();
            StatusText = $"ウィンドウにフィット ({ZoomPercentage:F0}%)";
        }
        else
        {
            StatusText = $"フィット計算エラー: 画像={imageWidth}x{imageHeight}, ビューポート={ViewportWidth:F0}x{ViewportHeight:F0}";
        }
    }

    [RelayCommand]
    private void FitToWidth()
    {
        _currentZoomMode = "Width";
        
        // 幅にフィット（横スクロールバーが表示されないように）
        if (CurrentImageSource == null)
        {
            StatusText = "画像が読み込まれていません";
            return;
        }

        var imageWidth = CurrentImageSource.PixelWidth;
        
        if (imageWidth > 0 && ViewportWidth > 0)
        {
            var scale = ViewportWidth / imageWidth;
            ZoomScale = Math.Max(scale, 0.1);
            ZoomPercentage = ZoomScale * 100;
            UpdateImageDisplaySize();
            StatusText = $"幅に合わせる ({ZoomPercentage:F0}%)";
        }
        else
        {
            StatusText = $"幅フィット計算エラー: 画像幅={imageWidth}, ビューポート幅={ViewportWidth:F0}";
        }
    }

    [RelayCommand]
    private void FitToHeight()
    {
        _currentZoomMode = "Height";
        
        // 高さにフィット（縦スクロールバーが表示されないように）
        if (CurrentImageSource == null) return;

        var imageHeight = CurrentImageSource.PixelHeight;
        
        if (imageHeight > 0 && ViewportHeight > 0)
        {
            var scale = ViewportHeight / imageHeight;
            ZoomScale = Math.Max(scale, 0.1);
            ZoomPercentage = ZoomScale * 100;
            UpdateImageDisplaySize();
            StatusText = $"高さに合わせる ({ZoomPercentage:F0}%)";
        }
    }

    /// <summary>
    /// 画像の表示サイズを更新
    /// </summary>
    private void UpdateImageDisplaySize()
    {
        if (CurrentImageSource == null)
        {
            ImageDisplayWidth = double.NaN;
            ImageDisplayHeight = double.NaN;
            return;
        }

        ImageDisplayWidth = CurrentImageSource.PixelWidth * ZoomScale;
        ImageDisplayHeight = CurrentImageSource.PixelHeight * ZoomScale;
    }

    #endregion

    #region View Commands

    [RelayCommand]
    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
        StatusText = IsFullScreen ? "フルスクリーンモード" : "通常モード";
    }

    [RelayCommand]
    private void ToggleThumbnailPanel()
    {
        IsThumbnailPanelVisible = !IsThumbnailPanelVisible;
    }

    [RelayCommand]
    private void TogglePreview()
    {
        IsPreviewVisible = !IsPreviewVisible;
    }

    [RelayCommand]
    private void SwitchToDetailView()
    {
        IsDetailViewMode = true;
        IsThumbnailViewMode = false;
    }

    [RelayCommand]
    private void SwitchToThumbnailView()
    {
        IsDetailViewMode = false;
        IsThumbnailViewMode = true;
    }

    [RelayCommand]
    private void SetBackground(string color)
    {
        IsBackgroundWhite = false;
        IsBackgroundGray = false;
        IsBackgroundBlack = false;
        IsBackgroundChecker = false;

        switch (color)
        {
            case "White":
                PreviewBackground = new SolidColorBrush(Colors.White);
                IsBackgroundWhite = true;
                break;
            case "Gray":
                PreviewBackground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                IsBackgroundGray = true;
                break;
            case "Black":
                PreviewBackground = new SolidColorBrush(Colors.Black);
                IsBackgroundBlack = true;
                break;
            case "Checker":
                // チェッカーボード（実装は簡易版）
                PreviewBackground = new SolidColorBrush(Color.FromRgb(192, 192, 192));
                IsBackgroundChecker = true;
                break;
        }

        _settings.BackgroundColor = color;
    }

    #endregion

    #region File Commands

    /// <summary>チェック済みアイテムを取得</summary>
    public IReadOnlyList<ThumbnailItem> CheckedItems =>
        ThumbnailItems.Where(i => i.IsChecked).ToList();

    /// <summary>全アイテムをチェック</summary>
    [RelayCommand]
    private void CheckAll()
    {
        foreach (var item in ThumbnailItems.Where(i => i.FileName != ".."))
            item.IsChecked = true;
    }

    /// <summary>全アイテムのチェックを解除</summary>
    [RelayCommand]
    private void UncheckAll()
    {
        foreach (var item in ThumbnailItems)
            item.IsChecked = false;
    }

    /// <summary>チェック済みアイテムを削除</summary>
    [RelayCommand]
    private void DeleteCheckedItems()
    {
        var targets = CheckedItems.Where(i => !i.IsFolder).ToList();
        if (targets.Count == 0)
        {
            StatusText = "削除対象のファイルがチェックされていません";
            return;
        }

        var message = targets.Count == 1
            ? $"'{targets[0].FileName}' をゴミ箱に移動しますか?"
            : $"チェックした {targets.Count} 個のファイルをゴミ箱に移動しますか?\n\n" +
              string.Join("\n", targets.Take(5).Select(f => f.FileName)) +
              (targets.Count > 5 ? $"\n... 他 {targets.Count - 5}個" : "");

        if (MessageBox.Show(message, "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var failed = new List<string>();
        foreach (var item in targets)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    item.FilePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                _imageFiles.Remove(item.FilePath);
                ThumbnailItems.Remove(item);
            }
            catch (Exception ex) { failed.Add($"{item.FileName}: {ex.Message}"); }
        }

        TotalImages = _imageFiles.Count;
        StatusText = failed.Count > 0
            ? $"{targets.Count - failed.Count}個削除、{failed.Count}個失敗"
            : $"{targets.Count}個のファイルを削除しました";

        if (_imageFiles.Count == 0) { CurrentImageSource = null; CurrentImageInfo = null; }
        else if (_internalImageIndex >= _imageFiles.Count) { _internalImageIndex = _imageFiles.Count - 1; _ = LoadCurrentImageAsync(); }
    }

    /// <summary>チェック済みアイテムをフォルダ選択ダイアログで移動</summary>
    [RelayCommand]
    private async Task MoveCheckedItemsAsync()
    {
        var targets = CheckedItems.Where(i => i.FileName != "..").ToList();
        if (targets.Count == 0)
        {
            StatusText = "移動対象のアイテムがチェックされていません";
            return;
        }

        // フォルダ選択ダイアログ
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "移動先フォルダを選択",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "フォルダ選択"
        };
        if (dialog.ShowDialog() != true) return;

        var destFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        if (string.IsNullOrEmpty(destFolder) || !Directory.Exists(destFolder)) return;
        if (string.Equals(destFolder, _currentFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            StatusText = "移動先が現在のフォルダと同じです";
            return;
        }

        await MoveItemsToFolderAsync(targets, destFolder);
    }

    /// <summary>指定アイテムリストを指定フォルダに移動（内部共通処理）</summary>
    private async Task MoveItemsToFolderAsync(List<ThumbnailItem> items, string destinationFolder)
    {
        var failed = new List<string>();
        foreach (var item in items)
        {
            try
            {
                var destPath = Path.Combine(destinationFolder, item.FileName);
                if (item.IsFolder) Directory.Move(item.FilePath, destPath);
                else File.Move(item.FilePath, destPath);
                _imageFiles.Remove(item.FilePath);
                await Application.Current.Dispatcher.InvokeAsync(() => ThumbnailItems.Remove(item));
            }
            catch (Exception ex) { failed.Add($"{item.FileName}: {ex.Message}"); }
        }

        TotalImages = _imageFiles.Count;
        if (failed.Count > 0)
            MessageBox.Show($"以下のアイテムの移動に失敗しました:\n{string.Join("\n", failed)}", "移動エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            StatusText = $"{items.Count - failed.Count}個のアイテムを移動しました";

        if (_internalImageIndex >= _imageFiles.Count)
        {
            _internalImageIndex = _imageFiles.Count - 1;
            if (_internalImageIndex >= 0) await LoadCurrentImageAsync();
            else { CurrentImageSource = null; CurrentImageInfo = null; }
        }
    }

    [RelayCommand]
    private void CopyImage()
    {
        if (CurrentImageSource != null)
        {
            Clipboard.SetImage(CurrentImageSource);
            StatusText = "画像をクリップボードにコピーしました";
        }
    }

    [RelayCommand]
    private void CopyPath()
    {
        if (_internalImageIndex >= 0 && _internalImageIndex < _imageFiles.Count)
        {
            Clipboard.SetText(_imageFiles[_internalImageIndex]);
            StatusText = "パスをクリップボードにコピーしました";
        }
    }

    [RelayCommand]
    private void DeleteImage()
    {
        // チェック済みアイテムがあればそちらを優先
        if (CheckedItems.Count > 0)
        {
            DeleteCheckedItems();
            return;
        }

        // 選択されたアイテムがある場合は複数削除
        if (SelectedItems.Count > 0)
        {
            DeleteSelectedItems();
            return;
        }

        // 選択がない場合は現在表示中の画像を削除（従来の動作）
        if (_internalImageIndex < 0 || _internalImageIndex >= _imageFiles.Count)
            return;

        var filePath = _imageFiles[_internalImageIndex];
        var result = MessageBox.Show(
            $"'{Path.GetFileName(filePath)}' をゴミ箱に移動しますか?",
            "削除確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    filePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                _imageFiles.RemoveAt(_internalImageIndex);
                var thumbnailItem = ThumbnailItems.FirstOrDefault(t => t.FilePath == filePath);
                if (thumbnailItem != null)
                {
                    ThumbnailItems.Remove(thumbnailItem);
                }
                TotalImages = _imageFiles.Count;

                if (_imageFiles.Count > 0)
                {
                    if (_internalImageIndex >= _imageFiles.Count)
                        _internalImageIndex = _imageFiles.Count - 1;

                    _ = LoadCurrentImageAsync();
                }
                else
                {
                    CurrentImageSource = null;
                    CurrentImageInfo = null;
                    StatusText = "画像がありません";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"削除に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 選択されたアイテムを削除
    /// </summary>
    private void DeleteSelectedItems()
    {
        if (SelectedItems.Count == 0)
            return;

        // フォルダを除外
        var filesToDelete = SelectedItems.Where(item => !item.IsFolder).ToList();
        
        if (filesToDelete.Count == 0)
        {
            MessageBox.Show(
                "削除可能なファイルが選択されていません。",
                "情報",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // 確認ダイアログ
        var message = filesToDelete.Count == 1
            ? $"'{filesToDelete[0].FileName}' をゴミ箱に移動しますか?"
            : $"{filesToDelete.Count}個のファイルをゴミ箱に移動しますか?\n\n" +
              string.Join("\n", filesToDelete.Take(5).Select(f => f.FileName)) +
              (filesToDelete.Count > 5 ? $"\n... 他 {filesToDelete.Count - 5}個" : "");

        var result = MessageBox.Show(
            message,
            "削除確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        // 削除処理
        var successCount = 0;
        var failedFiles = new List<string>();

        foreach (var item in filesToDelete)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    item.FilePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                // リストから削除
                _imageFiles.Remove(item.FilePath);
                ThumbnailItems.Remove(item);
                successCount++;
            }
            catch (Exception ex)
            {
                failedFiles.Add($"{item.FileName}: {ex.Message}");
            }
        }

        // 結果表示
        TotalImages = _imageFiles.Count;
        
        if (failedFiles.Count > 0)
        {
            var errorMessage = $"{successCount}個のファイルを削除しました。\n\n" +
                             $"以下の{failedFiles.Count}個のファイルの削除に失敗しました:\n" +
                             string.Join("\n", failedFiles.Take(5)) +
                             (failedFiles.Count > 5 ? $"\n... 他 {failedFiles.Count - 5}個" : "");
            MessageBox.Show(errorMessage, "削除結果", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            StatusText = $"{successCount}個のファイルを削除しました";
        }

        // 選択をクリア
        SelectedItems.Clear();

        // 画像表示を更新
        if (_imageFiles.Count > 0)
        {
            if (_internalImageIndex >= _imageFiles.Count)
                _internalImageIndex = _imageFiles.Count - 1;
            
            if (_internalImageIndex >= 0)
                _ = LoadCurrentImageAsync();
        }
        else
        {
            CurrentImageSource = null;
            CurrentImageInfo = null;
            StatusText = "画像がありません";
        }
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (_internalImageIndex >= 0 && _internalImageIndex < _imageFiles.Count)
        {
            var filePath = _imageFiles[_internalImageIndex];
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }

    [RelayCommand]
    private void RenameItem()
    {
        var items = SelectedItems.Count > 0 ? SelectedItems.ToList()
                  : SelectedThumbnail != null ? new List<ThumbnailItem> { SelectedThumbnail }
                  : new List<ThumbnailItem>();

        if (items.Count != 1)
        {
            StatusText = "名前の変更は1つのファイルを選択してください";
            return;
        }

        var item = items[0];
        var dialog = new RenameDialog(item.FileName);
        if (dialog.ShowDialog() == true)
        {
            var newName = dialog.NewName;
            var dir = Path.GetDirectoryName(item.FilePath) ?? string.Empty;
            var newPath = Path.Combine(dir, newName);
            try
            {
                if (item.IsFolder)
                    Directory.Move(item.FilePath, newPath);
                else
                    File.Move(item.FilePath, newPath);

                // リスト更新
                var idx = _imageFiles.IndexOf(item.FilePath);
                if (idx >= 0) _imageFiles[idx] = newPath;
                item.FilePath = newPath;
                item.FileName = newName;
                StatusText = $"名前を変更しました: {newName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"名前の変更に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 選択ファイルを指定フォルダに移動（D&Dから呼ばれる）
    /// </summary>
    public async Task MoveSelectedItemsToFolderAsync(string destinationFolder)
    {
        // チェック済みがあればそちらを優先、なければ選択アイテム
        var items = CheckedItems.Count > 0 ? CheckedItems.ToList()
                  : SelectedItems.Count > 0 ? SelectedItems.ToList()
                  : SelectedThumbnail != null ? new List<ThumbnailItem> { SelectedThumbnail }
                  : new List<ThumbnailItem>();

        var filesToMove = items.Where(i => i.FileName != "..").ToList();
        if (filesToMove.Count == 0) return;

        var message = filesToMove.Count == 1
            ? $"'{filesToMove[0].FileName}' を '{Path.GetFileName(destinationFolder)}' に移動しますか?"
            : $"{filesToMove.Count}個のアイテムを '{Path.GetFileName(destinationFolder)}' に移動しますか?";

        if (MessageBox.Show(message, "移動確認", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        await MoveItemsToFolderAsync(filesToMove, destinationFolder);
    }

    [RelayCommand]
    private void OpenWithDefaultApp()
    {
        if (_internalImageIndex >= 0 && _internalImageIndex < _imageFiles.Count)
        {
            var filePath = _imageFiles[_internalImageIndex];
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        LoadFolderCommand.Execute(null);
    }

    /// <summary>アドレスバーからパスを入力してナビゲート</summary>
    [RelayCommand]
    private async Task NavigateToAddressAsync()
    {
        var path = AddressBarPath.Trim();
        if (string.IsNullOrEmpty(path)) return;

        if (!Directory.Exists(path))
        {
            StatusText = $"フォルダが見つかりません: {path}";
            AddressBarPath = _currentFolderPath; // 元のパスに戻す
            return;
        }

        await LoadFolderAsync(path);
        OnRestoreTreeRequested?.Invoke(path);
    }

    #endregion

    #region Sort Commands

    [RelayCommand]
    private async Task SortAsync(string sortMode)
    {
        // 現在のフォルダを再読み込み
        if (!string.IsNullOrEmpty(_currentFolderPath))
        {
            await LoadFolderAsync(_currentFolderPath);
            
            // ソート適用
            var items = ThumbnailItems.ToList();
            var folders = items.Where(i => i.IsFolder && i.FileName != "..").ToList();
            var files = items.Where(i => !i.IsFolder).ToList();

            switch (sortMode)
            {
                case "NameAsc":
                    folders = folders.OrderBy(f => f.FileName).ToList();
                    files = files.OrderBy(f => f.FileName).ToList();
                    break;
                case "NameDesc":
                    folders = folders.OrderByDescending(f => f.FileName).ToList();
                    files = files.OrderByDescending(f => f.FileName).ToList();
                    break;
                case "DateAsc":
                    folders = folders.OrderBy(f => f.ModifiedDate).ToList();
                    files = files.OrderBy(f => f.ModifiedDate).ToList();
                    break;
                case "DateDesc":
                    folders = folders.OrderByDescending(f => f.ModifiedDate).ToList();
                    files = files.OrderByDescending(f => f.ModifiedDate).ToList();
                    break;
                case "SizeAsc":
                    folders = folders.OrderBy(f => f.FileName).ToList();
                    files = files.OrderBy(f => f.FileSize).ToList();
                    break;
                case "SizeDesc":
                    folders = folders.OrderByDescending(f => f.FileName).ToList();
                    files = files.OrderByDescending(f => f.FileSize).ToList();
                    break;
            }

            ThumbnailItems.Clear();
            
            // 親フォルダ
            var parentItem = items.FirstOrDefault(i => i.FileName == "..");
            if (parentItem != null)
                ThumbnailItems.Add(parentItem);
            
            // フォルダ
            foreach (var folder in folders)
                ThumbnailItems.Add(folder);
            
            // ファイル
            foreach (var file in files)
                ThumbnailItems.Add(file);
        }
    }

    #endregion

    #region Other Commands

    /// <summary>
    /// フォルダツリーをリフレッシュ
    /// </summary>
    [RelayCommand]
    private async Task RefreshFolderTreeAsync()
    {
        StatusText = "フォルダツリーを更新中...";
        try
        {
            var tree = await Task.Run(() => _fileSystemService.GetFolderTree());
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                FolderTree = tree;
            });
            StatusText = "フォルダツリーを更新しました";
        }
        catch (Exception ex)
        {
            StatusText = $"フォルダツリー更新エラー: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        MessageBox.Show("設定画面は未実装です", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void About()
    {
        MessageBox.Show(
            "Image Explorer v1.1.0\n\nエクスプローラー風イメージビューアー\n\n© 2026 Image Explorer Team",
            "バージョン情報",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    #endregion

    /// <summary>
    /// サムネイル選択変更時
    /// </summary>
    partial void OnSelectedThumbnailChanged(ThumbnailItem? value)
    {
        if (_isUpdatingSelection || value == null)
            return;

        if (!value.IsFolder)
        {
            // 画像ファイルの場合のみ表示
            var imageIndex = _imageFiles.IndexOf(value.FilePath);
            if (imageIndex >= 0 && imageIndex != _internalImageIndex)
            {
                _internalImageIndex = imageIndex;
                _ = LoadCurrentImageAsync();
            }
            else if (imageIndex < 0)
            {
                // 画像以外のファイルの場合
                StatusText = $"{value.FileName} は画像ファイルではありません";
            }
        }
    }

    /// <summary>
    /// 画像ソース変更時
    /// </summary>
    partial void OnCurrentImageSourceChanged(BitmapSource? value)
    {
        UpdateImageDisplaySize();
    }
}
