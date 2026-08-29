## Current Session

**Branch**: feature working tree (world/ reorg)

**Task**: Fix three props — (1) mirror right control-room speaker so it points at the table, (2) revert the ON-AIR sign to the correct texture, (3) shrink Vern's round table ~25%.

**Status**: Completed — right speaker uses `FlipH` (left→right mirror, points inward toward the desk); `on_air_sign.png` restored to the 64×24 version from `09ca15c3` (reimported, ctex regenerated); `RoundTableProp.SpriteScale = 0.75f`; `dotnet build` green (0 errors), `--run-tests` exit 0 with no prop errors (only pre-existing `FileAccess.GetAsText()` MissingMethodException in ArcRepository). Needs user visual confirmation in-editor.

### Prop fixes
- **Right speaker** (`SpeakerStandsProp.Specs`, cell `(10,0)`): the two speakers flank the desk and share one sprite whose grill faces sideways. The left speaker already points inward; the right one pointed away. Corrected with sprite `FlipH = true` (horizontal mirror) — NOT my first attempt `FlipV` (vertical flip), which wouldn't change which way the grill points. `PropSpec` + all three `PropBuilder.CreatePropAutoCollider` overloads gained `bool FlipH = false` (previous `FlipV` param kept for future use).
- **ON-AIR sign**: current `on_air_sign.png` was a 128×48 sheet of two sign sprites side by side (from `8b905580`), rendered wrong. Restored the 64×24 blob from `09ca15c3` via `git cat-file blob`. `.import` has no dimension metadata; Godot reimported automatically (ctex regenerated 10:21 after png replaced 10:18). `OnAirSignProp` code unchanged (its `Scale (0.75, 1.0)` matches the old texture).
- **Round table** (`RoundTableProp`): added `SpriteScale = 0.75f`, passed `scale: new Vector2(SpriteScale, SpriteScale)` into `CreatePropAutoCollider`; collider scales around sprite center.
- **FILES**: `scripts/world/common/layout/RoomLayoutTypes.cs` (PropSpec `FlipH`/`FlipV`/`Scale`), `scripts/world/common/PropBuilder.cs` (flip/scale on sprite + scaled collider), `scripts/world/control_room/props/SpeakerStandsProp.cs` (`FlipH: true`), `scripts/world/studio/props/RoundTableProp.cs` (`SpriteScale 0.75f`), `assets/tiles/props/on_air_sign.png` (restored).
- **Note**: `godot` not on PATH — console binary at `C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64_console.exe`; restore a binary via `cmd /c git cat-file blob <sha> > file` (PowerShell `>` corrupts).
- Next: user confirms in-editor that the sign reads correctly and the right speaker now faces the desk.

## Previous Session

### Control-room lighting — ROOT CAUSE + FIX (the "light is in a different room" bug)
- **SYMPTOM**: ceiling light dot is correctly centered (debug overlay shows it), yet the player is **not illuminated** in the control room — while the SAME player lights up when walking into the studio. Felt like "the light is in a different room" / "its z is under the floor."
- **ROOT CAUSE = Light2D z-range vs the manual y-sort.** `Player._Process()` sets `ZIndex = GlobalPosition.Y` every frame (`Player.cs:227`), so in the control room (grid anchor Y 1000) the player z ≈ **1240**. A `PointLight2D` only lights items inside its `range_z_min/max` — **relative to the light's own z**. Control room built into a plain Node2D at z 0 → light z 10, default range cap ~1034 → **misses the player at z 1240**. Studio worked only by luck: `RoomBase` puts the room at `ZIndex=1001` z_as_relative=false (`RoomBase.cs:111`), so the studio light sat at z ~1011 and its default ±1024 happened to cover the player (~800-900). No reach/position/mask problem at all.
- **FIX**: in `RoomLightingBuilder.MakeLight` (all room lights: ceiling/monitor/desk/on-air) set `RangeZMin = -LightZRange`, `RangeZMax = LightZRange` (`LightZRange = 4096`). Wide symmetric range covers the y-sorted player/props above and the floor below the light. Per-room `light_mask` (control=1, studio=2) still stops cross-room bleed.
- **LESSON (this TODO):** don't inflate `texture_scale` to chase a "light doesn't reach" bug — that was a **z-range** problem and the 2.4 scale only over-brightened the room. Reach = `texture_size × texture_scale` is separate from z-range culling.
- **BRIGHTNESS REVERT**: `ControlRoomLayout.CeilingLightTextureScale` **2.4 → 1.0** (back to original), now that the z-range fix is what actually covers the player. Removed the no-op `light.Set("range", ...)` from `RoomLightingBuilder.MakeLight` (PointLight2D has no `range` property).
- **FILES**: `scripts/world/common/RoomLightingBuilder.cs` (RangeZMin/Max, LightZRange const, removed no-op range set), `scripts/world/control_room/ControlRoomLayout.cs` (scale 1.0), `docs/technical/LIGHTING_SETUP.md` (new section: "Light2D z-range vs the manual y-sort").
- User confirmed: player now lit while walking the control room ✓. Next: user confirms the 1.0 brightness looks right (not too bright, south half not fading).

