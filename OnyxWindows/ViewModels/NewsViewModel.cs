using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Services;

namespace OnyxWindows.ViewModels;

public class NewsViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    public ObservableCollection<NewsEntry> Entries { get; } = new();

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public RelayCommand RefreshCommand { get; }

    public NewsViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        RefreshCommand = new RelayCommand(LoadNews);

        LoadNews();
    }

    public async void LoadNews()
    {
        IsLoading = true;
        ErrorMessage = null;
        Entries.Clear();

        try
        {
            await App.News.FetchNews();
            if (App.News.Error != null)
            {
                ErrorMessage = App.News.Error;
            }
            else
            {
                foreach (var entry in App.News.News)
                {
                    Entries.Add(entry);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Console.WriteLine($"[News] Failed to load news: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
