using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OnyxWindows.ViewModels;
using OnyxWindows.Services;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace OnyxWindows.Views.Screenshots;

public partial class ScreenshotsGalleryView : UserControl
{
    public ScreenshotsGalleryView()
    {
        this.InitializeComponent();
        
        // Listen to changes on Selection to trigger Lightbox
        this.RegisterPropertyChangedCallback(DataContextProperty, (s, p) =>
        {
            if (DataContext is ScreenshotsViewModel vm)
            {
                vm.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(ScreenshotsViewModel.SelectedScreenshot))
                    {
                        if (vm.SelectedScreenshot != null)
                        {
                            LightboxOverlay.Visibility = Visibility.Visible;
                        }
                    }
                };
            }
        });
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ScreenshotItem item)
        {
            if (DataContext is ScreenshotsViewModel vm)
            {
                vm.SelectedScreenshot = item;
                vm.DeleteCommand.Execute(null);
            }
        }
    }

    private void CloseLightbox_Click(object sender, RoutedEventArgs e)
    {
        LightboxOverlay.Visibility = Visibility.Collapsed;
        if (DataContext is ScreenshotsViewModel vm)
        {
            vm.SelectedScreenshot = null;
        }
    }

    private void CloseLightbox_Click(object sender, PointerRoutedEventArgs e)
    {
        LightboxOverlay.Visibility = Visibility.Collapsed;
        if (DataContext is ScreenshotsViewModel vm)
        {
            vm.SelectedScreenshot = null;
        }
    }

    private async void CopyClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScreenshotsViewModel vm && vm.SelectedScreenshot != null)
        {
            try
            {
                var package = new DataPackage();
                var file = await StorageFile.GetFileFromPathAsync(vm.SelectedScreenshot.FilePath);
                package.SetStorageItems(new List<IStorageItem> { file });
                Clipboard.SetContent(package);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Clipboard] Failed to copy screenshot: {ex}");
            }
        }
    }
}
