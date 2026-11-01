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
- [ ] Make path driving faster than dirt driving, using verified surface information and a clear speed rule. **Code is in place:** 7 m/s on dirt and 10.5 m/s on paths. Live path-speed comparison is pending because harness-placed roads remained construction sites in both test worlds.
- [ ] Support ramps and stairs while keeping the vehicle grounded and stable; define the maximum climbable slope/step. **Code is in place:** supports up to one block of rise per tile. Live climb remains unverified: the Diorama stair site stayed unfinished, and the settlement had zero planks and logs.
- [x] Verify smooth acceleration, braking, reverse, and steering at low and high speed, including when input is interrupted or driving mode exits. **Live check:** forward and reverse moved the car on flat dirt and Diorama terrain; a 2-second flat-dirt run moved about 13.4 m, reverse moved the car about 5.0 m, and a forward-to-coast steering sequence moved it about 2.5 m. Escape stopped driving and V resumed it.
- [x] Verify camera follow through turns and obstacles, and define how the camera behaves after Escape and when V resumes driving. **Live check:** the camera followed movement and steering, stayed centered when the car stopped at a river-bank edge, remained where left after Escape, and returned to the car after V.

#### P1 live-test status

- Test worlds: disposable `RcCarP1Verify-20261101` on the user's custom `flatmap` and disposable `RcCarP1Diorama-20261101` (Folktails, Timberborn `1.1.2.8`). The custom map source was left untouched.
- On flat dirt, a 2-second forward input moved the camera target from `(56.50, 4.36, 56.50)` to `(64.21, 4.36, 67.51)`, about 13.4 m. Forward, turning, and reverse inputs all moved the car in the live runs.
- On Diorama, a 1.2-second forward input moved the car about 1.5 m from its spawn point. Reverse from the river-bank edge moved it about 5.0 m back onto the land strip. A forward-then-coasting steering input moved it about 2.5 m while the camera continued to track it.
- Escape exited driving and stopped the car. After moving the camera with the harness, V resumed driving and returned the camera target to the car's position. The Diorama run also showed the car remain on dry land at the river edge.
- Path speed remains unverified: the harness placed path construction sites at `(17, 31, 3)` and `(16, 30, 3)` in Diorama; they remained visibly unfinished after the settlement advanced to day 2. The flatmap comparison also only produced construction sites, so there was no traversable path surface to compare against dirt.
- Stairs remain unverified: a `Stairs.Folktails` construction site was placed at `(15, 29, 3)` in Diorama, but it remained unfinished. The live resource summary showed zero stock of both planks and logs, so a climb test was not possible.
- No RC Car exception appeared in the Player.log during these checks.

### P2 — Regression checks and polish

- [ ] Test on flat dirt, paths, uneven terrain, ramps, stairs, shorelines, bridges, buildings, and natural obstacles.
- [ ] Test across pause/resume, settlement load, mod unload, and map boundaries; ensure input hooks and spawned objects are cleaned up.
- [ ] Add concise in-game controls/status feedback so the player can tell whether driving mode is active.

## Completion checks

- [x] On flat terrain, the car stays on the ground during sustained driving.
- [x] Open water and terrain edges block movement in live play; building and natural-cell blocking are implemented but were not directly exercised live.
- [ ] Paths produce a measurable speed increase over dirt. (Rule implemented; no completed test path was available in either disposable world.)
- [ ] Ramps and supported stairs can be climbed without the car flying or getting stuck. (One-block-per-tile climb limit implemented; the live stair site could not be completed with zero stock of construction materials.)
- [x] The visible vehicle reads as an RC car rather than a colored cube.
- [x] Camera follow, Escape to exit, and V to resume work in a live Timberborn 1.1 settlement.
