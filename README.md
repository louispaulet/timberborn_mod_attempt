# Timberborn AI Harness Mod

A small source-first Timberborn mod that exposes a localhost command harness for Codex, pi.dev, shell scripts, or a future MCP wrapper.

This project follows the official Timberborn 1.0 modding pipeline. It builds a C# DLL and installs it into Timberborn's local mods directory instead of using BepInEx.

## Requirements

- macOS with Timberborn installed through Steam.
- Timberborn app path:
  `/Users/louispaulet/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app`
- Local mods path:
  `/Users/louispaulet/Documents/Timberborn/Mods`
- .NET SDK. If `dotnet` is not installed globally, `make bootstrap` installs a local SDK under `.tools/dotnet`.

## Useful Commands

```sh
make verify-env
make bootstrap
make build
make package
make install
make launch
make logs
make clean
scripts/timberborn-ai status
```

## Build And Install

```sh
make bootstrap
make build
make package
make install
```

`make package` creates `dist/AiHarness` with the mod manifest and DLL. `make install` copies that folder to `/Users/louispaulet/Documents/Timberborn/Mods/AiHarness`.

## Harness Commands

The mod starts a localhost HTTP server from the main menu and game contexts. The default base URL is:

```text
http://localhost:8080
```

Every response uses this shape:

```json
{
  "ok": true,
  "command": "status",
  "commandId": "status-...",
  "data": {},
  "error": null
}
```

Available endpoints:

```text
GET /api/ai-harness/status
GET /api/ai-harness/commands
GET|POST /api/ai-harness/new-game?settlement=...&map=...&faction=...
GET|POST /api/ai-harness/log?message=...
GET|POST /api/ai-harness/popup?message=...
GET|POST /api/ai-harness/screenshot?name=...
GET|POST /api/ai-harness/speed?value=0|1|2|3
GET|POST /api/ai-harness/camera?x=...&y=...&z=...&zoom=...
```

The checked-in CLI wraps those endpoints with Python stdlib only:

```sh
scripts/timberborn-ai status
scripts/timberborn-ai commands
scripts/timberborn-ai new-game --settlement AiHarnessTest
scripts/timberborn-ai log "bridge check"
scripts/timberborn-ai popup "Hello from Codex"
scripts/timberborn-ai screenshot bridge-check
scripts/timberborn-ai speed 1
scripts/timberborn-ai camera --x 32 --y 0 --z 29 --zoom 0.4
```

Set `TIMBERBORN_AI_URL` or pass `--base-url` if Timberborn's HTTP API is not on `http://localhost:8080`.

## Verify In Timberborn

1. Run `make install`.
2. Run `make launch`, or open Steam and start Timberborn manually without save-loading launch parameters.
3. Press Escape to skip the intro video if it appears.
4. In the Timberborn Mod Manager, enable `AI Harness` and press OK.
5. From the main menu, run `scripts/timberborn-ai new-game --settlement AiHarnessTest --map Diorama --faction Folktails` to create a throwaway test settlement.
6. Run `scripts/timberborn-ai status` and confirm a JSON response with `"ok": true`.
7. Run `scripts/timberborn-ai popup "Hello from Codex"` and confirm the in-game popup.
8. Run `scripts/timberborn-ai speed 1`, then `scripts/timberborn-ai status`, and confirm the reported speed is `1`.
9. Check logs with `make logs`; the mod should log its startup and command messages in `Player.log`.

## References

- [Mechanistry timberborn-modding repository](https://github.com/mechanistry/timberborn-modding)
- [Official Timberborn modding wiki](https://github.com/mechanistry/timberborn-modding/wiki)
- [Quick start](https://github.com/mechanistry/timberborn-modding/wiki/Quick-start)
- [Coding basics](https://github.com/mechanistry/timberborn-modding/wiki/Coding-basics)
- [User interface](https://github.com/mechanistry/timberborn-modding/wiki/User-interface)
- [Mod directory structure](https://github.com/mechanistry/timberborn-modding/wiki/Mod-directory-structure)
- [Mod management](https://github.com/mechanistry/timberborn-modding/wiki/Mod-management)
- [Timberborn HTTP API guide](https://timberborn.io/)
