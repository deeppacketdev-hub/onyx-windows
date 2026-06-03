# TASK-002: Models

## Metadata
- **Phase**: 1 — Foundation
- **Priority**: 🔴 Critical
- **Dependencies**: TASK-001
- **Estimated LOC**: ~700
- **Source files**: `onyx/Onyx/Models/*.swift` (6 файлів, ~850 рядків Swift)
- **Output files**: 8 файлів у `src/OnyxWindows/Models/`

## Objective

Портувати ВСІ моделі даних з Swift у C#. JSON формат має бути 100% сумісним між macOS та Windows версіями.

## Steps

### 1. Enums — `ModLoaderType.cs`, `InstanceStatus.cs`, `ThemeType.cs`, `VersionFilter.cs`

**Source**: [Instance.swift](../../onyx/Onyx/Models/Instance.swift) lines 1-20, [GlobalConfig.swift](../../onyx/Onyx/Models/GlobalConfig.swift) lines 1-30

```csharp
// Models/ModLoaderType.cs
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModLoaderType { Fabric, Quilt, Forge, Neoforge }

// Models/InstanceStatus.cs
public enum InstanceStatus { Ready, Preparing, Downloading, Running, Stopped, Error }

// Models/ThemeType.cs
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThemeType { System, Dark, Light, Custom }

// Models/VersionFilter.cs
public enum VersionFilter { Release, Snapshot, OldBeta, OldAlpha }

// Models/AppLanguage.cs
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppLanguage { En, Uk, De, Es, Fr, Pl }
```

### 2. `GlobalConfig.cs`

**Source**: [GlobalConfig.swift](../../onyx/Onyx/Models/GlobalConfig.swift) — 121 рядків

Портувати ВСІ поля з defaults. Важливо: `CustomThemeColors` як вкладений клас.

Поля: Nickname, DefaultRamMB, Theme, CustomTheme, DefaultJavaPath, Language,
ShowConsoleOnLaunch, CloseLauncherOnLaunch, AccumulatedPlayTime, ActiveSkinName,
DefaultGameWidth, DefaultGameHeight, DefaultFullscreen, DefaultGuiScale,
HasCompletedOnboarding, EnableDiscordRPC, EnableTelemetry

### 3. `Instance.cs`

**Source**: [Instance.swift](../../onyx/Onyx/Models/Instance.swift) — 166 рядків

Ключове:
- `DirectoryName` computed property: санітизація імені через `Path.GetInvalidFileNameChars()`
- `InstalledModrinthFiles`: `Dictionary<string, string>?` (projectId → versionId)
- `InstalledCFMeta`: `Dictionary<string, CFInstalledMeta>?`
- `Status` не серіалізується (transient, default Ready)

### 4. `Account.cs`

**Source**: [Account.swift](../../onyx/Onyx/Models/Account.swift) — 85 рядків

Ключове:
- Токени `[JsonIgnore]` — зберігаються через CredentialStore (DPAPI)
- `IsOffline` computed property
- `HeadImageData` — `[JsonIgnore]`, byte[] для кешованого аватара

### 5. `VersionManifest.cs`

**Source**: [VersionManifest.swift](../../onyx/Onyx/Models/VersionManifest.swift) — 185 рядків

⚠️ **Найскладніша модель** — 18 вкладених типів:

```
VersionManifest → LatestVersions, VersionEntry[], VersionType
VersionDetail → GameArguments, ArgumentValue (UNION TYPE!), Library[],
                VersionDownloads, AssetIndexInfo, JavaVersionInfo, LoggingConfig
Library → LibraryDownloads, LibraryArtifact, Rule, OSRule, natives dict
AssetIndex → AssetObject dict
```

⚠️ **ArgumentValue** — це Swift enum з associated values:
```swift
enum ArgumentValue: Codable {
    case string(String)
    case conditional(ConditionalArgument)
}
```
В C# потрібен **custom JsonConverter**:
```csharp
[JsonConverter(typeof(ArgumentValueConverter))]
public abstract record ArgumentValue;
public record StringArgument(string Value) : ArgumentValue;
public record ConditionalArgument(List<Rule> Rules, List<string> Value) : ArgumentValue;
```

### 6. `WorldSettings.cs`

**Source**: [WorldSettings.swift](../../onyx/Onyx/Models/WorldSettings.swift) — 285 рядків

Портувати ObservableObject з усіма полями + enum'ами (MinecraftGameMode, MinecraftDifficulty).
Методи `initFromNBT()` та `applyToNBT()` — вони залежать від NBTService (TASK-021),
тому поки що створити placeholder методи.

## Acceptance Criteria

- [ ] Всі 8 файлів створені в `Models/`
- [ ] JSON серіалізація/десеріалізація працює для Instance, Account, GlobalConfig
- [ ] `instance.json` з macOS версії коректно десеріалізується в C# Instance
- [ ] `config.json` з macOS версії коректно десеріалізується в C# GlobalConfig
- [ ] ArgumentValueConverter коректно обробляє обидва типи (string та conditional)

## Verification

```csharp
// Тест: десеріалізація macOS instance.json
var json = File.ReadAllText("test_instance.json");
var instance = JsonSerializer.Deserialize<Instance>(json);
Assert.NotNull(instance);
Assert.Equal("My Instance", instance.Name);
```
