## Current Session

**Branch**: 3d-migration
**Task**: Complete the control-room Blender prop and World3D visual pass.
**Status**: In Progress

### Work Done
- Confirmed clean working tree; reusing accepted audio cabinet.

### Todo / Next Steps
- [ ] Inspect generator, scene, room scripts, and workflow guidance.
- [ ] Generate modular props, Blender sources, previews, and validation reports.
- [ ] Inspect previews and integrate supported, correctly oriented scene placements.
- [ ] Verify build, tests, Godot 4.6 import/runtime; document results and placements.

### Files Modified
- `SESSION_LOG.md`

---

## Previous Session

**Branch**: 3d-migration
**Task**: Document the GPT-assisted Blender 3D prop workflow for future development.
**Status**: Completed

### Work Done
- Reworked `docs/art/3D_ASSET_WORKFLOW.md` from a trial-result note into a reusable developer workflow.
- Documented toolchain requirements, folder conventions, GPT prompt template, generator pattern, validation checklist, Godot placement checklist, accepted trial assets, and troubleshooting.
- Preserved the audio cabinet and microphone stand dimensions/placement as concrete examples for future props.

### Files Modified
- `SESSION_LOG.md`
- `docs/art/3D_ASSET_WORKFLOW.md`

### Next Steps
1. Use this workflow for the next one or two simple props before scaling production.
2. If the workflow continues to hold up, add a small Godot review scene or automated prop placement helper.

---

## Previous Session

**Branch**: 3d-migration
**Task**: Import generated 3D prop GLBs into the world scene.
**Status**: Completed

### Work Done
- Started placement pass for `audio_cabinet.glb` and `microphone_stand.glb` in `World3D.tscn`.
- Preserving existing generated collision boxes while replacing only placeholder visuals.
- Added both GLBs as external packed-scene resources in `World3D.tscn`.
- Replaced the control-room audio cabinet placeholder visual with `audio_cabinet.glb` at bottom-origin room-local position `(4.1, 0, -2.3)`.
- Replaced the studio mic cylinder visual with `microphone_stand.glb` at room-local position `(0.95, 0.2, 0.65)`.
- Rotated both models 180 degrees around Y so their generated fronts face back toward the room/gameplay camera.
- Verified with `dotnet build`; build passes.
- Ran Godot 4.6.3 headless project check; GLBs imported without new model-reference errors. Existing unrelated invalid UID warning remains in `scenes/world/World.tscn`.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest `Game3D.tscn` and check scale/readability under actual station lighting.
2. Tune positions/rotations if the cabinet or mic face the wrong direction in the gameplay camera.

---

## Previous Session

**Branch**: 3d-migration
**Task**: Restore 3D room-based audio (control = full audio, studio = Vern only, else muffled) and fix the broken GoDotTest test toolchain.
**Status**: In Progress

### Work Done
- Diagnosed why all audio was muffled after the 3D migration: `RoomStateManager._Process()` looks up the player via `GetTree().GetFirstNodeInGroup("player") as Player` (2D `Player` type); `Player3D : CharacterBody3D` is never added to the `"player"` group, so the lookup always fails and `CurrentLocation` stays `Outside` forever; `BroadcastAudioService` then initializes everything muffled and `PlayerLocationChanged` never fires. The 3D world also never registered room bounds (`SetControlRoomBounds`/`SetStudioBounds`) — only the 2D rooms did.
- Added `RoomStateManager.SetPlayerLocation(PlayerLocation)` (-manual reporting API with a `_manualLocation` guard so 2D bounds detection cannot fight it); emits `PlayerLocationChanged` only on change.
- Wired `World3D._UpdatePlayerRoomState()` to report `InControlRoom` (room or control-room doorway), `InStudio` (studio), else `Outside` to `RoomStateManager` every frame via the cached `/root/RoomStateManager` autoload.
- Added `using KBTV.Core;` to `World3D.cs` to fix `CS0246: RoomStateManager could not be found`.
- Added `tests/unit/core/RoomStateManagerTests.cs` (3 tests: updates location, emits only on change, disables bounds detection in `_Process`).
- Root-caused the "tests hang" issue: `--run-tests` was never honored because the main scene is the game (`Game3D.tscn`); no harness checks the flag. GoDotTest must be launched with the test scene explicitly (`res://test/Tests.tscn`). The `godot` CLI binary was also not on PATH and the documented engine was the wrong version (4.5.1) — running the 4.6 project with 4.5.1 throws `FileAccess.GetAsText()` `MissingMethodException`.
- Added `run-tests.ps1`: auto-detects a Godot 4.6 mono console build (ignores a wrong-version `GODOT` env var with a warning), launches `test/Tests.tscn` via `--main-scene`/scene arg, parses the GoDotTest summary, and exits non-zero when any test fails.
- Copied the Godot 4.6.3 mono install from `opencode\godot463` (temp) into `D:\Software\Godot\Godot_v4.6.3-stable_mono_win64` so the toolchain is stable (Temp gets cleaned).
- Updated `report-tests.bat`, `run_tests_quick.bat`, `run_tests_capture.bat` (fixed `TestRunner.tscn` → `Tests.tscn`) and report-tests.sh to the 4.6 engine / correct args.
- Updated `AGENTS.md` and `docs/testing/TESTING.md`: replace `godot --run-tests` with `pwsh -NoProfile -File run-tests.ps1`, correct engine version notes, fix the CI example.
- Verified: `dotnet build` passes; full suite runs via `run-tests.ps1` → **Passed: 487 | Failed: 13 | Skipped: 0**. All 13 failures are pre-existing (AutoInject providers missing in tests: `No provider found for service GameStateManager/EventBus/TimeManager`, etc.) and unrelated to this work. The 3 new `RoomStateManagerTests` pass.

