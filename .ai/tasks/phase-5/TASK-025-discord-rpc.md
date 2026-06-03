# TASK-025: DiscordRPCService

## Metadata
- **Phase**: 5 | **Source**: DiscordRPCService.swift (203 lines) | **LOC**: ~200

## ⚠️ Critical Platform Change
- Unix domain socket (`$TMPDIR/discord-ipc-{i}`) → Named Pipe (`\\.\pipe\discord-ipc-{i}`)
- Transport: `new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut)`
- Protocol: IDENTICAL — JSON payload + 8-byte header (opcode LE uint32 + length LE uint32)
- Discord App ID: `1506949090498318437`

## Methods
- `SetActivity(instanceName, mcVersion, modLoader, startTime)` — set Rich Presence
- `ClearActivity()` — clear and disconnect

## Acceptance Criteria
- [ ] Connects to Discord via Named Pipe
- [ ] Shows "Playing via Onyx Launcher" in Discord
- [ ] Clears activity on game exit
