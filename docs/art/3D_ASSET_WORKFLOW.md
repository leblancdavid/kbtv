# 3D Asset Workflow

This workflow covers GPT-assisted Blender generation for simple KBTV 3D props.
Use it for low-risk props during the 3D migration visual pass, not for room
layout, collision design, character animation, or final hero assets.

For Vern's character-specific workflow and GPT-6 Astra handoff, see
[Vern 3D Model Brief](VERN_3D_MODEL_BRIEF.md). It defines matching prop style,
separate character/chair assets, and a rig-aware export path. Character
model/animation work must also apply
[CHARACTER_GUIDELINES.md](CHARACTER_GUIDELINES.md) (generic principles) and
[VERN_CHARACTER_GUIDELINES.md](VERN_CHARACTER_GUIDELINES.md) (Vern-specific
anchors, vocabulary, and validation).

## Current toolchain

- Blender 5.2.1 LTS, available as `blender` on PATH.
- Blender Python scripts generate source geometry and export `.glb` files.
- Godot imports `.glb` files from `assets/models3d/props/`.
- Existing Godot collision boxes stay authoritative until a prop is accepted.

No Blender add-on is required. Generation uses Blender's built-in Python API and
glTF exporter.

## Folder contract

| Path | Purpose |
|---|---|
| `Tools/modelgen/` | Repeatable Blender Python generators. |
| `Tools/modelgen/source/` | Editable `.blend` source outputs. Do not import these in Godot. |
| `assets/models3d/props/` | Game-facing `.glb` exports. |
| `docs/art/model_previews/` | PNG previews and JSON validation reports. |

Keep generation scripts as the source of truth. If a `.blend` file is manually
edited, either port the change back into Python or document that it is a one-off.

## GPT prompt pattern

Ask GPT-6 or another model for a Blender Python generator, not a binary model.
Require a script that creates primitives, assigns simple materials, sets a sane
origin, and exports GLB through Blender.

Prompt template:

```text
Create a Blender Python script that generates a stylized low-poly 3D prop for
Godot 4.

Prop: [specific prop].

Style: late-night noir radio station, desaturated charcoal and warm worn metal,
muted green/red indicators only where appropriate. No floor, no baked shadow,
no environment, no camera, no lights in the exported asset.

Technical requirements:
- 1 Blender/Godot unit = 1 meter.
- Approx dimensions: [width]m wide, [height]m tall, [depth]m deep.
- Origin at bottom center.
- Export GLB.
- Godot/front direction should be negative Z after export.
- Low-poly, clean mesh, simple PBR materials, no texture files.
- Use simple primitives and bevels; avoid fragile thin geometry.
```

Preferred prop candidates for this workflow are boxy or silhouette-readable:
equipment racks, filing cabinets, tables, counters, shelves, signs, speakers,
lamps, microphones, radios, coffee machines, and small station clutter.

Avoid starting with organic props, characters, cloth, complex cables, transparent
glass, dense greebles, or anything that needs animation.

## Generator pattern

The current implementation uses:

- `Tools/modelgen/common.py`: shared primitives, material helpers, reset/export helpers.
- `Tools/modelgen/audio_cabinet.py`: audio cabinet construction.
- `Tools/modelgen/microphone_stand.py`: microphone construction.
- `Tools/modelgen/generate.py`: builds assets, saves `.blend`, exports `.glb`, re-imports, validates, renders previews.

Run from the repository root:

```powershell
blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/generate.py
```

This overwrites generated GLBs, `.blend` sources, previews, and validation
reports. Commit the Python generator changes with the generated `.glb` outputs
so future developers can reproduce the model.

## Validation checklist

Before placing a generated prop in a scene, verify:

- The GLB re-imports into a clean Blender scene.
- Bounds match expected meter dimensions.
- The origin is at the bottom center; minimum height is near zero.
- It exports no cameras, lights, floors, or background objects.
- Material count is small and materials are embedded.
- Triangle count is reasonable for the prop's gameplay importance.
- Preview image reads clearly at approximate gameplay camera distance.

