# TASK-001: Project Setup

## Metadata
- **Phase**: 1 — Foundation
- **Priority**: 🔴 Critical (blocks everything)
- **Dependencies**: None
- **Estimated LOC**: ~100
- **Output files**: `OnyxWindows.sln`, `src/OnyxWindows/OnyxWindows.csproj`, `src/OnyxWindows/App.xaml`, `App.xaml.cs`

## Objective

Створити .NET 8 WPF проект з правильною структурою папок, NuGet пакетами та DI контейнером.

## Steps

### 1. Створити Solution та Project

```bash
cd c:\Users\akorn\Documents\GitHub\onyx-windows
dotnet new sln -n OnyxWindows
mkdir src\OnyxWindows
cd src\OnyxWindows
dotnet new wpf -n OnyxWindows --framework net8.0-windows
cd ..\..
dotnet sln add src\OnyxWindows\OnyxWindows.csproj
```

### 2. Додати NuGet пакети

Відредагуй `src/OnyxWindows/OnyxWindows.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>Resources\Assets\onyx.ico</ApplicationIcon>
    <AssemblyName>OnyxWindows</AssemblyName>
    <RootNamespace>OnyxWindows</RootNamespace>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.*" />
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.*" />
    <PackageReference Include="SharpCompress" Version="0.37.*" />
  </ItemGroup>
</Project>
```

### 3. Створити структуру папок

```
src/OnyxWindows/
├── Models/
├── ViewModels/
├── Services/
├── Views/
│   ├── Sidebar/
│   ├── Instances/
│   ├── Mods/
│   ├── Skins/
│   ├── Screenshots/
│   ├── Worlds/
│   ├── News/
│   ├── Settings/
│   ├── Accounts/
│   ├── Onboarding/
│   ├── Console/
│   └── Components/
├── Converters/
├── Helpers/
└── Resources/
    ├── Themes/
    ├── Icons/
    ├── Fonts/
    └── Assets/
```

### 4. Налаштувати App.xaml з DI

```csharp
// App.xaml.cs
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<AppDataManager>();
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<CredentialStore>();

        // HTTP
        services.AddHttpClient("OnyxClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OnyxLauncher/1.0");
        });

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
    }
}
```

### 5. Створити пустий MainWindow

```xml
<!-- MainWindow.xaml -->
<Window x:Class="OnyxWindows.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Onyx Launcher" Width="1100" Height="720"
        MinWidth="900" MinHeight="600"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <TextBlock Text="Onyx Launcher — Coming Soon"
                   HorizontalAlignment="Center" VerticalAlignment="Center"
                   FontSize="24" Foreground="White" />
    </Grid>
</Window>
```

### 6. Перевірити збірку

```bash
dotnet build src/OnyxWindows/OnyxWindows.csproj
```

## Acceptance Criteria

- [ ] Solution збирається без помилок
- [ ] NuGet пакети встановлені
- [ ] Структура папок створена
- [ ] DI контейнер налаштований в App.xaml.cs
- [ ] MainWindow відкривається при запуску

## Reference Files

- [OnyxApp.swift](../../onyx/Onyx/App/OnyxApp.swift) — DI setup reference
