using Microsoft.UI.Xaml.Controls;

namespace OnyxWindows.Views.Instances;

public partial class InstanceGridView : UserControl
{
    public InstanceGridView()
    {
        this.InitializeComponent();

        // Bind directly to MainViewModel's InstanceGrid viewmodel
        this.DataContext = App.MainVM.InstanceGrid;
    }
}