The generator already performs basic geometry validation: removes degenerate
bevel edges, recalculates normals, checks polygon area, compares source/imported
triangle counts, and writes measured bounds under `docs/art/model_previews/`.

## Godot placement checklist

1. Let Godot import `assets/models3d/props/*.glb`.
2. Add the `.glb` as a scene external `PackedScene` resource or drag it into a review scene.
3. Keep instance scale at `(1, 1, 1)` unless the asset is intentionally resized.
4. Use bottom-origin placement: floor props usually have `position.y = 0`.
5. Rotate around Y to face the room. Current generated GLBs front negative Z.
6. Keep simple Godot collision shapes separate until the visual asset is accepted.
7. Apply the correct visual layer/cull mask so existing station lights affect it.
8. Review in `Game3D.tscn`, not only in Blender preview renders.

If replacing an existing placeholder, do not copy the placeholder's Y center or
scale blindly. Box placeholders are usually center-origin and scaled; generated
props are bottom-origin and meter-sized.

## First accepted trial assets

| GLB | Dimensions in meters | Triangles | Materials | Scene placement |
|---|---:|---:|---:|---|
| `audio_cabinet.glb` | 0.700 x 1.000 x 0.658 | 12,876 | 7 | `ControlRoom3D/AudioCabinet` at `(4.45, 0, -3.5)`, Y rotation 180, scale 1.5x (back-right corner). |
| `microphone_stand.glb` | 0.350 x 1.200 x 0.350 | 4,296 | 5 | `StudioRoom3D/StudioMicStand` at `(0.95, 0.2, 0.65)`, Y rotation 180. |

The first in-game review looked good enough to keep this workflow for additional
simple props. Use the same process for the next one or two assets before scaling
up production.

## Vern character pipeline

Vern uses a separate rig-aware generator; do not pass him through the static
prop generator's mesh-only export path.

```powershell
blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern.py
```

- `vern.py`: silhouette, clothing, face, hair, glasses and headphones.
- `vern_mesh.py`: lofts/tubes with stable ring frames, seam-safe UVs and weighted vertices.
- `vern_rig.py`: neutral A-pose skeleton, seated transforms and inverse skinning.
- `vern_hands.py`: articulated digits (three joints per finger/thumb) and grip poses.
- `vern_materials.py`: deterministic painted PBR images (base color/roughness/normal), packed into the GLB.
- `vern_detail.py`: broad sculptural relief (cheek planes, sweater folds, swept hair locks).
- `vern_export.py`: skin-preserving export, every-action round-trip checks and previews.
- Source: `Tools/modelgen/source/vern.blend` (53 bones; live armature modifier).
- Game asset: `assets/models3d/characters/vern/vern.glb` (23,864 triangles,
  11 materials with embedded textures; approximately 1.5 MiB). Chair remains `office_chair.glb`.
- `seated_rest` is a one-second held pose. `VernCharacter3D` applies it before
  showing the model; deferred controller initialization then starts breathing.
  Bind pose stays available. `vern_animation.py` bakes an 8-second talking loop,
  4-second idle and 5.5-second smoking/coffee actions at 24fps, plus contact JSON.
  `VernPerformanceProps` attaches a held prop to the evaluated hand pose
  (skeleton * `hand_local_grip`) between pickup and release, keeps contract rest
  anchors otherwise, and uses the AnimationPlayer clock for timing and exhale.
- Geometry is authored seated then inverse-skinned into the neutral rest pose.
  Keep the weighted elbow/knee rings and shortest-arc bone orientation logic
  when changing shapes; arbitrary bone roll can corrupt the neutral mesh.

Import the GLB in Godot 4.6 mono, build C#, then run the focused test:

```powershell
dotnet build
pwsh -NoProfile -File run-tests.ps1 -Filter VernCharacterIntegrationTests
```

With a graphical Godot 4.6 mono executable, capture actual studio lighting and
the broadcast feed (the command does not start a show):

```powershell
godot --path . --script Tools/modelgen/preview_vern.gd
```

