using System;
using System.Collections.ObjectModel;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.ViewModels;

public class AccountViewModel : ObservableBase
{
    private readonly MainViewModel _mainVM;

    public ObservableCollection<Account> Accounts { get; } = new();

    private Account? _activeAccount;
    public Account? ActiveAccount
    {
        get => _activeAccount;
        set
        {
            if (SetProperty(ref _activeAccount, value))
            {
                if (value != null)
                {
                    App.AccountMgr.SelectAccount(value.Id);
                }
            }
        }
    }

    private string _offlineNickname = "";
    public string OfflineNickname { get => _offlineNickname; set => SetProperty(ref _offlineNickname, value); }

    public RelayCommand AddOfflineCommand { get; }
    public RelayCommand RemoveAccountCommand { get; }
    public RelayCommand LoginMicrosoftCommand { get; }

    public AccountViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;

        AddOfflineCommand = new RelayCommand(AddOfflineAccount);
        RemoveAccountCommand = new RelayCommand(RemoveSelectedAccount);
        LoginMicrosoftCommand = new RelayCommand(LoginMicrosoft);

        RefreshAccounts();
    }

    public void RefreshAccounts()
    {
        Accounts.Clear();
        foreach (var acc in App.AccountMgr.Accounts)
        {
            Accounts.Add(acc);
        }
        ActiveAccount = App.AccountMgr.ActiveAccount;
    }

    private void AddOfflineAccount()
    {
        if (string.IsNullOrWhiteSpace(OfflineNickname)) return;

        App.AccountMgr.AddOfflineAccount(OfflineNickname.Trim());
        RefreshAccounts();
        OfflineNickname = "";
    }

    private void RemoveSelectedAccount()
    {
        if (ActiveAccount == null) return;
        App.AccountMgr.RemoveAccount(ActiveAccount.Id);
        RefreshAccounts();
    }

    private async void LoginMicrosoft()
    {
        try
        {
            // Windows WebView2 login integration can trigger WebView2 window
            // and perform Microsoft Login. For now, trigger Microsoft login flow
            await App.AccountMgr.AddMicrosoftAccount();
            RefreshAccounts();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AccountAuth] Microsoft Authentication failed: {ex.Message}");
        }
    }
}
