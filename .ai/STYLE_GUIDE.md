# C# / WPF Code Style Guide

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase | `OnyxWindows.Services` |
| Class | PascalCase | `LaunchController` |
| Interface | I + PascalCase | `IDownloadManager` |
| Public method | PascalCase | `LaunchInstanceAsync()` |
| Private method | PascalCase | `BuildClasspath()` |
| Public property | PascalCase | `IsDownloading` |
| Private field | _camelCase | `_httpClient` |
| Observable field | _camelCase | `_instances` (→ `Instances` auto-generated) |
| Parameter | camelCase | `majorVersion` |
| Local variable | camelCase | `clientJar` |
| Constant | PascalCase | `MaxConcurrentDownloads` |
| Enum value | PascalCase | `ModLoaderType.Fabric` |
| XAML element x:Name | PascalCase | `InstanceGrid` |

## File Organization

```csharp
// 1. Using statements (sorted: System first, then third-party, then project)
using System;
using System.Collections.Generic;
using System.Text.Json;

using CommunityToolkit.Mvvm.ComponentModel;

using OnyxWindows.Models;

// 2. Namespace
namespace OnyxWindows.Services;

// 3. Class declaration
public partial class InstanceStore : ObservableObject
{
    // 4. Constants
    private const int MaxRetries = 3;

    // 5. Private fields (DI injected)
    private readonly AppDataManager _appData;
    private readonly HttpClient _httpClient;

    // 6. Observable properties
    [ObservableProperty]
    private ObservableCollection<Instance> _instances = new();

    // 7. Constructor
    public InstanceStore(AppDataManager appData, HttpClient httpClient)
    {
        _appData = appData;
        _httpClient = httpClient;
    }

    // 8. Public methods
    public void LoadInstances() { ... }

    // 9. Private methods
    private string SanitizeName(string name) { ... }
}
```

## Async Methods

- **Завжди** додавай суфікс `Async` до async методів: `DownloadJavaAsync()`, `LaunchAsync()`
- **Завжди** приймай `CancellationToken` де можливо
- **Ніколи** не блокуй UI thread: використовуй `await`, не `.Result` або `.Wait()`

```csharp
public async Task<string> DownloadJavaAsync(int version, CancellationToken ct = default)
{
    var response = await _httpClient.GetAsync(url, ct);
    // ...
}
```

## Error Handling

- Використовуй typed exceptions для domain errors:
```csharp
public class JavaNotFoundException : Exception
{
    public int MajorVersion { get; }
    public JavaNotFoundException(int version)
        : base($"Java {version} executable not found after installation")
    {
        MajorVersion = version;
    }
}
```

## JSON Serialization

- Використовуй `System.Text.Json` (не Newtonsoft)
- Використовуй source generators для performance де можливо
- Завжди вказуй `[JsonPropertyName]` для JSON mapping
- Використовуй `JsonSerializerOptions` з `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`

```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};
```

## WPF XAML Style

```xml
<!-- Завжди вказуй x:Name для інтерактивних елементів -->
<Button x:Name="LaunchButton"
        Content="{Binding Localize[play]}"
        Command="{Binding LaunchCommand}"
        Style="{StaticResource AccentButton}" />

<!-- Використовуй StaticResource для тем -->
<Border Background="{StaticResource SurfaceBrush}"
        CornerRadius="8"
        Padding="16">

<!-- Використовуй DataTemplate для списків -->
<ItemsControl ItemsSource="{Binding Instances}">
    <ItemsControl.ItemTemplate>
        <DataTemplate DataType="{x:Type models:Instance}">
            <local:InstanceCardView />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

## Logging

- Використовуй `Debug.WriteLine($"[ClassName] message")` для відповідності macOS стилю:
```csharp
Debug.WriteLine($"[InstanceStore] Failed to load {folder}: {ex.Message}");
Debug.WriteLine($"[DownloadManager] ✅ Downloaded {item.Label}");
```

## Comments

- Коментарі англійською
- XML docs для public API:
```csharp
/// <summary>
/// Downloads and installs Java JDK from Adoptium.
/// Falls back to x64 for Java 8 on ARM64 (Rosetta equivalent not needed on Windows).
/// </summary>
public async Task<string> GetJavaExecutableAsync(int majorVersion)
```

## Project Structure Rules

1. **Один клас = один файл** (виняток: маленькі enum/record в тому ж файлі)
2. **Моделі** — `Models/` (POCO, records, enums)
3. **Сервіси** — `Services/` (бізнес-логіка, без UI залежностей)
4. **ViewModels** — `ViewModels/` (ObservableObject, Commands, UI state)
5. **Views** — `Views/{Group}/` (XAML + code-behind, мінімум логіки)
6. **Converters** — `Converters/` (IValueConverter для XAML bindings)
7. **Helpers** — `Helpers/` (утилітарні класи)
