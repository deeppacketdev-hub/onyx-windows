using Microsoft.UI.Xaml;
using OnyxWindows.Services;
using OnyxWindows.ViewModels;

namespace OnyxWindows;

/// <summary>
/// Application entry point. Creates and wires all services and the main window.
/// Mirrors the macOS OnyxApp.init() initialization.
/// </summary>
public partial class App : Application
{
    // ── Services (singleton-like, created once) ──
    public static AppDataManager AppData { get; private set; } = null!;
    public static AccountManager AccountMgr { get; private set; } = null!;
    public static InstanceStore Instances { get; private set; } = null!;
    public static InstanceBuilder Builder { get; private set; } = null!;
    public static LaunchController Launcher { get; private set; } = null!;
    public static DownloadManager Downloads { get; private set; } = null!;
    public static ManifestService Manifest { get; private set; } = null!;
    public static JavaManager Java { get; private set; } = null!;
    public static FabricInstaller Fabric { get; private set; } = null!;
    public static ForgeInstaller Forge { get; private set; } = null!;
    public static ModLoaderService ModLoaders { get; private set; } = null!;
    public static ModrinthService Modrinth { get; private set; } = null!;
    public static CurseForgeService CurseForge { get; private set; } = null!;
    public static SkinManager Skins { get; private set; } = null!;
    public static DiscordRpcService DiscordRpc { get; private set; } = null!;
    public static UpdateService Updater { get; private set; } = null!;
    public static ThemeManager Themes { get; private set; } = null!;
    public static ScreenshotService Screenshots { get; private set; } = null!;
    public static WorldService Worlds { get; private set; } = null!;
    public static NbtService Nbt { get; private set; } = null!;
    public static NewsService News { get; private set; } = null!;
    public static TelemetryService Telemetry { get; private set; } = null!;
    public static VersionArtworkService Artwork { get; private set; } = null!;
    public static InstanceTransferService Transfer { get; private set; } = null!;
    public static Localization Loc { get; private set; } = null!;

    // ── Main ViewModel ──
    public static MainViewModel MainVM { get; private set; } = null!;

    private Window? _mainWindow;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 1. Initialize core services
        AppData = new AppDataManager();
        AppData.InitializeDirectories();

        Loc = new Localization();
        Loc.Language = AppData.Config.Language;

        Themes = new ThemeManager();
        Themes.LoadFrom(AppData.Config);

        // 2. Initialize data services
        Instances = new InstanceStore();
        Instances.LoadInstances(AppData.InstancesDirectory);

        AccountMgr = new AccountManager(AppData);
        AccountMgr.LoadAccounts();

        // 3. Initialize feature services
        Builder = new InstanceBuilder(AppData);
        Manifest = new ManifestService();
        Downloads = new DownloadManager();
        Java = new JavaManager(AppData.JavaDirectory);
        Fabric = new FabricInstaller(AppData);
        Forge = new ForgeInstaller(AppData);
        ModLoaders = new ModLoaderService();
        Modrinth = new ModrinthService();
        CurseForge = new CurseForgeService();
        Skins = new SkinManager(AppData);
        DiscordRpc = new DiscordRpcService();
        Updater = new UpdateService();
        Screenshots = new ScreenshotService();
        Worlds = new WorldService();
        Nbt = new NbtService();
        News = new NewsService();
        Artwork = new VersionArtworkService();
        Transfer = new InstanceTransferService();

        // 4. Initialize launch controller (depends on AppData, Instances)
        Launcher = new LaunchController(AppData, Instances);
        Launcher.DiscordRpc = DiscordRpc;

        // 5. Initialize telemetry
        Telemetry = new TelemetryService();
        Telemetry.Initialize(AppData.Config.EnableTelemetry);
        Telemetry.SendSignal("appLaunched", AppData.Config.EnableTelemetry);

        // 6. Create main ViewModel
        MainVM = new MainViewModel();

        // 7. Create and show the main window
        _mainWindow = new MainWindow();
        _mainWindow.Activate();

        // 8. Background startup tasks
        _ = Task.Run(async () =>
        {
            // Load Mojang manifest
            await Manifest.LoadManifest(AppData.MetaDirectory);

            // Pre-fetch version artwork
            var versions = Instances.AllInstances.Select(i => i.MinecraftVersion).ToList();
            await Artwork.PrefetchArtwork(versions, AppData.IconsDirectory);

            // Pre-download Java 21
            if (!Java.IsJavaAvailable(21))
            {
                try { await Java.GetJavaExecutable(21); }
                catch { /* silent */ }
            }

            // Check for updates after 3 seconds
            await Task.Delay(3000);
            await Updater.CheckForUpdates(silent: true);
        });
    }
}
