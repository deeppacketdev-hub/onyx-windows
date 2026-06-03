# TASK-020: SkinManager

## Metadata
- **Phase**: 4 | **Dependencies**: TASK-003, TASK-006 | **LOC**: ~550
- **Source**: [SkinManager.swift](../../onyx/Onyx/Services/SkinManager.swift) — 601 рядок

## Objective
Skin library, browser, download, CustomSkinLoader integration.

## Changes: NSImage → BitmapImage. All mc-heads.net API calls identical.

## Key Features
- 95 popular usernames pool для Browse tab
- Search by username/UUID via Mojang API
- Download skin texture + body render + head avatar
- Library persistence in skins/library.json
- CustomSkinLoader.json config writing per instance
- applySkinToInstances — copy skin PNG + write CSL config

## Acceptance Criteria
- [ ] Popular skins browse працює
- [ ] Search by username працює
- [ ] Skin download зберігає 3 файли (texture, render, head)
- [ ] CustomSkinLoader.json створюється правильно
