using Avalonia.Controls;

namespace QT.Packaging.Main.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        DataContext = new ViewModels.HomeViewModel();
    }
}


