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
        JvmArgs = string.Join(" ", instance.JvmArguments);
        JavaPath = instance.JavaPath;
        GameWidth = instance.GameWidth ?? double.NaN;
        GameHeight = instance.GameHeight ?? double.NaN;
        Fullscreen = instance.Fullscreen;
        CustomIconPath = instance.CustomIconPath;
    }

    private void SaveSettings()
    {
        if (_instance == null) return;

        _instance.Name = Name;
        _instance.RamMB = RamMB;
        
        var args = JvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        _instance.JvmArguments = new List<string>(args);
        
        _instance.JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath;
        _instance.GameWidth = double.IsNaN(GameWidth) ? null : (int)GameWidth;
        _instance.GameHeight = double.IsNaN(GameHeight) ? null : (int)GameHeight;
        _instance.Fullscreen = Fullscreen;
        _instance.CustomIconPath = string.IsNullOrWhiteSpace(CustomIconPath) ? null : CustomIconPath;

        App.Instances.SaveInstance(_instance);
        App.MainVM.InstanceGrid.RefreshInstances();

        _mainVM.NavigateTo(ActivePage.Instances);
    }

    private void CancelSettings()
    {
        _mainVM.NavigateTo(ActivePage.Instances);
    }
}
