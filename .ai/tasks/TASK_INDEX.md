# Task Index — Onyx Windows Migration

> Master checklist. AI-агент повинен виконувати таски **по порядку**, дотримуючись залежностей.

## Як працювати

1. Знайди перший `[ ]` таск
2. Відкрий його файл у `tasks/phase-N/`
3. Виконай всі кроки з файлу
4. Познач `[x]` тут і в `progress.md`
5. Переходь до наступного

---

## Phase 1: Foundation (Проект + Моделі + Core Services)

> Залежності: немає. Починай звідси.

- [ ] **TASK-001** — [Project Setup](tasks/phase-1/TASK-001-project-setup.md) — Створити .NET 8 WPF проект, NuGet пакети, структуру папок
- [ ] **TASK-002** — [Models](tasks/phase-1/TASK-002-models.md) — Портувати всі моделі: Instance, Account, GlobalConfig, VersionManifest, WorldSettings, enums
- [ ] **TASK-003** — [AppDataManager](tasks/phase-1/TASK-003-app-data-manager.md) — Менеджер директорій, конфігурації, Maven paths
- [ ] **TASK-004** — [ThemeManager](tasks/phase-1/TASK-004-theme-manager.md) — Dark/Light/Custom теми, ResourceDictionary switching
- [ ] **TASK-005** — [Localization](tasks/phase-1/TASK-005-localization.md) — Портувати 2680 рядків перекладів (EN/UK/DE/ES/FR/PL)
- [ ] **TASK-006** — [NetworkConfig](tasks/phase-1/TASK-006-network-config.md) — HttpClient factory, timeouts, User-Agent
- [ ] **TASK-007** — [CredentialStore](tasks/phase-1/TASK-007-credential-store.md) — DPAPI encrypted token storage (замість Keychain)

## Phase 2: Download Engine + Minecraft Core

> Залежності: Phase 1 повністю завершена

- [ ] **TASK-008** — [ManifestService](tasks/phase-2/TASK-008-manifest-service.md) — Mojang version manifest fetch/cache
- [ ] **TASK-009** — [DownloadManager](tasks/phase-2/TASK-009-download-manager.md) — Паралельний завантажувач з SHA1 верифікацією
- [ ] **TASK-010** — [JavaManager](tasks/phase-2/TASK-010-java-manager.md) — Автоматичне завантаження Java з Adoptium
- [ ] **TASK-011** — [InstanceBuilder](tasks/phase-2/TASK-011-instance-builder.md) — Build download list, classpath, natives для Windows
- [ ] **TASK-012** — [InstanceStore](tasks/phase-2/TASK-012-instance-store.md) — CRUD операції з інстансами

## Phase 3: Launch Engine + Mod Loaders

> Залежності: Phase 2 повністю завершена

- [ ] **TASK-013** — [LaunchController](tasks/phase-3/TASK-013-launch-controller.md) — ⚠️ НАЙСКЛАДНІШИЙ ТАСК (955 рядків) — запуск Minecraft
- [ ] **TASK-014** — [FabricInstaller](tasks/phase-3/TASK-014-fabric-installer.md) — Fabric/Quilt mod loader installation
- [ ] **TASK-015** — [ForgeInstaller](tasks/phase-3/TASK-015-forge-installer.md) — Forge/NeoForge installer runner
- [ ] **TASK-016** — [ModLoaderService](tasks/phase-3/TASK-016-mod-loader-service.md) — Fetch available mod loader versions

## Phase 4: Account System + Mod Browsing

> Залежності: Phase 1 (CredentialStore), Phase 2 (DownloadManager)

- [ ] **TASK-017** — [AccountManager](tasks/phase-4/TASK-017-account-manager.md) — MS OAuth flow, Xbox Live, XSTS, token refresh
- [ ] **TASK-018** — [ModrinthService](tasks/phase-4/TASK-018-modrinth-service.md) — Modrinth API: search, versions, download
- [ ] **TASK-019** — [CurseForgeService](tasks/phase-4/TASK-019-curseforge-service.md) — CurseForge API: search, files, download
- [ ] **TASK-020** — [SkinManager](tasks/phase-4/TASK-020-skin-manager.md) — Skin browser, library, CustomSkinLoader

