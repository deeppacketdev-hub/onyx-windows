# TASK-010: JavaManager

## Metadata
- **Phase**: 2 | **Dependencies**: TASK-003 | **LOC**: ~180
- **Source**: [JavaManager.swift](../../onyx/Onyx/Services/JavaManager.swift) — 175 рядків
- **Output**: `src/OnyxWindows/Services/JavaManager.cs`

## Objective
Автоматичне завантаження Java JDK з Adoptium для Windows.

## ⚠️ Critical Platform Changes
| macOS | Windows |
|-------|---------|
| Architecture: `uname()` → aarch64/x64 | `RuntimeInformation.OSArchitecture` |
| OS param: `mac` | `windows` |
| Archive: `.tar.gz` | `.zip` |
| Extract: `/usr/bin/tar -xzf` | `ZipFile.ExtractToDirectory()` |
| Java path: `Contents/Home/bin/java` | `bin\java.exe` |
| chmod +x: потрібен | НЕ потрібен |
| xattr -cr: потрібен | НЕ потрібен |
| Java 8 на ARM: fallback x64 (Rosetta) | x64 завжди (ARM Windows рідкість) |

## Adoptium API URL
```
https://api.adoptium.net/v3/binary/latest/{majorVersion}/ga/windows/{arch}/jdk/hotspot/normal/eclipse
```

## Java Executable Lookup Paths (Windows)
```
{versionDir}/bin/java.exe
```

## Methods
- `GetJavaExecutableAsync(int majorVersion)` — отримати шлях, завантажити якщо потрібно
- `IsJavaAvailable(int majorVersion)` — чи вже встановлена
- `InstalledVersions()` — список встановлених

## Acceptance Criteria
- [ ] Java 21 завантажується з Adoptium
- [ ] ZIP розпаковується правильно
- [ ] `java.exe` знаходиться після розпакування
- [ ] Повторний виклик не завантажує знову (кеш)
