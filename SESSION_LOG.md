## Current Session

**Branch**: `feature/tight-prop-colliders`

**Task**: Auto-derive tight prop collision boxes from sprite alpha + adjust studio_table collider

**Status**: Completed

**Files Modified**:
- `scripts/world/PropBuilder.cs` (new `GetBaseFootprint` alpha-scan helper, `ImageFootprintToSpriteLocal` + `FootprintToCollisionCenter` converters, `CreatePropAutoCollider` overload with `floorScanHeight` + `Vector4? colliderOverride`; studio_table collider narrowed 124→92 and shifted up 1 tile)
- `scripts/world/builders/ControlRoomBuilder.cs` (speaker_stand, audio_cabinet, storage_shelf switched to `CreatePropAutoCollider`)
- `scripts/world/builders/StudioBuilder.cs` (bookcase, round_table switched to `CreatePropAutoCollider`; added `CreatePropAutoCollider` helper)
- `tests/unit/world/PropBuilderTests.cs` (new — 16 tests for alpha-scan + collision-position math)
- `docs/technical/TOPDOWN_BUILDING_PATTERN.md` (new "Tight Base Collider (Auto-Derived)" subsection)
- `docs/art/ART_STYLE.md` (new "Tight Collider Per-Prop Footprint" reference table)
- `SESSION_LOG.md`

