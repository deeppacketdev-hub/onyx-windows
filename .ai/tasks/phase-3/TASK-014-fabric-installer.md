# TASK-014: FabricInstaller

## Metadata
- **Phase**: 3 | **Dependencies**: TASK-003, TASK-006 | **LOC**: ~120
- **Source**: [FabricInstaller.swift](../../onyx/Onyx/Services/FabricInstaller.swift) — 109 рядків

## Objective
Fabric/Quilt loader installation. Логіка 100% ідентична macOS — тільки API виклики.

## Key Method
`PrepareLoaderAsync(mcVersion, loaderVersion, isQuilt)` → returns (mainClass, jvmArgs, classpath)

## APIs
- Fabric: `https://meta.fabricmc.net/v2/versions/loader/{mc}/{loader}/profile/json`
- Quilt: `https://meta.quiltmc.org/v3/versions/loader/{mc}/{loader}/profile/json`

## Changes from macOS: NONE (platform-independent API calls + Maven path resolution)

## Acceptance Criteria
- [ ] Fabric libraries завантажуються
- [ ] Quilt libraries завантажуються
- [ ] Intermediary mappings включені в classpath
