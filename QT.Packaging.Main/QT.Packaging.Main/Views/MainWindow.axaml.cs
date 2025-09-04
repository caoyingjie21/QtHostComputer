using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using QT.Packaging.Base;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Models;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QT.Packaging.Main.Views;

public partial class MainWindow : SukiWindow
{
    public static ISukiDialogManager DialogManager = new SukiDialogManager();

    public MainWindow()
    {
        InitializeComponent();
        
        // 设置对话框管理器
        DialogHost.Manager = DialogManager;
        
        // 窗口加载完成后设置为覆盖任务栏的最大化
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetWindowToOverrideTaskbar();
        
        // 顶部设置按钮已移除，悬停效果不再需要
    }

    // 右上角设置按钮 hover 逻辑已删除

    private void SetWindowToOverrideTaskbar()
    {
        try
        {
            // 使用跨平台的方法设置全屏
            SetCrossPlatformFullscreen();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置窗口全屏失败: {ex.Message}");
            // fallback 到标准最大化
            WindowState = WindowState.Maximized;
        }
    }

    private void SetCrossPlatformFullscreen()
    {
        // 获取主屏幕信息
        var screen = Screens.Primary;
        if (screen != null)
        {
            var bounds = screen.Bounds;
            
            // 设置窗口为正常状态，然后手动设置位置和大小
            WindowState = WindowState.Normal;
            
            // 等待窗口完全加载后再设置尺寸
            Dispatcher.UIThread.Post(() =>
            {
                // 设置窗口位置和大小以覆盖整个屏幕
                Position = new PixelPoint(bounds.X, bounds.Y);
                Width = bounds.Width;
                Height = bounds.Height;
                
                // 设置窗口属性以确保覆盖任务栏
                CanResize = false; // 禁止调整大小
                
                Console.WriteLine($"窗口设置为全屏: {bounds.Width}x{bounds.Height} at ({bounds.X}, {bounds.Y})");
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        // 显示设置菜单（由 Flyout 自动处理）
    }

    /// <summary>
    /// 关于作者菜单项点击事件
    /// </summary>
    private void AboutAuthor_Click(object? sender, RoutedEventArgs e)
    {
        var aboutContent = $@"桥头产线管理系统

开发者信息：
• 主要开发者：桥头科技团队
• 联系邮箱：634807923@qq.com
• 技术支持：18996478118

特别感谢：
• Avalonia UI 团队
• SukiUI 开源项目
• .NET 社区贡献者";

        ShowInfoDialog("关于作者", aboutContent);
    }

    /// <summary>
    /// 版权信息菜单项点击事件
    /// </summary>
    private void CopyrightInfo_Click(object? sender, RoutedEventArgs e)
    {
        var copyrightContent = $@"桥头产线管理系统 v{GetApplicationVersion()}

版权声明：
© 2025 桥德星科技有限公司。保留所有权利。

软件许可：
本软件受版权法和国际条约保护。未经授权不得复制、分发或修改本软件。

开源组件许可：
• Avalonia UI - MIT License
• SukiUI - MIT License  
• MQTTnet - MIT License
• .NET Runtime - MIT License

免责声明：
本软件按""现状""提供，不提供任何明示或暗示的担保。
在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责。

技术支持：
如需技术支持，请联系：634807923@qq.com";

        ShowInfoDialog("版权信息", copyrightContent);
    }

    /// <summary>
    /// 系统信息菜单项点击事件
    /// </summary>
    private void SystemInfo_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mqttService = ServiceProvider.MqttHostService;

            var systemContent = $@"系统信息

• 应用名称：桥头产线管理系统
• 版本号：{GetApplicationVersion()}
• MQTT Broker：{mqttService.CurrentIpAddress ?? "未启动"}:{mqttService.Port}";

            ShowInfoDialog("系统信息", systemContent);
        }
        catch (Exception ex)
        {
            ShowInfoDialog("系统信息", $"获取系统信息时发生错误：\n{ex.Message}");
        }
    }

    /// <summary>
    /// 显示信息对话框（使用SukiUI对话框系统）
    /// </summary>
    private void ShowInfoDialog(string title, string content)
    {
        var textBlock = new TextBlock
        {
            Text = content,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontFamily = "Consolas,Monaco,monospace",
            FontSize = 12,
            LineHeight = 18,
            Margin = new Thickness(20)
        };

        DialogManager.CreateDialog()
            .WithTitle(title)
            .WithContent(textBlock)
            .WithActionButton("确定", _ => { }, true, "Flat", "Accent")
            .Dismiss().ByClickingBackground()
            .TryShow();
    }

    /// <summary>
    /// 获取应用程序版本
    /// </summary>
    private string GetApplicationVersion()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }

    /// <summary>
    /// 获取构建日期
    /// </summary>
    private string GetBuildDate()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileInfo = new System.IO.FileInfo(assembly.Location);
            return fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 检查是否为调试版本
    /// </summary>
    private bool IsDebugBuild()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
