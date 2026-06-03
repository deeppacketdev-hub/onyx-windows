# TASK-032: Instance Views
- **Phase**: 6 | **LOC**: ~1200
- **Sources**: InstanceGridView (385), InstanceCardView (290), CreateInstanceView (313), InstanceSettingsView (619), ExportInstanceView (319), EmptyStateView (35)
- Grid: WrapPanel of InstanceCards. Context menu. Drag-and-drop import.
- Card: 180x240, version artwork, name, version badge, status, play time, hover scale effect
- Create: Modal dialog with MC version picker, mod loader picker, RAM slider
- Settings: Tabs (General, Java, Mods, RP, Shaders, Worlds)
- Export: Checkboxes + SaveFileDialog
- See THEME_COLORS.md for card design specs
