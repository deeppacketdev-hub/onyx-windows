# Swift → C# Architecture Mapping

> Цей файл — довідник для AI-агента. Кожна конструкція Swift має точний еквівалент C#.

## Reactive State Management

### Swift: @Observable
```swift
@Observable
final class InstanceStore {
    var instances: [Instance] = []
    var isLoading = false
}
```

### C#: CommunityToolkit.Mvvm ObservableObject
```csharp
public partial class InstanceStore : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Instance> _instances = new();

    [ObservableProperty]
    private bool _isLoading;
}
```

> **Правило**: `[ObservableProperty]` генерує публічне властивість з `OnPropertyChanged`.
> Поле `_isLoading` → властивість `IsLoading` (автоматично).

---

## Dependency Injection

### Swift: @Environment
```swift
@main struct OnyxApp: App {
    @State private var appData = AppDataManager()
    var body: some Scene {
        WindowGroup { ContentView().environment(appData) }
    }
}
// Usage:
@Environment(AppDataManager.self) var appData
```

### C#: Microsoft.Extensions.DependencyInjection
```csharp
// App.xaml.cs
var services = new ServiceCollection();
services.AddSingleton<AppDataManager>();
services.AddSingleton<InstanceStore>();
services.AddSingleton<LaunchController>();
// ... etc
services.AddSingleton<MainViewModel>();
_serviceProvider = services.BuildServiceProvider();

// Usage in ViewModel:
public class MainViewModel : ObservableObject
{
    private readonly AppDataManager _appData;
    public MainViewModel(AppDataManager appData) { _appData = appData; }
}
```

---

## JSON Serialization

### Swift: Codable
```swift
struct Instance: Codable {
    var id: UUID = UUID()
    var name: String
    var minecraftVersion: String
    var modLoader: ModLoaderType?

    enum CodingKeys: String, CodingKey {
        case id, name
        case minecraftVersion = "minecraft_version"
        case modLoader = "mod_loader"
    }
}
```

### C#: System.Text.Json
```csharp
public class Instance
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("minecraft_version")]
    public string MinecraftVersion { get; set; } = "";

    [JsonPropertyName("mod_loader")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModLoaderType? ModLoader { get; set; }
}
```

---

## Async/Await

### Swift
```swift
func downloadJava(majorVersion: Int) async throws {
    let (tempURL, response) = try await URLSession.onyxSession.download(from: url)
    guard let httpResponse = response as? HTTPURLResponse,
          (200...399).contains(httpResponse.statusCode) else {
        throw JavaError.downloadFailed(majorVersion)
    }
}
```

### C#
```csharp
public async Task DownloadJavaAsync(int majorVersion)
{
    using var response = await _httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    await using var stream = await response.Content.ReadAsStreamAsync();
    // ...
}
```

---

## Process Management

### Swift (macOS)
```swift
let process = Process()
process.executableURL = URL(filePath: javaPath)
process.arguments = args
process.environment = environment
let stdoutPipe = Pipe()
process.standardOutput = stdoutPipe
try process.run()
```

### C# (Windows)
```csharp
var process = new Process();
process.StartInfo.FileName = javaPath;
process.StartInfo.Arguments = string.Join(" ", args);
process.StartInfo.WorkingDirectory = gameDir;
process.StartInfo.UseShellExecute = false;
process.StartInfo.RedirectStandardOutput = true;
process.StartInfo.RedirectStandardError = true;
process.StartInfo.CreateNoWindow = true;
process.OutputDataReceived += (s, e) => { /* handle */ };
process.ErrorDataReceived += (s, e) => { /* handle */ };
process.EnableRaisingEvents = true;
process.Exited += (s, e) => { /* handle */ };
process.Start();
process.BeginOutputReadLine();
process.BeginErrorReadLine();
```

---

## File Operations

| Swift | C# |
|-------|-----|
| `FileManager.default.fileExists(atPath:)` | `File.Exists(path)` / `Directory.Exists(path)` |
| `FileManager.default.createDirectory(at:)` | `Directory.CreateDirectory(path)` |
| `FileManager.default.removeItem(at:)` | `File.Delete(path)` / `Directory.Delete(path, true)` |
| `FileManager.default.copyItem(at:to:)` | `File.Copy(src, dst)` |
| `FileManager.default.moveItem(at:to:)` | `File.Move(src, dst)` |
| `FileManager.default.contentsOfDirectory(at:)` | `Directory.GetDirectories(path)` / `Directory.GetFiles(path)` |
| `Data(contentsOf: url)` | `await File.ReadAllBytesAsync(path)` |
| `data.write(to: url)` | `await File.WriteAllBytesAsync(path, data)` |
| `url.appendingPathComponent("x")` | `Path.Combine(path, "x")` |
| `url.lastPathComponent` | `Path.GetFileName(path)` |
| `url.deletingLastPathComponent()` | `Path.GetDirectoryName(path)` |
| `url.path(percentEncoded: false)` | просто `path` (string) |

