using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class ScreenshotsViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    private ObservableCollection<ScreenshotItem> _screenshots = new();
    public ObservableCollection<ScreenshotItem> Screenshots { get => _screenshots; set => SetProperty(ref _screenshots, value); }

    private ScreenshotItem? _selectedScreenshot;
    public ScreenshotItem? SelectedScreenshot
    {
        get => _selectedScreenshot;
        set
        {
            if (SetProperty(ref _selectedScreenshot, value))
            {
                OnPropertyChanged(nameof(IsScreenshotSelected));
                App.Screenshots.ViewerSelectedItem = value;
            }
        }
    }

    public bool IsScreenshotSelected => SelectedScreenshot != null;
    public bool HasNoScreenshots => Screenshots.Count == 0;

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public ScreenshotsViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        RefreshCommand = new RelayCommand(LoadScreenshots);
        DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedScreenshot != null);

        LoadScreenshots();
    }

    public async void LoadScreenshots()
    {
        IsLoading = true;
        try
        {
            var instances = App.Instances.AllInstances;
            await App.Screenshots.LoadScreenshots(instances, App.AppData.InstancesDirectory.LocalPath);

            Screenshots.Clear();
            foreach (var s in App.Screenshots.Screenshots)
            {
                Screenshots.Add(s);
            }
            OnPropertyChanged(nameof(HasNoScreenshots));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screenshots] Failed to load screenshots: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DeleteSelected()
    {
        if (SelectedScreenshot == null) return;

        App.Screenshots.DeleteScreenshot(SelectedScreenshot);
        Screenshots.Remove(SelectedScreenshot);
        SelectedScreenshot = null;
        OnPropertyChanged(nameof(HasNoScreenshots));
    }
}
