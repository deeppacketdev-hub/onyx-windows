# TASK-030: MainWindow + Shell

## Metadata
- **Phase**: 6 | **Dependencies**: ALL services (Phase 1-5) | **LOC**: ~300
- **Source**: [ContentView.swift](../../onyx/Onyx/App/ContentView.swift) — 302 lines
- **Output**: `MainWindow.xaml`, `Views/Components/TopBarView.xaml`

## Objective
Custom chrome window з sidebar + content area. Акрилік/Mica ефект.

## Layout
```
┌─────────┬──────────────────────────────────┐
│         │  TopBar (search + account + add) │
│ Sidebar │──────────────────────────────────│
│  200px  │  ContentPresenter               │
│         │  (bound to SelectedSection)      │
└─────────┴──────────────────────────────────┘
```

## Key Features
- Custom WindowChrome (CaptionHeight=0, ResizeBorderThickness=4)
- Min 900x600, Default 1100x720
- Sidebar shadow (right edge)
- Download overlay (semi-transparent black + centered panel)
- Import overlay with progress
- Acrylic/Mica via WindowHelper (SetWindowCompositionAttribute)

## See: `.ai/THEME_COLORS.md` for exact layout tokens
