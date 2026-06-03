# TASK-004: ThemeManager

## Metadata
- **Phase**: 1 | **Dependencies**: TASK-001, TASK-002 (ThemeType, CustomThemeColors) | **LOC**: ~200
- **Source**: [ThemeManager.swift](../../onyx/Onyx/Services/ThemeManager.swift) — 156 рядків
- **Output**: `src/OnyxWindows/Services/ThemeManager.cs`, `Resources/Themes/DarkTheme.xaml`, `LightTheme.xaml`, `SharedStyles.xaml`

## Objective
Система тем з точними кольорами macOS версії. Підтримка System/Dark/Light/Custom + градієнтні фони.

## Exact Colors (дивись `.ai/THEME_COLORS.md`)
- Dark: Background=#0F1721, Surface=#1A2636, Accent=#4D8AEB
- Light: Background=#F5F7FA, Surface=#FFFFFF, Accent=#3878D9

## Key Changes
- System theme detection: Registry `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
- `NSApp.appearance` → ResourceDictionary swap: `Application.Current.Resources.MergedDictionaries`
- Color.FromHex() → custom extension method
- Gradient → `LinearGradientBrush` з angle mapping (дивись THEME_COLORS.md)

## Methods
- `Colors` property → поточна ThemePalette
- `LoadFrom(GlobalConfig)` — завантажити тему з конфігу
- `SaveTo(GlobalConfig)` — зберегти тему в конфіг
- `ApplyTheme()` — switch ResourceDictionary

## Acceptance Criteria
- [ ] 3 XAML ResourceDictionary файли створені (Dark, Light, Shared)
- [ ] Тема перемикається без перезапуску
- [ ] Custom theme з градієнтом працює
- [ ] System theme detection працює
