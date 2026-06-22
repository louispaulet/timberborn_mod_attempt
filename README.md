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
make up
make down
make logs
make clean
scripts/timberborn-ai status
scripts/timberborn-ai place-water-tank
scripts/timberborn-ai interaction-request --topic "current situation"
scripts/timberborn-ai interaction
scripts/timberborn-ai water-readiness
scripts/timberborn-pi-companion
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
GET /api/ai-harness/buildings?query=...
GET|POST /api/ai-harness/place-building?template=water_tank&x=...&y=...&z=...&orientation=Cw0&flipped=false
GET /api/ai-harness/interaction
GET|POST /api/ai-harness/interaction/request?topic=...
GET|POST /api/ai-harness/interaction/show?interactionId=...&menuId=...&question=...&label1=...&kind1=...&payload1=... through label4/kind4/payload4
GET|POST /api/ai-harness/interaction/answer?button=1|2|3|4
GET|POST /api/ai-harness/interaction/tool-result?tool=...&ok=true|false&summary=...
GET|POST /api/ai-harness/interaction/clear
GET /api/ai-harness/game-context
GET /api/ai-harness/resource-summary?good=water
GET /api/ai-harness/water-readiness
```

`place-building` uses Timberborn block coordinates: `x` and `y` are map-plane coordinates, and `z` is elevation. If `x`, `y`, and `z` are omitted, the harness searches for a valid nearby placement around the current camera target.

The template aliases `water_tank`, `path`, `stairs`, and `platform` are intended for Pi and CLI use when exact runtime template names are not known.

The checked-in CLI wraps those endpoints with Python stdlib only:

```sh
scripts/timberborn-ai status
scripts/timberborn-ai commands
scripts/timberborn-ai new-game --settlement AiHarnessTest
scripts/timberborn-ai buildings tank
scripts/timberborn-ai log "bridge check"
scripts/timberborn-ai popup "Hello from Codex"
scripts/timberborn-ai screenshot bridge-check
scripts/timberborn-ai speed 1
scripts/timberborn-ai camera --x 32 --y 0 --z 29 --zoom 0.4
scripts/timberborn-ai place-water-tank
scripts/timberborn-ai place-path
scripts/timberborn-ai place-stairs
scripts/timberborn-ai place-platform
scripts/timberborn-ai place-building SmallTank.Folktails --x 32 --y 29 --z 3
scripts/timberborn-ai interaction-request --topic "current situation"
scripts/timberborn-ai interaction-show --question "What should Pi do?" \
  --label "Water check" --kind tool --payload timberborn_water_readiness \
  --label "Build tips" --kind menu --payload building.pathing \
  --label "Game context" --kind tool --payload timberborn_game_context \
  --label "No" --kind no
scripts/timberborn-ai interaction-answer 1
scripts/timberborn-ai interaction-tool-result timberborn_water_readiness --ok --summary "Water readiness checked."
scripts/timberborn-ai game-context
scripts/timberborn-ai resource-summary water
scripts/timberborn-ai water-readiness
```

Set `TIMBERBORN_AI_URL` or pass `--base-url` if the AI Harness server is not on `http://localhost:8080`.

## Pi Adapter

The project-local Pi extension lives at `.pi/extensions/timberborn-ai-harness/index.ts`. It registers:

```text
timberborn_status
timberborn_list_buildings
timberborn_place_building
timberborn_interaction_state
timberborn_show_menu
timberborn_wait_for_choice
timberborn_record_tool_result
timberborn_game_context
timberborn_resource_summary
timberborn_water_readiness
```

Quick one-shot test:

```sh
pi -p --no-session --approve \
  -e .pi/extensions/timberborn-ai-harness/index.ts \
  --no-builtin-tools \
  --tools timberborn_place_building \
  "Use the timberborn_place_building tool to place one simple water tank in the current Timberborn game. Use template water_tank and omit coordinates so the harness searches near the camera."
```

## Four-Button Pi Loop

