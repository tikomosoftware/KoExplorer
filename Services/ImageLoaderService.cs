using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KoExplorer.Models;

namespace KoExplorer.Services;

/// <summary>
/// 画像読み込みサービスのインターフェース
/// </summary>
public interface IImageLoaderService
{
    /// <summary>
    /// 画像を読み込む
    /// </summary>
    Task<BitmapSource?> LoadImageAsync(string filePath);

    /// <summary>
    /// 画像情報を取得
    /// </summary>
    Task<ImageFileInfo?> GetImageInfoAsync(string filePath);

    /// <summary>
    /// サポートされている拡張子かチェック
    /// </summary>
    bool IsSupportedFormat(string filePath);
}

/// <summary>
/// 画像読み込みサービス
/// </summary>
public class ImageLoaderService : IImageLoaderService
{
    private static readonly string[] SupportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".svg"
    };

    /// <summary>
    /// 画像を読み込む
    /// </summary>
    public async Task<BitmapSource?> LoadImageAsync(string filePath)
    {
        try
        {
            return await Task.Run(() =>
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".svg")
                {
                    return LoadSvgImage(filePath);
                }
                else
                {
                    return LoadBitmapImage(filePath);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"画像読み込みエラー: {filePath}, {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// ビットマップ画像を読み込む
    /// </summary>
    private BitmapSource? LoadBitmapImage(string filePath)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// SVG画像を読み込む
    /// </summary>
    private BitmapSource? LoadSvgImage(string filePath)
    {
        try
        {
            // SVG読み込みはSkiaSharpを使用
            // ここでは簡易実装としてBitmapImageを返す
            // 実際の実装ではSvg.Skiaを使用
            return LoadBitmapImage(filePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 画像情報を取得
    /// </summary>
    public async Task<ImageFileInfo?> GetImageInfoAsync(string filePath)
    {
        try
        {
            return await Task.Run(() =>
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return null;

                var imageInfo = new ImageFileInfo
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    Format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
                    ModifiedDate = fileInfo.LastWriteTime,
                    CreatedDate = fileInfo.CreationTime
                };

                // 画像サイズを取得
                try
                {
                    using var stream = File.OpenRead(filePath);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        imageInfo.Width = frame.PixelWidth;
                        imageInfo.Height = frame.PixelHeight;
                    }
                }
                catch
                {
                    // サイズ取得失敗時はデフォルト値
                    imageInfo.Width = 0;
                    imageInfo.Height = 0;
                }

                return imageInfo;
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// サポートされている拡張子かチェック
    /// </summary>
    public bool IsSupportedFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }
}
