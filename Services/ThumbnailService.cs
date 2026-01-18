using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KoExplorer.Services;

/// <summary>
/// サムネイル生成サービスのインターフェース
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// サムネイルを生成
    /// </summary>
    Task<ImageSource?> GenerateThumbnailAsync(string filePath, int size);

    /// <summary>
    /// サムネイルキャッシュをクリア
    /// </summary>
    void ClearCache();
}

/// <summary>
/// サムネイル生成サービス
/// </summary>
public class ThumbnailService : IThumbnailService
{
    private readonly Dictionary<string, ImageSource> _cache = new();
    private readonly SemaphoreSlim _semaphore = new(4); // 同時生成数を制限

    private static readonly string[] ImageExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".tif", ".ico"
    };

    /// <summary>
    /// サムネイルを生成
    /// </summary>
    public async Task<ImageSource?> GenerateThumbnailAsync(string filePath, int size)
    {
        var cacheKey = $"{filePath}_{size}";

        // キャッシュチェック
        if (_cache.TryGetValue(cacheKey, out var cachedImage))
        {
            return cachedImage;
        }

        await _semaphore.WaitAsync();
        try
        {
            // ダブルチェック
            if (_cache.TryGetValue(cacheKey, out cachedImage))
            {
                return cachedImage;
            }

            var thumbnail = await Task.Run(() => GenerateThumbnail(filePath, size));
            if (thumbnail != null)
            {
                _cache[cacheKey] = thumbnail;
            }

            return thumbnail;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// サムネイル生成（同期版）
    /// </summary>
    private ImageSource? GenerateThumbnail(string filePath, int size)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // 画像ファイルの場合は実際のサムネイルを生成
            if (ImageExtensions.Contains(extension))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = size;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            else
            {
                // 画像以外のファイルはアイコンを生成
                return GenerateFileIcon(extension, size);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"サムネイル生成エラー: {filePath}, {ex.Message}");
            return GenerateFileIcon(Path.GetExtension(filePath).ToLowerInvariant(), size);
        }
    }

    /// <summary>
    /// ファイル拡張子に応じたアイコンを生成
    /// </summary>
    private ImageSource? GenerateFileIcon(string extension, int size)
    {
        try
        {
            var emoji = GetFileEmoji(extension);
            var visual = new System.Windows.Media.DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                // 背景
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    null,
                    new System.Windows.Rect(0, 0, size, size));

                // 絵文字またはテキスト
                var text = new System.Windows.Media.FormattedText(
                    emoji,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface("Segoe UI Emoji"),
                    size * 0.5,
                    System.Windows.Media.Brushes.Black,
                    96);

                context.DrawText(text, new System.Windows.Point(
                    (size - text.Width) / 2,
                    (size - text.Height) / 2));

                // 拡張子テキスト
                if (!string.IsNullOrEmpty(extension))
                {
                    var extText = new System.Windows.Media.FormattedText(
                        extension.TrimStart('.').ToUpper(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Segoe UI"),
                        size * 0.15,
                        System.Windows.Media.Brushes.Gray,
                        96);

                    context.DrawText(extText, new System.Windows.Point(
                        (size - extText.Width) / 2,
                        size * 0.75));
                }
            }

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// ファイル拡張子に応じた絵文字を取得
    /// </summary>
    private string GetFileEmoji(string extension)
    {
        return extension switch
        {
            ".txt" => "📄",
            ".pdf" => "📕",
            ".doc" or ".docx" => "📘",
            ".xls" or ".xlsx" => "📗",
            ".ppt" or ".pptx" => "📙",
            ".zip" or ".rar" or ".7z" => "📦",
            ".mp3" or ".wav" or ".flac" => "🎵",
            ".mp4" or ".avi" or ".mkv" => "🎬",
            ".exe" => "⚙️",
            ".html" or ".htm" => "🌐",
            ".css" => "🎨",
            ".js" or ".ts" => "📜",
            ".json" => "📋",
            ".xml" => "📰",
            ".cs" => "💻",
            ".py" => "🐍",
            ".java" => "☕",
            ".cpp" or ".c" or ".h" => "🔧",
            _ => "📄"
        };
    }

    /// <summary>
    /// サムネイルキャッシュをクリア
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
