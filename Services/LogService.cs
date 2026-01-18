using System.IO;

namespace KoExplorer.Services;

/// <summary>
/// ログサービス
/// </summary>
public static class LogService
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "KoExplorer_Debug.log"
    );

    private static readonly object _lock = new object();

    /// <summary>
    /// ログを書き込む
    /// </summary>
    //public static void Log(string message)
    //{
    //    try
    //    {
    //        lock (_lock)
    //        {
    //            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
    //            var logMessage = $"[{timestamp}] {message}";

    //            // コンソールにも出力
    //            System.Diagnostics.Debug.WriteLine(logMessage);
    //            Console.WriteLine(logMessage);

    //            // ファイルに追記
    //            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
    //        }
    //    }
    //    catch
    //    {
    //        // ログ出力エラーは無視
    //    }
    //}

    public static void Log(string message)
    {
        // ログ出力を一時的に無効化（パフォーマンス改善のため）
        // try
        // {
        //     lock (_lock)
        //     {
        //         var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        //         var logMessage = $"[{timestamp}] {message}";
        //
        //         File.AppendAllText(
        //             LogFilePath,
        //             logMessage + Environment.NewLine
        //         );
        //     }
        // }
        // catch
        // {
        //     // ログ中の例外は「何もしない」
        //     // ※ここでさらにログを書いたら死亡
        // }
    }

    /// <summary>
    /// ログファイルをクリア
    /// </summary>
    public static void ClearLog()
    {
        // ログ出力を一時的に無効化（パフォーマンス改善のため）
        // try
        // {
        //     lock (_lock)
        //     {
        //         if (File.Exists(LogFilePath))
        //         {
        //             File.Delete(LogFilePath);
        //         }
        //         Log("=== KoImageViewer デバッグログ開始 ===");
        //     }
        // }
        // catch
        // {
        //     // エラーは無視
        // }
    }

    /// <summary>
    /// ログファイルのパスを取得
    /// </summary>
    public static string GetLogFilePath()
    {
        return LogFilePath;
    }
}
