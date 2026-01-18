using System.IO;

namespace KoExplorer.Services;

/// <summary>
/// ログサービス
/// デバッグ用のログ出力機能（現在は無効化）
/// </summary>
public static class LogService
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "KoExplorer_Debug.log"
    );

    private static readonly object _lock = new();
    private static readonly bool _isEnabled = false; // ログ出力の有効/無効

    /// <summary>
    /// ログを書き込む
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    public static void Log(string message)
    {
        if (!_isEnabled) return;

        try
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}";
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
        }
        catch
        {
            // ログ出力エラーは無視
        }
    }

    /// <summary>
    /// ログファイルをクリア
    /// </summary>
    public static void ClearLog()
    {
        if (!_isEnabled) return;

        try
        {
            lock (_lock)
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
                Log("=== KoExplorer デバッグログ開始 ===");
            }
        }
        catch
        {
            // エラーは無視
        }
    }

    /// <summary>
    /// ログファイルのパスを取得
    /// </summary>
    /// <returns>ログファイルの絶対パス</returns>
    public static string GetLogFilePath() => LogFilePath;
}
