using Avalonia.Controls;
using Avalonia.Interactivity;
using SukiUI.Controls;
using SukiUI.Models;
using Material.Icons;
using Material.Icons.Avalonia;
using SukiUI;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia;
using Avalonia.VisualTree;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media; 
using QT.Packaging.Base;
using SukiUI.Dialogs;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Input;

namespace QT.Packaging.Main.Views;

public partial class MainView : UserControl
{
    private bool _isDarkTheme = false;
    private readonly Stack<Control?> _navigationStack = new();

    public MainView()
    {
        InitializeComponent();
        
        // 设置初始主题图标
        UpdateThemeIcon();
        
        // 3D样式现在通过XAML样式类应用，主题切换时需要更新样式类
        UpdateButtonThemeClasses();

        // Android 平台：订阅主题变化并同步更新根视图背景
        if (OperatingSystem.IsAndroid())
        {
            AttachedToVisualTree += OnAttachedToVisualTree;
            DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        // 初始构建一次面包屑（仅显示“主页”）
        RebuildBreadcrumbs();
    }

    private void NavigateTo(Control? view)
    {
        if (MainContentHost == null) return;

        // 将当前内容入栈
        var current = MainContentHost.Content as Control;
        if (current != null)
        {
            _navigationStack.Push(current);
        }
        MainContentHost.Content = view;
        RebuildBreadcrumbs();
    }

    private void GoBack()
    {
        if (MainContentHost == null) return;
        if (_navigationStack.Count == 0)
        {
            MainContentHost.Content = null;
            RebuildBreadcrumbs();
            return;
        }
        var previous = _navigationStack.Pop();
        MainContentHost.Content = previous;
        RebuildBreadcrumbs();
    }

    private void ExitButton_Click(object? sender, RoutedEventArgs e)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (lifetime != null)
        {
            lifetime.Shutdown();
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window window)
        {
            window.Close();
            return;
        }

        Environment.Exit(0);
    }

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (MainContentHost == null) return;

        if (MainContentHost.Content is ModulesView)
        {
            GoBack();
            return;
        }

