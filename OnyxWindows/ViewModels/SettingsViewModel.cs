using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Models;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class SettingsViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    public ObservableCollection<AppLanguage> Languages { get; } = new()
    {
        AppLanguage.DE,
        AppLanguage.EN,
        AppLanguage.ES,
        AppLanguage.FR,
        AppLanguage.PL,
        AppLanguage.UK
    };

    public AppLanguage SelectedLanguage
    {
        get => App.AppData.Config.Language;
        set
        {
            if (App.AppData.Config.Language != value)
            {
                App.AppData.Config.Language = value;
                App.Loc.Language = value;
                App.AppData.SaveConfig();
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ThemeType> Themes { get; } = new()
    {
        ThemeType.System,
        ThemeType.Dark,
        ThemeType.Light,
        ThemeType.Custom
    };

    public ThemeType SelectedTheme
    {
        get => App.AppData.Config.Theme;
        set
        {
            if (App.AppData.Config.Theme != value)
            {
                App.Themes.CurrentTheme = value;
                OnPropertyChanged();
            }
        }
    }

    public int DefaultRamMB
    {
        get => App.AppData.Config.DefaultRamMB;
        set
        {
            if (App.AppData.Config.DefaultRamMB != value)
            {
                App.AppData.Config.DefaultRamMB = value;
                App.AppData.SaveConfig();
                OnPropertyChanged();
            }
        }
    }

    public bool ShowConsoleOnLaunch
    {
        get => App.AppData.Config.ShowConsoleOnLaunch;
        set
        {
            if (App.AppData.Config.ShowConsoleOnLaunch != value)
            {
                App.AppData.Config.ShowConsoleOnLaunch = value;
                App.AppData.SaveConfig();
                OnPropertyChanged();
            }
        }
    }

    public bool CloseLauncherOnLaunch
    {
        get => App.AppData.Config.CloseLauncherOnLaunch;
        set
        {
            if (App.AppData.Config.CloseLauncherOnLaunch != value)
            {
                App.AppData.Config.CloseLauncherOnLaunch = value;
                App.AppData.SaveConfig();
                OnPropertyChanged();
            }
        }
    }

    public bool EnableDiscordRpc
    {
        get => App.AppData.Config.EnableDiscordRPC;
        set
        {
            if (App.AppData.Config.EnableDiscordRPC != value)
            {
                App.AppData.Config.EnableDiscordRPC = value;
                App.AppData.SaveConfig();
                OnPropertyChanged();
            }
        }
    }

    public bool EnableTelemetry
    {
        get => App.AppData.Config.EnableTelemetry;
        set
        {
            if (App.AppData.Config.EnableTelemetry != value)
            {
                App.AppData.Config.EnableTelemetry = value;
                App.AppData.SaveConfig();
                App.Telemetry.Initialize(value);
                OnPropertyChanged();
            }
        }
    }

    public string DataDirectoryPath => App.AppData.BaseDirectory;

    public RelayCommand OpenDataFolderCommand { get; }
    public RelayCommand CheckUpdatesCommand { get; }

    public SettingsViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        CheckUpdatesCommand = new RelayCommand(CheckUpdates);
    }

    private void OpenDataFolder()
    {
        var dir = App.AppData.BaseDirectory;
        if (System.IO.Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
    }

    private async void CheckUpdates()
    {
        await App.Updater.CheckForUpdates();
    }
}
