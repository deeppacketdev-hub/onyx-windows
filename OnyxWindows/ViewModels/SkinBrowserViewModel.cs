using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Models;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class SkinBrowserViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    public ObservableCollection<SavedSkin> SavedSkins { get; } = new();
    public ObservableCollection<BrowsableSkin> BrowseResults { get; } = new();

    private SavedSkin? _selectedSkin;
    public SavedSkin? SelectedSkin
    {
        get => _selectedSkin;
        set
        {
            if (SetProperty(ref _selectedSkin, value))
            {
                OnPropertyChanged(nameof(IsSkinSelected));
            }
        }
    }

    public bool IsSkinSelected => SelectedSkin != null;

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = SearchSkins();
            }
        }
    }

    private bool _isSearching;
    public bool IsSearching { get => _isSearching; set => SetProperty(ref _isSearching, value); }

    public RelayCommand ApplyCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public SkinBrowserViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        ApplyCommand = new RelayCommand(ApplySelected, () => SelectedSkin != null);
        DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedSkin != null);
        RefreshCommand = new RelayCommand(LoadPopular);

        RefreshLibrary();
    }

    public void RefreshLibrary()
    {
        SavedSkins.Clear();
        foreach (var skin in App.Skins.SavedSkins)
        {
            SavedSkins.Add(skin);
        }
    }

    private async void LoadPopular()
    {
        IsSearching = true;
        BrowseResults.Clear();
        try
        {
            await App.Skins.BrowsePopular();
            foreach (var result in App.Skins.BrowseResults)
            {
                BrowseResults.Add(result);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinManager] Browse popular failed: {ex}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task SearchSkins()
    {
        IsSearching = true;
        BrowseResults.Clear();
        try
        {
            await App.Skins.SearchSkin(SearchQuery);
            foreach (var result in App.Skins.BrowseResults)
            {
                BrowseResults.Add(result);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinManager] Search failed: {ex}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    private void ApplySelected()
    {
        if (SelectedSkin == null) return;

        var activeAccount = App.AccountMgr.ActiveAccount;
        if (activeAccount != null)
        {
            App.Skins.SetActiveSkin(SelectedSkin);
            App.AccountMgr.UpdateAccountSkin(activeAccount, SelectedSkin.Filename);
        }
    }

    private void DeleteSelected()
    {
        if (SelectedSkin == null) return;
        App.Skins.DeleteSkin(SelectedSkin);
        RefreshLibrary();
        SelectedSkin = null;
    }
}
