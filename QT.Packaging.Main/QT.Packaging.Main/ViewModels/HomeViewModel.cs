using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QT.Packaging.Main.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private int lineOutput;

    [ObservableProperty]
    private int robotPickCount;

    [ObservableProperty]
    private int printCount;

    [ObservableProperty]
    private string sealParametersSummary = "阈值: 0.85, 曝光: 12ms, ROI: (120,80,320,220)";

    // 左侧配置补充绑定
    [ObservableProperty]
    private int lineTargetPerHour = 1200;

    [ObservableProperty]
    private string robotGripperType = "两指夹爪";

    [ObservableProperty]
    private string printerMode = "高分辨率";

    [ObservableProperty]
    private string lastPrintJob = "批次#A1023";

    [ObservableProperty]
    private double sealThreshold = 0.85;

    [ObservableProperty]
    private int exposureMs = 12;

    [ObservableProperty]
    private string roiDisplay = "(120,80,320,220)";

    // 封口检测右侧统计
    [ObservableProperty]
    private int sealInspectedCount;

    [ObservableProperty]
    private int sealPassCount;

    [ObservableProperty]
    private int sealFailCount;

    [ObservableProperty]
    private double speedFactor = 1.0;

    // 扫码输入框内容（每次提交后清空）
    [ObservableProperty]
    private string scanInput = string.Empty;

    public HomeViewModel()
    {
        // 初始化模拟数据（后续可接入实时数据）
        _ = SimulateDataAsync();
    }

    private async Task SimulateDataAsync()
    {
        // 简单的模拟增长（演示用，可移除）
        var rnd = new Random();
        while (false)
        {
            await Task.Delay(2000);
            LineOutput += rnd.Next(1, 8);
            RobotPickCount += rnd.Next(0, 5);
            PrintCount += rnd.Next(0, 6);
        }
    }

    [RelayCommand]
    private void ApplyFlexAdjust()
    {
        // 应用柔性调整（占位，后续可写入服务/下发到设备）
    }

    [RelayCommand]
    private void SubmitScan()
    {
        // 处理扫码内容（占位：可解析命令/参数）
        var content = ScanInput?.Trim();
        if (!string.IsNullOrEmpty(content))
        {
            // TODO: 将扫码内容路由到具体处理逻辑
        }
        // 提交后清空，方便无键盘连续扫码
        ScanInput = string.Empty;
    }

    [RelayCommand]
    private void LoadConfig()
    {
        // TODO: 从持久化存储加载配置并更新绑定属性
    }

    [RelayCommand]
    private void SaveConfig()
    {
        // TODO: 将当前参数保存到持久化存储
    }
}


