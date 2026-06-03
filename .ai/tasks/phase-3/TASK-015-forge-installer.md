# TASK-015: ForgeInstaller

## Metadata
- **Phase**: 3 | **Dependencies**: TASK-003, TASK-010 (JavaManager) | **LOC**: ~250
- **Source**: [ForgeInstaller.swift](../../onyx/Onyx/Services/ForgeInstaller.swift) — 225 рядків

## Objective
Forge/NeoForge installer. Завантажує JAR, запускає його через Java.

## Changes from macOS
- `/usr/bin/unzip` → `ZipFile` для legacy Forge extraction
- Process launch ідентичний (java -jar installer.jar --installClient)
- Legacy Forge detection: MC <= 1.12

## APIs
- Forge: `https://maven.minecraftforge.net/net/minecraftforge/forge/{version}/forge-{version}-installer.jar`
- NeoForge: `https://maven.neoforged.net/releases/net/neoforged/neoforge/{version}/neoforge-{version}-installer.jar`

## Acceptance Criteria
- [ ] Forge installer завантажується та запускається
- [ ] NeoForge installer працює
- [ ] Legacy Forge (<= 1.12) екстрагується через ZipFile
- [ ] isValidJarFile — перевірка ZIP magic bytes