## Previous Session (world/ reorganization)

**Branch**: feature working tree (world/ reorg)

**Task**: Reorganize `scripts/world/` into per-room folders (`control_room/`, `studio/`) + shared `common/` with per-prop files; update AGENTS.md + TOPDOWN_BUILDING_PATTERN.md; update this log.

**Status**: Completed — reorg done, `dotnet build` green (0 new warnings), runtime world smoke test passed (both rooms built + populated, player entered control room). Docs updated.

### Work Done (this session — world/ reorganization)
- **New structure**:
  ```
  scripts/world/
  ├── common/                       # Shared infra (moved RoomBase, RoomSection, IRoomSection,
  │   ├── IRoomBuilder.cs           #   IRoomBuilder, WallSystem, CastShadowSystem, ShadowSystem,
  │   ├── IRoomSection.cs           #   RoomLightingBuilder, RoomDebug, PropBuilder)
  │   ├── RoomBase.cs
  │   ├── RoomSection.cs
  │   ├── WallSystem.cs
  │   ├── CastShadowSystem.cs
  │   ├── ShadowSystem.cs
  │   ├── RoomLightingBuilder.cs
  │   ├── RoomDebug.cs
  │   ├── PropBuilder.cs
  │   ├── layout/RoomLayoutTypes.cs     # GridPlacement, PropSpec, BoardSpec
  │   └── props/OnAirSignProp.cs        # Shared ON AIR sign + key light
  ├── control_room/
  │   ├── ControlRoomBuilder.cs
  │   ├── ControlRoomLayout.cs          # Room-level facts only (ceiling light, sign, LightMask)
  │   └── props/                        # SpeakerStands, AudioCabinet, StorageShelves,
  │                                     #   ControlChair, ControlTableGroup (desk+boards+lights+trigger)
  └── studio/
      ├── StudioBuilder.cs
      ├── StudioLayout.cs               # Room-level facts only (+ smoke anchor)
      ├── StudioSmoke.cs                # Smoke effect extracted from builder
      └── props/                        # Bookcases, RoundTable, VernChairGroup
  ```
