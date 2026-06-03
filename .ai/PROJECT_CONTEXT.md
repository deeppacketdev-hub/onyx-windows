# Onyx Windows — Project Context

> Цей файл містить ВСЮ необхідну інформацію про проект для AI-агента.
> Прочитай його ПОВНІСТЮ перед початком будь-якого таску.

## Що таке Onyx?

**Onyx** — це кастомний Minecraft Launcher, оригінально написаний для macOS на **Swift 5.10 + SwiftUI**.
Мета цього репо — створити **ідентичну Windows версію** на **C# 12 + WPF + .NET 8**.

## Ключові фічі лаунчера

1. **Instance Management** — створення/видалення/дублювання збірок Minecraft
2. **Multi-version Support** — Vanilla, Fabric, Quilt, Forge, NeoForge
3. **Mod Browser** — пошук і встановлення модів з Modrinth + CurseForge
4. **Account System** — Microsoft OAuth + Offline акаунти
5. **Skin System** — браузер скінів, завантаження, призначення через CustomSkinLoader
6. **World Editor** — редагування level.dat через NBT парсер
7. **Screenshots Gallery** — перегляд скріншотів з усіх інстансів
8. **News Feed** — новини з minecraft.net
9. **Discord Rich Presence** — статус гри через IPC
10. **Instance Export/Import** — ZIP архіви для обміну збірками
11. **Auto-Update** — через Sparkle (macOS) → Velopack (Windows)
12. **Theme System** — Dark/Light/Custom з градієнтами
13. **Localization** — EN, UK, DE, ES, FR, PL (2680 рядків перекладів)
14. **Java Manager** — автоматичне завантаження Java з Adoptium

## Архітектура macOS версії

### Патерн: Observable Objects + Environment DI

```swift
// Swift: @Observable + @Environment для DI
@Observable final class LaunchController { ... }

// Ін'єкція в SwiftUI:
@main struct OnyxApp: App {
    @State private var launchController = LaunchController()
    var body: some Scene {
        WindowGroup { ContentView().environment(launchController) }
    }
}
```

### Файлова структура macOS (onyx/Onyx/)

```
App/
├── OnyxApp.swift              — Entry point, DI hub (126 рядків)
└── ContentView.swift          — Main layout: sidebar + content (302 рядків)

Models/ (6 файлів, ~850 рядків)
├── GlobalConfig.swift         — Глобальні налаштування (121 рядок)
├── Instance.swift             — Модель збірки (166 рядків)
├── Account.swift              — Модель акаунта (85 рядків)
├── VersionManifest.swift      — Mojang version JSON models (185 рядків)
├── WorldSettings.swift        — Налаштування світу + NBT маппінг (285 рядків)
└── VersionFilter.swift        — Enum фільтрів (9 рядків)

Services/ (27 файлів, ~7,300 рядків)
├── AppDataManager.swift       — Структура директорій (104 рядки)
├── ThemeManager.swift         — Теми Dark/Light/Custom (142 рядки)
├── InstanceStore.swift        — CRUD інстансів (246 рядків)
├── InstanceBuilder.swift      — Download list + classpath (164 рядки)
├── LaunchController.swift     — Запуск Minecraft (955 рядків) ← НАЙСКЛАДНІШИЙ
├── AccountManager.swift       — MS OAuth + Keychain (547 рядків)
├── ManifestService.swift      — Mojang version manifest (129 рядків)
├── DownloadManager.swift      — Паралельний завантажувач (160 рядків)
├── JavaManager.swift          — Авто-завантаження Java (175 рядків)
├── FabricInstaller.swift      — Fabric/Quilt loader (109 рядків)
├── ForgeInstaller.swift       — Forge/NeoForge installer (225 рядків)
├── ModLoaderService.swift     — Версії mod loaders (247 рядків)
├── ModrinthService.swift      — Modrinth API (484 рядки)
├── CurseForgeService.swift    — CurseForge API (328 рядків)
├── SkinManager.swift          — Скіни + CustomSkinLoader (503 рядки)
├── ScreenshotService.swift    — Галерея скріншотів (150 рядків)
├── WorldService.swift         — Читання level.dat (433 рядки)
├── NBTService.swift           — NBT парсер/writer (354 рядки)
├── NewsService.swift          — Новини minecraft.net (62 рядки)
├── DiscordRPCService.swift    — Discord RPC через Unix socket (162 рядки)
├── InstanceTransferService.swift — Export/Import ZIP (550 рядків)
├── UpdateService.swift        — Sparkle auto-update (199 рядків)
├── VersionArtworkService.swift— Іконки версій MC (237 рядків)
├── TelemetryService.swift     — Аналітика (37 рядків)
├── NetworkConfig.swift        — URLSession config (10 рядків)
├── Localization.swift         — 2680 рядків перекладів
└── ModLoaderService.swift     — Версії завантажувачів (247 рядків)

Views/ (12 груп, ~8,500 рядків)
├── Sidebar/SidebarView.swift
├── Instances/ (6 файлів: Grid, Card, Create, Settings, Export, EmptyState)
├── Mods/ (2 файли: ModBrowser 2049!, InstalledContent)
├── Skins/SkinBrowserPanel.swift
├── Screenshots/ScreenshotsGalleryView.swift
├── Worlds/ (2 файли: Gallery, Settings)
├── News/NewsView.swift
├── Settings/ (2 файли: GlobalSettings, UpdateBanner)
├── Accounts/ (5 файлів: Switcher, Avatar, SkinPicker, AddOffline, MSAuth)
├── Onboarding/OnboardingView.swift
├── Console/ConsoleView.swift
└── Components/ (2 файли: DownloadProgress, VisualEffectBackground)
```

