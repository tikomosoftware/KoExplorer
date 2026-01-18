namespace KoExplorer.Models;

/// <summary>
/// アプリケーション設定を表すモデル
/// </summary>
public class AppSettings
{
    /// <summary>
    /// サムネイルサイズ
    /// </summary>
    public int ThumbnailSize { get; set; } = 128;

    /// <summary>
    /// サムネイルキャッシュサイズ（MB）
    /// </summary>
    public int ThumbnailCacheSize { get; set; } = 500;

    /// <summary>
    /// 先読み画像数
    /// </summary>
    public int PreloadCount { get; set; } = 3;

    /// <summary>
    /// 最大メモリ使用量（MB）
    /// </summary>
    public int MaxMemoryUsage { get; set; } = 1024;

    /// <summary>
    /// 背景色
    /// </summary>
    public string BackgroundColor { get; set; } = "Gray";

    /// <summary>
    /// フォルダツリー表示
    /// </summary>
    public bool ShowFolderTree { get; set; } = true;

    /// <summary>
    /// サムネイルパネル表示
    /// </summary>
    public bool ShowThumbnailPanel { get; set; } = true;

    /// <summary>
    /// 画像情報表示
    /// </summary>
    public bool ShowImageInfo { get; set; } = true;

    /// <summary>
    /// 前回開いたフォルダ
    /// </summary>
    public string LastOpenedFolder { get; set; } = string.Empty;

    /// <summary>
    /// ウィンドウの幅
    /// </summary>
    public double WindowWidth { get; set; } = 1400;

    /// <summary>
    /// ウィンドウの高さ
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// ウィンドウの左位置
    /// </summary>
    public double WindowLeft { get; set; } = 100;

    /// <summary>
    /// ウィンドウの上位置
    /// </summary>
    public double WindowTop { get; set; } = 100;

    /// <summary>
    /// デフォルトズームモード
    /// </summary>
    public string DefaultZoomMode { get; set; } = "Fit";

    /// <summary>
    /// 画像補間方法
    /// </summary>
    public string InterpolationMode { get; set; } = "HighQuality";
}
