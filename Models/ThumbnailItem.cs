using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KoExplorer.Models;

/// <summary>
/// サムネイルアイテムを表すモデル
/// </summary>
public partial class ThumbnailItem : ObservableObject
{
    /// <summary>
    /// ファイルパス
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// ファイル名
    /// </summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// サムネイル画像ソース
    /// </summary>
    [ObservableProperty]
    private ImageSource? _thumbnailSource;

    /// <summary>
    /// 読み込み中フラグ
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// エラーフラグ
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// インデックス
    /// </summary>
    [ObservableProperty]
    private int _index;

    /// <summary>
    /// フォルダかどうか
    /// </summary>
    [ObservableProperty]
    private bool _isFolder;

    /// <summary>
    /// ファイルサイズ（バイト）
    /// </summary>
    [ObservableProperty]
    private long _fileSize;

    /// <summary>
    /// 更新日時
    /// </summary>
    [ObservableProperty]
    private DateTime _modifiedDate;
}
