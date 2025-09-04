using Avalonia.Controls;
using QT.Packaging.Main.ViewModels;

namespace QT.Packaging.Main.Views;

public partial class ModulesView : UserControl
{
    public ModulesView()
    {
        InitializeComponent();
        DataContext = new ModulesViewModel();
    }
}