### Files Modified
- `SESSION_LOG.md`
- `scripts/core/RoomStateManager.cs`
- `scripts/world3d/World3D.cs`
- `tests/unit/core/RoomStateManagerTests.cs` (new)
- `run-tests.ps1` (new)
- `report-tests.bat`
- `report-tests.sh`
- `run_tests_quick.bat`
- `run_tests_capture.bat`
- `AGENTS.md`
- `docs/testing/TESTING.md`

### Next Steps
1. Playtest audio in 3D: control room = full audio, studio = Vern only, corridors/equipment = muffled.
2. Optionally fix the 13 pre-existing test failures (missing AutoInject providers) in a follow-up pass.
3. Consider making the temp/`GODOT` env var point at the new stable 4.6.3 engine path (currently auto-detected).

---

## Previous Session

**Branch**: 3d-migration
**Task**: Generate first Blender-authored 3D props: audio cabinet and microphone stand.
**Status**: Completed

### Work Done
- Confirmed clean working tree and selected reproducible Blender Python to GLB workflow.
- Targeting meter-scale, bottom-center origins, and Godot-facing negative Z.
- Generated both GLBs, editable Blender sources, neutral-lit previews, and validation reports.
- Fixed degenerate bevel geometry before export and verified both GLBs by clean re-import.
- Inspected both preview images; documented generation and Godot placement workflow.

### Files Modified
- `SESSION_LOG.md`
- `AGENTS.md`
- `Tools/modelgen/` (Python generators and editable `.blend` sources)
- `assets/models3d/props/audio_cabinet.glb`
- `assets/models3d/props/microphone_stand.glb`
- `docs/art/3D_ASSET_WORKFLOW.md`
- `docs/art/model_previews/` (PNG previews and JSON validation reports)

### Next Steps
1. Import/place trial props in Godot and review under station lighting.
2. Tune silhouette and small details based on gameplay-camera feedback.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Remove rectangular haze veil artifacts and rely on true room-filling studio fog.

**Status**: Completed

