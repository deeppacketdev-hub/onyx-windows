# TASK-003: AppDataManager

## Metadata
- **Phase**: 1 | **Dependencies**: TASK-001 | **LOC**: ~120
- **Source**: [AppDataManager.swift](../../onyx/Onyx/Services/AppDataManager.swift) — 104 рядки
- **Output**: `src/OnyxWindows/Services/AppDataManager.cs`

## Objective
Порт менеджера директорій. Створює структуру папок при першому запуску, керує config.json.

## Key Changes from macOS
- Base: `Environment.GetFolderPath(SpecialFolder.ApplicationData) + "OnyxLauncher"` (було `~/Library/Application Support/com.onyx.launcher`)
- Всі URL → string paths з `Path.Combine()`
- `FileManager.default` → `File` / `Directory` static methods

## Properties (ідентичні)
```
BaseDirectory, JavaDirectory, CacheDirectory, InstancesDirectory, MetaDirectory,
IconsDirectory, SkinsDirectory, ConfigFile, AccountsFile,
AssetsDirectory, AssetsIndexesDirectory, AssetsObjectsDirectory,
LibrariesDirectory, VersionsDirectory
```

## Methods
- `InitializeDirectories()` — створити всі папки
- `SaveConfig()` — зберегти GlobalConfig в config.json
- `LibraryPath(string mavenName)` — Maven layout path (group:artifact:version → path)

## Acceptance Criteria
- [ ] Всі директорії створюються при першому запуску
- [ ] config.json зберігається/завантажується
- [ ] LibraryPath("com.mojang:authlib:1.0") → правильний шлях
