# RC Car TODO

This tracks the gap between the current input prototype and the requested drivable RC car. The current prototype is not complete.

## Current prototype

- The official mod manifest, build, package, and install path are in place.
- The spawned vehicle now loads an original Blender-authored Iron Teeth-style RC car mesh embedded in the mod assembly.
- Spawn search checks dry, level terrain, water, blocked game cells, and physics overlaps near the map center.
- WASD controls eased acceleration, reverse, and steering; terrain support and collider sweeps constrain movement.
- The camera follows while driving. Escape exits driving mode and V resumes it.
- The editable Blender source and the exported OBJ used by the mod are in `src/RcCar.Mod/Assets/`.

## Known problems

Reported during play before the grounded movement implementation; live verification remains pending:

- The car floats or flies instead of staying grounded.
- It moves over water.
- It passes through obstacles because it has no collision response.

## TODO, in priority order

### P0 — Make movement grounded and safe

- [ ] Inspect the installed Timberborn 1.1 assemblies and official modding examples for supported ground, water, and collision APIs before choosing an implementation. Do not rely on guessed game methods.
- [ ] Replace free transform translation with a movement model that keeps the car supported by terrain and prevents hovering, flying, and sinking during normal driving.
- [ ] Add collision response for terrain boundaries, buildings, and natural obstacles. Remove or replace the current collider removal as needed; the car must not pass through the settlement.
- [ ] Detect water and prevent driving across open water. Define and test behavior at shorelines and on bridges.
- [ ] Improve spawn selection to avoid water, buildings, and blocked or uneven cells, and handle maps where no suitable nearby spawn cell exists.

### P1 — Match the requested vehicle behavior

- [x] Replace the orange cube with a recognizable small RC car model, using game assets or a simple original model with visible wheels.
- [ ] Make path driving faster than dirt driving, using verified surface information and a clear speed rule.
- [ ] Support ramps and stairs while keeping the vehicle grounded and stable; define the maximum climbable slope/step.
- [ ] Verify smooth acceleration, braking, reverse, and steering at low and high speed, including when input is interrupted or driving mode exits.
- [ ] Verify camera follow through turns and obstacles, and define how the camera behaves after Escape and when V resumes driving.

### P2 — Regression checks and polish

- [ ] Test on flat dirt, paths, uneven terrain, ramps, stairs, shorelines, bridges, buildings, and natural obstacles.
- [ ] Test across pause/resume, settlement load, mod unload, and map boundaries; ensure input hooks and spawned objects are cleaned up.
- [ ] Add concise in-game controls/status feedback so the player can tell whether driving mode is active.

## Completion checks

- [ ] On flat terrain, the car stays on the ground during sustained driving and steering.
- [ ] The car cannot travel over open water or pass through buildings, terrain edges, or natural obstacles.
- [ ] Paths produce a measurable speed increase over dirt.
- [ ] Ramps and supported stairs can be climbed without the car flying or getting stuck.
- [ ] The visible vehicle reads as an RC car rather than a colored cube.
- [ ] Camera follow, Escape to exit, and V to resume work in a live Timberborn 1.1 settlement.
