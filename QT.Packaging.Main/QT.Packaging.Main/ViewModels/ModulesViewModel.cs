using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using QT.Packaging.Base;
using QT.Packaging.Base.PackagingDbContext;
using SukiUI.Dialogs;

namespace QT.Packaging.Main.ViewModels;

public partial class ModulesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ModuleItemViewModel> modules = new();

    public ISukiDialogManager DialogManager { get; } = new SukiDialogManager();

    public ModulesViewModel()
    {
        _ = LoadModulesAsync();
    }

    private async Task LoadModulesAsync()
    {
        try
        {
            var moduleService = ServiceProvider.ModuleService;
            var moduleConfigs = await moduleService.GetModulesAsync();
            
            Modules.Clear();
            foreach (var config in moduleConfigs)
            {
                var moduleItem = CreateModuleItem(config);
                if (moduleItem != null)
                {
                    Modules.Add(moduleItem);
                }
            }
        }
        catch (Exception)
        {
            // 静默处理加载失败
        }
    }

    private ModuleItemViewModel? CreateModuleItem(ModuleConfig moduleConfig)
    {
        try
        {
            var moduleIcon = GetModuleIcon(moduleConfig.Name);
            var isAssemblyAvailable = CheckAssemblyAvailability(moduleConfig.Assembly);
            
            return new ModuleItemViewModel
            {
                Name = moduleConfig.Name,
                Icon = moduleIcon,
                Description = moduleConfig.Name + "模块",
                ModuleConfig = moduleConfig,
                IsAssemblyAvailable = isAssemblyAvailable
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private bool CheckAssemblyAvailability(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
            return false;

        try
        {
            // 检查构建输出目录中的程序集
            var baseDirectory = AppContext.BaseDirectory;
            var assemblyPath = Path.Combine(baseDirectory, assemblyName);
            
            return File.Exists(assemblyPath);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private MaterialIconKind GetModuleIcon(string moduleName)
    {
        return moduleName switch
        {
            var name when name.Contains("机器人", StringComparison.OrdinalIgnoreCase) => MaterialIconKind.Robot,
            var name when name.Contains("视觉打印", StringComparison.OrdinalIgnoreCase) => MaterialIconKind.Printer3d,
            var name when name.Contains("密封检测", StringComparison.OrdinalIgnoreCase) => MaterialIconKind.ShieldCheck,
            _ => MaterialIconKind.Puzzle
        };
    }

    [RelayCommand]
    private void OpenModule(ModuleItemViewModel moduleItem)
    {
        // TODO: 实现模块打开逻辑
    }

    [RelayCommand]
    private async Task RefreshModules()
    {
        await LoadModulesAsync();
    }

    [RelayCommand]
    private void ShowPurchaseDialog(ModuleItemViewModel moduleItem)
    {
        try
        {
            var dialogContent = $"模块 \"{moduleItem.Name}\" 需要购买许可证才能使用。\n\n" +
                              "请联系我们的销售团队获取更多信息：\n" +
                              "• 邮箱：qt@qiaotoutiaoliao.com\n" +
                              "• 官网：www.yjcabin.com\n\n" +
                              "我们将为您提供专业的解决方案和优惠的价格！";

            DialogManager.CreateDialog()
                .WithTitle("模块购买")
                .WithContent(dialogContent)
                .WithActionButton("确定", _ => { }, true)
                .WithActionButton("取消", _ => { }, true)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        catch (Exception)
        {
            // 静默处理对话框显示失败
        }
    }
}

public partial class ModuleItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private MaterialIconKind icon;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private ModuleConfig? moduleConfig;

    [ObservableProperty]
    private bool isAssemblyAvailable = true;
}