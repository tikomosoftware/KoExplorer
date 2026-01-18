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

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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
}