When the mod is active in a settlement, it adds an `Ask AI` HUD module with four numbered answer buttons. Timberborn never sends freeform text to Pi. The game records a request or one of the four answers; a local Pi companion session polls the harness, chooses deterministic tools when needed, and posts the next four-option menu.

Interaction states are:

```text
idle -> requested -> menuShown -> answerSubmitted
idle -> requested -> menuShown -> toolRequested -> toolCompleted
```

Menus posted by Pi must have exactly four options. Non-confirmation menus must include at least one deterministic tool option, one navigation/menu option, and one back/cancel/no option. Confirmation menus must include yes and no before any mutating action such as placing a building.

Generated/replayable menus are stored as JSON under:

```text
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness/generated/interactions
```

Replay keys are based on `modVersion + skillId + menuPath + contextHash`. Pi should reuse a matching local menu before asking the model to generate another equivalent question.

The first bundled Pi skill snippets live under `.pi/extensions/timberborn-ai-harness/skills`:

```text
building.pathing
building.vertical-stacking
water.storage-readiness
construction.triage
```

Deterministic context tools intentionally do simple arithmetic in the mod. For example, `water-readiness` returns `waterPerBeaver`, `waterCapacityPerBeaver`, and `daysOfWater = availableWater / max(1, beavers * 2)` so Pi can advise without recomputing those values from prose.

For local manual testing without starting a full Pi session, run:

```sh
make up
```

The companion keeps Timberborn foregrounded, watches the interaction state, posts a four-button menu when `Ask AI` is pressed, runs deterministic tool choices such as water readiness or game context, records the tool result, and posts the next menu.

Stop it with:

```sh
make down
```

## Verify In Timberborn

1. Run `make install`.
2. Run `make launch`, or open Steam and start Timberborn manually without save-loading launch parameters.
3. Press Escape to skip the intro video if it appears.
4. In the Timberborn Mod Manager, enable `AI Harness` and press OK.
5. From the main menu, run `scripts/timberborn-ai new-game --settlement AiHarnessTest --map Diorama --faction Folktails` to create a throwaway test settlement.
6. Run `scripts/timberborn-ai status` and confirm a JSON response with `"ok": true`.
7. Run `scripts/timberborn-ai popup "Hello from Codex"` and confirm the in-game popup.
8. Run `scripts/timberborn-ai speed 1`, then `scripts/timberborn-ai status`, and confirm the reported speed is `1`.
9. Run `scripts/timberborn-ai place-water-tank` and confirm it places `SmallTank.Folktails` as a construction site.
10. Run `scripts/timberborn-ai interaction-request --topic "current situation"` and confirm the HUD shows a requested Pi interaction.
11. Run `scripts/timberborn-ai interaction-show` with four labels/kinds, then answer using one of the four in-game HUD buttons or `scripts/timberborn-ai interaction-answer 1`.
12. Run `scripts/timberborn-ai water-readiness` and confirm deterministic water metrics are returned.
13. Run the Pi one-shot command above and confirm Pi reports a placed water tank.
14. For final proof, capture screenshots showing the four-button HUD, a Pi-posted four-option menu, a submitted answer, and a tool-result state.
15. Check logs with `make logs`; the mod should log its startup, interaction, and command messages in `Player.log`.

## References

- [Mechanistry timberborn-modding repository](https://github.com/mechanistry/timberborn-modding)
- [Official Timberborn modding wiki](https://github.com/mechanistry/timberborn-modding/wiki)
- [Quick start](https://github.com/mechanistry/timberborn-modding/wiki/Quick-start)
- [Coding basics](https://github.com/mechanistry/timberborn-modding/wiki/Coding-basics)
- [User interface](https://github.com/mechanistry/timberborn-modding/wiki/User-interface)
- [Mod directory structure](https://github.com/mechanistry/timberborn-modding/wiki/Mod-directory-structure)
- [Mod management](https://github.com/mechanistry/timberborn-modding/wiki/Mod-management)
- [Timberborn HTTP API guide](https://timberborn.io/)
