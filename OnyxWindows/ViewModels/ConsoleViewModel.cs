using System;
using System.Collections.ObjectModel;
using OnyxWindows.Helpers;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class ConsoleViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    public ObservableCollection<LogLine> LogLines { get; } = new();

    public RelayCommand ClearCommand { get; }
    public RelayCommand StopGameCommand { get; }

    public ConsoleViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        ClearCommand = new RelayCommand(ClearLogs);
        StopGameCommand = new RelayCommand(StopGame, CanStopGame);

        // Bind LaunchController log updates to UI
        App.Launcher.PropertyChanged += Launcher_PropertyChanged;
    }

    private void Launcher_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LaunchController.LogLines))
        {
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                LogLines.Clear();
                foreach (var line in App.Launcher.LogLines)
                {
                    LogLines.Add(line);
                }
            });
        }
    }

    private void ClearLogs()
    {
        App.Launcher.LogLines.Clear();
        LogLines.Clear();
    }

    private void StopGame()
    {
        // Force stop game process
        App.Launcher.StopGame();
    }

    private bool CanStopGame()
    {
        return App.Launcher.IsLaunching || LogLines.Count > 0;
    }
}
