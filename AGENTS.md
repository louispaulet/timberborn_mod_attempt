# AGENTS.md

## Repository Instructions

- Always read `AGENTS.md` before starting any task.
- Always commit and push, even if on main, to track progression with Conventional Commits 1.0.0 style for title and description.
- Keep generated build outputs, copied game assemblies, local tools, logs, screenshots, and packaged mod archives out of git unless explicitly requested.
- Prefer the official Timberborn modding pipeline over legacy BepInEx-based approaches for this project.
- Keep changes small and verifiable; after each meaningful action, commit and push.
- Pi interactions must be companion-polled: Timberborn records requests and four-button answers, while a local Pi session polls the harness and posts the next menu/tool result.
- The in-game side must not require freeform text input. All player responses to Pi must be possible through the four HUD answer buttons while the mod is active.
- Replay generated Pi menus locally as JSON under the mod's generated files so repeated contextual menus can be reused instead of regenerated.
- Prefer deterministic game tools for simple facts and arithmetic. For example, compute water-to-population ratios in C# tools and let Pi interpret the result, not recompute the division from prose.
- Expose deterministic helpers as Pi-callable tools whenever Pi may need that context or action.
