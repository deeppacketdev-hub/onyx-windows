using System;
using Microsoft.UI.Xaml.Controls;
using OnyxWindows.Services;

namespace OnyxWindows.Views.News;

public partial class NewsView : UserControl
{
    public NewsView()
    {
        this.InitializeComponent();
    }

    private void NewsItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewsEntry entry && !string.IsNullOrEmpty(entry.ReadMoreLink))
        {
            try
            {
                // Open the news article in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = entry.ReadMoreLink,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[News] Failed to open link: {ex.Message}");
            }
        }
    }
}