This writes `vern_godot_feed.png` and `vern_godot_studio.png` in
`docs/art/model_previews/`. Use the correct executable path if `godot` on PATH
is not version 4.6 mono. Blender-only previews use neutral review lighting.

For the animation pass, extend validation beyond the active seated action:
enumerate/select every imported clip, verify loop seams and one-shot endpoints,
and review moving footage in Godot. Keep mug/cigarette exports separate from the
skinned character and use the same resting/grip anchors for authoring and runtime.
Record pickup/release/exhale timing outside glTF for Godot-side prop and smoke
coordination. Static screenshots alone cannot validate contact or transitions.

Current animation review commands (replace `$godot` with a Godot 4.6 mono console executable):

```powershell
blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern.py -- --skip-previews
blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern.py -- --preview-only
python Tools/modelgen/vern_godot_validate.py --godot $godot
& $godot --headless --editor --path . --import
dotnet build
& $godot --path . --script Tools/modelgen/preview_vern.gd -- --animations
python Tools/modelgen/vern_pack_review.py "$env:LOCALAPPDATA/Temp/opencode/vern_godot_frames" docs/art/model_previews vern_godot
pwsh -NoProfile -File run-tests.ps1 -Filter VernAnimationControllerTests
pwsh -NoProfile -File run-tests.ps1 -Filter VernCharacterIntegrationTests
```

Blender moving reviews use `vern_review.py` / `vern_pack_review.py`; loops are
packaged as two cycles. Godot validation checks every baked frame for fixed
pelvis/feet, loop endpoints, hand-grip alignment, moving mouth contact and wrist
travel. Runtime tests cover short caller admission, consecutive speech, stale
events, one-shot completion and single-prop restoration.

### Baked talk clip (`talk_calm`)

Vern's on-air talking animation is a production retarget of the reference
library's CC0 `Sitting_Talking` clip, ported byte-faithfully from the approved
spike (V5) and shipped as a standalone `Animation` resource (not part of
`vern.glb`):

- Script: `Tools/modelgen/bake_talk_calm.gd` (self-contained; maps the raw
  reference GLB's `DEF-*` bones to Vern's native names via the `retarget/`
  BoneMap profile join, fused with Vern-only jaw/eyelid/grip tracks sampled
  from `talking_default`).
- Output: `assets/models3d/characters/vern/animations/talk_calm.tres`
  (52 tracks, `VernRig/Skeleton3D:<bone>` paths, 2.933 s @ 24 fps,
  LOOP_LINEAR).
- Runtime: `VernCharacter3D._Ready` injects the clip into the player's root
  library; `VernAnimationController` plays `talk_calm` for Vern lines, falling
  back to `talking_default` if injection failed.
- Re-bake and validate:
  ```powershell
  & $godot --headless --path . --script res://Tools/modelgen/bake_talk_calm.gd
  & $godot --headless --path . --script res://Tools/modelgen/reimpl_validate.gd   # needs Tools/modelgen/tmp/oracle_reference.scn
  dotnet build
  pwsh -NoProfile -File run-tests.ps1 -Filter VernAnimationControllerTests
  pwsh -NoProfile -File run-tests.ps1 -Filter VernCharacterIntegrationTests
  ```
  Fidelity against the approved V5 bake is checked by
  `val_fidelity_vs_v5.gd` (one-time; it reads the temp spike path).

## Troubleshooting

- `blender` not found: restart the shell after updating PATH, or call Blender by full path.
- Godot shows the prop but lighting is wrong: check `StationLighting3D.ApplyLayerToTree` and visual layers.
- Prop floats or sinks: use bottom-origin positioning, not placeholder center Y.
- Prop faces away: rotate the scene instance around Y in Godot; do not regenerate solely for yaw.
- Thin pieces flicker or disappear: thicken them in the generator and reduce dense detail.
- File is too large: reduce cylinders' vertex counts, join repeated tiny details, and prefer material surfaces over textures.

See also [3D Migration Plan](../technical/THREED_MIGRATION_PLAN.md) and
[Art Style Guide](ART_STYLE.md).
