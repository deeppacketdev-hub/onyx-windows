using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnyxWindows.Models;
using OnyxWindows.ViewModels;

namespace OnyxWindows.Views.Instances;

public partial class CreateInstanceDialog : ContentDialog
{
    private readonly CreateInstanceViewModel _vm;

    public CreateInstanceDialog()
    {
        this.InitializeComponent();
        _vm = new CreateInstanceViewModel();
        this.DataContext = _vm;
    }

    private void Loader_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb)
        {
            if (rb == LoaderNone) _vm.SelectedLoader = ModLoaderType.None;
            else if (rb == LoaderFabric) _vm.SelectedLoader = ModLoaderType.Fabric;
            else if (rb == LoaderQuilt) _vm.SelectedLoader = ModLoaderType.Quilt;
            else if (rb == LoaderForge) _vm.SelectedLoader = ModLoaderType.Forge;
            else if (rb == LoaderNeoforge) _vm.SelectedLoader = ModLoaderType.NeoForge;
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Set the name from the textbox into the ViewModel
        _vm.InstanceName = InstanceNameBox.Text;

        if (string.IsNullOrWhiteSpace(_vm.InstanceName) || string.IsNullOrEmpty(_vm.SelectedMinecraftVersion))
        {
            args.Cancel = true; // Prevent close if validation fails
            return;
        }

        // Execute create
        _vm.CreateCommand.Execute(null);
    }
}
