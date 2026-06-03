# TASK-005: Localization Service

## Metadata
- **Phase**: 1 | **Dependencies**: TASK-001, TASK-002 (AppLanguage enum) | **LOC**: ~2800
- **Source**: [Localization.swift](../../onyx/Onyx/Services/Localization.swift) — 2680 рядків!
- **Output**: `src/OnyxWindows/Services/LocalizationService.cs`

## Objective
Портувати ПОВНІСТЮ 2680 рядків перекладів. 6 мов: EN, UK, DE, ES, FR, PL.

## Implementation

```csharp
public class LocalizationService : ObservableObject
{
    [ObservableProperty] private AppLanguage _currentLanguage = AppLanguage.En;

    private readonly Dictionary<string, Dictionary<AppLanguage, string>> _strings = new();

    public string T(string key) =>
        _strings.TryGetValue(key, out var langs) &&
        langs.TryGetValue(CurrentLanguage, out var value) ? value : key;

    public LocalizationService() { InitializeStrings(); }

    private void InitializeStrings()
    {
        // Портувати ВСІ рядки з Localization.swift
        Add("play", en: "Play", uk: "Грати", de: "Spielen", es: "Jugar", fr: "Jouer", pl: "Graj");
        Add("settings", en: "Settings", uk: "Налаштування", ...);
        // ... 2680 рядків
    }
}
```

## ⚠️ Важливо
- **НЕ ПРОПУСКАЙ жоден рядок** — все має бути портовано
- Ключі залишаються як є (lowercase snake_case з Swift)
- Відкрий Localization.swift і портуй блоками по 100 рядків

## Acceptance Criteria
- [ ] Всі ключі перекладів присутні
- [ ] T("play") повертає "Грати" коли мова UK
- [ ] Зміна мови оновлює всі bindings
