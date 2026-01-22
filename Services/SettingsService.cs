using System.IO;
using System.Text.Json;
using KoExplorer.Models;

namespace KoExplorer.Services;

/// <summary>
/// 設定サービスのインターフェース
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 設定を読み込む
    /// </summary>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// 設定を保存
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);
}

/// <summary>
/// 設定サービス
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "KoExplorer");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    /// <summary>
    /// 設定を読み込む
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"設定読み込みエラー: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <summary>
    /// 設定を保存
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"設定保存エラー: {ex.Message}");
        }
    }
}
