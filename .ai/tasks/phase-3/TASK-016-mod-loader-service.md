# TASK-016: ModLoaderService

## Metadata
- **Phase**: 3 | **Dependencies**: TASK-006 | **LOC**: ~250
- **Source**: [ModLoaderService.swift](../../onyx/Onyx/Services/ModLoaderService.swift) — 247 рядків

## Objective
Fetch available mod loader versions for each loader type.

## APIs (platform-independent)
- Fabric: `https://meta.fabricmc.net/v2/versions/loader/{mcVersion}`
- Quilt: `https://meta.quiltmc.org/v3/versions/loader/{mcVersion}`
- Forge: `https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml` (XML parsing!)
- NeoForge: `https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge`

## Changes: NONE — pure API calls. Direct 1:1 port.

## Acceptance Criteria
- [ ] Fabric versions fetch працює
- [ ] Forge versions parse з XML працює
- [ ] NeoForge versions fetch працює