## Шляхи зберігання даних

### macOS (REFERENCE)
```
~/Library/Application Support/com.onyx.launcher/
├── config.json
├── accounts.json
├── java/temurin-{ver}-{arch}/
├── cache/
│   ├── assets/indexes/, assets/objects/
│   ├── libraries/
│   └── versions/{id}/{id}.json, {id}.jar
├── instances/{name}/
│   ├── instance.json
│   └── .minecraft/ (mods/, saves/, resourcepacks/, shaderpacks/)
├── icons/
├── skins/ (library.json + PNG files)
└── meta/ (mojang_manifest.json)
```

### Windows (TARGET — ІДЕНТИЧНА СТРУКТУРА)
```
%APPDATA%\OnyxLauncher\
├── config.json
├── accounts.json
├── credentials\{accountId}.dat     ← NEW (DPAPI замість Keychain)
├── java\temurin-{ver}-{arch}\
├── cache\
│   ├── assets\indexes\, assets\objects\
│   ├── libraries\
│   └── versions\{id}\{id}.json, {id}.jar
├── instances\{name}\
│   ├── instance.json
│   └── .minecraft\ (mods\, saves\, resourcepacks\, shaderpacks\)
├── icons\
├── skins\ (library.json + PNG files)
└── meta\ (mojang_manifest.json)
```

## Зовнішні API (однакові на обох платформах)

| API | URL | Для чого |
|-----|-----|----------|
| Mojang Manifest | `https://launchermeta.mojang.com/mc/game/version_manifest_v2.json` | Список версій MC |
| Mojang Assets | `https://resources.download.minecraft.net/{hash_prefix}/{hash}` | Game assets |
| Adoptium | `https://api.adoptium.net/v3/binary/latest/{ver}/ga/{os}/{arch}/jdk/hotspot/normal/eclipse` | Java JDK |
| Fabric Meta | `https://meta.fabricmc.net/v2/versions/loader/{mcVer}` | Fabric versions |
| Quilt Meta | `https://meta.quiltmc.org/v3/versions/loader/{mcVer}` | Quilt versions |
| Forge Maven | `https://maven.minecraftforge.net/...` | Forge installer |
| NeoForge Maven | `https://maven.neoforged.net/...` | NeoForge installer |
| Modrinth | `https://api.modrinth.com/v2/...` | Mods search/download |
| CurseForge | `https://api.curseforge.com/v1/...` | Mods search/download (needs API key) |
| Mojang Auth | `https://api.mojang.com/...` | Username → UUID |
| MS OAuth | `https://login.live.com/oauth20_authorize.srf` | Microsoft login |
| Xbox Live | `https://user.auth.xboxlive.com/user/authenticate` | Xbox token |
| XSTS | `https://xsts.auth.xboxlive.com/xsts/authorize` | XSTS token |
| MC Services | `https://api.minecraftservices.com/...` | MC access token |
| mc-heads.net | `https://mc-heads.net/skin/{name}`, `/body/{name}`, `/avatar/{name}` | Skin renders |

## Критичні значення (HARDCODED)

```
MS OAuth Client ID:     "00000000402b5328"
MS OAuth Redirect:      "https://login.live.com/oauth20_desktop.srf"
CurseForge API Key:     "$2a$10$GqkoXk2OSamTNQaAabwTweQ4znpO7YfZIxsyB7wjA2R38JTEc0kY6"
Discord App ID:         "1506949090498318437"
Modrinth User-Agent:    "Onyx-Launcher/1.0 (contact@onyx.dev)"
Download Concurrency:   32 parallel downloads
URLSession Timeout:     30s request, 120s resource
Default RAM:            4096 MB
```
