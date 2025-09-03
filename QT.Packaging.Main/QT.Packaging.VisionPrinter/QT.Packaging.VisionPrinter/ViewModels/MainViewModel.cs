using CommunityToolkit.Mvvm.ComponentModel;

namespace QT.Packaging.VisionPrinter.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";
    }
}
