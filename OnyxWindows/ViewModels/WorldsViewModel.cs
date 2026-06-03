using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class WorldsViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    private ObservableCollection<WorldItem> _worlds = new();
    public ObservableCollection<WorldItem> Worlds { get => _worlds; set => SetProperty(ref _worlds, value); }

    private WorldItem? _selectedWorld;
    public WorldItem? SelectedWorld
    {
        get => _selectedWorld;
        set
        {
            if (SetProperty(ref _selectedWorld, value))
            {
                OnPropertyChanged(nameof(IsWorldSelected));
            }
        }
    }

    public bool IsWorldSelected => SelectedWorld != null;
    public bool HasNoWorlds => Worlds.Count == 0;

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public WorldsViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        RefreshCommand = new RelayCommand(LoadWorlds);
        DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedWorld != null);

        LoadWorlds();
    }

    public async void LoadWorlds()
    {
        IsLoading = true;
        try
        {
            var instances = App.Instances.AllInstances;
            await App.Worlds.LoadWorlds(instances, App.AppData.InstancesDirectory.LocalPath);

            Worlds.Clear();
            foreach (var w in App.Worlds.Worlds)
            {
                Worlds.Add(w);
            }
            OnPropertyChanged(nameof(HasNoWorlds));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Worlds] Failed to load worlds: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DeleteSelected()
    {
        if (SelectedWorld == null) return;

        App.Worlds.DeleteWorld(SelectedWorld);
        Worlds.Remove(SelectedWorld);
        SelectedWorld = null;
        OnPropertyChanged(nameof(HasNoWorlds));
    }
}
