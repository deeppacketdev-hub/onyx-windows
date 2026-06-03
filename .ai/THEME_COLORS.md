# Onyx Theme Colors & Design Tokens

> Точні кольори з macOS версії. НІЧОГО не змінювати — UI має бути ідентичним.

## Dark Theme (Default)

```
Background:     #0F1721  (rgb: 15, 23, 33)     — Color(red: 0.06, green: 0.09, blue: 0.13)
Surface:        #1A2636  (rgb: 26, 38, 54)     — Color(red: 0.10, green: 0.15, blue: 0.21)
Accent:         #4D8AEB  (rgb: 77, 138, 235)   — Color(red: 0.30, green: 0.54, blue: 0.92)
PrimaryText:    #FFFFFF  (White)
SecondaryText:  #FFFFFF @ 55% opacity (#8C8C8C effective on dark)
```

### WPF Resource Dictionary
```xml
<Color x:Key="DarkBackground">#FF0F1721</Color>
<Color x:Key="DarkSurface">#FF1A2636</Color>
<Color x:Key="DarkAccent">#FF4D8AEB</Color>
<Color x:Key="DarkPrimaryText">#FFFFFFFF</Color>
<Color x:Key="DarkSecondaryText">#8CFFFFFF</Color>
<!-- 8C = 140/255 ≈ 55% opacity -->

<SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource DarkBackground}" />
<SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource DarkSurface}" />
<SolidColorBrush x:Key="AccentBrush" Color="{StaticResource DarkAccent}" />
<SolidColorBrush x:Key="PrimaryTextBrush" Color="{StaticResource DarkPrimaryText}" />
<SolidColorBrush x:Key="SecondaryTextBrush" Color="{StaticResource DarkSecondaryText}" />
```

## Light Theme

```
Background:     #F5F7FA  (rgb: 245, 247, 250)  — Color(red: 0.96, green: 0.97, blue: 0.98)
Surface:        #FFFFFF  (White)
Accent:         #3878D9  (rgb: 56, 120, 217)   — Color(red: 0.22, green: 0.47, blue: 0.85)
PrimaryText:    #1A1A1F  (rgb: 26, 26, 31)     — Color(red: 0.10, green: 0.10, blue: 0.12)
SecondaryText:  #666B73  (rgb: 102, 107, 115)   — Color(red: 0.40, green: 0.42, blue: 0.45)
```

## Custom Theme

Користувач вибирає hex-кольори для:
- `backgroundHex` — фон
- `surfaceHex` — панелі/карточки
- `accentHex` — кнопки/акценти

Опціонально:
- `enableGradient` (bool) — увімкнути градієнт на фоні
- `gradientStartHex`, `gradientEndHex` — кольори градієнта
- `gradientAngle` (int: 0, 90, 135, 180) — напрямок

### CustomThemeColors struct (з macOS)
```
backgroundHex: String = "#0F1721"
surfaceHex: String = "#1A2636"
accentHex: String = "#4D8AEB"
enableGradient: Bool = false
gradientStartHex: String = "#1a1a2e"
gradientEndHex: String = "#16213e"
gradientAngle: Int = 135
```

### Gradient Angles → LinearGradientBrush
| Angle | SwiftUI | WPF StartPoint → EndPoint |
|-------|---------|--------------------------|
| 0     | .leading → .trailing | (0,0.5) → (1,0.5) |
| 90    | .top → .bottom | (0.5,0) → (0.5,1) |
| 135   | .topLeading → .bottomTrailing | (0,0) → (1,1) |
| 180   | .trailing → .leading | (1,0.5) → (0,0.5) |

## Layout Tokens

```
Window Min Size:     900 × 600
Window Default:      1100 × 720
Sidebar Width:       200px
Corner Radius:       8px (cards), 12px (large cards), 6px (buttons, inputs)
Card Padding:        16px
Section Padding:     20px
Spacing (small):     8px
Spacing (medium):    12px
Spacing (large):     16px
Shadow:              Offset(0, 2), Blur=8, Color=#40000000 (black 25%)
Sidebar Shadow:      Offset(2, 0), Blur=8, Color=#40000000
```

## Typography

macOS використовує системний шрифт (SF Pro). На Windows використовуємо **Segoe UI Variable** (Windows 11) або **Segoe UI** (Windows 10):

```
Title:              20px, SemiBold (FontWeight=600)
Subtitle:           16px, SemiBold
Body:               14px, Regular
Caption:            12px, Regular
Small:              11px, Regular (secondary info)
Monospace (Console): Cascadia Code / Consolas, 12px
```

## Instance Card Design

```
┌─────────────────────┐
│  ╔═══════════════╗  │ ← Version artwork (full width, 120px height)
│  ║   MC 1.21.4   ║  │
│  ╚═══════════════╝  │
│                     │
│  Instance Name      │ ← 14px, SemiBold, PrimaryText
│  1.21.4 • Fabric    │ ← 12px, SecondaryText
│                     │
│  🟢 Ready           │ ← Status dot + text
│  ⏱ 12h 34m         │ ← Play time
│                     │
│  [▶ Play]           │ ← Accent button (on hover)
└─────────────────────┘
Card width:  ~180px
Card height: ~240px
Background:  Surface color
Border:      none (shadow only)
Hover:       scale(1.02) + shadow increase
```

## Status Colors

```
Ready:       #4ADE80 (green)
Running:     #4D8AEB (accent blue)
Preparing:   #FBBF24 (yellow/amber)
Downloading: #FBBF24 (yellow/amber)
Error:       #EF4444 (red)
Stopped:     #6B7280 (gray)
```

## Icon Set (Segoe Fluent Icons)

Для sidebar та кнопок використовуємо Segoe Fluent Icons:
```
Instances:     \uF0E2 (Grid)
Skins:         \uE77B (Contact)
Screenshots:   \uEB9F (Photo)
Worlds:        \uE707 (Map)
News:          \uE7BF (News)
Settings:      \uE713 (Settings)
Search:        \uE721
Add:           \uE710
Close:         \uE711
Play:          \uE768
Stop:          \uE71A
Folder:        \uE8B7
Delete:        \uE74D
Copy:          \uE8C8
Edit:          \uE70F
Download:      \uE896
Check:         \uE73E
Warning:       \uE7BA
```

## Animations

### Card Hover (ScaleTransform)
```xml
<EventTrigger RoutedEvent="MouseEnter">
    <BeginStoryboard>
        <Storyboard>
            <DoubleAnimation Storyboard.TargetProperty="RenderTransform.ScaleX"
                             To="1.02" Duration="0:0:0.15" />
            <DoubleAnimation Storyboard.TargetProperty="RenderTransform.ScaleY"
                             To="1.02" Duration="0:0:0.15" />
        </Storyboard>
    </BeginStoryboard>
</EventTrigger>
```

### Page Transition (FadeIn)
```xml
<DoubleAnimation Storyboard.TargetProperty="Opacity"
                 From="0" To="1" Duration="0:0:0.2"
                 EasingFunction="{StaticResource EaseOut}" />
```

### Progress Bar
```xml
<DoubleAnimation Storyboard.TargetProperty="Value"
                 Duration="0:0:0.3"
                 EasingFunction="{StaticResource EaseInOut}" />
```
