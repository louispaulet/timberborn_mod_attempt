# RC Car TODO

P0 movement and safety work is complete. P1 behavior and P2 regression coverage remain.

## Current prototype

- The official mod manifest, build, package, and install path are in place.
- The spawned vehicle now loads an original Blender-authored Iron Teeth-style RC car mesh embedded in the mod assembly.
- Spawn search checks dry, level terrain, water, blocked game cells, and physics overlaps near the map center.
- WASD controls eased acceleration, reverse, and steering; terrain support and collider sweeps constrain movement.
- The camera follows while driving. Escape exits driving mode and V resumes it.
- The editable Blender source and the exported OBJ used by the mod are in `src/RcCar.Mod/Assets/`.

## P0 live verification

- Built and installed against Timberborn `1.1.2.8-ffc2de0-xsm`; the game loaded the mod and the Iron Teeth car model.
- Spawned on dry, level ground at `(125.50, 2.36, 120.50)` in the disposable `RcCarP0Check-20261031` settlement.
- Sustained driving stayed grounded; the car stopped at a raised terrain cliff instead of clipping through it.
- Reverse driving at the river bank stayed on dry land and did not enter open water.
- Escape exited driving and V resumed it. The game log recorded each state change and no mod exceptions.
- No bridge, building, or natural-obstacle scenario was available for direct live verification. Bridge support is limited to dry path cells with a physical deck collider at support height; ramps and stairs are not part of P0.

## TODO, in priority order

### P0 — Make movement grounded and safe

- [x] Inspect the installed Timberborn 1.1 assemblies and official modding examples for supported ground, water, and collision APIs before choosing an implementation. Do not rely on guessed game methods.
- [x] Replace free transform translation with a movement model that keeps the car supported by terrain and prevents hovering, flying, and sinking during normal driving.
- [x] Add collision response for terrain boundaries, buildings, and natural obstacles. Remove or replace the current collider removal as needed; the car must not pass through the settlement.
- [x] Detect water and prevent driving across open water. Define bridge support as a dry path cell with a physical deck collider at support height; shoreline blocking was checked live. A bridge was not available for a live test.
- [x] Improve spawn selection to avoid water, buildings, and blocked or uneven cells, and handle maps where no suitable nearby spawn cell exists.

### P1 — Match the requested vehicle behavior

- [x] Replace the orange cube with a recognizable small RC car model, using game assets or a simple original model with visible wheels.
- [ ] Make path driving faster than dirt driving, using verified surface information and a clear speed rule. **Code is in place:** 7 m/s on dirt and 10.5 m/s on paths. Live path-speed comparison is pending; the flatmap road tiles placed so far still appear as construction sites.
- [ ] Support ramps and stairs while keeping the vehicle grounded and stable; define the maximum climbable slope/step. **Code is in place:** supports up to one block of rise per tile. Live stair/ramp climb is pending.
- [ ] Verify smooth acceleration, braking, reverse, and steering at low and high speed, including when input is interrupted or driving mode exits. **Partially checked live:** on the flatmap, forward, combined forward/left, and reverse inputs moved the car; full low/high-speed and interruption behavior remains to check.
- [ ] Verify camera follow through turns and obstacles, and define how the camera behaves after Escape and when V resumes driving. **Partially checked live:** the camera tracked movement while turning, held position after Escape, and returned to the car after V; obstacle-follow behavior remains to check.

#### P1 live-test status

- Test world: the user-requested custom `flatmap`, in disposable settlement `RcCarP1Flat-20261031` (Folktails, Timberborn `1.1.2.8`). The source map was left untouched.
- The installed P1 build spawned the car on the flatmap. Computer-controlled forward, turn, and reverse inputs moved it on dry dirt; no RC Car exception appeared in the log during this pass.
- Escape exited driving and stopped the car. After moving the camera while exited, V resumed driving and the camera returned to the car.
- A five-tile path run was placed near the car for the speed comparison, but its tiles were still shown as construction sites at the last inspection. Path speed and stairs therefore remain unverified in game.

### P2 — Regression checks and polish

- [ ] Test on flat dirt, paths, uneven terrain, ramps, stairs, shorelines, bridges, buildings, and natural obstacles.
- [ ] Test across pause/resume, settlement load, mod unload, and map boundaries; ensure input hooks and spawned objects are cleaned up.
- [ ] Add concise in-game controls/status feedback so the player can tell whether driving mode is active.

## Completion checks

- [x] On flat terrain, the car stays on the ground during sustained driving.
- [x] Open water and terrain edges block movement in live play; building and natural-cell blocking are implemented but were not directly exercised live.
- [ ] Paths produce a measurable speed increase over dirt. (Rule implemented; flatmap comparison pending construction of the test path.)
- [ ] Ramps and supported stairs can be climbed without the car flying or getting stuck. (One-block-per-tile climb limit implemented; live test pending.)
- [x] The visible vehicle reads as an RC car rather than a colored cube.
- [x] Camera follow, Escape to exit, and V to resume work in a live Timberborn 1.1 settlement.
