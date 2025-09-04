using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using QT.Packaging.Main.ViewModels;
using QT.Packaging.Main.Views;
using QT.Packaging.Main.Models;
using QT.Packaging.Base;
using QT.Packaging.Base.Services;
using QT.Packaging.Base.PackagingDbContext;
using System;
using System.Linq;
using System.Text.Json;
using System.IO;
using Avalonia.Media;
using SukiUI;
using SukiUI.Models;

namespace QT.Packaging.Main;

public partial class App : Application
{
    private ModuleLogger? _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 初始化服务提供者
        InitializeServices();

        // 注册并应用自定义主题色（Orange）
        ApplyCustomColorTheme();
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    private async void InitializeServices()
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

            // 初始化模块数据库
            await InitializeModuleDatabase();
        }
        catch (Exception ex)
        {
            // 如果服务初始化失败，使用控制台输出错误
            Console.WriteLine($"服务初始化失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 注册并应用自定义颜色主题
    /// </summary>
    private void ApplyCustomColorTheme()
    {
        try
        {
            // 定义“亮黑色”主题（偏黑的强调色）
            var primary = Color.FromRgb(0x2B, 0x2B, 0x2B);    // #2B2B2B 亮黑
            var darker  = Color.FromRgb(0x12, 0x12, 0x12);    // #121212 更深黑

            var blackTheme = new SukiColorTheme("Black", primary, darker);
            var themeMgr = SukiTheme.GetInstance();

            themeMgr.AddColorTheme(blackTheme);
            themeMgr.ChangeColorTheme(blackTheme);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"应用自定义颜色主题失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从 JSON 文件加载模块配置
    /// </summary>
    private async Task LoadModulesFromJson(ModuleService moduleService)
    {
        try
        {
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Modules.json");
            if (!File.Exists(jsonPath))
            {
                _logger?.LogWarning($"模块配置文件不存在: {jsonPath}");
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var moduleConfigs = JsonSerializer.Deserialize<ModuleConfigJson[]>(jsonContent);

            if (moduleConfigs == null || moduleConfigs.Length == 0)
            {
                _logger?.LogWarning("模块配置文件为空或格式错误");
                return;
            }

            foreach (var jsonConfig in moduleConfigs)
            {
                var moduleConfig = new ModuleConfig
                {
                    Id = Guid.TryParse(jsonConfig.Id, out var id) ? id : Guid.NewGuid(),
                    Name = jsonConfig.Name,
                    CreateTime = DateTime.Now,
                    IsPublished = jsonConfig.IsPublished,
                    Assembly = jsonConfig.Assembly,
                    ViewModelType = jsonConfig.ViewModelType,
                    ViewType = jsonConfig.ViewType
                };

                await moduleService.AddModuleAsync(moduleConfig);
                _logger?.LogInfo($"已添加模块配置: {moduleConfig.Name}");
            }

            _logger?.LogInfo($"成功加载 {moduleConfigs.Length} 个模块配置");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"加载模块配置文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化模块数据库
    /// </summary>
    private async Task InitializeModuleDatabase()
    {
        try
        {
            var moduleService = ServiceProvider.ModuleService;
            _logger?.LogInfo("模块数据库初始化完成");
            
            // 可以在这里添加默认模块数据
            var existingModules = await moduleService.GetModulesAsync();
            if (!existingModules.Any())
            {
                _logger?.LogInfo("正在添加默认模块配置...");
                await LoadModulesFromJson(moduleService);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"模块数据库初始化失败: {ex.Message}");
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