        NavigateTo(new ModulesView());
    }

    private void RebuildBreadcrumbs()
    {
        if (BreadcrumbBar == null) return;
        BreadcrumbBar.Children.Clear();

        // 构建一个列表：根(首页) + 栈中的页面 + 当前页面
        var items = new List<(string title, Control? view)>();
        items.Add(("主页", null));

        // 注意：栈是后进先出，我们需要从底到顶显示
        var stackArray = _navigationStack.ToArray();
        for (int i = stackArray.Length - 1; i >= 0; i--)
        {
            var v = stackArray[i];
            if (v != null)
            {
                items.Add((GetViewTitle(v), v));
            }
        }

        // 当前视图
        var current = MainContentHost?.Content as Control;
        if (current != null)
        {
            items.Add((GetViewTitle(current), current));
        }

        for (int i = 0; i < items.Count; i++)
        {
            var (title, view) = items[i];

            var crumb = new TextBlock
            {
                Text = title,
                Margin = new Thickness(0, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeight.SemiBold
            };
            crumb.Cursor = new Cursor(StandardCursorType.Hand);

            // hover 效果：加下划线 + 变换为强调色
            // 根据主题设置默认颜色和强调色
            var normalBrush = _isDarkTheme 
                ? new SolidColorBrush(Colors.White) 
                : new SolidColorBrush(Colors.Black);
            var accentBrush = new SolidColorBrush(Colors.DeepSkyBlue);
            
            // 设置初始前景色
            crumb.Foreground = normalBrush;
            crumb.PointerEntered += (_, __) =>
            {
                crumb.TextDecorations = TextDecorations.Underline;
                crumb.Foreground = accentBrush;
            };
            crumb.PointerExited += (_, __) =>
            {
                crumb.TextDecorations = null;
                crumb.Foreground = normalBrush;
            };

            // 点击导航
            crumb.PointerReleased += (_, __) =>
            {
                if (view == null)
                {
                    _navigationStack.Clear();
                    MainContentHost.Content = null;
                }
                else
                {
                    while (_navigationStack.Count > 0 && !ReferenceEquals(_navigationStack.Peek(), view))
                    {
                        _navigationStack.Pop();
                    }
                    MainContentHost.Content = view;
                }
                RebuildBreadcrumbs();
            };

            BreadcrumbBar.Children.Add(crumb);

            if (i < items.Count - 1)
            {
                var sep = new TextBlock 
                { 
                    Text = ">", 
                    Margin = new Thickness(8, 0, 8, 0), 
                    VerticalAlignment = VerticalAlignment.Center, 
                    FontSize = 18, 
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _isDarkTheme 
                        ? new SolidColorBrush(Colors.White) 
                        : new SolidColorBrush(Colors.Black)
                };
                BreadcrumbBar.Children.Add(sep);
            }
        }
    }

    private string GetViewTitle(Control? view)
    {
        if (view is ModulesView) return "功能模块";
        return view?.GetType().Name ?? "主页";
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Flyout is FlyoutBase flyout)
        {
            flyout.ShowAt(button);
        }
    }

    /// <summary>
    /// 主题切换按钮点击事件
    /// </summary>
    private void ThemeToggleButton_Click(object? sender, RoutedEventArgs e)
    {
        // 切换主题状态
        _isDarkTheme = !_isDarkTheme;
        
        // 更新图标和背景
        UpdateThemeIcon();
        
        // 更新按钮主题样式类
        UpdateButtonThemeClasses();
        
        // 更新面包屑主题色
        RebuildBreadcrumbs();
        
        // 切换 SukiUI 主题
        ToggleSukiTheme();

        // Android 平台：立即同步根视图背景
        if (OperatingSystem.IsAndroid())
        {
            UpdateAndroidRootBackground();
        }
    }

    private void UpdateButtonThemeClasses()
    {
        var darkThemeClass = "DarkTheme";
        
        // 根据当前主题状态添加或移除 DarkTheme 样式类
        if (_isDarkTheme)
        {
            ThemeToggleButton?.Classes.Add(darkThemeClass);
            SettingsButton?.Classes.Add(darkThemeClass);
            MenuButton?.Classes.Add(darkThemeClass);
            // 退出按钮已经是危险色，不需要暗黑主题类
        }
        else
        {
            ThemeToggleButton?.Classes.Remove(darkThemeClass);
            SettingsButton?.Classes.Remove(darkThemeClass);
            MenuButton?.Classes.Remove(darkThemeClass);
        }
    }

    /// <summary>
    /// 更新主题图标和颜色
    /// </summary>
    private void UpdateThemeIcon()
    {
        if (ThemeIcon != null)
        {
            if (_isDarkTheme)
            {
                // 暗黑主题：暗黑模式图标，白色（与暗黑主题文本色一致）
                ThemeIcon.Kind = MaterialIconKind.ThemeLightDark;
                ThemeIcon.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                // 亮色主题：亮色模式图标，黑色（与亮色主题文本色一致）
                ThemeIcon.Kind = MaterialIconKind.ThemeLightDark;
                ThemeIcon.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }

    private void AboutAuthor_Click(object? sender, RoutedEventArgs e)
    {
        var aboutContent = "桥头产线管理系统\n\n开发者信息：\n• 主要开发者：桥头科技团队\n• 联系邮箱：634807923@qq.com\n• 技术支持：18996478118\n\n特别感谢：\n• Avalonia UI 团队\n• SukiUI 开源项目\n• .NET 社区贡献者";
        ShowInfoDialog("关于作者", aboutContent);
    }

    private void CopyrightInfo_Click(object? sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        var copyrightContent = $"桥头产线管理系统 v{version}\n\n版权声明：\n© 2025 桥德星科技有限公司。保留所有权利。\n\n软件许可：\n本软件受版权法和国际条约保护。未经授权不得复制、分发或修改本软件。\n\n开源组件许可：\n• Avalonia UI - MIT License\n• SukiUI - MIT License\n• MQTTnet - MIT License\n• .NET Runtime - MIT License\n\n免责声明：\n本软件按\"现状\"提供，不提供任何明示或暗示的担保。\n在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责。\n\n技术支持：\n如需技术支持，请联系：634807923@qq.com";
        ShowInfoDialog("版权信息", copyrightContent);
    }

    private void SystemInfo_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mqttService = ServiceProvider.MqttHostService;
            var systemContent = $"系统信息\n\n• 应用名称：桥头产线管理系统\n• 版本号：{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0"}\n• MQTT Broker：{mqttService.CurrentIpAddress ?? "未启动"}:{mqttService.Port}";
            ShowInfoDialog("系统信息", systemContent);
        }
        catch (Exception ex)
        {
            ShowInfoDialog("系统信息", $"获取系统信息时发生错误：\n{ex.Message}");
        }
    }

    private void ShowInfoDialog(string title, string content)
    {
        try
        {
            var textBlock = new TextBlock
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = "Consolas,Monaco,monospace",
                FontSize = 12,
                LineHeight = 18,
                Margin = new Thickness(20)
            };

            MainWindow.DialogManager.CreateDialog()
                .WithTitle(title)
                .WithContent(textBlock)
                .WithActionButton("确定", _ => { }, true, "Flat", "Accent")
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        catch (Exception)
        {
            // 忽略对话框错误
        }
    }

    /// <summary>
    /// 切换 SukiUI 主题
    /// </summary>
    private void ToggleSukiTheme()
    {
        try
        {
            var app = Avalonia.Application.Current;
            if (app != null)
            {
                if (_isDarkTheme)
                {
                    // 切换到暗色主题
                    SukiTheme.GetInstance().ChangeBaseTheme(ThemeVariant.Dark);
                }
                else
                {
                    // 切换到亮色主题
                    SukiTheme.GetInstance().ChangeBaseTheme(ThemeVariant.Light);
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换主题时出错: {ex.Message}");
        }
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // 初始同步一次背景
        UpdateAndroidRootBackground();

        // 订阅主题变化
        this.PropertyChanged += OnSelfPropertyChanged;
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        this.PropertyChanged -= OnSelfPropertyChanged;
    }

    private void OnSelfPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ThemeVariantScope.ActualThemeVariantProperty)
        {
            if (OperatingSystem.IsAndroid())
            {
                UpdateAndroidRootBackground();
            }
        }
    }

    /// <summary>
    /// Android 平台：根据当前主题设置根视图背景色
    /// </summary>
    private void UpdateAndroidRootBackground()
    {
        if (!OperatingSystem.IsAndroid())
        {
            return;
        }

        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        Background = isDark
            ? new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x12))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    }

    // 3D 按钮样式已移至 IconButtonStyles.axaml，代码大幅简化
}