## Phase 5: Additional Services

> Залежності: Phase 2 (InstanceStore), Phase 1 (AppDataManager)

- [ ] **TASK-021** — [NBTService](tasks/phase-5/TASK-021-nbt-service.md) — NBT binary parser/writer
- [ ] **TASK-022** — [WorldService](tasks/phase-5/TASK-022-world-service.md) — World list, level.dat reading
- [ ] **TASK-023** — [ScreenshotService](tasks/phase-5/TASK-023-screenshot-service.md) — Screenshot scanning, preview
- [ ] **TASK-024** — [NewsService](tasks/phase-5/TASK-024-news-service.md) — Minecraft.net news feed
- [ ] **TASK-025** — [DiscordRPC](tasks/phase-5/TASK-025-discord-rpc.md) — Discord Rich Presence via Named Pipes
- [ ] **TASK-026** — [InstanceTransfer](tasks/phase-5/TASK-026-instance-transfer.md) — Export/Import as ZIP
- [ ] **TASK-027** — [UpdateService](tasks/phase-5/TASK-027-update-service.md) — Auto-update (Velopack or GitHub Releases)
- [ ] **TASK-028** — [VersionArtwork](tasks/phase-5/TASK-028-version-artwork.md) — MC version icons/artwork
- [ ] **TASK-029** — [TelemetryService](tasks/phase-5/TASK-029-telemetry-service.md) — Analytics signals

## Phase 6: UI/UX (WPF Views)

> Залежності: Phase 1-5 (всі сервіси та ViewModels)

- [ ] **TASK-030** — [MainWindow + Shell](tasks/phase-6/TASK-030-main-window.md) — Custom chrome, layout, DI wiring
- [ ] **TASK-031** — [Sidebar](tasks/phase-6/TASK-031-sidebar.md) — Navigation sidebar
- [ ] **TASK-032** — [Instance Views](tasks/phase-6/TASK-032-instance-views.md) — Grid, Card, Create, Settings, Export, EmptyState
- [ ] **TASK-033** — [Mod Browser](tasks/phase-6/TASK-033-mod-browser.md) — Modrinth + CurseForge browser UI
- [ ] **TASK-034** — [Skin Browser](tasks/phase-6/TASK-034-skin-browser.md) — Skin gallery, search, download
- [ ] **TASK-035** — [Screenshots](tasks/phase-6/TASK-035-screenshots-gallery.md) — Screenshot gallery with fullscreen view
- [ ] **TASK-036** — [Worlds Views](tasks/phase-6/TASK-036-worlds-views.md) — World gallery + settings editor
- [ ] **TASK-037** — [News View](tasks/phase-6/TASK-037-news-view.md) — News cards
- [ ] **TASK-038** — [Settings View](tasks/phase-6/TASK-038-settings-view.md) — Global settings + update banner
- [ ] **TASK-039** — [Account Views](tasks/phase-6/TASK-039-accounts-views.md) — Switcher, MS Auth, Offline, Avatar, SkinPicker
- [ ] **TASK-040** — [Onboarding](tasks/phase-6/TASK-040-onboarding.md) — First-run wizard
- [ ] **TASK-041** — [Console View](tasks/phase-6/TASK-041-console-view.md) — Game log window
- [ ] **TASK-042** — [Converters & Helpers](tasks/phase-6/TASK-042-converters-helpers.md) — Value converters, WindowHelper, ColorHelper

---

## Post-Migration

- [ ] **TASK-043** — Integration Testing — запустити кожен сценарій вручну
- [ ] **TASK-044** — Performance Optimization — profiling, memory, startup time
- [ ] **TASK-045** — Installer/Packaging — MSIX або Inno Setup
