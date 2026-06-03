using Microsoft.UI.Xaml.Controls;

namespace OnyxWindows.Views.Skins;

public partial class SkinBrowserPanel : UserControl
{
    public SkinBrowserPanel()
    {
        this.InitializeComponent();

        // Bind directly to MainViewModel's Skins viewmodel
        this.DataContext = App.MainVM.Skins;
    }
}
