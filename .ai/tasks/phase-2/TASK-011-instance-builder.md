# TASK-011: InstanceBuilder

## Metadata
- **Phase**: 2 | **Dependencies**: TASK-002 (VersionManifest), TASK-003 | **LOC**: ~200
- **Source**: [InstanceBuilder.swift](../../onyx/Onyx/Services/InstanceBuilder.swift) — 164 рядки
- **Output**: `src/OnyxWindows/Services/InstanceBuilder.cs`

## Objective
Побудова download list, classpath та нативних бібліотек для Windows.

## ⚠️ Critical Platform Changes

### shouldIncludeLibrary() — КЛЮЧОВА ЗМІНА
```csharp
// macOS: os.name == "osx" || os.name == "macos", arch == "aarch64" || "arm64"
// Windows:
private bool ShouldIncludeLibrary(Library library)
{
    // os.name == "windows", arch == null || "x64" || "x86"
    if (rule.Os?.Name != null && rule.Os.Name != "windows") ruleMatches = false;
    if (rule.Os?.Arch != null && rule.Os.Arch != "x64") ruleMatches = false;
}
```

### nativeLibraries() — КЛЮЧОВА ЗМІНА
```csharp
// macOS: natives["osx"] ?? natives["macos"], name.contains("natives-macos")
// Windows:
if (natives.ContainsKey("windows")) classifierKey = natives["windows"];
if (library.Name.Contains("natives-windows")) // modern LWJGL
```

### buildClasspath() — КЛЮЧОВА ЗМІНА
```csharp
// macOS: paths.joined(separator: ":")
// Windows:
return string.Join(";", paths);  // SEMICOLON on Windows!
```

## Methods
- `BuildDownloadList(VersionDetail)` → `List<DownloadItem>`
- `BuildAssetDownloadList(assetIndexId)` → `List<DownloadItem>`
- `RequiredJavaVersion(VersionDetail)` → `int`
- `BuildClasspath(VersionDetail)` → `string` (with `;` separator)
- `NativeLibraries(VersionDetail)` → `List<LibraryArtifact>`

## Acceptance Criteria
- [ ] Download list будується для vanilla 1.21.4
- [ ] Classpath використовує `;` замість `:`
- [ ] Нативні бібліотеки фільтруються для `windows` / `natives-windows`
- [ ] OS rules фільтрують `os.name == "windows"`
