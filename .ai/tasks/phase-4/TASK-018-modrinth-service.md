# TASK-018: ModrinthService

## Metadata
- **Phase**: 4 | **Dependencies**: TASK-006 | **LOC**: ~500
- **Source**: [ModrinthService.swift](../../onyx/Onyx/Services/ModrinthService.swift) — 559 рядків

## Objective
Modrinth API client. 100% platform-independent — direct 1:1 port.

## Models to port
ModrinthSearchResult, ModrinthProject, ModrinthProjectDetail, ModrinthVersion,
ModrinthFile, ModrinthHashes, ModrinthGalleryImage, ModrinthLicense, DonationLink,
ModrinthSortIndex (enum), ModrinthProjectType (enum), ModrinthError

## Key Methods
- `SearchAsync(query, gameVersion, loader, projectType, sortBy, limit, offset)`
- `LoadMoreAsync()` — infinite scroll pagination
- `GetProjectDetailAsync(slugOrId)`
- `GetVersionsAsync(projectId, gameVersion, loader)`
- `FetchVersionsAsync(projectId, ...)` — without updating state
- `FetchProjectsAsync(ids)` — batch fetch by IDs
- `DownloadModAsync(file, directory)` — download to mods/resourcepacks/etc.

## API: `https://api.modrinth.com/v2/...`
User-Agent: "Onyx-Launcher/1.0 (contact@onyx.dev)"

## Changes: NONE. Direct port. All [JsonPropertyName] with snake_case.
