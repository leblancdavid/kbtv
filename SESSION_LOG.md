## Current Session

**Branch**: feature working tree (3D migration planning)

**Task**: Plan the migration from the current 2D top-down presentation to a 3D presentation focused on the current UI menus, control room, and studio.

**Status**: In progress — added a dedicated 3D migration plan doc and linked it from the roadmap, technical spec, game design doc, and topdown building notes. Next step is to define the first concrete 3D scene skeleton and implementation order.

### Work Done
- Added `docs/technical/THREED_MIGRATION_PLAN.md` covering scope, what stays, phased rollout, suggested scene structure, room requirements, UI strategy, asset strategy, risks, and first-definition-of-done.
- Updated `docs/technical/TECHNICAL_SPEC.md` to point at the 3D migration plan and note the planned `Game3D.tscn` path.
- Updated `docs/design/ROADMAP.md` with a new `World Presentation Migration` section.
- Updated `docs/design/GAME_DESIGN.md` to reflect the planned 3D world presentation layer.
- Updated `docs/technical/TOPDOWN_BUILDING_PATTERN.md` to direct readers to the 3D migration plan.

### Files Modified
- `docs/technical/THREED_MIGRATION_PLAN.md`
- `docs/technical/TECHNICAL_SPEC.md`
- `docs/design/ROADMAP.md`
- `docs/design/GAME_DESIGN.md`
- `docs/technical/TOPDOWN_BUILDING_PATTERN.md`
- `SESSION_LOG.md`

### Next Steps
1. Decide the first concrete 3D scene contract (`World3D`, `ControlRoom3D`, `StudioRoom3D`, `Player3D`).
2. Choose the first-pass presentation style: placeholder meshes, imported art, or hybrid.
3. Start implementing the 3D bootstrap once the scene contract is confirmed.
