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

    #region Properties

    [ObservableProperty]
    private ObservableCollection<FolderTreeNode> _folderTree = new();

    [ObservableProperty]
    private ObservableCollection<ThumbnailItem> _thumbnailItems = new();

    [ObservableProperty]
    private ThumbnailItem? _selectedThumbnail;

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
            _ = Task.Run(async () =>
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

            StatusText = "準備完了";
        }
        catch (Exception ex)
        {
            StatusText = $"初期化エラー: {ex.Message}";
        }
    }

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
        ZoomScale = Math.Min(ZoomScale * 1.25, 16.0);
        ZoomPercentage = ZoomScale * 100;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomScale = Math.Max(ZoomScale / 1.25, 0.1);
        ZoomPercentage = ZoomScale * 100;
    }

    [RelayCommand]
    private void ActualSize()
    {
        ZoomScale = 1.0;
        ZoomPercentage = 100.0;
    }

    [RelayCommand]
    private void FitToWindow()
    {
        // ウィンドウにフィット（実装は簡易版）
        ZoomScale = 1.0;
        ZoomPercentage = 100.0;
    }

    [RelayCommand]
    private void FitToWidth()
    {
        // 幅にフィット（実装は簡易版）
        ZoomScale = 1.0;
        ZoomPercentage = 100.0;
    }

    [RelayCommand]
    private void FitToHeight()
    {
        // 高さにフィット（実装は簡易版）
        ZoomScale = 1.0;
        ZoomPercentage = 100.0;
    }

    #endregion

    #region View Commands

    [RelayCommand]
    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
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
                ThumbnailItems.RemoveAt(_internalImageIndex);
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

    [RelayCommand]
    private void OpenSettings()
    {
        MessageBox.Show("設定画面は未実装です", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void About()
    {
        MessageBox.Show(
            "KoExplorer v1.0.0\n\nエクスプローラー風イメージビューアー\n\n© 2026 KoExplorer Team",
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
}