### Work Done
- Started implementation pass for a persistent smoky 3D studio.
- Confirmed active main scene is `Game3D.tscn`, so smoke belongs in `StudioRoom3D` rather than the older 2D `StudioSmoke` path.
- Added `StudioSmoke3D`, a procedural 3D billboard-smoke node with persistent ambient haze and periodic cigarette puff bursts.
- Wired `StudioRoom3D` to create the smoke node with exported tuning values for density, opacity, puff timing, drift, origin, and room extents.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Removed the three `StudioFogVeil` billboard fallback planes after playtest showed them as distinct fog rectangles.
- Removed obsolete haze texture/shader generation and `HazeSwirlSpeed` / `HazeSwirlStrength` exports.
- Raised the actual studio-local `FogVolume` density default via `AmbientSmokeOpacity` from `0.24` to `0.45` so the fog fills the room without visible cards.
- Kept Vern puffs and continuous door-leak smoke behavior intact.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Lowered true studio `FogVolume` density default from `0.45` to `0.38` after playtest showed the no-rectangle fog looked good but too dense.
- Added `FogMotionSpeed` and `FogMotionStrength` exports that subtly animate true fog density, position, and X/Z size so the room haze breathes without reintroducing haze-card rectangles.
- Set default fog motion to `FogMotionSpeed = 0.24` and `FogMotionStrength = 0.08`.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Increased true fog motion visibility after playtest showed the movement was still hard to notice.
- Raised `FogMotionSpeed` from `0.24` to `0.55` and `FogMotionStrength` from `0.08` to `0.22`.
- Widened fog density pulsing and increased fog volume position/size modulation while keeping the effect on the real `FogVolume` only.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started follow-up after playtest showed haze swirl was not noticeable and door smoke should continuously leak while doors are open.
- Increased haze animation defaults: `HazeSwirlSpeed` to `0.11`, `HazeSwirlStrength` to `0.46`, and added more visible slow veil position/scale movement.
- Lowered door leak opacity to `0.045` and added `DoorLeakInterval` so door wisps emit sparsely and continuously while a studio door is open.
- Replaced the one-shot player-transition leak trigger with door-state-driven leak activation from `DoorLightLinkChanged` for the control/studio and studio/hall doors.
- Added separate state/timers for each studio door leak so either studio exit can leak independently while open.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Raised default studio haze opacity from `0.16` to `0.24` in `StudioRoom3D` and `StudioSmoke3D` after playtest feedback that the fog was not visible enough.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started animation pass after the broad haze read better but still felt too flat/static.
- Replaced the haze veil standard material with a small shader that slowly drifts and blends two haze texture samples for subtle swirl/movement.
- Added `HazeSwirlSpeed` and `HazeSwirlStrength` exports on `StudioRoom3D` and `StudioSmoke3D`.
- Added a separate door-leak smoke pool and `EmitDoorLeak` method for subtle outward wisps at studio exits.
- Added `StudioRoom3D.EmitDoorSmokeLeak(...)` and wired `World3D` to trigger it once when the player leaves `STUDIO` for any other resolved room/doorway.
- Kept Vern's existing cigarette puff behavior unchanged.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started visibility pass after `AmbientSmokeOpacity` changes were not visibly affecting the studio haze.
- Added broad non-puffy studio fog veils as a visible orthographic fallback, driven by the same `AmbientSmokeOpacity` knob as the local `FogVolume`.
- Raised `AmbientSmokeOpacity` default to `0.16` in both `StudioRoom3D` and `StudioSmoke3D` so the active room passes a visible value at runtime.
- Lightened the fog color and flattened the `FogVolume` falloff so the haze reads more like cigarette smoke in the whole room.
- Kept Vern's local cigarette puff behavior unchanged.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started fog-only revision after playtest showed the room smoke cloud billboards still read as unnatural isolated puffs.
- Removed the whole-room billboard cloud layer and its exports (`RoomCloudCount`, `RoomCloudOpacity`).
- Tuned the local studio `FogVolume` to carry the room smoke: `AmbientSmokeOpacity` now defaults to `0.085`, with flatter `HeightFalloff` and softer `EdgeFade`.
- Kept cigarette puff behavior and opacity unchanged.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started room-fog visibility pass after playtest confirmed puffs look good but the overall studio smoke is not noticeable enough.
- Raised studio-only `FogVolume` density via `AmbientSmokeOpacity` from `0.022` to `0.048`.
- Raised full-room smoke cloud layer from `7` to `9` clouds and `RoomCloudOpacity` from `0.045` to `0.07`.
- Kept cigarette puff opacity/timing unchanged because the puff effect is already reading well.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.
- Started iteration after playtest showed the first-pass billboard quads render as visible blocky rectangles.
- Replaced the ambient billboard field with a studio-local `FogVolume` using a low-density cool grey `FogMaterial`.
- Reworked cigarette puffs to use procedural soft/noisy alpha textures on billboard quads, avoiding visible rectangular cards.
- Fixed the Godot C# fog shape enum to `RenderingServer.FogVolumeShape.Box`.
- Enabled volumetric fog globally at zero density in `StationLighting3D` so local `FogVolume` nodes can render without adding world-wide haze.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started tuning pass after playtest confirmed the improved smoke shape works but should be subtler, with a separate whole-room smoky cloud.
- Lowered default studio smoke intensity: `AmbientSmokeOpacity` from `0.105` to `0.022`, puff opacity from `0.24` to `0.16`.
- Added `RoomCloudCount` and `RoomCloudOpacity` exports to drive a separate subtle whole-room smoke cloud layer.
- Added slow oversized procedural smoke cloud billboards across the studio volume, separate from the local fog volume and cigarette puffs.
- Removed the unused ambient smoke count export from the 3D smoke implementation.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StudioSmoke3D.cs`
- `scripts/world3d/StudioSmoke3D.cs.uid`
- `scripts/world3d/StudioRoom3D.cs`
- `scripts/world3d/StationLighting3D.cs`

### Next Steps
1. Playtest true `FogVolume` visibility at `AmbientSmokeOpacity = 0.38`.
2. Tune `FogMotionSpeed` / `FogMotionStrength` if the haze motion is still too subtle or becomes distracting.
3. Playtest both studio exits and tune `DoorLeakSmokeOpacity` / `DoorLeakInterval` if the continuous leak is too visible or too sparse.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Fix fluorescent shadow halting in the rest of the world.

**Status**: Completed

### Work Done
- Started shadow-system pass for fluorescent lights and stuck-looking shadows.
- Found fluorescent wash/fill lights had `ShadowEnabled = false`.
- Found flat generated 3D floor/route-marker meshes use default mesh shadow casting, which can create fixed ground shadows.
- Found 2D `PropBuilder` accepts `createCastShadow` but never creates the shadow.
- Enabled shadows on fluorescent wash spotlights while leaving broad fill lights non-shadowed.
- Disabled mesh shadow casting on generated floors, walkway, route markers, and scene-authored 3D floor/threshold meshes.
- Wired 2D prop cast-shadow creation in `PropBuilder` for both auto-collider and explicit-collider paths.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.
- User confirmed the halting/stuck-looking shadow issue still appears under the rest-of-world fluorescent lights.
- Changed station fluorescents so all wash/fill lights still illuminate, but only the nearest fluorescent wash spotlight casts shadows each frame.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world/common/PropBuilder.cs`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/World3D.cs`

### Next Steps
1. Playtest the hallway/rest-of-world fluorescents and confirm the player shadow no longer appears to halt or leave fixed duplicates behind.
2. If the nearest-light handoff is too abrupt, add a small distance hysteresis before switching fluorescent shadow casters.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Fix 3D control room window frame aliasing and stray green object.

**Status**: Completed

### Work Done
- Started a focused pass on the 2D control room north wall/window rendering.
- Identified that `WallSystem` hardcodes the generic `wall_window_atlas.png`, which contains the visible green object.
- Identified unused control-room-specific assets: `control_room_north_atlas.png` and `control_room_north_window_atlas.png`.
- Added configurable window texture, frame count, and offset settings to `WallSystem` while preserving the old generic defaults.
- Configured `ControlRoom` to use `control_room_north_atlas.png` and `control_room_north_window_atlas.png`.
- Reduced the control room visual window span from columns `3..9` to `6..7` to match the 2-frame control room window asset.
- Verified with `dotnet build`; build passes with existing warnings.
- User provided a screenshot showing the artifact is in the 3D control room, not the 2D wall system.
- Identified the scene-authored `OnAirSign` mesh at world `z ~= -0.05`, directly behind/on the generated control/studio window plane.
- Identified the generated 3D window half-wall and frame pieces share the same `z = 0` plane/depth as adjacent wall geometry, making z-fighting likely.
- Restored the generated 3D control/studio window half-wall and frame pieces to the wall centerline/full wall depth so they remain part of the wall.
- Hid the scene-authored green `OnAirSign` placeholder in `World3D.tscn` so it no longer renders inside the window.
- Corrected the initial 3D offset approach after playtest showed the trim no longer lined up with the wall.
- Removed duplicate generated corner posts at the control/studio window jambs so the window frames themselves fill those wall endpoints without overlapping extra wall blocks.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world/common/WallSystem.cs`
- `scripts/world/control_room/ControlRoom.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest the 3D control room and confirm the window frame no longer flickers/aliases.
2. Replace the hidden placeholder `OnAirSign` with a real red sign on a non-window wall when signage art/layout is ready.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Change door-open lighting to localized doorway spill.

**Status**: Completed

### Work Done
- Started a refinement pass so open doors create localized doorway light spill instead of lighting the whole adjacent room.
- Removed open-door whole-room light mask widening from `StationLighting3D`.
- Kept primary control, studio, equipment, and station lights permanently isolated to their own visual layers.
- Added disabled-by-default doorway spill lights for Control/Studio, Control/Station, Studio/Station, and Equipment/Station links.
- Door-open events now toggle only the localized short-range spill lights for the matching doorway.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`