**Work Done**:
- **Alpha-scan tight collider**: replaced hand-tuned collider sizes (e.g. 24×16 for speaker_stand) with auto-derived bounding boxes computed by scanning the bottom `floorScanHeight` rows of each prop sprite's alpha channel. The visible footprint on the floor matches the actual base of each oblique/cabinet-projection sprite, so the player can walk right up to the prop without being blocked by an arbitrary box.
- **Per-prop `floorScanHeight` tuning**: 8–12px for tall standing furniture (captures just the feet/base strip so the player can approach closely), 24px for the round_table (wraps the oval surface).
- **`Vector4 colliderOverride` escape hatch**: still allows hardcoded colliders for special cases (e.g. the studio_table's surface-only strip via `CreateTableGroup`).
- **Bug fix during dev**: initial implementation had the collision-position math wrong (was using the rect's left edge as the center and subtracting half-height instead of adding), which left colliders floating ~12px above the floor. Extracted the correct math into `FootprintToCollisionCenter` helper and added regression tests for the speaker_stand, audio_cabinet, storage_shelf, round_table, asymmetric footprint, and the bottom-edge formula.
- **Studio_table collider tweak**: 124×10 strip narrowed to 92×10 (1 tile = 16px narrower on each side) and shifted up by 1 tile (`RoomBase.TileSize`) so the player can approach the table from below more closely.
- **Tests**: 16 new `PropBuilderTests` covering null/transparent/all-opaque/opaque-in-bottom/floor-scan-clamping/single-row/triangle/alpha-threshold paths, plus 6 `FootprintToCollisionCenter_*` position tests verifying the bottom edge lands at the floor for bottom-anchored sprites.
- `dotnet build`: 0 errors, same 9 pre-existing warnings
- `godot --run-tests`: all 16 `PropBuilderTests` pass cleanly. Other test suite failures are pre-existing (`FileAccess.GetAsText` API incompatibility with installed Godot 4.5.1, unrelated to this change).

**Next Steps**:
- Open in Godot and toggle `RoomDebug` (`ui_select`) to visualize the new tight colliders vs the previous hand-tuned values
- Verify the storage_shelf / bookcase thin-feet strip collider doesn't let the player feel like they're "missing" the shelves (may need to bump `floorScanHeight` from 8 to 12 if player gets stuck on invisible geometry)
- Optional: re-tighten the round_table collider further if the player can walk too far under the table (current is 52×18 wrapped around the oval, leaving a small visible gap)

**Related Docs**:
- `docs/technical/TOPDOWN_BUILDING_PATTERN.md` (new "Tight Base Collider (Auto-Derived)" subsection under Bottom-Anchor Rule)
- `docs/art/ART_STYLE.md` (new "Tight Collider Per-Prop Footprint" reference table)

**Blockers**:
- None.
- `docs/art/ART_STYLE.md` (corrected "Prop Visual Style" section from "Isometric" to **"Oblique/Cabinet Projection Standard"** with ASCII diagram, exact rules, exception table, rejection criteria)
- `docs/art/PIXELLAB_MCP_GUIDE.md` (added `view` parameter guidance, KBTV oblique prompt template, palette swatch generator snippet, corrected base64 truncation limit from "~10KB" to "**~600-800 chars**")
- `scripts/world/builders/ControlRoomBuilder.cs` (added `PlaceDesk` export + `DeskGridPosition` export + `PropBuilder.CreateProp` for `desk.png` at grid `(10, 4)` with 28×14 collider, light mask applied)
- `assets/tiles/props/desk.png` (regenerated 64×64 → resized 32×32 — oblique wooden desk with charcoal frame, drawer visible)
- `assets/tiles/props/round_table.png` (regenerated 96×96 → resized 48×48 — oval wooden table, oblique depth with legs)
- `assets/tiles/props/audio_cabinet.png` (regenerated 64×112 → resized 32×56 — tall rack with TWO COLUMNS of audio gear, phosphor green LEDs, vent slots)
- `assets/tiles/props/computer_station.png` (regenerated 64×64 → resized 32×32 — CRT monitor + keyboard + mouse + desktop tower with phosphor green screen)
- `assets/tiles/props/studio_table.png` (regenerated 192×72 → resized 96×36 — long wide wooden table, oblique depth)
- `assets/tiles/props/poster.png` (regenerated 96×64 → resized 48×32 — framed noir wall poster with red eye + "MEETING" caption)
- `assets/tiles/props/clock.png` (regenerated 64×64 → resized 32×32 — wall clock with roman numerals, frame depth visible)
- `assets/tiles/props/filing_cabinet.png` (regenerated 64×64 → resized 32×32 — small oblique cabinet with drawers + handles + side depth)

**Work Done**:
- **Documentation correction**: The previous session called the prop style "isometric" — that was wrong. The anchor props (`storage_shelf`, `cabinet_tall`, `filing_cabinet`) are all **oblique/cabinet projection**: flat front face (only H+V edges) + 45° depth lines going back at 1/2 scale. Rewrote the "Prop Visual Style" section in `ART_STYLE.md` with the correct terminology, an ASCII diagram showing isometric vs oblique side-by-side, the exact rules (front face MUST be flat, all depth lines at 45°, depth at 1/2 scale), and an exception table for wall-mounted items (poster, clock, on_air_sign) which can stay flat face-on.
- **PixelLab view parameter clarification**: Documented that `create_image_pixflux` has three `view` values and `view: "side"` is the right one for KBTV (combined with oblique keywords in the prompt). `view: "low top-down"` produces isometric, which is what we DON'T want.
- **Base64 truncation limit corrected**: Previous docs claimed "~10KB" inline base64 worked. Reality: MCP client truncates at ~600-800 chars, even 1KB inputs get corrupted. Documented this with measured numbers from our failed attempts.
- **Prompt template added**: Copy-paste template for KBTV oblique props with required/required keywords (cabinet projection, oblique projection, 2.5D pixel art, Stardew Valley style, flat front face, 45 degree depth lines going back) and forbidden keywords (isometric, top-down, 3D).
- **Palette swatch generator snippet**: PowerShell snippet to generate the 16×16 KBTV noir palette swatch (264 chars base64, fits the truncation limit). Used as `color_image_base64` on every `create_image_pixflux` call to force the noir palette onto outputs.
- **Regenerated 8 props**: All used `view: "side"` + the oblique prompt template + the palette swatch. Generated at 2× target dimensions, step-downsampled with nearest-neighbor. First poster attempt came back as just eyes (lost the frame), retried with more explicit framing in the prompt and got the framed noir poster with "MEETING" caption.
- **Wired desk into control room**: Added `PlaceDesk` boolean export (default true) and `DeskGridPosition` Vector2I export (default `(10, 4)` — right wall, mid-height) to `ControlRoomBuilder.cs`. `PropBuilder.CreateProp` call mirrors the pattern used for the audio cabinet (collider 28×14, light mask, shadows).
- `dotnet build`: succeeds with **0 errors** (9 pre-existing warnings unchanged from prior sessions)

**PixelLab credits**: ~1946 generations remaining (used ~9 for the 8 prop regenerations + 1 poster retry)

**Visual results**:
- All 8 updated props now share the **oblique/cabinet** look matching the anchor assets
- Audio cabinet clearly shows TWO COLUMNS of audio gear (mixer left + VU meters/equalizer right) with phosphor green LEDs
- Computer station shows CRT + keyboard + mouse + tower, all visible with proper oblique depth
- Round table is clearly oval, wider than tall, with 4 charcoal legs visible
- Desk is a small wooden desk with drawer, flat front + side depth
- Studio table is a long wooden surface, fits the control room table group proportions
- Poster is framed noir wall art with red eye
- Clock is a wall clock with depth showing
- Filing cabinet is a small oblique cabinet with 2 drawers + handles

**Style terminology reference**:
- **Isometric**: 30° on all 3 axes (equal foreshortening, all faces angled) — what I was accidentally generating before
- **Oblique/cabinet** (correct): flat front face (H+V only), 45° depth lines going back, 1/2 scale depth — what the KBTV anchor props use
- **Cavalier**: same as oblique but full scale depth (we don't use this)
- Also called "2.5D" or "Stardew Valley style" in pixel art vernacular

**Next Steps**:
- Open in Godot to verify the desk placement at (10, 4) doesn't overlap the speaker_stand at (10, 1) or the audio_cabinet at (12, 1)
- Optional: regenerate bookcase.png with oblique style (currently borderline-flat)
- Optional: regenerate the few remaining flat props (phone, coffee_mug, papers, ashtray, boom_mic) if a strict pass is wanted

**Related Docs**:
- `docs/art/ART_STYLE.md` (corrected oblique spec)
- `docs/art/PIXELLAB_MCP_GUIDE.md` (corrected prompt template + truncation guidance)

**Blockers**:
- None

---

## Previous Session

