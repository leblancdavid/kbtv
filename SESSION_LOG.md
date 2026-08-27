## Current Session

**Branch**: `feature/tight-prop-colliders`

**Task**: Regenerate Vern's seated studio sprite to resemble Art Bell + add a seated idle breathing animation

**Status**: In Progress (sprite + idle animation wired; awaiting in-editor visual verification)

**Files Modified**:
- `assets/sprites/characters/vern/vern.png` (overwritten with new 80×80 Art Bell-style seated Vern — frame_4 from PixelLab, palette-reduced to 12 colors, edge-corrected; original saved as `vern.old.png`)
- `assets/sprites/characters/vern/vern_idle.png` (new 720×80 horizontal atlas — 9×80×80 cells of the `seated_idle` breathing animation, 12-color palette)
- `scripts/world/builders/StudioBuilder.cs` (`CreateVernChairGroup` — replaced static `Sprite2D` with `AnimatedSprite2D` + `SpriteFrames` `seated_idle` animation from the atlas; shadow still sourced from the 80×80 `vern.png` base so the 720-wide atlas doesn't distort the cast shadow)
- `SESSION_LOG.md`

**Work Done**:
- Researched existing Vern setup: only `vern.png` (80×80) is actually used, as a static `Sprite2D` in `CreateVernChairGroup` (`StudioBuilder.cs:513`). The `idle_*`/`drinking_*` frames and `vern_portrait.png` are legacy/unused.
- Generated a 16-candidate PixelLab pack (`create_1_direction_object`, 80×80, front-facing seated in office chair). User selected **frame_4** as the favorite.
- Post-processing: `reduce_colors` to 12 colors (KBTV noir palette spirit), then `correct_pixelart` (strength 0.1) to sharpen edges. Verified transparent bg + content bbox x[17..61] y[5..79].
- Overwrote `vern.png` with the finalized sprite (original backed up at `vern.old.png`). Same file path/size → no code change needed for the base swap.
- Generated `seated_idle` breathing animation via `animate_object` (v3, 9 frames: 8 generated + reference), promoted group `733883d6-13e6-4458-8619-4097b4e695f1`.
- Reduced all 9 idle frames to a shared 12-color palette using the direct `https` frame URLs (inline base64 kept truncating through the MCP layer, as documented in `PIXELLAB_MCP_GUIDE.md`).
- Assembled the 9 frames into a 720×80 horizontal atlas (`vern_idle.png`) and wired `CreateVernChairGroup` to use an `AnimatedSprite2D` playing `seated_idle` at 3.0 fps, looping, scale 0.75, following the existing `SpriteFrames`+`AtlasTexture` pattern from the smoke system.
- Fixed a shadow bug: passing the 720-wide atlas to `CreateShadowForObject` would have produced a huge/misplaced shadow — so the shadow still sources from the 80×80 `vern.png` base.
- `dotnet build`: 0 errors (same 9 pre-existing warnings). New PNGs will re-import in Godot on next editor open (`.import` regenerated automatically).

**Next Steps**:
- Open Godot and verify the new seated Vern renders correctly behind the round table (grid 6,3) at scale 0.75 with proper ZIndex and correct cast shadow.
- Confirm the idle breathing animation plays smoothly (may tune `SetAnimationSpeed` — currently 3.0 fps).
- Optional future work: talking/mouth animation synced to Vern's broadcast audio (requires wiring to broadcast events); the current scope was idle-only per user.
- Consider deleting legacy unused `vern` sprites (`idle_*`, `drinking_*`, `vern_portrait.png`) in a cleanup pass.

**Related Docs**:
- `docs/art/ART_STYLE.md` (noir palette; Vern is the warm accent — black turtleneck + warm skin)
- `docs/art/PIXELLAB_MCP_GUIDE.md` (base64 truncation limit ~600-800 chars — used URL variants instead)

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

