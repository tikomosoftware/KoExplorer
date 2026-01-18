using System.Collections.ObjectModel;
using System.IO;
using KoExplorer.Models;

namespace KoExplorer.Services;

/// <summary>
/// ファイルシステムサービスのインターフェース
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// フォルダツリーを取得
    /// </summary>
    ObservableCollection<FolderTreeNode> GetFolderTree();

    /// <summary>
    /// フォルダ内の画像ファイルを取得
    /// </summary>
    Task<List<string>> GetImageFilesAsync(string folderPath);

    /// <summary>
    /// フォルダ内のすべてのファイルを取得
    /// </summary>
    Task<List<string>> GetAllFilesAsync(string folderPath);

    /// <summary>
    /// サブフォルダを取得
    /// </summary>
    Task<List<string>> GetSubFoldersAsync(string folderPath);
}

/// <summary>
/// ファイルシステムサービス
/// </summary>
public class FileSystemService : IFileSystemService
{
    private static readonly string[] SupportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".svg"
    };

    /// <summary>
    /// フォルダツリーを取得
    /// </summary>
    public ObservableCollection<FolderTreeNode> GetFolderTree()
    {
        var tree = new ObservableCollection<FolderTreeNode>();

        try
        {
            // クイックアクセス
            var quickAccess = new FolderTreeNode
            {
                Name = "クイックアクセス",
                FullPath = "",
                Icon = "Resources/Icons/quick-access.png",
                IsExpanded = false // デフォルトで閉じる
            };

            // マイピクチャ
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (Directory.Exists(picturesPath))
            {
                var picturesNode = new FolderTreeNode
                {
                    Name = "マイピクチャ",
                    FullPath = picturesPath,
                    Icon = "Resources/Icons/pictures.png"
                };
                // ダミーノードを常に追加（遅延読み込み）
                picturesNode.Children.Add(new FolderTreeNode { Name = "読み込み中..." });
                quickAccess.Children.Add(picturesNode);
            }

            // デスクトップ
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktopPath))
            {
                var desktopNode = new FolderTreeNode
                {
                    Name = "デスクトップ",
                    FullPath = desktopPath,
                    Icon = "Resources/Icons/desktop.png"
                };
                // ダミーノードを常に追加（遅延読み込み）
                desktopNode.Children.Add(new FolderTreeNode { Name = "読み込み中..." });
                quickAccess.Children.Add(desktopNode);
            }

            tree.Add(quickAccess);

            // PC（ドライブ）
            var pc = new FolderTreeNode
            {
                Name = "PC",
                FullPath = "",
                Icon = "Resources/Icons/pc.png",
                IsExpanded = false // デフォルトで閉じる
            };

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    var driveNode = new FolderTreeNode
                    {
                        Name = drive.Name,
                        FullPath = drive.RootDirectory.FullName,
                        Icon = "Resources/Icons/drive.png"
                    };
                    // ドライブには常にダミーノードを追加
                    driveNode.Children.Add(new FolderTreeNode { Name = "読み込み中..." });
                    pc.Children.Add(driveNode);
                }
            }

            tree.Add(pc);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"フォルダツリー取得エラー: {ex.Message}");
        }

        return tree;
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
    /// フォルダ内の画像ファイルを取得
    /// </summary>
    public async Task<List<string>> GetImageFilesAsync(string folderPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return new List<string>();

                var files = Directory.GetFiles(folderPath)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f)
                    .ToList();

                return files;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像ファイル取得エラー: {folderPath}, {ex.Message}");
                return new List<string>();
            }
        });
    }

    /// <summary>
    /// フォルダ内のすべてのファイルを取得（サムネイル表示用）
    /// </summary>
    public async Task<List<string>> GetAllFilesAsync(string folderPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return new List<string>();

                var files = Directory.GetFiles(folderPath)
                    .OrderBy(f => f)
                    .ToList();

                return files;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ファイル取得エラー: {folderPath}, {ex.Message}");
                return new List<string>();
            }
        });
    }

    /// <summary>
    /// サブフォルダを取得
    /// </summary>
    public async Task<List<string>> GetSubFoldersAsync(string folderPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return new List<string>();

                return Directory.GetDirectories(folderPath).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"サブフォルダ取得エラー: {folderPath}, {ex.Message}");
                return new List<string>();
            }
        });
    }
}
