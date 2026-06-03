using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OnyxWindows.Models;
using System;

namespace OnyxWindows.Views.Onboarding;

public partial class OnboardingView : UserControl
{
    private AppLanguage _selectedLang = AppLanguage.EN;
    private string _nickname = "Player";
    private ThemeType _selectedTheme = ThemeType.Dark;

    public OnboardingView()
    {
        this.InitializeComponent();
    }

    private void Language_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string langStr)
        {
            if (Enum.TryParse<AppLanguage>(langStr, out var lang))
            {
                _selectedLang = lang;
                App.AppData.Config.Language = lang;
                App.Loc.Language = lang;
                App.AppData.SaveConfig();
            }

            // Transition to Step 2
            Step1_Container.Visibility = Visibility.Collapsed;
            Step2_Container.Visibility = Visibility.Visible;

            Indicator1.Fill = new SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#253545"));
            Indicator2.Fill = new SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#4C8AEA"));
            SubtitleText.Text = "Set up your in-game identity";
        }
    }

    private void Nickname_Confirmed(object sender, RoutedEventArgs e)
    {
        ConfirmNickname();
    }

    private void NicknameInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ConfirmNickname();
        }
    }

    private void ConfirmNickname()
    {
        var text = NicknameInput.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _nickname = text;

        // Generate offline UUID
        var uuid = OnyxWindows.Services.LaunchController.OfflineUUID(_nickname);

        // Add Offline Account to AccountManager
        var account = new Account
        {
            Username = _nickname,
            Uuid = uuid,
            Type = AccountType.Offline,
            IsActive = true
        };

        App.AccountMgr.AllAccounts.Add(account);
        App.AccountMgr.SaveAccounts();

        // Transition to Step 3
        Step2_Container.Visibility = Visibility.Collapsed;
        Step3_Container.Visibility = Visibility.Visible;

        Indicator2.Fill = new SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#253545"));
        Indicator3.Fill = new SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#4C8AEA"));
        SubtitleText.Text = "Choose your presentation style";
    }

    private void Theme_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string themeStr)
        {
            if (Enum.TryParse<ThemeType>(themeStr, out var theme))
            {
                _selectedTheme = theme;
                App.Themes.CurrentTheme = theme;
            }
        }
    }

    private void Complete_Onboarding(object sender, RoutedEventArgs e)
    {
        // Finish onboarding configuration
        App.AppData.Config.HasCompletedOnboarding = true;
        App.AppData.SaveConfig();

        // Navigate to Instances Grid View
        App.MainVM.NavigateTo(OnyxWindows.ViewModels.ActivePage.Instances);
    }
}
