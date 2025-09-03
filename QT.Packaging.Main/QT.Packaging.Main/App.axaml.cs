using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using QT.Packaging.Main.ViewModels;
using QT.Packaging.Main.Views;
using QT.Packaging.Base;
using QT.Packaging.Base.Services;
using System;

namespace QT.Packaging.Main;

public partial class App : Application
{
    private ModuleLogger? _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 初始化服务提供者
        InitializeServices();
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    private void InitializeServices()
    {
        try
        {
            // 初始化服务提供者
            var host = ServiceProvider.Initialize(options =>
            {
                options.MqttPort = 1883;
                options.EnableDebugLogging = true;
                options.LogRetentionDays = 30;
            });

            // 启动后台服务
            _ = Task.Run(async () =>
            {
                await ServiceProvider.StartAsync();
            });

            // 获取日志服务
            _logger = ServiceProvider.LogService.CreateModule("MainApp");
            _logger.LogInfo("应用程序服务初始化完成");

            // 检查许可证
            var licenseService = ServiceProvider.LicenseService;
            if (!licenseService.CheckLicense())
            {
                _logger.LogWarning("许可证验证失败");
                // 这里可以显示许可证验证界面
            }

            // 获取平台信息
            var platformService = ServiceProvider.PlatformService;
            _logger.LogInfo($"平台支持状态: Windows={platformService.IsWindowsSupported}, Android={platformService.IsAndroidSupported}");
        }
        catch (Exception ex)
        {
            // 如果服务初始化失败，使用控制台输出错误
            Console.WriteLine($"服务初始化失败: {ex.Message}");
            throw;
        }
    }



    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
                
                // 应用程序退出时清理服务
                desktop.Exit += OnApplicationExit;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }

            _logger?.LogInfo("应用程序框架初始化完成");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"应用程序框架初始化失败: {ex.Message}");
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 应用程序退出事件处理
    /// </summary>
    private async void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            _logger?.LogInfo("应用程序正在退出，清理服务...");
            await ServiceProvider.StopAsync();
            _logger?.LogInfo("服务清理完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"服务清理失败: {ex.Message}");
        }
    }
}
