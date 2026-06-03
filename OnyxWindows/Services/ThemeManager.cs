using System;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public class ThemePalette
{
    public string BackgroundHex { get; set; } = "#0F1923";
    public string AccentHex { get; set; } = "#4C8AEA";
    public string SurfaceHex { get; set; } = "#1A2836";
    public string PrimaryTextHex { get; set; } = "#FFFFFF";
    public string SecondaryTextHex { get; set; } = "#8C9BAE";

    public static readonly ThemePalette Dark = new()
    {
        BackgroundHex = "#0F1923",
        SurfaceHex = "#1A2836",
        AccentHex = "#4C8AEA",
        PrimaryTextHex = "#FFFFFF",
        SecondaryTextHex = "#8C9BAE"
    };

    public static readonly ThemePalette Light = new()
    {
        BackgroundHex = "#F4F5F7",
        SurfaceHex = "#FFFFFF",
        AccentHex = "#3878D8",
        PrimaryTextHex = "#1A1A1E",
        SecondaryTextHex = "#666A73"
    };
}

public class ThemeManager : Helpers.ObservableBase
{
    private ThemeType _currentTheme = ThemeType.System;
    public ThemeType CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (SetProperty(ref _currentTheme, value))
            {
                ApplyAppearance();
            }
        }
    }

    private CustomThemeColors _customColors = new();
    public CustomThemeColors CustomColors
    {
        get => _customColors;
        set
        {
            if (SetProperty(ref _customColors, value))
            {
                if (CurrentTheme == ThemeType.Custom)
                {
                    ApplyAppearance();
                }
            }
        }
    }

    private readonly AppDataManager _appData;

    public ThemePalette Colors
    {
        get
        {
            switch (CurrentTheme)
            {
                case ThemeType.System:
                    // Check standard Windows app theme
                    var isDark = Microsoft.UI.Xaml.Application.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Dark;
                    return isDark ? ThemePalette.Dark : ThemePalette.Light;
                case ThemeType.Dark:
                    return ThemePalette.Dark;
                case ThemeType.Light:
                    return ThemePalette.Light;
                case ThemeType.Custom:
                    return new ThemePalette
                    {
                        BackgroundHex = CustomColors.BackgroundHex,
                        SurfaceHex = CustomColors.SurfaceHex,
                        AccentHex = CustomColors.AccentHex,
                        PrimaryTextHex = "#FFFFFF",
                        SecondaryTextHex = "#8C9BAE"
                    };
                default:
                    return ThemePalette.Dark;
            }
        }
    }

    public ThemeManager(AppDataManager appData)
    {
        _appData = appData;
        LoadThemeSettings();
    }

    private void LoadThemeSettings()
    {
        CurrentTheme = _appData.GlobalConfig.Theme;
        if (_appData.GlobalConfig.CustomTheme != null)
        {
            CustomColors = _appData.GlobalConfig.CustomTheme;
        }
    }

    public void ApplyAppearance()
    {
        // 1. Update Global Config
        _appData.GlobalConfig.Theme = CurrentTheme;
        _appData.GlobalConfig.CustomTheme = CustomColors;
        _appData.SaveConfig();

        // 2. WinUI Window Theme switching
        var rootElement = Microsoft.UI.Xaml.Window.Current?.Content as Microsoft.UI.Xaml.FrameworkElement;
        if (rootElement != null)
        {
            rootElement.RequestedTheme = CurrentTheme switch
            {
                ThemeType.System => Microsoft.UI.Xaml.ElementTheme.Default,
                ThemeType.Dark => Microsoft.UI.Xaml.ElementTheme.Dark,
                ThemeType.Light => Microsoft.UI.Xaml.ElementTheme.Light,
                ThemeType.Custom => Microsoft.UI.Xaml.ElementTheme.Dark, // Custom themes are built on dark base
                _ => Microsoft.UI.Xaml.ElementTheme.Default
            };
        }
    }
}
