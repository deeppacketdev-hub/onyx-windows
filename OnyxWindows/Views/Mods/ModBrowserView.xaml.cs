using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnyxWindows.ViewModels;
using OnyxWindows.Services;

namespace OnyxWindows.Views.Mods;

public partial class ModBrowserView : UserControl
{
    public ModBrowserView()
    {
        this.InitializeComponent();
    }

    private void Project_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModrinthProject project)
        {
            if (DataContext is ModBrowserViewModel vm)
            {
                vm.SelectedProject = project;
            }
        }
    }
}
