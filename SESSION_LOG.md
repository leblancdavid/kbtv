## Current Session

**Branch**: feature working tree (control-room QA fixes)

**Task**: Control-room fixes — (1) desk must fit the window width so all three boards (phone/sound/computer) sit on it; (2) speakers were not appearing at all.

**Status**: Completed — desk accepted; pro method set as default for all future prop generation

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

## Previous Session

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

