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
                    EnterFullScreen();
                else
                    ExitFullScreen();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsFolderTreeVisible))
            {
                var col0 = MainContentGrid.ColumnDefinitions[0];
                var col1 = MainContentGrid.ColumnDefinitions[1];
                if (viewModel.IsFolderTreeVisible)
                {
                    col0.Width = new GridLength(250);
                    col0.MinWidth = 150;
                    col1.Width = new GridLength(5);
                }
                else
                {
                    col0.Width = new GridLength(0);
                    col0.MinWidth = 0;
                    col1.Width = new GridLength(0);
                }
            }
        };

        // 起動時のツリー展開要求を購読
        viewModel.OnRestoreTreeRequested += (path) =>
        {
            // ツリーのレイアウトが完了してから展開する
            Dispatcher.InvokeAsync(() => ExpandTreeToPath(path),
                System.Windows.Threading.DispatcherPriority.Loaded);
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
        if (sender is not Image image) return;

        _lastMousePosition = e.GetPosition(ImageScrollViewer);

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Ctrl+ドラッグ = パン操作
            _isDragging = true;
            image.CaptureMouse();
        }
        else
        {
            // 通常ドラッグ = ファイルD&D開始準備
            _isDragging = false;
            _dragStartPoint = e.GetPosition(null);
            _isDragStarting = true;
        }
        e.Handled = true;
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
        }
        _isDragStarting = false;
        e.Handled = true;
    }

    /// <summary>
    /// 画像のマウス移動イベント（パン操作 or ファイルD&D）
    /// </summary>
    private void PreviewImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        if (_isDragging)
        {
            // Ctrl+ドラッグ = パン
            Point currentPosition = e.GetPosition(ImageScrollViewer);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - deltaX);
            ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - deltaY);
            _lastMousePosition = currentPosition;
            e.Handled = true;
        }
        else if (_isDragStarting)
        {
            // 通常ドラッグ = ファイルD&D
            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            _isDragStarting = false;

            if (DataContext is not MainViewModel vm) return;

            // 現在表示中のファイルをD&Dデータに乗せる
            var items = vm.SelectedThumbnail != null
                ? new List<KoExplorer.Models.ThumbnailItem> { vm.SelectedThumbnail }
                : new List<KoExplorer.Models.ThumbnailItem>();
            if (items.Count == 0 || items[0].IsFolder) return;

            var paths = items.Select(i => i.FilePath).ToArray();
            var data = new DataObject(DataFormats.FileDrop, paths);
            data.SetData("KoExplorerItems", items);

            if (sender is DependencyObject dep)
                DragDrop.DoDragDrop(dep, data, DragDropEffects.Move | DragDropEffects.Copy);
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
    /// アドレスバーのキー入力（Enterで移動、Escで元に戻す）
    /// </summary>
    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel vm)
                vm.NavigateToAddressCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (DataContext is MainViewModel vm)
                AddressBar.Text = vm.AddressBarPath = vm.AddressBarPath; // 元に戻す
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    /// <summary>
    /// アドレスバーにフォーカスが当たったとき全選択
    /// </summary>
    private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
    {
        AddressBar.SelectAll();
    }

    /// <summary>
    /// アドレスバーの移動ボタン
    /// </summary>
    private void AddressBarGo_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.NavigateToAddressCommand.Execute(null);
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

    #region 右クリックメニュー

    /// <summary>
    /// DataGridヘッダーのチェックボックスクリック（全選択/全解除）
    /// </summary>
    private void CheckAllHeader_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is CheckBox cb)
        {
            if (cb.IsChecked == true)
                vm.CheckAllCommand.Execute(null);
            else
                vm.UncheckAllCommand.Execute(null);
        }
    }

    /// <summary>
    /// 右クリック時、既存の複数選択を保持する（右クリックした行が選択済みなら選択を変えない）
    /// </summary>
    private void FileList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        // クリックされた行のアイテムを取得
        ThumbnailItem? clickedItem = null;
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is DataGridRow row) { clickedItem = row.Item as ThumbnailItem; break; }
            if (element is ListBoxItem lbi) { clickedItem = lbi.Content as ThumbnailItem; break; }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        if (clickedItem == null) return;

        // 右クリックしたアイテムが既に選択済みなら選択を変えない（複数選択を保持）
        if (vm.SelectedItems.Contains(clickedItem))
        {
            e.Handled = false; // ContextMenuは開く
            return;
        }

        // 選択されていないアイテムを右クリックした場合はそのアイテムだけ選択
        vm.SelectedThumbnail = clickedItem;
    }

    private void ContextMenu_Open_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var item = vm.SelectedThumbnail;
        if (item == null) return;
        if (item.IsFolder)
            vm.LoadFolderCommand.Execute(item);
        else
            vm.OpenWithDefaultAppCommand.Execute(null);
    }

    private void ContextMenu_Cut_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var items = vm.SelectedItems.Count > 0 ? vm.SelectedItems.ToList()
                  : vm.SelectedThumbnail != null ? new List<KoExplorer.Models.ThumbnailItem> { vm.SelectedThumbnail }
                  : new List<KoExplorer.Models.ThumbnailItem>();
        if (items.Count == 0) return;

        var paths = items.Select(i => i.FilePath).ToArray();
        var fileDropList = new System.Collections.Specialized.StringCollection();
        fileDropList.AddRange(paths);
        var data = new DataObject();
        data.SetFileDropList(fileDropList);
        var moveEffect = new byte[] { 2, 0, 0, 0 };
        data.SetData("Preferred DropEffect", new System.IO.MemoryStream(moveEffect));
        Clipboard.SetDataObject(data);
        vm.StatusText = $"{items.Count}個のアイテムを切り取りました";
    }

    #endregion

    #region ドラッグ&ドロップ（右ペイン → 左ペインのツリー / 右ペイン内フォルダ）

    private Point _dragStartPoint;
    private bool _isDragStarting = false;
    private bool _isDeferringSelection = false;
    private TreeViewItem? _lastHighlightedTreeItem = null;
    private DataGridRow? _lastHighlightedRow = null;
    private List<KoExplorer.Models.ThumbnailItem> _dragSnapshot = new();

    // DataGrid全体でPreviewMouseLeftButtonDownをフック
    private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift)
        {
            _isDragStarting = false;
            _isDeferringSelection = false;
            _dragSnapshot.Clear();
            return;
        }

        if (DataContext is not MainViewModel vm) return;

        // クリックされた要素がDataGridRow上かどうか確認
        ThumbnailItem? clickedItem = null;
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is DataGridRow dgr) { clickedItem = dgr.Item as ThumbnailItem; break; }
            // ScrollBar等のDataGridRow外の要素はドラッグ対象外
            if (element is System.Windows.Controls.Primitives.ScrollBar ||
                element is System.Windows.Controls.Primitives.Track ||
                element is System.Windows.Controls.Primitives.Thumb)
            {
                _isDragStarting = false;
                _isDeferringSelection = false;
                _dragSnapshot.Clear();
                return;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        // DataGridRow上でなければドラッグ開始しない
        if (clickedItem == null)
        {
            _isDragStarting = false;
            _isDeferringSelection = false;
            _dragSnapshot.Clear();
            return;
        }

        _dragStartPoint = e.GetPosition(null);

        if (vm.SelectedItems.Count > 1 && vm.SelectedItems.Contains(clickedItem))
        {
            _dragSnapshot = vm.SelectedItems.ToList();
            _isDeferringSelection = true;
            _isDragStarting = true;
            e.Handled = true;
        }
        else
        {
            _dragSnapshot.Clear();
            _isDeferringSelection = false;
            _isDragStarting = true;
        }
    }

    // マウスボタンを離したとき（ドラッグせずに離した場合）選択を確定する
    private void DataGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDeferringSelection)
        {
            _isDeferringSelection = false;
            _isDragStarting = false;
            _dragSnapshot.Clear();

            // クリックされた行を選択確定
            var element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is DataGridRow dgr)
                {
                    FileDataGrid.SelectedItems.Clear();
                    FileDataGrid.SelectedItem = dgr.Item;
                    break;
                }
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
        }
        else
        {
            _isDragStarting = false;
        }
    }

    // DataGrid全体でPreviewMouseMoveをフック（確実にドラッグ開始できる）
    private void DataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragStarting || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        var diff = _dragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        _isDragStarting = false;
        _isDeferringSelection = false;

        if (DataContext is not MainViewModel vm) return;

        var items = _dragSnapshot.Count > 0 ? _dragSnapshot
                  : vm.SelectedItems.Count > 0 ? vm.SelectedItems.ToList()
                  : vm.SelectedThumbnail != null ? new List<KoExplorer.Models.ThumbnailItem> { vm.SelectedThumbnail }
                  : new List<KoExplorer.Models.ThumbnailItem>();
        _dragSnapshot.Clear();

        if (items.Count == 0) return;

        var paths = items.Select(i => i.FilePath).ToArray();
        var data = new DataObject(DataFormats.FileDrop, paths);
        data.SetData("KoExplorerItems", items);

        DragDrop.DoDragDrop(FileDataGrid, data, DragDropEffects.Move | DragDropEffects.Copy);
    }

    // 旧DataGridRow用ハンドラ（XAMLのRowStyleから削除済みだが参照が残る場合のため空実装）
    private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
    private void DataGridRow_MouseMove(object sender, MouseEventArgs e) { }

    // 右ペイン内のフォルダ行へのDragOver
    private void DataGridRow_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("KoExplorerItems") && !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (sender is DataGridRow row && row.Item is ThumbnailItem item && item.IsFolder && item.FileName != "..")
        {
            if (_lastHighlightedRow != null && _lastHighlightedRow != row)
                _lastHighlightedRow.Background = System.Windows.Media.Brushes.Transparent;
            row.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(80, 0, 120, 212));
            _lastHighlightedRow = row;
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            if (_lastHighlightedRow != null)
            {
                _lastHighlightedRow.Background = System.Windows.Media.Brushes.Transparent;
                _lastHighlightedRow = null;
            }
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    // 右ペイン内のフォルダ行へのDrop
    private void DataGridRow_Drop(object sender, DragEventArgs e)
    {
        if (_lastHighlightedRow != null)
        {
            _lastHighlightedRow.Background = System.Windows.Media.Brushes.Transparent;
            _lastHighlightedRow = null;
        }

        if (sender is not DataGridRow row || row.Item is not ThumbnailItem folderItem || !folderItem.IsFolder) return;
        if (DataContext is not MainViewModel vm) return;

        _ = vm.MoveSelectedItemsToFolderAsync(folderItem.FilePath);
        e.Handled = true;
    }

    // ListBox用
    private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift)
        {
            _isDragStarting = false;
            _dragSnapshot.Clear();
            return;
        }

        if (DataContext is not MainViewModel vm) return;

        // クリックされた要素がListBoxItem上かどうか確認
        ThumbnailItem? clickedItem = null;
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is ListBoxItem lbi) { clickedItem = lbi.Content as ThumbnailItem; break; }
            // ScrollBar等のListBoxItem外の要素はドラッグ対象外
            if (element is System.Windows.Controls.Primitives.ScrollBar ||
                element is System.Windows.Controls.Primitives.Track ||
                element is System.Windows.Controls.Primitives.Thumb)
            {
                _isDragStarting = false;
                _dragSnapshot.Clear();
                return;
            }
            // チェックボックスはドラッグ開始しない（チェック操作を優先）
            if (element is CheckBox)
            {
                _isDragStarting = false;
                _dragSnapshot.Clear();
                return;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        // ListBoxItem上でなければドラッグ開始しない
        if (clickedItem == null)
        {
            _isDragStarting = false;
            _dragSnapshot.Clear();
            return;
        }

        if (vm.SelectedItems.Count > 1 && vm.SelectedItems.Contains(clickedItem))
            _dragSnapshot = vm.SelectedItems.ToList();
        else
            _dragSnapshot.Clear();

        _dragStartPoint = e.GetPosition(null);
        _isDragStarting = true;
    }

    private void FileList_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragStarting || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        var diff = _dragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        _isDragStarting = false;

        if (DataContext is not MainViewModel vm) return;

        var items = _dragSnapshot.Count > 0 ? _dragSnapshot
                  : vm.SelectedItems.Count > 0 ? vm.SelectedItems.ToList()
                  : vm.SelectedThumbnail != null ? new List<KoExplorer.Models.ThumbnailItem> { vm.SelectedThumbnail }
                  : new List<KoExplorer.Models.ThumbnailItem>();
        _dragSnapshot.Clear();

        if (items.Count == 0) return;

        var paths = items.Select(i => i.FilePath).ToArray();
        var data = new DataObject(DataFormats.FileDrop, paths);
        data.SetData("KoExplorerItems", items);

        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move | DragDropEffects.Copy);
    }

    // サムネイルListBox内のフォルダアイテムへのDragOver/Drop
    private ListBoxItem? _lastHighlightedListBoxItem = null;

    private void ThumbnailList_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("KoExplorerItems") && !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        // ホバー中のListBoxItemを取得
        ListBoxItem? hoveredItem = null;
        ThumbnailItem? hoveredData = null;
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is ListBoxItem lbi && lbi.Content is ThumbnailItem ti)
            {
                hoveredItem = lbi;
                hoveredData = ti;
                break;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        // 前のハイライトを解除
        if (_lastHighlightedListBoxItem != null && _lastHighlightedListBoxItem != hoveredItem)
            _lastHighlightedListBoxItem.Background = System.Windows.Media.Brushes.Transparent;

        if (hoveredItem != null && hoveredData != null && hoveredData.IsFolder && hoveredData.FileName != "..")
        {
            hoveredItem.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(80, 0, 120, 212));
            _lastHighlightedListBoxItem = hoveredItem;
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            _lastHighlightedListBoxItem = null;
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ThumbnailList_Drop(object sender, DragEventArgs e)
    {
        if (_lastHighlightedListBoxItem != null)
        {
            _lastHighlightedListBoxItem.Background = System.Windows.Media.Brushes.Transparent;
            _lastHighlightedListBoxItem = null;
        }

        // ドロップ先のフォルダアイテムを取得
        ThumbnailItem? folderItem = null;
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is ListBoxItem lbi && lbi.Content is ThumbnailItem ti && ti.IsFolder)
            {
                folderItem = ti;
                break;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        if (folderItem == null || DataContext is not MainViewModel vm) return;

        _ = vm.MoveSelectedItemsToFolderAsync(folderItem.FilePath);
        e.Handled = true;
    }

    private TreeViewItem? GetTreeViewItemFromPoint(TreeView treeView, Point point)
    {
        var element = treeView.InputHitTest(point) as DependencyObject;
        while (element != null)
        {
            if (element is TreeViewItem tvi) return tvi;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private void FolderTreeView_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("KoExplorerItems") && !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        // ハイライト更新
        var tvi = GetTreeViewItemFromPoint(FolderTreeView, e.GetPosition(FolderTreeView));
        if (_lastHighlightedTreeItem != null && _lastHighlightedTreeItem != tvi)
            _lastHighlightedTreeItem.Background = System.Windows.Media.Brushes.Transparent;

        if (tvi != null && tvi.DataContext is KoExplorer.Models.FolderTreeNode node && !string.IsNullOrEmpty(node.FullPath))
        {
            tvi.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 0, 120, 212));
            _lastHighlightedTreeItem = tvi;
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void FolderTreeView_Drop(object sender, DragEventArgs e)
    {
        // ハイライト解除
        if (_lastHighlightedTreeItem != null)
        {
            _lastHighlightedTreeItem.Background = System.Windows.Media.Brushes.Transparent;
            _lastHighlightedTreeItem = null;
        }

        var tvi = GetTreeViewItemFromPoint(FolderTreeView, e.GetPosition(FolderTreeView));
        if (tvi?.DataContext is not KoExplorer.Models.FolderTreeNode node || string.IsNullOrEmpty(node.FullPath)) return;

        if (DataContext is not MainViewModel vm) return;

        _ = vm.MoveSelectedItemsToFolderAsync(node.FullPath);
        e.Handled = true;
    }

    #endregion
}
