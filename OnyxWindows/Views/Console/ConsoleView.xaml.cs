using Microsoft.UI.Xaml.Controls;
using OnyxWindows.ViewModels;
using System.Collections.Specialized;

namespace OnyxWindows.Views.Console;

public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        this.InitializeComponent();

        this.RegisterPropertyChangedCallback(DataContextProperty, (s, p) =>
        {
            if (DataContext is ConsoleViewModel vm)
            {
                vm.LogLines.CollectionChanged += LogLines_CollectionChanged;
                // Initially scroll to bottom if there's existing log content
                ScrollToBottom();
            }
        });
    }

    private void LogLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (LogsListView.Items.Count > 0)
        {
            LogsListView.ScrollIntoView(LogsListView.Items[^1]);
        }
    }
}
