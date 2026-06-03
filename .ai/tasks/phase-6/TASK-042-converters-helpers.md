# TASK-042: Converters and Helpers
- **Phase**: 6 | **LOC**: ~300

## Converters (IValueConverter for XAML bindings)
- `BoolToVisibilityConverter` — bool → Visibility.Visible/Collapsed
- `InverseBoolToVisibilityConverter` — inverted
- `HexToColorConverter` — hex string → SolidColorBrush
- `TimeSpanToStringConverter` — seconds → "12h 34m" format
- `FileSizeConverter` — bytes → "1.5 MB" format
- `EnumToStringConverter` — enum → localized display name
- `NullToVisibilityConverter` — null → Collapsed

## Helpers
- `ColorHelper.cs` — hex parsing, color manipulation
- `WindowHelper.cs` — Mica/Acrylic effect, custom chrome setup, SetWindowCompositionAttribute
- `RelayCommand.cs` / `AsyncRelayCommand.cs` — (or use CommunityToolkit.Mvvm built-in)
