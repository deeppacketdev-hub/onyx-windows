using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class ModBrowserViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = SearchMods();
            }
        }
    }

    private bool _isSearching;
    public bool IsSearching { get => _isSearching; set => SetProperty(ref _isSearching, value); }

    public ObservableCollection<ModrinthProject> Projects { get; } = new();

    private ModrinthProject? _selectedProject;
    public ModrinthProject? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                OnPropertyChanged(nameof(IsProjectSelected));
                _ = LoadProjectDetails();
            }
        }
    }

    public bool IsProjectSelected => SelectedProject != null;

    public ObservableCollection<ModrinthVersion> ProjectVersions { get; } = new();

    private bool _isLoadingVersions;
    public bool IsLoadingVersions { get => _isLoadingVersions; set => SetProperty(ref _isLoadingVersions, value); }

    public RelayCommand InstallCommand { get; }

    public ModBrowserViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;
        InstallCommand = new RelayCommand(InstallSelected, () => SelectedProject != null);
    }

    public async Task SearchMods()
    {
        IsSearching = true;
        Projects.Clear();

        try
        {
            var results = await App.Modrinth.Search(SearchQuery);
            foreach (var proj in results.Hits)
            {
                Projects.Add(proj);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Modrinth] Search failed: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task LoadProjectDetails()
    {
        ProjectVersions.Clear();
        if (SelectedProject == null) return;

        IsLoadingVersions = true;
        try
        {
            var versions = await App.Modrinth.GetVersions(SelectedProject.ProjectId);
            foreach (var ver in versions)
            {
                ProjectVersions.Add(ver);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Modrinth] Failed to load versions: {ex.Message}");
        }
        finally
        {
            IsLoadingVersions = false;
        }
    }

    private async void InstallSelected()
    {
        if (SelectedProject == null || _mainVM.InstanceGrid.SelectedInstance == null) return;

        var instance = _mainVM.InstanceGrid.SelectedInstance;
        var destinationDir = Path.Combine(App.AppData.InstancesDirectory, instance.DirectoryName, ".minecraft", "mods");

        try
        {
            // Simple install: download the first compatible file from the latest version
            if (ProjectVersions.Count > 0)
            {
                var latestVersion = ProjectVersions[0];
                if (latestVersion.Files.Count > 0)
                {
                    var file = latestVersion.Files[0];
                    await App.Modrinth.DownloadFile(file, destinationDir);
                    Console.WriteLine($"[Modrinth] Installed {SelectedProject.Title} successfully!");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Modrinth] Installation failed: {ex.Message}");
        }
    }
}
