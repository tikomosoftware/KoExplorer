using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KoExplorer.Models;

/// <summary>
/// フォルダツリーのノードを表すモデル
/// </summary>
public partial class FolderTreeNode : ObservableObject
{
    /// <summary>
    /// フォルダ名
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// フォルダの完全パス
    /// </summary>
    [ObservableProperty]
    private string _fullPath = string.Empty;

    /// <summary>
    /// アイコンパス
    /// </summary>
    [ObservableProperty]
    private string _icon = "Resources/Icons/folder.png";

    /// <summary>
    /// 子ノード
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FolderTreeNode> _children = new();

    /// <summary>
    /// 画像ファイル数
    /// </summary>
    [ObservableProperty]
    private int _imageCount;

    /// <summary>
    /// 画像ファイルが存在するか
    /// </summary>
    public bool HasImages => ImageCount > 0;

    /// <summary>
    /// 展開状態
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 選択状態
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}
