using System;
using System.Collections.Generic;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.ViewModels;

public class InstanceSettingsViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;
    private Instance? _instance;

    private string _name = "";
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    private int _ramMB;
    public int RamMB { get => _ramMB; set => SetProperty(ref _ramMB, value); }

    private string _jvmArgs = "";
    public string JvmArgs { get => _jvmArgs; set => SetProperty(ref _jvmArgs, value); }

    private string? _javaPath;
    public string? JavaPath { get => _javaPath; set => SetProperty(ref _javaPath, value); }

    private double _gameWidth = double.NaN;
    public double GameWidth { get => _gameWidth; set => SetProperty(ref _gameWidth, value); }

    private double _gameHeight = double.NaN;
    public double GameHeight { get => _gameHeight; set => SetProperty(ref _gameHeight, value); }

    private bool _fullscreen;
    public bool Fullscreen { get => _fullscreen; set => SetProperty(ref _fullscreen, value); }

    private string? _customIconPath;
    public string? CustomIconPath { get => _customIconPath; set => SetProperty(ref _customIconPath, value); }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public InstanceSettingsViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;
        SaveCommand = new RelayCommand(SaveSettings);
        CancelCommand = new RelayCommand(CancelSettings);
    }

    public void LoadInstance(Instance instance)
    {
        _instance = instance;
        Name = instance.Name;
        RamMB = instance.RamMB;
        JvmArgs = instance.JvmArguments;
        JavaPath = instance.CustomJavaPath;
        GameWidth = instance.WindowWidth.HasValue ? (double)instance.WindowWidth.Value : double.NaN;
        GameHeight = instance.WindowHeight.HasValue ? (double)instance.WindowHeight.Value : double.NaN;
        Fullscreen = instance.Fullscreen;
        CustomIconPath = instance.IconFilename;
    }

    private void SaveSettings()
    {
        if (_instance == null) return;

        _instance.Name = Name;
        _instance.RamMB = RamMB;
        _instance.JvmArguments = JvmArgs;
        _instance.CustomJavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath;
        _instance.WindowWidth = double.IsNaN(GameWidth) ? (int?)null : (int)GameWidth;
        _instance.WindowHeight = double.IsNaN(GameHeight) ? (int?)null : (int)GameHeight;
        _instance.Fullscreen = Fullscreen;
        _instance.IconFilename = string.IsNullOrWhiteSpace(CustomIconPath) ? null : CustomIconPath;

        App.Instances.SaveInstance(_instance);
        App.MainVM.InstanceGrid.RefreshInstances();

        _mainVM.NavigateTo(ActivePage.Instances);
    }

    private void CancelSettings()
    {
        _mainVM.NavigateTo(ActivePage.Instances);
    }
}