### Next Steps
1. Playtest open doors and tune doorway spill `LightEnergy`, `OmniRange`, and `OmniAttenuation` in `StationLighting3D` if the glow is too wide or too subtle.
2. If omnidirectional spill still feels too round, replace specific doorway spills with directional spot spill lights aimed through each opening.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add room-isolated 3D lighting with door-open light spill.

**Status**: Completed

### Work Done
- Started a lighting isolation pass to prevent light bleed through walls while letting open doors link light between adjacent rooms.
- Converted `StationLighting3D` into a stateful node that tracks control, studio, equipment, and station light groups.
- Added visual/light layer constants and cull-mask refresh logic so each room's lights affect only that room by default.
- Added door-open light links for Control/Studio, Control/Station, Studio/Station, and Equipment/Station doors.
- Assigned generated station floors/props to room-specific visual layers, with shared walls/doors on interior layers.
- Assigned scene-authored control/studio meshes and the player visual to the correct lighting layers, including multi-room player lighting at thresholds.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/World3D.cs`

### Next Steps
1. Playtest closed doors between control/studio/hall/equipment to confirm light no longer bleeds across floors/props.
2. Playtest opening those doors to confirm adjacent-room light spill appears only while the door trigger is active.
3. If walls still look too globally lit, split shared wall meshes into per-room visual layers in a follow-up pass.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Tune 3D lighting with centered room lamps and hidden brighter fluorescents.

**Status**: Completed

### Work Done
- Started a lighting tune to simplify control/studio/equipment lighting to one centered overhead lamp per room and make fluorescents brighter, whiter, and hidden.
- Replaced the two-light control room setup with one centered overhead pendant/spot.
- Replaced the two-light studio setup with one centered overhead pendant/spot.
- Added one centered overhead pendant/spot for the equipment room.
- Changed fluorescent hue closer to white, increased fill/wash strength, and stopped rendering visible fluorescent fixture bars.
- Raised ambient energy slightly to keep the noir mood readable.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`

