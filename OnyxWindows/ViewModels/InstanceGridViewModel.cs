using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.ViewModels;

public class InstanceGridViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    private ObservableCollection<Instance> _instances = new();
    public ObservableCollection<Instance> Instances { get => _instances; set => SetProperty(ref _instances, value); }

    private Instance? _selectedInstance;
    public Instance? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (SetProperty(ref _selectedInstance, value))
            {
                OnPropertyChanged(nameof(IsInstanceSelected));
            }
        }
    }

    public bool IsInstanceSelected => SelectedInstance != null;

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                FilterInstances();
            }
        }
    }

    public RelayCommand PlayCommand { get; }
    public RelayCommand CreateCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DuplicateCommand { get; }
    public RelayCommand OpenFolderCommand { get; }

    public InstanceGridViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        PlayCommand = new RelayCommand(LaunchSelected, () => SelectedInstance != null);
        CreateCommand = new RelayCommand(() => _mainVM.NavigateTo(ActivePage.Instances)); // will show dialog
        DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedInstance != null);
        EditCommand = new RelayCommand(EditSelected, () => SelectedInstance != null);
        DuplicateCommand = new RelayCommand(DuplicateSelected, () => SelectedInstance != null);
        OpenFolderCommand = new RelayCommand(OpenFolderSelected, () => SelectedInstance != null);

        RefreshInstances();
    }

    public void RefreshInstances()
    {
        Instances.Clear();
        foreach (var inst in App.Instances.AllInstances)
        {
            if (string.IsNullOrEmpty(SearchQuery) || inst.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                Instances.Add(inst);
            }
        }
    }

    private void FilterInstances()
    {
        RefreshInstances();
    }

    private async void LaunchSelected()
    {
        if (SelectedInstance == null) return;

        var activeAccount = App.AccountMgr.ActiveAccount;
        if (activeAccount == null)
        {
            // Direct to accounts tab to sign in
            _mainVM.NavigateTo(ActivePage.Skins); // Skins includes accounts
            return;
        }

        try
        {
            // Switch current active page to Console if configured
            if (App.AppData.Config.ShowConsoleOnLaunch)
            {
                _mainVM.NavigateTo(ActivePage.Skins); // Show console
            }

            await App.Launcher.Launch(SelectedInstance, activeAccount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Launch] Failed to launch instance: {ex.Message}");
        }
    }

    private void DeleteSelected()
    {
        if (SelectedInstance == null) return;
        App.Instances.RemoveInstance(SelectedInstance);
        RefreshInstances();
        SelectedInstance = null;
    }

    private void EditSelected()
    {
        if (SelectedInstance == null) return;
        // Navigation to per-instance settings page
        _mainVM.NavigateTo(ActivePage.Settings);
    }

    private void DuplicateSelected()
    {
        if (SelectedInstance == null) return;
        var clone = new Instance($"{SelectedInstance.Name} (Copy)", SelectedInstance.MinecraftVersion)
        {
            ModLoader = SelectedInstance.ModLoader,
            ModLoaderVersion = SelectedInstance.ModLoaderVersion,
            RamMB = SelectedInstance.RamMB,
            JvmArguments = new List<string>(SelectedInstance.JvmArguments),
            CustomIconPath = SelectedInstance.CustomIconPath
        };
        App.Instances.AddInstance(clone);
        RefreshInstances();
    }

    private void OpenFolderSelected()
    {
        if (SelectedInstance == null) return;
        var dir = Path.Combine(App.AppData.InstancesDirectory.LocalPath, SelectedInstance.DirectoryName);
        if (Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
    }
}
