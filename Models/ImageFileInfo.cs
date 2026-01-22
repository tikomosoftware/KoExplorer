namespace KoExplorer.Models;

/// <summary>
/// 画像ファイル情報を表すモデル
/// </summary>
public class ImageFileInfo
{
    /// <summary>
    /// ファイルの完全パス
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// ファイル名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// ファイルサイズ（バイト）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// ファイルサイズ（表示用）
    /// </summary>
    public string FileSizeFormatted => FormatFileSize(FileSize);

    /// <summary>
    /// 画像の幅
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 画像の高さ
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 解像度（表示用）
    /// </summary>
    public string Resolution => $"{Width} x {Height}";

    /// <summary>
    /// ファイル形式
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// 更新日時
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// 作成日時
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// ファイルサイズをフォーマット
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
