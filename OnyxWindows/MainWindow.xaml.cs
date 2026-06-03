using Microsoft.UI.Xaml;

namespace OnyxWindows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Extends the content into the title bar for a seamless borderless look
        ExtendsContentIntoTitleBar = true;
    }
}
