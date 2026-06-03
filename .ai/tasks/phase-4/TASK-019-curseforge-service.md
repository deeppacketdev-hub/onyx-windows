# TASK-019: CurseForgeService

## Metadata
- **Phase**: 4 | **Dependencies**: TASK-006 | **LOC**: ~380
- **Source**: [CurseForgeService.swift](../../onyx/Onyx/Services/CurseForgeService.swift) — 383 рядки

## Objective
CurseForge API client. 100% platform-independent — direct 1:1 port.

## API Key: `$2a$10$GqkoXk2OSamTNQaAabwTweQ4znpO7YfZIxsyB7wjA2R38JTEc0kY6`
Header: `x-api-key`

## Models: CFSearchResponse, CFMod, CFFile, CFPagination, CFLinks, CFImage, CFAuthor, CFCategory

## Key Methods
- `SearchAsync(query, gameVersion, loader, classId, limit, sortField, categoryId)`
- `LoadMoreAsync()`
- `GetFilesAsync(modId, gameVersion, loader)`
- `DownloadFileAsync(file, directory)` — з CDN fallback для restricted mods

## CDN Fallback (ідентична логіка)
```csharp
// Для модів з downloadUrl == null
var part1 = fileId.ToString()[..4];
var part2 = fileId.ToString()[4..];
var url = $"https://edge.forgecdn.net/files/{part1}/{part2}/{fileName}";
```

## Changes: NONE. Direct port.
