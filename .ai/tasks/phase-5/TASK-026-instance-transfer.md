# TASK-026: InstanceTransferService
- **Phase**: 5 | **Source**: InstanceTransferService.swift (648 lines) | **LOC**: ~500
- Export/Import instances as ZIP archives.
- Changes: `/usr/bin/zip` → ZipFile.CreateFromDirectory, `/usr/bin/unzip` → ZipFile.ExtractToDirectory
- NSSavePanel → SaveFileDialog, NSOpenPanel → OpenFileDialog
- Security-scoped resources: NOT NEEDED on Windows
- OnyxProfile JSON: identical cross-platform format
