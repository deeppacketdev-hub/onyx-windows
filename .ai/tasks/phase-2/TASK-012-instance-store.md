# TASK-012: InstanceStore

## Metadata
- **Phase**: 2 | **Dependencies**: TASK-002, TASK-003 | **LOC**: ~280
- **Source**: [InstanceStore.swift](../../onyx/Onyx/Services/InstanceStore.swift) — 246 рядків
- **Output**: `src/OnyxWindows/Services/InstanceStore.cs`

## Objective
CRUD операції з інстансами на диску. Ідентична логіка до macOS.

## Methods (всі портуються 1:1)
- `LoadInstances(directory)` — сканувати instances/, десеріалізувати instance.json
- `CreateInstance(instance, rootDir)` — створити папку + .minecraft/{mods,saves,...}
- `SaveInstance(instance)` — оновити instance.json
- `DeleteInstance(instance)` — видалити папку
- `DuplicateInstance(instance)` — deep copy всіх файлів, новий UUID
- `RenameInstance(instance, newName)` — переіменувати папку + оновити JSON
- `MinecraftDirectory(instance)` → path to .minecraft
- `ModsDirectory`, `ShaderpacksDirectory`, `ResourcepacksDirectory`, `SavesDirectory`
- Auto-install CustomSkinLoader при створенні (з Modrinth API)

## Key Notes
- Reset stale statuses при завантаженні (running/preparing → ready)
- Name deduplication: "Name (1)", "Name (2)" etc.
- ISO 8601 dates для createdAt/lastPlayedAt
- Sorted by createdAt descending

## Acceptance Criteria
- [ ] Інстанси завантажуються з instances/ директорії
- [ ] Створення інстансу генерує правильну структуру папок
- [ ] Дублювання копіює всі файли з новим UUID
- [ ] Видалення очищує всю папку
