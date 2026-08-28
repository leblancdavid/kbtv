## Current Session

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

