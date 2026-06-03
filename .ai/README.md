# 🤖 Onyx Windows — AI Agent Workspace

## Що це за проект?

**Onyx** — це Minecraft Launcher для macOS, написаний на Swift + SwiftUI.
Мета цього репозиторію — **повністю перенести** Onyx на **Windows** з використанням **C# 12 + WPF + .NET 8**.

## Структура AI-файлів

```
.ai/
├── README.md                    ← Ти тут
├── PROJECT_CONTEXT.md           ← Повний опис проекту, архітектура, залежності
├── ARCHITECTURE_MAP.md          ← Маппінг Swift → C# (що чим замінювати)
├── STYLE_GUIDE.md               ← Code style, naming conventions, patterns
├── THEME_COLORS.md              ← Точні кольори, шрифти, відступи з macOS версії
├── tasks/
│   ├── TASK_INDEX.md            ← Мастер-список всіх тасків з залежностями
│   ├── phase-1/
│   │   ├── TASK-001-project-setup.md
│   │   ├── TASK-002-models.md
│   │   ├── TASK-003-app-data-manager.md
│   │   ├── TASK-004-theme-manager.md
│   │   ├── TASK-005-localization.md
│   │   ├── TASK-006-network-config.md
│   │   └── TASK-007-credential-store.md
│   ├── phase-2/
│   │   ├── TASK-008-manifest-service.md
│   │   ├── TASK-009-download-manager.md
│   │   ├── TASK-010-java-manager.md
│   │   ├── TASK-011-instance-builder.md
│   │   └── TASK-012-instance-store.md
│   ├── phase-3/
│   │   ├── TASK-013-launch-controller.md
│   │   ├── TASK-014-fabric-installer.md
│   │   ├── TASK-015-forge-installer.md
│   │   └── TASK-016-mod-loader-service.md
│   ├── phase-4/
│   │   ├── TASK-017-account-manager.md
│   │   ├── TASK-018-modrinth-service.md
│   │   ├── TASK-019-curseforge-service.md
│   │   └── TASK-020-skin-manager.md
│   ├── phase-5/
│   │   ├── TASK-021-nbt-service.md
│   │   ├── TASK-022-world-service.md
│   │   ├── TASK-023-screenshot-service.md
│   │   ├── TASK-024-news-service.md
│   │   ├── TASK-025-discord-rpc.md
│   │   ├── TASK-026-instance-transfer.md
│   │   ├── TASK-027-update-service.md
│   │   ├── TASK-028-version-artwork.md
│   │   └── TASK-029-telemetry-service.md
│   └── phase-6/
│       ├── TASK-030-main-window.md
│       ├── TASK-031-sidebar.md
│       ├── TASK-032-instance-views.md
│       ├── TASK-033-mod-browser.md
│       ├── TASK-034-skin-browser.md
│       ├── TASK-035-screenshots-gallery.md
│       ├── TASK-036-worlds-views.md
│       ├── TASK-037-news-view.md
│       ├── TASK-038-settings-view.md
│       ├── TASK-039-accounts-views.md
│       ├── TASK-040-onboarding.md
│       ├── TASK-041-console-view.md
│       └── TASK-042-converters-helpers.md
└── progress.md                  ← Лог прогресу виконання
```

## Як працювати з тасками

1. **Прочитай `PROJECT_CONTEXT.md`** — зрозумій проект
2. **Прочитай `ARCHITECTURE_MAP.md`** — зрозумій маппінг Swift → C#
3. **Прочитай `STYLE_GUIDE.md`** — дотримуйся code style
4. **Відкрий `tasks/TASK_INDEX.md`** — знайди наступний незавершений таск
5. **Відкрий конкретний таск** — виконай його крок за кроком
6. **Оновлюй `progress.md`** — після кожного завершеного таску

## Де знаходиться вихідний код macOS

```
onyx/Onyx/          ← Swift source code (REFERENCE ONLY, не чіпати!)
├── App/            ← Entry point, ContentView
├── Models/         ← Data models
├── Services/       ← Business logic (27 сервісів)
├── Views/          ← SwiftUI views (12 груп)
└── Resources/      ← Assets, icons
```

## Де створювати код Windows

```
src/OnyxWindows/    ← C# WPF project (СТВОРЮВАТИ ТУТ)
├── Models/
├── ViewModels/
├── Services/
├── Views/
├── Converters/
├── Helpers/
└── Resources/
```
