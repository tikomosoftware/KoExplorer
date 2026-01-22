using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KoExplorer.Models;
using KoExplorer.ViewModels;

namespace KoExplorer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// メインウィンドウのコードビハインド
/// </summary>
public partial class MainWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

    /// <summary>
    /// デバッグログを追加
    /// </summary>
    private void AddDebugLog(string message)
    {
#if DEBUG
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            DebugLogTextBox.AppendText($"[{timestamp}] {message}\n");
            DebugLogTextBox.ScrollToEnd();
        });
#endif
    }

    /// <summary>
    /// デバッグログをクリア
    /// </summary>
    private void ClearDebugLog_Click(object sender, RoutedEventArgs e)
    {
#if DEBUG
        DebugLogTextBox.Clear();
#endif
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // ViewModelのIsFullScreenプロパティ変更を監視
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsFullScreen))
            {
                if (viewModel.IsFullScreen)
                {
                    EnterFullScreen();
                }
                else
                {
                    ExitFullScreen();
                }
            }
        };
        
        // ウィンドウのContentRenderedイベントで初期サイズを設定
        ContentRendered += (s, e) =>
        {
            if (ImageScrollViewer != null && DataContext is MainViewModel vm)
            {
                // レイアウトが完了した後にサイズを取得
                Dispatcher.InvokeAsync(() =>
                {
                    vm.ViewportWidth = ImageScrollViewer.ActualWidth;
                    vm.ViewportHeight = ImageScrollViewer.ActualHeight;
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        };
        
        // DataGridとListBoxの選択変更イベントを購読
        Loaded += (s, e) =>
        {
            if (FileDataGrid != null)
            {
                FileDataGrid.SelectionChanged += FileDataGrid_SelectionChanged;
            }
            
            // すべてのListBoxに選択変更イベントを追加
            if (ThumbnailListBox != null)
            {
                ThumbnailListBox.SelectionChanged += ListBox_SelectionChanged;
            }
            
            // 他のListBoxも探して追加（名前が自動生成されている場合）
            AddSelectionHandlerToListBoxes(this);
            
            // ScrollViewerのサイズ変更を監視
            if (ImageScrollViewer != null)
            {
                // 初期サイズを設定
                if (DataContext is MainViewModel vm)
                {
                    vm.ViewportWidth = ImageScrollViewer.ActualWidth;
                    vm.ViewportHeight = ImageScrollViewer.ActualHeight;
                }
                
                ImageScrollViewer.SizeChanged += (sender, args) =>
                {
                    if (DataContext is MainViewModel vm2)
                    {
                        vm2.ViewportWidth = ImageScrollViewer.ActualWidth;
                        vm2.ViewportHeight = ImageScrollViewer.ActualHeight;
                    }
                };
            }
        };
    }

    /// <summary>
    /// フルスクリーンモードに入る
    /// </summary>
    private void EnterFullScreen()
    {
        _previousWindowState = WindowState;
        _previousWindowStyle = WindowStyle;
        _previousResizeMode = ResizeMode;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;
    }

    /// <summary>
    /// フルスクリーンモードを終了
    /// </summary>
    private void ExitFullScreen()
    {
        WindowStyle = _previousWindowStyle;
        ResizeMode = _previousResizeMode;
        WindowState = _previousWindowState;
    }

    /// <summary>
    /// すべてのListBoxに選択変更イベントを追加
    /// </summary>
    private void AddSelectionHandlerToListBoxes(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is ListBox listBox && listBox.Name.StartsWith("ThumbnailListBox"))
            {
                listBox.SelectionChanged -= ListBox_SelectionChanged; // 重複登録を防ぐ
                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
            
            AddSelectionHandlerToListBoxes(child);
        }
    }

    /// <summary>
    /// ListBoxの選択変更イベント
    /// </summary>
    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is ListBox listBox)
        {
            viewModel.SelectedItems.Clear();
            foreach (var item in listBox.SelectedItems)
            {
                if (item is ThumbnailItem thumbnailItem)
                {
                    viewModel.SelectedItems.Add(thumbnailItem);
                }
            }
        }
    }

    /// <summary>
    /// DataGridの選択変更イベント
    /// </summary>
    private void FileDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is DataGrid dataGrid)
        {
            viewModel.SelectedItems.Clear();
            foreach (var item in dataGrid.SelectedItems)
            {
                if (item is ThumbnailItem thumbnailItem)
                {
                    viewModel.SelectedItems.Add(thumbnailItem);
                }
            }
        }
    }

    /// <summary>
    /// フォルダツリーの選択変更イベント
    /// </summary>
    private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel viewModel && e.NewValue is FolderTreeNode node)
        {
            if (!string.IsNullOrEmpty(node.FullPath))
            {
                viewModel.LoadFolderCommand.Execute(node);
            }
        }
    }

    /// <summary>
    /// DataGridのダブルクリックイベント
    /// </summary>
    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is DataGrid dataGrid)
        {
            if (dataGrid.SelectedItem is ThumbnailItem item && item.IsFolder)
            {
                viewModel.LoadFolderCommand.Execute(item);
            }
        }
    }

    /// <summary>
    /// ListBoxのダブルクリックイベント
    /// </summary>
    private void ThumbnailListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is ListBox listBox)
        {
            if (listBox.SelectedItem is ThumbnailItem item && item.IsFolder)
            {
                viewModel.LoadFolderCommand.Execute(item);
            }
        }
    }

    /// <summary>
    /// TreeViewItem展開時のイベント
    /// </summary>
    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is FolderTreeNode node)
        {
            LoadSubFolders(node);
        }
    }

    /// <summary>
    /// サブフォルダを読み込む
    /// </summary>
    private void LoadSubFolders(FolderTreeNode node)
    {
        // すでに読み込み済みの場合はスキップ
        if (node.Children.Count > 0 && node.Children[0].Name != "読み込み中...")
            return;

        node.Children.Clear();

        if (string.IsNullOrEmpty(node.FullPath))
            return;

        try
        {
            var directories = Directory.GetDirectories(node.FullPath);
            foreach (var dir in directories)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // 隠しフォルダやシステムフォルダをスキップ
                    if ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                        (dirInfo.Attributes & FileAttributes.System) == FileAttributes.System)
                        continue;

                    var childNode = new FolderTreeNode
                    {
                        Name = dirInfo.Name,
                        FullPath = dirInfo.FullName,
                        Icon = "Resources/Icons/folder.png"
                    };

                    // サブフォルダがあるかチェック（ダミーノードを追加）
                    if (HasSubFolders(dirInfo.FullName))
                    {
                        childNode.Children.Add(new FolderTreeNode { Name = "読み込み中..." });
                    }

                    node.Children.Add(childNode);
                }
                catch
                {
                    // アクセス権限がない場合などはスキップ
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"サブフォルダ読み込みエラー: {ex.Message}");
        }
    }

    /// <summary>
    /// サブフォルダが存在するかチェック
    /// </summary>
    private bool HasSubFolders(string path)
    {
        try
        {
            return Directory.GetDirectories(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 画像のマウスホイールイベント（ズーム制御）
    /// </summary>
    private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (e.Delta > 0)
                {
                    viewModel.ZoomInCommand.Execute(null);
                }
                else
                {
                    viewModel.ZoomOutCommand.Execute(null);
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// スクロールビューアのマウスホイールイベント（画像切り替え）
    /// </summary>
    private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (e.Delta > 0)
                {
                    viewModel.PreviousImageCommand.Execute(null);
                }
                else
                {
                    viewModel.NextImageCommand.Execute(null);
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// 画像のマウス左ボタン押下イベント（ドラッグ開始）
    /// </summary>
    private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Image image)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(ImageScrollViewer);
            image.CaptureMouse();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 画像のマウス左ボタン解放イベント（ドラッグ終了）
    /// </summary>
    private void PreviewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Image image && _isDragging)
        {
            _isDragging = false;
            image.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 画像のマウス移動イベント（パン操作）
    /// </summary>
    private void PreviewImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPosition = e.GetPosition(ImageScrollViewer);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;

            ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - deltaX);
            ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - deltaY);

            _lastMousePosition = currentPosition;
            e.Handled = true;
        }
    }

    /// <summary>
    /// 指定パスのTreeViewItemを展開・選択
    /// </summary>
    public void ExpandTreeToPath(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
            return;

        // TreeViewのルートノードから検索
        foreach (var item in FolderTreeView.Items)
        {
            var container = FolderTreeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container != null)
            {
                if (ExpandTreeViewItemToPath(container, targetPath))
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// フォルダを開くボタンクリック
    /// </summary>
    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
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

        var folderPath = Path.GetDirectoryName(dialog.FileName);
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        // 左ペインのツリーを展開・選択するだけ
        // → FolderTreeView_SelectedItemChanged が発火して右ペインも更新される
        ExpandTreeToPath(folderPath);
    }

    /// <summary>
    /// テストボタンクリック
    /// </summary>
    private void TestTreeButton_Click(object sender, RoutedEventArgs e)
    {
#if DEBUG
        // テスト用パス
        var testPath = @"H:\(01)写真データ";
        
        AddDebugLog("=== テスト開始 ===");
        AddDebugLog($"対象パス: {testPath}");

        if (DataContext is MainViewModel vm)
        {
            vm.StatusText = $"テスト開始: {testPath}";
            AddDebugLog($"FolderTree.Count: {vm.FolderTree.Count}");
        }

        if (!Directory.Exists(testPath))
        {
            AddDebugLog($"❌ フォルダが存在しません: {testPath}");
            if (DataContext is MainViewModel vm2)
            {
                vm2.StatusText = $"テスト失敗: フォルダが存在しません";
            }
            MessageBox.Show($"フォルダが存在しません:\n{testPath}", "テストエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // ツリーを展開
        ExpandTreeToPath(testPath);

        if (DataContext is MainViewModel vm3)
        {
            vm3.StatusText = $"テスト完了: {testPath}";
        }
        
        AddDebugLog("=== テスト完了 ===");
#endif
    }

    /// <summary>
    /// TreeViewItemを再帰的に展開して目的のパスを選択
    /// </summary>
    private bool ExpandTreeViewItemToPath(TreeViewItem treeViewItem, string targetPath)
    {
        if (treeViewItem.DataContext is FolderTreeNode node)
        {
            AddDebugLog($"チェック中: {node.Name} ({node.FullPath})");

            // 完全一致
            if (string.Equals(node.FullPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                AddDebugLog($"✅ 完全一致: {node.FullPath}");
                treeViewItem.IsExpanded = true;
                treeViewItem.IsSelected = true;
                treeViewItem.BringIntoView();
                return true;
            }

            // パスの途中（展開して子を検索）
            if (!string.IsNullOrEmpty(node.FullPath))
            {
                // パス比較用の正規化
                var nodePathForComparison = node.FullPath;
                // ドライブルート（例: "C:\"）は既に \ で終わっているので追加しない
                if (!nodePathForComparison.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    nodePathForComparison += Path.DirectorySeparatorChar;
                }

                AddDebugLog($"比較: '{targetPath}' が '{nodePathForComparison}' で始まるか？");

                if (targetPath.StartsWith(nodePathForComparison, StringComparison.OrdinalIgnoreCase))
                {
                    AddDebugLog($"📂 部分一致（展開）: {node.FullPath}");
                    
                    // 展開
                    treeViewItem.IsExpanded = true;
                    
                    // 子ノードを読み込む（遅延読み込み対応）
                    LoadSubFolders(node);
                    AddDebugLog($"子ノード数: {node.Children.Count}");
                    
                    // ItemContainerGeneratorを更新
                    treeViewItem.UpdateLayout();

                    // 子アイテムを検索
                    int childContainerCount = 0;
                    foreach (var childItem in treeViewItem.Items)
                    {
                        var childContainer = treeViewItem.ItemContainerGenerator.ContainerFromItem(childItem) as TreeViewItem;
                        if (childContainer != null)
                        {
                            childContainerCount++;
                            if (ExpandTreeViewItemToPath(childContainer, targetPath))
                            {
                                return true;
                            }
                        }
                    }
                    
                    if (childContainerCount == 0)
                    {
                        AddDebugLog($"⚠ 子コンテナが取得できない（Items={treeViewItem.Items.Count}）");
                    }
                }
                else
                {
                    AddDebugLog($"❌ 一致しない");
                }
            }

            // FullPathが空の場合（クイックアクセス、PCなど）、子を検索
            if (string.IsNullOrEmpty(node.FullPath))
            {
                AddDebugLog($"📁 ルートノード展開: {node.Name}");
                treeViewItem.IsExpanded = true;
                treeViewItem.UpdateLayout();

                foreach (var childItem in treeViewItem.Items)
                {
                    var childContainer = treeViewItem.ItemContainerGenerator.ContainerFromItem(childItem) as TreeViewItem;
                    if (childContainer != null)
                    {
                        if (ExpandTreeViewItemToPath(childContainer, targetPath))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
