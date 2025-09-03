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

namespace QT.Packaging.Main.Views;

public partial class MainView : UserControl
{
    private bool _isDarkTheme = false;

    public MainView()
    {
        InitializeComponent();
        
        // 设置初始主题图标
        UpdateThemeIcon();
        
        // 设置按钮 hover 效果
        SetupThemeButtonHover();
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
        
        // 更新按钮背景以匹配新主题
        if (ThemeToggleButton != null)
        {
            var newBackground = Create3DBackground(false, false);
            ThemeToggleButton.Background = newBackground;
        }
        
        // 切换 SukiUI 主题
        ToggleSukiTheme();
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

    /// <summary>
    /// 设置主题按钮的 3D hover 效果
    /// </summary>
    private void SetupThemeButtonHover()
    {
        if (ThemeToggleButton != null)
        {
            // 创建默认的 3D 背景
            var defaultBackground = Create3DBackground(false, false);
            ThemeToggleButton.Background = defaultBackground;
            
            // 初始化时更新图标颜色
            UpdateThemeIcon();

            ThemeToggleButton.PointerEntered += (s, e) =>
            {
                // 悬停时的 3D 效果 - 更亮的渐变
                var hoverBackground = Create3DBackground(true, false);
                ThemeToggleButton.Background = hoverBackground;
                
                // 增强阴影效果
                var enhancedShadow = new DropShadowDirectionEffect
                {
                    BlurRadius = 4,
                    ShadowDepth = 3,
                    Direction = 315,
                    Color = Color.FromArgb(0x60, 0x00, 0x00, 0x00),
                    Opacity = 0.4
                };
                ThemeToggleButton.Effect = enhancedShadow;
            };

            ThemeToggleButton.PointerExited += (s, e) =>
            {
                // 恢复默认 3D 效果
                var defaultBackground = Create3DBackground(false, false);
                ThemeToggleButton.Background = defaultBackground;
                
                // 恢复默认阴影
                var defaultShadow = new DropShadowDirectionEffect
                {
                    BlurRadius = 3,
                    ShadowDepth = 2,
                    Direction = 315,
                    Color = Color.FromArgb(0x40, 0x00, 0x00, 0x00),
                    Opacity = 0.3
                };
                ThemeToggleButton.Effect = defaultShadow;
            };

            ThemeToggleButton.PointerPressed += (s, e) =>
            {
                // 按下时的 3D 效果 - 内陷效果
                var pressedBackground = Create3DBackground(false, true);
                ThemeToggleButton.Background = pressedBackground;
                
                // 减小阴影，模拟按下效果
                var pressedShadow = new DropShadowDirectionEffect
                {
                    BlurRadius = 1,
                    ShadowDepth = 1,
                    Direction = 135, // 反向阴影
                    Color = Color.FromArgb(0x30, 0x00, 0x00, 0x00),
                    Opacity = 0.5
                };
                ThemeToggleButton.Effect = pressedShadow;
            };

            ThemeToggleButton.PointerReleased += (s, e) =>
            {
                if (ThemeToggleButton.IsPointerOver)
                {
                    // 如果仍在悬停，恢复悬停效果
                    var hoverBackground = Create3DBackground(true, false);
                    ThemeToggleButton.Background = hoverBackground;
                    
                    var enhancedShadow = new DropShadowDirectionEffect
                    {
                        BlurRadius = 4,
                        ShadowDepth = 3,
                        Direction = 315,
                        Color = Color.FromArgb(0x60, 0x00, 0x00, 0x00),
                        Opacity = 0.4
                    };
                    ThemeToggleButton.Effect = enhancedShadow;
                }
                else
                {
                    // 恢复默认状态
                    var defaultBackground = Create3DBackground(false, false);
                    ThemeToggleButton.Background = defaultBackground;
                    
                    var defaultShadow = new DropShadowDirectionEffect
                    {
                        BlurRadius = 3,
                        ShadowDepth = 2,
                        Direction = 315,
                        Color = Color.FromArgb(0x40, 0x00, 0x00, 0x00),
                        Opacity = 0.3
                    };
                    ThemeToggleButton.Effect = defaultShadow;
                }
            };
        }
    }

    /// <summary>
    /// 创建 3D 背景效果
    /// </summary>
    private LinearGradientBrush Create3DBackground(bool isHover, bool isPressed)
    {
        if (isPressed)
        {
            // 按下状态 - 内陷效果（反向渐变）
            if (_isDarkTheme)
            {
                // 暗黑主题：深色背景，内陷效果（白色图标需要深色背景）
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Offset = 0, Color = Color.FromRgb(0x10, 0x10, 0x10) },
                        new GradientStop { Offset = 0.5, Color = Color.FromRgb(0x20, 0x20, 0x20) },
                        new GradientStop { Offset = 1, Color = Color.FromRgb(0x30, 0x30, 0x30) }
                    }
                };
            }
            else
            {
                // 亮色主题：浅色背景，内陷效果（黑色图标需要浅色背景）
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Offset = 0, Color = Color.FromRgb(0xE0, 0xE0, 0xE0) },
                        new GradientStop { Offset = 0.5, Color = Color.FromRgb(0xF0, 0xF0, 0xF0) },
                        new GradientStop { Offset = 1, Color = Color.FromRgb(0xFF, 0xFF, 0xFF) }
                    }
                };
            }
        }
        else if (isHover)
        {
            // 悬停状态 - 更亮的 3D 效果
            if (_isDarkTheme)
            {
                // 暗黑主题：更深的黑色背景（悬停时）
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Offset = 0, Color = Color.FromRgb(0x40, 0x40, 0x40) },
                        new GradientStop { Offset = 0.5, Color = Color.FromRgb(0x30, 0x30, 0x30) },
                        new GradientStop { Offset = 1, Color = Color.FromRgb(0x20, 0x20, 0x20) }
                    }
                };
            }
            else
            {
                // 亮色主题：更亮的白色背景（悬停时）
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Offset = 0, Color = Color.FromRgb(0xFF, 0xFF, 0xFF) },
                        new GradientStop { Offset = 0.5, Color = Color.FromRgb(0xF8, 0xF8, 0xFF) },
                        new GradientStop { Offset = 1, Color = Color.FromRgb(0xF0, 0xF0, 0xF8) }
                    }
                };
            }
        }
        else
        {
            // 默认状态 - 标准 3D 效果
            if (_isDarkTheme)
            {
                // 暗黑主题：深色背景（默认状态）
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Offset = 0, Color = Color.FromRgb(0x20, 0x20, 0x20) },
                        new GradientStop { Offset = 0.5, Color = Color.FromRgb(0x18, 0x18, 0x18) },
                        new GradientStop { Offset = 1, Color = Color.FromRgb(0x10, 0x10, 0x10) }
                    }
                };
            }
            else
            {
                // 亮色主题：浅色背景（默认状态）
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Offset = 0, Color = Color.FromRgb(0xFF, 0xFF, 0xFF) },
                        new GradientStop { Offset = 0.5, Color = Color.FromRgb(0xF0, 0xF0, 0xF0) },
                        new GradientStop { Offset = 1, Color = Color.FromRgb(0xE0, 0xE0, 0xE0) }
                    }
                };
            }
        }
    }
}