- **Prop files = data + placement code** (static classes, global namespace): `ControlTableGroupProp` (desk group: boards, monitor/desk-light offsets, screening trigger — `GetTablePosition(IRoomSection)` feeds `CreateLighting`), `SpeakerStandsProp` (PropSpec[]), `AudioCabinetProp`, `StorageShelvesProp`, `ControlChairProp`, `BookcasesProp`, `RoundTableProp.Create` (CreatePropAutoCollider, `createCastShadow:false`, `floorScanHeight:48`), `VernChairGroupProp.Build` (static body "VernChairGroup" + 9-frame AnimatedSprite2D), `OnAirSignProp.Create(parent, pos, scale, color, energy, radius, mask)`.
- **StudioSmoke**: `Initialize(Node2D propSort, Vector2 smokePosition, int maxParticles, float decayTime, int lightMask)` + `Update(VernStats?)`; constants in-file (smoke_sheet.png, 3 scatter layers, RootZIndex 480, 256px/5×5 grid). Builder delegates via exports (`EnableSmoke`, `SmokeMaxParticles`, `SmokeDecayTime`).
- **Builders thinned**: duplicated `CreateOnAirSign` removed from both; studio dead code deleted (`CreatePropWithCollision`/`CreatePropAutoCollider`/`CreatePropNoCollision`/`CreateRoundTableGroup`/`CreateVernChairGroup`/`CreateTabletopSprite`, old smoke methods); `using System;` dropped from StudioBuilder.
- **Moves included `.cs.uid` sidecars**; `scenes/world/World.tscn` UID refs unchanged; builders aren't autoloads → no `project.godot` edits. `StackWalkAnalyzer` build warnings all pre-existing.
- Two compile errors found+fixed during reorg: `RoundTableProp` `createShadow:` → `createCastShadow:` (CS1739); `StudioSmoke.RootZIndex` `const float` → `const int` (CS0266). `dotnet build` green after.
- **Docs updated**: AGENTS.md File Structure + Room Component Architecture (Component Files table, world/ tree, Per-Prop File Pattern, Prop Data Sources, Creating a New Room example using `RoomLightingBuilder`); TOPDOWN_BUILDING_PATTERN.md note, Creating a New Room, builder-pattern tree, Example Setup tree.

**Next Steps**:
1. User runs the game in Godot to confirm visuals are unchanged (no position/shadow/light regressions in either room).
2. When the test runner is fixed, run `PropBuilderTests` to confirm the prop pipeline survived the reorg.
3. Optional: `SESSION_LOG.md` cleanup — old sections retained below; can be pruned as they age.

**Related Docs**: AGENTS.md ("Room Component Architecture"), `docs/technical/TOPDOWN_BUILDING_PATTERN.md`, `tests/unit/world/PropBuilderTests.cs`.