### Next Steps
1. Playtest the latest lighting pass in Godot and tune brightness/range if needed.
2. Do the separate 3D player/prop shadow policy pass after the lighting baseline is approved.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Correct first-pass 3D lighting readability and fluorescent behavior.

**Status**: Completed

### Work Done
- Started a correction pass after playtest showed the noir rooms were too dark and fluorescent fixtures were visible without useful light.
- Raised the dark ambient environment and exposure enough to keep unlit geometry readable.
- Broadened and brightened the control/studio overhead spots while keeping shadows only on the main noir practicals.
- Changed fluorescent fixtures from shadowed spotlights into broad no-shadow omni fill plus a soft downward wash.
- Disabled shadow casting on decorative light fixtures so light bars, pendant shades, cords, and bulbs do not block their own lights.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/StationLighting3D.cs.uid`
- `scripts/world3d/World3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest readability in Godot and tune `StationLighting3D` light energy/range if any room is still too dark or too flat.
2. Do the separate 3D player/prop shadow policy pass after the lighting baseline feels right.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add first-pass 3D noir and fluorescent lighting.

**Status**: Completed

### Work Done
- Started the first 3D lighting pass for dark noir control/studio rooms and cooler fluorescent station spaces.
- Added `StationLighting3D`, which builds a dark `WorldEnvironment`, warm overhead spotlights with visible pendant fixtures for the control room and studio, accent glows, and cooler fluorescent station lighting.
- Wired `StationLighting3D` into `World3D._Ready()`.
- Removed the old broad `DirectionalLight3D` from `World3D.tscn` so practical lights define the mood.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/World3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest the room readability and tune light energy/range/positions in `StationLighting3D`.
2. Do a separate 3D shadow pass for explicit player/prop shadow policy and bias/contact-shadow tuning.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Tune double doors and move the control/studio doorway left to avoid speaker overlap.

**Status**: Completed

### Work Done
- Started a follow-up pass to make double doors twice the regular door size, open one/both leaves based on player position, and shift the control/studio doorway left.
- Changed double doors to use `SingleDoorWidth * 2f` while keeping regular doors narrow.
- Expanded exterior double-door wall gaps back out to fit the larger two-leaf doors.
- Added per-door center/orientation and per-leaf side signs so double doors open one side when the player is off-center and both sides when the player is near the split.
- Kept double-door leaves swinging outward by retaining explicit per-leaf open rotations.
- Moved the control/studio doorway left in generated greybox markers, generated door placement, room doorway checks, and scene trigger/threshold nodes.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest each double door by entering on left/right/center to confirm one-leaf vs two-leaf behavior feels correct.
2. Check the moved control/studio doorway against the left speaker and wall jambs in-editor.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Tune 3D greybox doors after playtest: narrower doors, no player collision, and outward double-door swing.

**Status**: Completed

