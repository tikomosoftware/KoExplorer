using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace KoExplorer.Converters;

/// <summary>
/// ファイルの更新日時を取得するコンバーター
/// </summary>
public class FileModifiedDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string filePath && File.Exists(filePath))
        {
            return File.GetLastWriteTime(filePath);
        }
        return DateTime.MinValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ファイルの種類を取得するコンバーター
/// </summary>
public class FileTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "JPEG 画像",
                ".png" => "PNG 画像",
                ".gif" => "GIF 画像",
                ".bmp" => "ビットマップ 画像",
                ".webp" => "WebP 画像",
                ".tiff" or ".tif" => "TIFF 画像",
                ".ico" => "アイコン",
                ".svg" => "SVG 画像",
                ".txt" => "テキスト ドキュメント",
                ".pdf" => "PDF ドキュメント",
                ".doc" or ".docx" => "Microsoft Word ドキュメント",
                ".xls" or ".xlsx" => "Microsoft Excel ワークシート",
                ".ppt" or ".pptx" => "Microsoft PowerPoint プレゼンテーション",
                ".zip" or ".rar" or ".7z" => "圧縮ファイル",
                ".mp3" or ".wav" or ".flac" => "オーディオ ファイル",
                ".mp4" or ".avi" or ".mkv" => "ビデオ ファイル",
                ".exe" => "アプリケーション",
                ".dll" => "アプリケーション拡張",
                _ => $"{extension.TrimStart('.')} ファイル"
            };
        }
        return "ファイル";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ファイルサイズを取得するコンバーター
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long size = 0;
        
        if (value is long longSize)
        {
            size = longSize;
        }
        else if (value is string filePath && File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            size = fileInfo.Length;
        }

        if (size == 0)
            return "";
        else if (size < 1024)
            return $"{size} B";
        else if (size < 1024 * 1024)
            return $"{size / 1024.0:F1} KB";
        else if (size < 1024 * 1024 * 1024)
            return $"{size / (1024.0 * 1024.0):F1} MB";
        else
            return $"{size / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 複数のBool値をAND演算してVisibilityに変換するコンバーター
/// </summary>
public class BooleanAndToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
            return System.Windows.Visibility.Collapsed;

        foreach (var value in values)
        {
            if (value is bool boolValue && !boolValue)
                return System.Windows.Visibility.Collapsed;
        }

        return System.Windows.Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
