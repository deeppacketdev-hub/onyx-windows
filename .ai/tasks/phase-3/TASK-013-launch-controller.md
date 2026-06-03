# TASK-013: LaunchController ⚠️ НАЙСКЛАДНІШИЙ ТАСК

## Metadata
- **Phase**: 3 | **Dependencies**: TASK-008 через TASK-012, TASK-010 (JavaManager) | **LOC**: ~1000
- **Source**: [LaunchController.swift](../../onyx/Onyx/Services/LaunchController.swift) — 955 рядків
- **Output**: `src/OnyxWindows/Services/LaunchController.cs`

## Objective
Портувати весь flow запуску Minecraft. Це ядро лаунчера.

## Launch Flow (зберігається ідентичним)

```
1. Validate account (online: check token; offline: generate UUID)
2. Fetch version detail JSON
3. Resolve mod loader (Fabric/Forge/NeoForge)
4. Build download list (client JAR + libraries + assets)
5. Download all files (via DownloadManager)
6. Download/ensure Java
7. Extract native libraries
8. Build classpath
9. Build JVM + game arguments
10. Patch options.txt (language, fullscreen, resolution)
11. Apply skins (CustomSkinLoader config)
12. Start Minecraft process
13. Monitor process (log output, track play time)
14. Cleanup on exit
```

## ⚠️⚠️⚠️ CRITICAL PLATFORM CHANGES ⚠️⚠️⚠️

### 1. JVM Arguments — ВИДАЛИТИ -XstartOnFirstThread!
```csharp
// macOS: args.insert("-XstartOnFirstThread", at: 0)
// Windows: НЕ ДОДАВАТИ! Це macOS-only прапорець для LWJGL/AWT.
// Якщо додати на Windows — CRASH.
```

### 2. Classpath Separator
```csharp
// macOS: ":"
// Windows: ";"
var classpath = string.Join(";", classpathParts);
```

### 3. Native Library Extraction
```csharp
// macOS: /usr/bin/unzip + filter .dylib/.jnilib + codesign + xattr
// Windows: ZipFile + filter .dll
using var archive = ZipFile.OpenRead(jarPath);
foreach (var entry in archive.Entries)
{
    if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
        && !entry.FullName.Contains("META-INF"))
    {
        entry.ExtractToFile(Path.Combine(nativesDir, entry.Name), overwrite: true);
    }
}
// НЕ потрібно: codesign, xattr, chmod
```

### 4. Template Substitution
```csharp
// ${classpath_separator} → ";"     (macOS: ":")
// ${library_directory} → Windows paths (backslash)
// ${natives_directory} → Windows paths
// ${launcher_name} → "onyx-windows"
// ${launcher_version} → "1.0.0"
```

### 5. Process Launch
```csharp
var process = new Process();
process.StartInfo.FileName = javaPath;  // .../bin/java.exe
process.StartInfo.Arguments = string.Join(" ", allArgs);
process.StartInfo.WorkingDirectory = gameDirPath;
process.StartInfo.UseShellExecute = false;
process.StartInfo.RedirectStandardOutput = true;
process.StartInfo.RedirectStandardError = true;
process.StartInfo.CreateNoWindow = true;
process.StartInfo.EnvironmentVariables.Clear(); // or selectively set

process.OutputDataReceived += (sender, e) => {
    if (e.Data != null) AppendLog(e.Data);
};
process.ErrorDataReceived += (sender, e) => {
    if (e.Data != null) AppendLog(e.Data);
};
process.EnableRaisingEvents = true;
process.Exited += (sender, e) => OnGameExited();

process.Start();
process.BeginOutputReadLine();
process.BeginErrorReadLine();
```

### 6. Offline UUID Generation (ІДЕНТИЧНИЙ алгоритм)
```csharp
// MD5 hash з "OfflinePlayer:{username}"
using var md5 = MD5.Create();
var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
bytes[6] = (byte)((bytes[6] & 0x0F) | 0x30); // version 3
bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // variant 1
return new Guid(bytes).ToString();
```

### 7. options.txt Patching (ідентична логіка)
```
lang:{mapped_lang}      — uk_ua, de_de, etc.
fullscreen:{true/false}
overrideWidth:{width}
overrideHeight:{height}
guiScale:{scale}
```

### 8. Mod Loader Classpath Merging
Для Fabric: додати classpath з FabricInstaller.prepareLoader()
Для Forge/NeoForge: знайти {forgeVersionId}.json в cache/versions/, merge libraries

### 9. Close Launcher on Launch
```csharp
// macOS: NSApplication.shared.terminate(nil)
// Windows:
Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
```

### 10. Discord RPC Integration
```csharp
if (config.EnableDiscordRPC)
    _discordRPC.SetActivity(instance.Name, instance.MinecraftVersion, loaderName);
// On exit:
_discordRPC.ClearActivity();
```

## State Management
```csharp
[ObservableProperty] private bool _isLaunching;
[ObservableProperty] private string _status = "";
[ObservableProperty] private string _launchingInstanceId = "";
[ObservableProperty] private ObservableCollection<string> _consoleLog = new();
```

## Acceptance Criteria
- [ ] Vanilla Minecraft 1.21.4 запускається
- [ ] Fabric mod loader працює
- [ ] Forge/NeoForge працюють
- [ ] Offline account працює (UUID генерується правильно)
- [ ] Console log відображає вивід гри
- [ ] Play time трекається
- [ ] -XstartOnFirstThread НЕ присутній в аргументах
- [ ] Classpath використовує `;`
- [ ] Нативні бібліотеки .dll екстрагуються
