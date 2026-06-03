using System;
using OnyxWindows.Helpers;

namespace OnyxWindows.ViewModels;

public enum ActivePage
{
    Instances,
    Mods,
    Skins,
    Screenshots,
    Worlds,
    News,
    Settings,
    Onboarding
}

public class MainViewModel : ObservableBase
{
    private ActivePage _currentPage = ActivePage.Instances;
    public ActivePage CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsInstancesPage));
                OnPropertyChanged(nameof(IsModsPage));
                OnPropertyChanged(nameof(IsSkinsPage));
                OnPropertyChanged(nameof(IsScreenshotsPage));
                OnPropertyChanged(nameof(IsWorldsPage));
                OnPropertyChanged(nameof(IsNewsPage));
                OnPropertyChanged(nameof(IsSettingsPage));
                OnPropertyChanged(nameof(IsOnboardingPage));
            }
        }
    }

    public bool IsInstancesPage => CurrentPage == ActivePage.Instances;
    public bool IsModsPage => CurrentPage == ActivePage.Mods;
    public bool IsSkinsPage => CurrentPage == ActivePage.Skins;
    public bool IsScreenshotsPage => CurrentPage == ActivePage.Screenshots;
    public bool IsWorldsPage => CurrentPage == ActivePage.Worlds;
    public bool IsNewsPage => CurrentPage == ActivePage.News;
    public bool IsSettingsPage => CurrentPage == ActivePage.Settings;
    public bool IsOnboardingPage => CurrentPage == ActivePage.Onboarding;

    // ── Child ViewModels ──
    public InstanceGridViewModel InstanceGrid { get; }
    public AccountViewModel Accounts { get; }
    public SettingsViewModel Settings { get; }
    public SkinBrowserViewModel Skins { get; }
    public ModBrowserViewModel Mods { get; }
    public ConsoleViewModel Console { get; }
    public WorldsViewModel Worlds { get; }
    public ScreenshotsViewModel Screenshots { get; }
    public NewsViewModel News { get; }

    public MainViewModel()
    {
        // Instantiate child viewmodels and pass this (parent context) if needed
        InstanceGrid = new InstanceGridViewModel(this);
        Accounts = new AccountViewModel(this);
        Settings = new SettingsViewModel(this);
        Skins = new SkinBrowserViewModel(this);
        Mods = new ModBrowserViewModel(this);
        Console = new ConsoleViewModel(this);
        Worlds = new WorldsViewModel(this);
        Screenshots = new ScreenshotsViewModel(this);
        News = new NewsViewModel(this);

        // Check if onboarding is completed, else direct to onboarding
        if (!App.AppData.Config.HasCompletedOnboarding)
        {
            CurrentPage = ActivePage.Onboarding;
        }
    }

    public void NavigateTo(ActivePage page)
    {
        CurrentPage = page;
    }
}