---

## Enums

### Swift: enum with rawValue
```swift
enum ModLoaderType: String, Codable, CaseIterable {
    case fabric, quilt, forge, neoforge
}
```

### C#: enum + JsonStringEnumConverter
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModLoaderType
{
    [JsonPropertyName("fabric")] Fabric,
    [JsonPropertyName("quilt")] Quilt,
    [JsonPropertyName("forge")] Forge,
    [JsonPropertyName("neoforge")] NeoForge,
}
```

---

## Platform-Specific Replacements

| macOS API | Windows Replacement |
|-----------|-------------------|
| `Keychain` (Security.framework) | `ProtectedData.Protect/Unprotect` (DPAPI) |
| `NSImage` | `BitmapImage` (System.Windows.Media.Imaging) |
| `NSSavePanel` | `Microsoft.Win32.SaveFileDialog` |
| `NSOpenPanel` | `Microsoft.Win32.OpenFileDialog` |
| `WKWebView` | `Microsoft.Web.WebView2.Wpf.WebView2` |
| `NSVisualEffectView` | Mica/Acrylic via `SetWindowCompositionAttribute` |
| `NSApplication.shared.terminate` | `Application.Current.Shutdown()` |
| `Bundle.main.infoDictionary` | `Assembly.GetExecutingAssembly().GetName()` |
| `ProcessInfo.processInfo.environment` | `Environment.GetEnvironmentVariables()` |
| `utsname()` / `uname()` | `RuntimeInformation.OSArchitecture` |
| `/usr/bin/tar` | `System.Formats.Tar` or `SharpCompress` |
| `/usr/bin/zip` / `/usr/bin/unzip` | `System.IO.Compression.ZipFile` |
| `/bin/chmod +x` | Не потрібно на Windows |
| `xattr -cr` | Не потрібно на Windows |
| `codesign -f -s -` | Не потрібно на Windows |
| Unix domain socket (`$TMPDIR/discord-ipc-{i}`) | Named pipe (`\\.\pipe\discord-ipc-{i}`) |
| `Sparkle` (SPUUpdater) | `Velopack` або GitHub Releases API |

---

## Classpath & Launch Critical Differences

```
macOS                          →  Windows
───────────────────────────────────────────
Classpath separator: ':'       →  ';'
Native libs: .dylib, .jnilib  →  .dll
OS rule: "osx" / "macos"      →  "windows"
Arch rule: "aarch64"/"arm64"  →  "x64"
Java path: Contents/Home/bin/java → bin\java.exe
JVM flag: -XstartOnFirstThread →  ⚠️ ВИДАЛИТИ! (crash на Windows)
Adoptium OS: "mac"            →  "windows"
Archive: .tar.gz              →  .zip
Native classifier: natives-macos → natives-windows
```

---

## WPF XAML Patterns

### SwiftUI → WPF Layout Mapping

| SwiftUI | WPF |
|---------|-----|
| `VStack` | `StackPanel Orientation="Vertical"` |
| `HStack` | `StackPanel Orientation="Horizontal"` |
| `ZStack` | `Grid` (overlapping children) |
| `ScrollView` | `ScrollViewer` |
| `LazyVGrid` | `ItemsControl` + `WrapPanel` |
| `List` | `ListBox` / `ListView` |
| `NavigationStack` | `ContentControl` bound to current view |
| `.sheet()` / `.alert()` | Popup / child Window |
| `.onAppear` | `Loaded` event |
| `.task {}` | `Loaded` + async call in ViewModel |
| `@Binding` | Two-way `{Binding}` in XAML |
| `.foregroundColor` | `Foreground="{StaticResource ...}"` |
| `.background` | `Background="{StaticResource ...}"` |
| `.clipShape(RoundedRectangle)` | `Border CornerRadius="8"` |
| `.shadow()` | `Border.Effect > DropShadowEffect` |
| `.opacity()` | `Opacity="0.5"` |
| `.animation(.easeInOut)` | `Storyboard` + `DoubleAnimation` |
| `.onHover` | `MouseEnter` / `MouseLeave` triggers |
