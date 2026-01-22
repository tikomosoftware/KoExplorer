using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using KoExplorer.Services;
using KoExplorer.ViewModels;

namespace KoExplorer;

/// <summary>
/// Interaction logic for App.xaml
/// アプリケーションのエントリーポイント
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// アプリケーション起動時の処理
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 依存性注入の設定
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // メインウィンドウの表示
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// サービスの登録
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // サービスの登録
        services.AddSingleton<IImageLoaderService, ImageLoaderService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModelsの登録
        services.AddSingleton<MainViewModel>();

        // ウィンドウの登録
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// アプリケーション終了時の処理
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
