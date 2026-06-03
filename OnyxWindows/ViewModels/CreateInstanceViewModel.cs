using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.ViewModels;

public class CreateInstanceViewModel : ObservableBase
{
    private string _instanceName = "";
    public string InstanceName { get => _instanceName; set => SetProperty(ref _instanceName, value); }

    private string _selectedMinecraftVersion = "";
    public string SelectedMinecraftVersion
    {
        get => _selectedMinecraftVersion;
        set
        {
            if (SetProperty(ref _selectedMinecraftVersion, value))
            {
                _ = LoadLoaderVersions();
            }
        }
    }

    private ModLoaderType _selectedLoader = ModLoaderType.None;
    public ModLoaderType SelectedLoader
    {
        get => _selectedLoader;
        set
        {
            if (SetProperty(ref _selectedLoader, value))
            {
                OnPropertyChanged(nameof(IsLoaderSelected));
                _ = LoadLoaderVersions();
            }
        }
    }

    private string? _selectedLoaderVersion;
    public string? SelectedLoaderVersion { get => _selectedLoaderVersion; set => SetProperty(ref _selectedLoaderVersion, value); }

    public ObservableCollection<string> MinecraftVersions { get; } = new();
    public ObservableCollection<string> LoaderVersions { get; } = new();

    private bool _isLoadingVersions;
    public bool IsLoadingVersions { get => _isLoadingVersions; set => SetProperty(ref _isLoadingVersions, value); }

    public bool IsLoaderSelected => SelectedLoader != ModLoaderType.None;

    public RelayCommand CreateCommand { get; }

    public CreateInstanceViewModel()
    {
        CreateCommand = new RelayCommand(CreateInstance, CanCreate);
        _ = LoadMinecraftVersions();
    }

    private async Task LoadMinecraftVersions()
    {
        IsLoadingVersions = true;
        MinecraftVersions.Clear();

        try
        {
            if (App.Manifest.Manifest == null)
            {
                await App.Manifest.LoadManifest(App.AppData.MetaDirectory);
            }

            if (App.Manifest.Manifest != null)
            {
                foreach (var ver in App.Manifest.Manifest.Versions)
                {
                    if (ver.Type == VersionType.Release)
                    {
                        MinecraftVersions.Add(ver.Id);
                    }
                }
            }

            if (MinecraftVersions.Count > 0)
            {
                SelectedMinecraftVersion = MinecraftVersions[0];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateInstance] Failed to load MC versions: {ex}");
        }
        finally
        {
            IsLoadingVersions = false;
        }
    }

    private async Task LoadLoaderVersions()
    {
        LoaderVersions.Clear();
        SelectedLoaderVersion = null;

        if (SelectedLoader == ModLoaderType.None || string.IsNullOrEmpty(SelectedMinecraftVersion))
            return;

        IsLoadingVersions = true;
        try
        {
            List<string> versions = new();
            switch (SelectedLoader)
            {
                case ModLoaderType.Fabric:
                    versions = await App.ModLoaders.GetFabricVersions(SelectedMinecraftVersion);
                    break;
                case ModLoaderType.Quilt:
                    versions = await App.ModLoaders.GetQuiltVersions(SelectedMinecraftVersion);
                    break;
                case ModLoaderType.Forge:
                    versions = await App.ModLoaders.GetForgeVersions(SelectedMinecraftVersion);
                    break;
                case ModLoaderType.NeoForge:
                    versions = await App.ModLoaders.GetNeoForgeVersions(SelectedMinecraftVersion);
                    break;
            }

            foreach (var ver in versions)
            {
                LoaderVersions.Add(ver);
            }

            if (LoaderVersions.Count > 0)
            {
                SelectedLoaderVersion = LoaderVersions[0];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateInstance] Failed to load loader versions: {ex}");
        }
        finally
        {
            IsLoadingVersions = false;
        }
    }

    private bool CanCreate()
    {
        if (string.IsNullOrWhiteSpace(InstanceName)) return false;
        if (string.IsNullOrEmpty(SelectedMinecraftVersion)) return false;
        if (SelectedLoader != ModLoaderType.None && string.IsNullOrEmpty(SelectedLoaderVersion)) return false;
        return true;
    }

    private void CreateInstance()
    {
        if (!CanCreate()) return;

        var instance = new Instance(InstanceName.Trim(), SelectedMinecraftVersion)
        {
            ModLoader = SelectedLoader,
            ModLoaderVersion = SelectedLoaderVersion,
            RamMB = App.AppData.Config.DefaultRamMB
        };

        App.Instances.AddInstance(instance);
        App.MainVM.InstanceGrid.RefreshInstances();
    }
}
