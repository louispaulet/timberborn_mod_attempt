# Verification

## Fast Loop Verification

Verified on 2026-06-25 CEST without launching Timberborn.

### Commands

```sh
make verify-fast
```

### Results

- `make test-fast` restored and ran `tests/AiHarness.Core.Tests` with 11 passing xUnit tests.
- `make smoke-fake` started `scripts/fake-timberborn-harness` on `http://127.0.0.1:18080`, exercised `scripts/timberborn-ai` and `scripts/timberborn-pi-companion --allow-no-game --once`, verified deterministic water/building endpoints, and confirmed invalid placement returns `"ok": false`.
- `make build` compiled `AiHarness.Core` and `AiHarness.Mod` against the installed Timberborn managed assemblies.

The fast path is now:

```sh
make test-fast
make smoke-fake
make build
```

Use `make verify-fast` to run all three. Use `make smoke-live` only when Timberborn is already running with the mod loaded; it does not launch the game.

Verified on 2026-06-22 11:39:35 CEST with Timberborn `v1.1.0.2-3c063a9-xsm` running from the Steam install.

## Commands

```sh
make verify-env
make build
make install
```

`make install` copied the packaged mod to:

```text
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness
```

The installed mod folder contained:

```text
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness/manifest.json
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness/Code.dll
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness/AiHarness.Core.dll
```

## Game Check

- Timberborn was launched normally through Steam with no save-loading command-line parameters.
- Escape was pressed to skip the cinematic intro.
- Timberborn's Mod Manager listed `AI Harness v0.1.0` and it was enabled.
- No `Continue` or existing save was used.
- A throwaway game was started from the main menu through the harness:

```sh
scripts/timberborn-ai new-game --settlement AiHarnessTest-20260622 --map Diorama --faction Folktails
```

## CLI And HTTP Results

All checked CLI calls returned JSON with `"ok": true`:

```sh
scripts/timberborn-ai status
scripts/timberborn-ai commands
scripts/timberborn-ai new-game --settlement AiHarnessTest-20260622 --map Diorama --faction Folktails
scripts/timberborn-ai log "bridge check"
scripts/timberborn-ai popup "Hello from Codex"
scripts/timberborn-ai screenshot bridge-check-20260622
scripts/timberborn-ai speed 0
scripts/timberborn-ai speed 1
scripts/timberborn-ai camera --x 32 --y 0 --z 29 --zoom 0.4
```

After `scripts/timberborn-ai speed 1`, a follow-up `scripts/timberborn-ai status` reported:

```json
{
  "speed": 1
}
```

The screenshot command produced:

```text
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness/generated/screenshots/bridge-check-20260622.png
```

The PNG was verified as `2940 x 1912` and is intentionally kept out of git with generated mod outputs.

## Player Log

`Player.log` contained:

```text
- AI Harness (v0.1.0)
[LouisPaulet.AiHarness] AI Harness mod started from: /Users/louispaulet/Documents/Timberborn/Mods/AiHarness
[LouisPaulet.AiHarness] AI Harness runner loaded.
[LouisPaulet.AiHarness] AI Harness HTTP server listening on http://localhost:8080/
[LouisPaulet.AiHarness] Started throwaway new game: AiHarnessTest-20260622 map=Diorama faction=Folktails
[LouisPaulet.AiHarness] bridge check
[LouisPaulet.AiHarness] Showing popup from AI Harness: Hello from Codex
[LouisPaulet.AiHarness] Screenshot requested: /Users/louispaulet/Documents/Timberborn/Mods/AiHarness/generated/screenshots/bridge-check-20260622.png
[LouisPaulet.AiHarness] Speed changed to 0
[LouisPaulet.AiHarness] Speed changed to 1
```

No AI Harness exceptions or errors were found in the checked log lines.

## Pi Adapter And Building Placement

Verified on 2026-06-22 12:43:40 CEST with Timberborn `v1.1.0.2-3c063a9-xsm`.

### Commands

```sh
make build
make install
scripts/timberborn-ai new-game --settlement AiHarnessPiTank2-20260622 --map Diorama --faction Folktails
scripts/timberborn-ai buildings tank
scripts/timberborn-ai place-water-tank
pi -p --no-session --approve -e .pi/extensions/timberborn-ai-harness/index.ts --no-builtin-tools --tools timberborn_place_building "Use the timberborn_place_building tool to place one simple water tank in the current Timberborn game. Use template water_tank and omit coordinates so the harness searches near the camera."
scripts/timberborn-ai screenshot pi-water-tank-placement-20260622
scripts/timberborn-ai place-building definitely-not-a-template
scripts/timberborn-ai status
```

### Results

- `scripts/timberborn-ai buildings tank` resolved Timberborn's water tank templates, including `SmallTank.Folktails`.
- `scripts/timberborn-ai place-water-tank` placed `SmallTank.Folktails` as a construction site at block coordinates `x=32, y=29, z=3`.
- Pi used the project-local extension tool `timberborn_place_building` and reported a second water tank placed at `x=32, y=28, z=3`.
- An intentional invalid template request returned `"ok": false`; a follow-up status request remained healthy.
- The screenshot command produced:

```text
/Users/louispaulet/Documents/Timberborn/Mods/AiHarness/generated/screenshots/pi-water-tank-placement-20260622.png
```

`Player.log` contained:

```text
[LouisPaulet.AiHarness] Started throwaway new game: AiHarnessPiTank2-20260622 map=Diorama faction=Folktails
[LouisPaulet.AiHarness] Placed building SmallTank.Folktails at x=32, y=29, z=3, orientation=Cw0, flipped=False
[LouisPaulet.AiHarness] Placed building SmallTank.Folktails at x=32, y=28, z=3, orientation=Cw0, flipped=False
```