### Work Done
- Started a door tuning pass from playtest feedback.
- Reduced generated door panel widths to about half the previous size.
- Tightened generated wall gaps around door openings so the narrower greybox doors fit better visually.
- Removed temporary `StaticBody3D` collision from generated door panels so they no longer block or snag the player.
- Changed double exterior doors to store explicit per-leaf open rotations so both leaves swing toward the outside.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest in Godot to confirm the tightened wall gaps still leave comfortable player clearance.
2. Check each exterior double door from camera view to verify its outward swing reads correctly.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add 3D greybox doors at marked level openings with fixed-direction hinge animation and auto-close triggers.

**Status**: Completed

### Work Done
- Started a focused pass on 3D greybox doors using the existing marked threshold locations in `StationGreybox3D`.
- Added generated greybox door leaves for every marked threshold in `BuildRouteMarkers()`.
- Added single-door generation for interior openings and double-door generation for exterior openings.
- Added `Area3D` trigger volumes per doorway; doors open while the player overlaps the trigger and close after the player exits.
- Kept hinge rotation fixed per doorway so doors do not flip direction based on approach side.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest the 3D scene in Godot to confirm each door swings to the expected side and does not snag the player capsule.
2. If any door feels too tight, widen that doorway trigger or disable temporary panel collision for greybox traversal.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add automatic 3D greybox wall corner caps across the full layout and document the pattern.

**Status**: Completed

### Work Done
- Started a focused pass to make corner caps a generated wall-layout rule instead of a manually patched exception.
- Replaced the partial hardcoded cap list in `StationGreybox3D` with endpoint-driven cap generation for all horizontal/vertical generated wall segments.
- Added a shared wall-corner-post position set so each wall endpoint receives one deduplicated wall-height cap/post.
- Documented the 3D greybox wall construction pattern in `docs/technical/THREED_MIGRATION_PLAN.md`: trim wall segments, fill L/T/cross seams with explicit posts, and avoid arbitrary wall extension.
- Added the 3D migration plan to `AGENTS.md` referenced docs so future agents check the greybox wall rules.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `AGENTS.md`
- `docs/technical/THREED_MIGRATION_PLAN.md`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest the full station layout to confirm the generated endpoint caps fill every visible wall seam without over-framing door openings.
2. If any doorway cap reads too chunky, add a small rule to skip caps for selected door threshold endpoints.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Fix the 3D greybox control-room audio cabinet clipping through the east wall.

**Status**: Completed

### Work Done
- Started a focused audio cabinet fit pass after playtest showed it clipping through the right wall.
- Moved `AudioCabinet` inward from the east wall and reduced its width/depth scale so it fits within the control room.
- Updated `AudioCabinetCollider` to match the smaller cabinet.
- Verified with `dotnet build`; build passes with existing warnings.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`

### Next Steps
1. Playtest the cabinet against the east wall and tune another small nudge if it still visually touches the wall from the camera angle.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Polish the 3D greybox control-room artifact, cabinet placement, wall corners, and wall fading.

**Status**: Completed

### Work Done
- Started greybox polish pass from latest playtest feedback.
- Removed the green board behind/next to the control/studio window; it was the `BoardWall` mesh in `World3D.tscn`.
- Moved the audio cabinet farther right against the control room east wall and updated its collider.
- Added explicit wall corner posts to fill the square gaps created by trimmed wall segments at L/T junctions.
- Added per-wall material instances and player-proximity wall fading for generated greybox walls.
- Wired `World3D` to pass the player to `StationGreybox3D` for wall fading, with a fallback lookup in the greybox.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/World3D.cs`

### Next Steps
1. Playtest wall fading in-editor and tune `WallFadeAlpha`, fade distance, or whether vertical side walls should fade more/less aggressively.
2. Add more corner posts if playtest reveals gaps in support-room wall junctions outside the current visible route.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Correct the 3D greybox control room desk, chair, wall, doorway, window, and camera framing.

**Status**: Completed

### Work Done
- Started control/studio correction pass from latest playtest feedback.
- Increased generated greybox wall height and changed wall helper placement so corners meet flush instead of visually doubling up.
- Closed the control/studio west wall connection and moved the control-to-studio doorway to the upper-left side of the shared wall.
- Added a larger control/studio half-wall window frame aligned with the control desk.
- Pushed the control desk against the north wall and moved the phoneboard, soundboard, computer, wall board, and speakers onto/against the desk area.
- Removed the control chair collider and added gentle proximity-based chair movement so it moves out of the player's way without blocking navigation.
- Zoomed the 3D camera in by reducing orthographic size from 12.5 to 10.5.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest the control room to confirm the upper-left studio doorway and window frame read correctly from the new camera framing.
2. Tune prop spacing if the desk-top items overlap visually in the editor.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Rework the 3D greybox control room and studio layout from the supplied sketch.

**Status**: Completed

