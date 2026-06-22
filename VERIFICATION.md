# Verification

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
