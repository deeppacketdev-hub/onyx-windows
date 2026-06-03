using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnyxWindows.ViewModels;
using OnyxWindows.Services;

namespace OnyxWindows.Views.Worlds;

public partial class WorldsGalleryView : UserControl
{
    public WorldsGalleryView()
    {
        this.InitializeComponent();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is WorldItem world)
        {
            if (DataContext is WorldsViewModel vm)
            {
                vm.SelectedWorld = world;
                vm.DeleteCommand.Execute(null);
            }
        }
    }
}