### Work Done
- Started a 3D-only control room / studio greybox layout pass.
- Confirmed active work is in `scenes/world3d/World3D.tscn` plus `scripts/world3d/ControlRoom3D.cs`, `scripts/world3d/StudioRoom3D.cs`, and `scripts/world3d/StationGreybox3D.cs`.
- Removed duplicate room-local visible wall meshes from `ControlRoom3D` and `StudioRoom3D` in the 3D scene so the generated station greybox owns the visible wall grid.
- Removed old room-local wall colliders from `ControlRoom3D.cs` and `StudioRoom3D.cs`; kept floor and prop colliders aligned to the new layout.
- Repositioned the control room greybox: desk on the north side, boards/computer on the desk, chair below it, speakers flanking it, audio cabinet at the upper right, shelves along the lower wall.
- Repositioned the studio greybox: bookcases on the north wall, table and Vern group in the lower-middle, mic stand near the table, and a control-window marker on the studio/control boundary.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`

### Next Steps
1. Playtest the 3D greybox to confirm the room-local wall duplicates are gone and movement has no invisible blockers.
2. Tune individual prop positions after reviewing the camera framing in-editor.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Clean up the 3D station greybox wall grid, corners, and door openings.

**Status**: Completed

### Work Done
- Started first expanded greybox modeling pass based on `docs/design/station-layout-notes.md`.
- Added generated support-room greybox geometry for hallway, equipment room, kitchen / break room, and bathroom.
- Opened the control-room east wall so the player can leave the broadcast core into the new hallway.
- Added support-room labels, simple placeholder props, colliders, and floor threshold markers.
- Expanded camera X/Z bounds and status-label room detection to cover the new spaces.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.
- Playtest feedback: collisions work, camera is too high, control room and studio are too large, support rooms are too small, and several rough-draft rooms are missing.
- Lowered the camera pitch from steep top-down toward a more perspective-heavy angle and added subtle yaw for an isometric-like view.
- Shrank the control room and studio footprint from the initial oversized blockout.
- Enlarged support rooms and rebuilt the generated station greybox around the rough Excalidraw room list.
- Added greybox spaces for document/archive, office, lobby/front desk, parking lot, backyard, toolshed, walkway, and ladder to roof.
- Re-ran `dotnet build`; build passes with existing warnings.
- New playtest feedback: camera yaw is too strong, room sizes are improved, but the station layout must be rearranged to match the supplied rough plan.
- Reduced camera yaw from 12 degrees to 6 degrees while keeping the lower perspective angle.
- Rebuilt the greybox layout to follow the supplied sketch: backyard/toolshed west, equipment/studio/control stack left, hallway spine center, document/archive, office, kitchen, bathroom and lobby east, parking lot farther east, walkway and roof ladder south.
- Split right-side hallway walls and studio/control divider walls around intended door openings.
- Opened the studio east wall toward the hallway.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: corrected topology is closer, but some walls/doors are missing, lobby/front desk placement needs adjustment, and the camera should move lower.
- Lowered camera height and reduced pitch again for a more grounded view while preserving the subtle 6-degree yaw.
- Split front desk and lobby into separate top-right zones matching the sketch: front desk above, lobby below.
- Added missing wall segments around archive, office, kitchen, bathroom, front desk, lobby, and parking-lot boundary.
- Added door markers for archive/front-desk and kitchen/bathroom connections.
- Added a hallway-to-lobby connector floor so the central open passage matches the sketch better.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: remove yaw, reduce camera pitch, add missing office wall, keep lobby empty, and replace the lobby/front-desk divider with one long counter.
- Set camera yaw to 0 degrees and centered the camera X offset.
- Reduced camera pitch from -48 degrees to -42 degrees for a lower, more straight-on view.
- Added the missing office north wall.
- Removed the wall-like lobby/front-desk divider and replaced it with one long counter.
- Removed the extra front-desk block so the lobby reads as empty except for the counter relationship.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: reduce pitch to -35, fix the bathroom south wall protrusion, and add north/south outside doors from the middle hallway.
- Set camera pitch to -35 degrees in both `World3D.cs` and `World3D.tscn`.
- Split the main building north and south walls around the central hallway to create exterior door gaps.
- Added north and south exterior door floor markers.
- Shortened/nudged the bathroom south wall so it no longer reads as protruding beyond the building.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: the bathroom south wall still extends too far to the right in runtime.
- Trimmed `MainBuildingSouthEast` from an 18m-wide wall segment down to the 8m bathroom/right-support-room edge.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: general layout is roughly correct; clean walls so they are straight, connected at 90-degree corners, and use consistent door openings.
- Replaced the hand-tuned wall list with helper-driven horizontal and vertical wall segments so corners and openings snap to straight 90-degree geometry.
- Added consistent `DoorGap` spacing and wall helper methods for rectilinear segments.
- Re-ran `dotnet build`; build passes with existing warnings.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/World3D.cs`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/StationGreybox3D.cs.uid`
- `scripts/world3d/StudioRoom3D.cs`

### Next Steps
1. Playtest the rectilinear wall cleanup for any remaining misplaced openings.
2. Tune specific room proportions only after the wall grid reads cleanly.

---

## Previous Session

**Branch**: feature working tree (3D migration planning)

**Task**: Fix the first 3D migration slice by making the control room and studio one connected floorplan with camera follow and working collisions.

**Status**: Completed - corrected the 3D connected floorplan orientation to match the original 2D layout: control room south, studio north, camera following player movement through room depth. `dotnet build` passes.

### Work Done
- Added `docs/technical/THREED_MIGRATION_PLAN.md` covering scope, what stays, phased rollout, suggested scene structure, room requirements, UI strategy, asset strategy, risks, and first-definition-of-done.
- Updated `docs/technical/TECHNICAL_SPEC.md` to point at the 3D migration plan and note the planned `Game3D.tscn` path.
- Updated `docs/design/ROADMAP.md` with a new `World Presentation Migration` section.
- Updated `docs/design/GAME_DESIGN.md` to reflect the planned 3D world presentation layer.
- Updated `docs/technical/TOPDOWN_BUILDING_PATTERN.md` to direct readers to the 3D migration plan.
- Added `scenes/Game3D.tscn`, `scenes/world3d/World3D.tscn`, and `scripts/world3d/*` for the first 3D scaffold.
- Switched `project.godot` to launch `res://scenes/Game3D.tscn` by default.
- Added a basic 3D room switch loop with `interact` doorway checks and a visible player blockout.
- Tightened the 3D camera toward a more angled top-down framing and nudged room spacing/proportions closer to a flatter layout.
- Started connected-floorplan correction pass after playtest feedback: camera should follow the player, rooms should read clearly, and collisions should work.
- Refactored `World3D` so the control room and studio stay visible together instead of switching/hiding and teleporting the player.
- Added a smoothed orthographic follow camera with exported offset/speed/bounds.
- Split the shared room wall into doorway segments and added floor threshold markers.
- Removed incorrect 90-degree rotations from side-wall meshes so they align with their intended dimensions.
- Added generated `StaticBody3D` wall and major-prop colliders in `ControlRoom3D` and `StudioRoom3D`.
- Verified C# compilation with `dotnet build`.
- Started movement/collision correction after playtest showed the player spawning near wall geometry and unable to move freely around the floor.
- Moved the control room player start to the center of the open floor instead of the lower half near wall/prop paths.
- Added generated floor `StaticBody3D` colliders for both rooms.
- Widened the control-room/studio doorway gap in both visual meshes and collision segments.
- Relaxed camera Z follow bounds so the camera can track room-depth movement.
- Re-ran `dotnet build`; build passes with existing warnings.
- Started north/south orientation correction after playtest feedback: the 3D layout was incorrectly east/west and did not match the original control-room-south/studio-north arrangement.
- Repositioned `ControlRoom3D` south on positive Z and `StudioRoom3D` north on negative Z.
- Moved the shared doorway from east/west walls to the control north / studio south boundary.
- Split the control room north wall around the doorway and removed duplicate studio south wall geometry/collision to avoid overlap.
- Updated doorway trigger positions, doorway detection, and collision segments to use Z-axis movement.
- Adjusted camera bounds so the camera position can actually follow player Z movement after applying its offset.
- Moved the player start farther south of the control room desk to avoid spawning next to the desk collider.
- Re-ran `dotnet build`; build passes with existing warnings.

### Files Modified
- `docs/technical/THREED_MIGRATION_PLAN.md`
- `docs/technical/TECHNICAL_SPEC.md`
- `docs/design/ROADMAP.md`
- `docs/design/GAME_DESIGN.md`
- `docs/technical/TOPDOWN_BUILDING_PATTERN.md`
- `project.godot`
- `scenes/Game3D.tscn`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/World3D.cs`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`
- `scripts/world3d/Player3D.cs`
- `SESSION_LOG.md`

### Next Steps
1. Re-test movement in Godot editor/runtime and confirm up/north reaches the studio.
2. Tune visual prop placement against the old 2D room composition.
3. Run `godot --check-only project.godot` from an environment where the Godot executable is on PATH.