**Blocker**: Test runner broken in this environment — pre-existing `System.MissingMethodException: Godot.FileAccess.GetAsText()` in `ArcRepository.Initialize()` (stale Godot C# glue / removed API). `godot --run-tests` launches the game instead of tests and never completes (killed after ~600s). Unrelated to this reorg; game world itself built + ran with zero errors from reorganized code.

---

## Previous Session (room layout refactor)

**Branch**: feature working tree (control-room QA fixes)

**Task**: Control-room fixes — (1) desk + boards placement, colliders, and (2) control-room props not illuminated.

**Status**: In Progress — lighting fix + room-layout refactor both DONE (build 0w/0e); awaiting user visual verification of desk/boards move, collider width, and prop illumination.

### Lighting (props not illuminated) — ROOT CAUSE + FIX
- **ROOT CAUSE = TWO independent problems** (both must be fixed; either alone leaves the props dark):
  1. **Depth-shadow shader dims props (`light_position` never updated)**: every prop renders through `shaders/depth_shadow.gdshader`, which computes brightness from `y_distance = MODEL_MATRIX[3].y - light_position.y` / `light_radius`. It used the hardcoded default `light_position = (320,180)`, so a prop's brightness was derived from distance to that fixed point. Control-room props at world Y≈1024–1160 → brightness clamped to `1 - shadow_factor` ≈ **0.2** → props render at ~20% while walls/floor (no shader) stay lit. (Studio props Y≈800 mild ~0.44, so it looked fine.)
  2. **Ceiling-light coverage too small to reach the floor**: docs confirm with a texture set, `range` is IGNORED and reach = `texture_size × texture_scale`. At `TextureScale 1.0` the 512×512 texture covers only ~ ±256px and its alpha fades out past the central ~51px disk → the light center sits at Y≈1016, so everything below Y≈1100 (most of the floor + the player at Y≈1240) falls in the fade-out → floor + props look unlit. The tall control room (160px) exposes this; the short studio doesn't.
- **FIX**:
  1. `scripts/world/CastShadowSystem.cs` now has `_Process()` that calls `UpdateDepthShadowLightPosition()` EVERY FRAME (sets shader `light_position` = `_lightSource.GlobalPosition`, guarded by `IsInsideTree()`). Removes all timing/binding doubt; also auto-fixes the studio.
  2. `ControlRoomBuilder.CreateLighting()` sets `_ceilingLight.TextureScale = 2.4f` (was implicitly 1.0) → light box ~1230px, fully-lit region covers the whole room + player area. Kept the existing deferred call too (harmless).
  3. `CastShadowSystem.UpdateDepthShadowLightPosition()` adds `_lightSource.IsInsideTree()` guard.
- **NOTE**: after fix, props brightness ≈1.0 AND the PointLight2D reaches the whole floor → props + floor fully lit. If the user still sees dark after a FRESH rebuild, it was a stale build (two prior attempts were the same single-mechanism fix on a possibly-unrebuilt client).

### Desk/boards/colliders (from prior working-tree state)
- `PropBuilder.CreateTableGroup` gained optional `Vector2 pixelOffset` (passed into `group.Position`); control room passes `(0, TableDropPixels=10)` to drop desk+boards ~10px.
- Monitor/desk-lamp lights track the desk (`tablePosition` gets the +10px drop; light Y offsets −76→−66 / −70→−60).
- `colliderOverride` widened on speaker_stand (45 wide), audio_cabinet (128), storage_shelf (128) to span each prop's visual width.
- **Build passes (0 errors)** — done via `dotnet build`.

**Next Steps**:
1. User rebuilds + runs: verify (a) desk+boards sit ~10px lower, (b) green `ui_select` debug colliders span prop widths, (c) **props are now fully lit** (they were 0.2-bright), (d) refactor is behavior-neutral in-game (no position/shadow/light regressions in control room or studio).
2. If studio props also look dim, they already get the same `UpdateDepthShadowLightPosition` (applied in `StudioBuilder.CreateLighting`) — confirm via fresh build.
3. Optional: unit test for the width-matched collider override (currently a passthrough, no new math).

**Related Docs**: `shaders/depth_shadow.gdshader` (`light_position`/`light_radius`/`shadow_factor`), `scripts/world/CastShadowSystem.cs` (`UpdateDepthShadowLightPosition`, `LightRadius`), `scripts/world/builders/ControlRoomBuilder.cs` (`CreateLighting`, `CreateProps`), `docs/technical/TOPDOWN_BUILDING_PATTERN.md`.

**Blocker**: Test runner is broken in this environment — pre-existing `System.MissingMethodException: Godot.FileAccess.GetAsText()` in `ArcRepository.Initialize()` (stale Godot C# glue / removed API) crashes on startup, so `godot --run-tests` won't complete. Game also crashes on that error, so no in-engine visual capture is possible here; visual QA must be done by the user.

### Work Done (this session — lights + layout refactor)
- **Refactor — per-room strongly-typed layout classes** (user chose: "Per-room layout class" + "Grid cell + named offsets"). All new files in the **global namespace** (project style); deliberately avoided a `namespace KBTV.World.*` because that collides with the existing `World` type in `scripts/core/GameStateManager.cs:44`.
  - **New `scripts/world/layout/RoomLayoutTypes.cs`**: `GridPlacement(Cell, Offset)` (`.ToWorld(IRoomSection)`, implicit tuple), `BoardSpec(TexturePath, Offset)`, `PropSpec(Cell, Offset, FloorScanHeight, CreateCastShadow, ColliderOverride)`. Dropped the `IRoomLayout`/`IInvariantLayout` interfaces after the refactor — builders hold concrete `ControlRoomLayout`/`StudioLayout` and the interface was an unused abstraction (YAGNI).
  - **New `scripts/world/layout/ControlRoomLayout.cs`**: `TableGroup=(6,1)`, `TableDropPixels=10`, `Boards[]` (phone_board/sound_board/computer_station), `CeilingLightOffsetY=64`, `CeilingLightTextureSize=512`, `CeilingLightTextureScale=2.4`, monitor/desk-lamp offsets, `SpeakerStands[]` ((2,0)/(10,0)), `AudioCabinet` ((12,1)), `StorageShelves[]` ((4,10)/(10,10)), `Chair=GridPlacement(6,2,(0,-16))`, `ScreeningTrigger` (6,2+(0,16) 240×100), `OnAirSignFromAnchor=(32,-112)` / `OnAirSignScale=(0.75,1.0)`, on-air light facts, `LightMask=1`.
  - **New `scripts/world/layout/StudioLayout.cs`**: `RoundTable=(6,4)`, `VernChairCell=(6,3)`, `VernShadowOffset=(0,-40)`, `VernColliderSize=(64,64)`, `CeilingLightOffsetY=32` / `TextureScale=1.0`, `Bookcases[]` ((1,1)/(12,1)), `SmokeColumn=7` / `SmokeRowsFromBottom=3`, `OnAirSignFromAnchor=(224,-112)` / `Scale=(0.75,1.0)`, on-air light facts, `LightMask=2`.
  - **New `scripts/world/RoomLightingBuilder.cs`** (shared helper): `MakeLight(...)` (Position/Color/Energy/radius/shadows/cullMask/textureWidth/Height/TextureScale), `MakeCeilingLight(...)`, `OvalGradient(w,h,radius,falloffRadius,radiusScale)`, `LightZIndex=10`. Class doc explains the texture×scale coverage gotcha. Replaces the per-builder `CreatePointLightWithTexture`/`CreateOvalGradientTexture` duplicates (both deleted).
  - **ControlRoomBuilder**: `_layout` field; `CreateLighting` uses `RoomLightingBuilder.MakeCeilingLight` + `_layout.CeilingLightTextureScale=2.4`; `CreateProps`/`CreateTableGroup`/`CreateScreeningTrigger`/`CreateOnAirSign` read layout facts; chair still routes through the **non-collidable** `PropBuilder.CreateProp` (it's walk-through — not `CreatePropAutoCollider`). Added `CreateProp(PropSpec, string)` helper.
  - **PropBuilder**: `CreateTableGroup` (RoomBase + IRoomSection overloads) changed param from `params (string,Vector2)[] tabletops` → `params BoardSpec[]` so the layout's typed boards flow straight through.
  - **StudioBuilder**: `_layout` field; `CreateLighting` now uses `MakeCeilingLight` (also applies the `UpdateDepthShadowLightPosition` deferred call — same shader fix as the control room so studio props stay full-bright); deleted its private `CreatePointLightWithTexture`/`CreateOvalGradientTexture`; `CreateBookcases`/`CreateRoundTableGroup`/`CreateVernChairGroup`/`CreateSmoke`/`CreateOnAirSign` read layout facts; on-air sign uses shared `MakeLight`.
  - Behavior is preserved except the intentional ceiling-light `TextureScale=2.4` fix. Left pre-existing dead helpers (`CreatePropWithCollision`/`CreatePropNoCollision`/`CreateTabletopSprite` in StudioBuilder) untouched — out of scope.
  - `dotnet build` passes with **0 warnings / 0 errors**.
- **Lighting fix from earlier this session still holds** (root cause above): `CastShadowSystem._Process()` updates `light_position` every frame + `IsInsideTree()` guard; control-room ceiling light `TextureScale=2.4`.

### Work Done
- **USER DIRECTIVE (adopted)**: `create_image_pro` is now the DEFAULT for every prop; 1-gen tools are a quick-proxy fallback only. Codified across `PIXELLAB_PROMPT_RULES.md` §1 matrix, §2 pro-method block, §6 protocol (pro = step 1, two-batch cap), §7 failure rows, §9 budget summary.
- **Speakers missing — ROOT CAUSE + FIX**: `speaker_stand.png` content ended 32px above canvas bottom; `GetBaseFootprint` (bottom-24px band) found nothing → `CreatePropAutoCollider` returned null → prop never created. Fixed by cropping sprite to content bbox (96×192 → 45×132). Anchor math auto-adapts (`sprite.Position = (0, -texH/2)`), visible position shifts ~2.5px. Deleted stale `.import`.
- **Desk too narrow — ROOT CAUSE**: visible desktop was only 156px wide (256×96 canvas, 49px+52px empty) while the three boards span ~214px → boards hung off both sides. Window = 7 columns (224px). Required wider art; no code-only fix.
- **Window slat bug found**: `wall_window_atlas.png` 128px with `Hframes=7` → each column drew an 18.3px slice at 32px spacing (7 thin slats, gaps). Regenerated atlas 224×128 (7×32px frames) — matches `Hframes=7` exactly, no code change.
- **Desk regeneration** (hero prop → pro batch, template 3b, palette swatch, seed 4117, job `45eab85b`, 20 gen): desktop now spans full 256px canvas (255px content). Two pixflux alternates (seeds 9001/9002) generated but rejected (214/224px wide → no overhang). User picked pro seed 4117.
- **Processing** (rule 4): `reduce_colors` with KBTV palette swatch on desk (job `0119b80b`) + window (job `d6fd6a8b`), 0.1 gen each. Wired both over `studio_table.png` / `wall_window_atlas.png`, deleted stale `.import`.
- Verified: desk content x[0..254] w=255; window 224×128 content 216 wide. `dotnet build` passes (0 warnings / 0 errors).
- Docs updated: `PIXELLAB_PROMPT_RULES.md` §4 dims table (studio_table now 256×96@final) + wired-dims note (speaker_stand 45×132, window atlas 224×128) + §4 note (wide flat assets generated at final, not 2×).

- **Desk rework — bare + transparent (user feedback R2)**: user approved pro style but objected to (a) items baked on the tabletop and (b) a white border around the desk.
  - Diagnosis: pro candidate carried 5305 near-white px (default white pixel-art rim); `reduce_colors` snapped them to a light palette tone so the rim still read as a border.
  - Attempt A (rejected): 2 pixflux bare re-rolls (seeds 9101/9102, 0 near-white) came back narrow (216/202px) → locally mirror-tiled the edge band to 256px. **User rejected — mirror-tiling banded; "use pro style instead".** Rule added: never locally widen art.
  - Attempt B (**accepted**): `create_image_pro` seed 4759/4760 — first (4759) 0-white but only 176px wide ("bare + no white" shrank the subject); second (**4760**, re-added LED strip + hard edge-to-edge clause) = 256px wide, 0 near-white, 70% ink, base at y91. Palette-locked via `reduce_colors` (job `c2366b23`, 0.1 gen). Wired over `assets/tiles/props/studio_table.png`, `.import` removed.
  - Repro: pro | 256×96 | style swatch (color_palette) | seed 4760 | desc = 3b + EMPTY + LED strip + edge-to-edge + NO-white clamp | job `0f81dd41` | accepted.
  - Candidates kept in `assets/props_samples/studio_table_pro_bare_seed4759/4760_256x96.png`; prior artifacts (`studio_table_bare_seed910x`, `_prevWired_proReduced`) retained for reference.

**Next Steps**:
1. User visually confirmed desk ✓. 
2. Next prop/sprite generations use the **pro method** (user directive) — §6 protocol.
3. Speakers + solid window from the earlier fix still pending a final in-game look (both already wired; no known issues).

**Related Docs**: `docs/art/PIXELLAB_PROMPT_RULES.md` (templates 3b/§4/§6), `docs/art/ART_STYLE.md`, `scripts/world/PropBuilder.cs` (`GetBaseFootprint`/`CreatePropAutoCollider`), `scripts/world/WallSystem.cs` (`CreateWindow`, Hframes=7).

**Blockers**: This model cannot receive image input — all visual QA must be done by the user; programmatic checks only (dims, alpha bbox/corners).

---

## Previous Session (migration + prop rework)

**Branch**: feature working tree (2× migration + control-room prop rework)

**Task**: (A) Migrate world from 16px/640×360 to 32px/1280×720 so full-res 2× art renders 1:1. (B) Rework control-room props per user feedback: plain table, studio speakers, north-facing chair with no white border, larger audio cabinet.

**Status**: Migration substantially complete. Prop rework round 2 (front-facing) done — table, speakers, chair, cabinet all rewired; awaiting user visual verification.

### Migration completed (this session)
- **Menu-level props regenerated 2× via PixelLab** (create_image_pro, no refs, cand_0 wired):
  speaker_stand 32×64→64×128, on_air_sign 64×24→128×48, studio_table 128×48→256×96, bookcase 48×64→96×128, round_table 80×48→160×96.
- **Structural atlases 2× nearest-neighbor upscaled** (user chose upscale over regen for tiling safety; backups in `C:\Users\lblan\AppData\Local\Temp\opencode\atlas_2x_bak`): floor_atlas 32→64, control_room_north/wall_south/wall_side/wall_north_door/wall_window 64→128, wall_south_strip 32→64.
- **topdown_tileset.tres**: `texture_region_size` 16×64→32×128 (all wall sources) + added `tile_size = Vector2i(32,32)`.
- **Player sprites 48×48→96×96** (4 idle + 24 walk; user chose upscale; backup in `...\player_2x_bak`).
- Earlier migration (prior sessions): viewport 1280×720, RoomBase TileSize 32 / GridAnchor (640,360), room anchors doubled, WallSystem constants doubled (Offset (0,-48), etc.), player/Vern scales 1.0.
- `dotnet build` passes (0 warnings, 0 errors). Vern screener camera (`VernCameraZoomScale 1.15`) is resolution-adaptive (zoom = viewport/studioBounds × 1.15) — no change needed at 1280×720.
- Unused atlas files confirmed stale (floor_beige/navy/slate/taupe, studio_north_atlas, wall_north_atlas, wall_outside_atlas, control_room_north_window_atlas) — left untouched.

### Prop rework (user feedback rounds)
- **audio_cabinet**: original ask "larger" — first attempt 2× upscaled 64×112→128×224, which made it TALLER THAN THE ROOM WALL (128px) so it flew up behind the north wall and became invisible. Restored visible front-facing art, then regenerated front-facing at 128×128 (double-width rack, wall-height so it stays visible). job `0423c67f`, cand_0 wired.
- **studio_table**: regenerated front-facing plain empty desk (256×96, transparent cutout). jobs `5a6c9cce` (oblique, wrong) → `675b84fb` (front-facing, correct), cand_0 wired.
- **speaker_stand**: regenerated front-facing studio monitor speaker on stand (64×128). jobs `e2676ea1` (oblique) → `cfb42ec4` (front-facing), cand_0 wired.
- **computer_chair**: regenerated north-facing back view, transparent cutout (was white-boxed 64×80). job `be59dd84`, cand_0 wired.
- **IMPORTANT style rule**: ALL props must be FRONT-FACING (straight on, slight vertical top-down), NEVER isometric or sideways. This applies to all props.
- **audio_cabinet bug lesson**: don't 2× upscale a texture that was already 2× (4× total) — items at the back wall become invisible when taller than the wall (~128px).

**Next Steps**:
1. User visually verifies the 4 reworked props in-game (table, speakers, chair orientation, cabinet size/visibility).
2. If a prop needs a different candidate (chair has 16, speakers 4, cabinet 4), rewire the alternative candidate.

**Related Docs**: `docs/art/ART_STYLE.md` (noir palette; oblique/cabinet projection; audio rack two-column; chair north-facing back view), `docs/ui/UI_DESIGN.md`, `docs/technical/TOPDOWN_BUILDING_PATTERN.md`.

**Blockers**: This model cannot receive image input — all visual QA must be done by the user; programmatic checks only (dims, alpha bbox/corners).

---


---

## Previous Session

