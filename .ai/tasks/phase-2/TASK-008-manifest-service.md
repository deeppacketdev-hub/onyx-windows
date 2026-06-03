# TASK-008: ManifestService

## Metadata
- **Phase**: 2 | **Dependencies**: TASK-003, TASK-006 | **LOC**: ~150
- **Source**: [ManifestService.swift](../../onyx/Onyx/Services/ManifestService.swift) — 129 рядків
- **Output**: `src/OnyxWindows/Services/ManifestService.cs`

## Objective
Fetch та кешування Mojang version manifest. 100% platform-independent логіка.

## Key Methods
- `LoadManifestAsync(cacheDir)` — завантажити з кешу, оновити якщо >24h
- `RefreshAsync(cacheDir)` — примусове оновлення
- `FilteredVersions(VersionFilter)` — фільтр за типом
- `FetchVersionDetailAsync(versionId, cacheDir)` — деталі конкретної версії

## URL: `https://launchermeta.mojang.com/mc/game/version_manifest_v2.json`

## Acceptance Criteria
- [ ] Manifest завантажується та кешується в `meta/mojang_manifest.json`
- [ ] Кеш перевіряється за віком (>24h → рефреш)
- [ ] VersionDetail завантажується та кешується в `cache/versions/{id}/{id}.json`
- [ ] Фільтрація за типом працює (release, snapshot, etc.)
