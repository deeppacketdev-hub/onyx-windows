using Microsoft.UI.Xaml.Controls;
using OnyxWindows.ViewModels;

namespace OnyxWindows.Views;

public partial class ContentPage : UserControl
{
    public ContentPage()
    {
        this.InitializeComponent();

        // Wire Main Root ViewModel
        this.DataContext = App.MainVM;
    }

    private void Sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is ListViewItem item)
        {
            // Clear selection of other list view to prevent dual active indicators
            if (listView == MainNavigationList)
            {
                SettingsNavigationList.SelectedItem = null;
            }
            else if (listView == SettingsNavigationList)
            {
                MainNavigationList.SelectedItem = null;
            }

            if (item == InstancesItem) App.MainVM.NavigateTo(ActivePage.Instances);
            else if (item == ModsItem) App.MainVM.NavigateTo(ActivePage.Mods);
            else if (item == SkinsItem) App.MainVM.NavigateTo(ActivePage.Skins);
            else if (item == ScreenshotsItem) App.MainVM.NavigateTo(ActivePage.Screenshots);
            else if (item == WorldsItem) App.MainVM.NavigateTo(ActivePage.Worlds);
            else if (item == NewsItem) App.MainVM.NavigateTo(ActivePage.News);
            else if (item == SettingsItem) App.MainVM.NavigateTo(ActivePage.Settings);
        }
    }
}
