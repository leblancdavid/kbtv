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

Before MPFB-informed proportion edits, capture a reproducible baseline:

```powershell
blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern_proportion_audit.py
blender --background --python-exit-code 1 --python Tools/modelgen/mpfb_vern_proportion_compare.py
```

This writes `docs/art/model_previews/vern_proportion_audit.json` with current
mesh bounds, visual mesh slice widths, skeleton landmarks, limb lengths and
ratios. The comparison command uses normal Blender startup (not
`--factory-startup`) because MPFB needs its registered preferences; it writes
`docs/art/model_previews/mpfb_vern_proportion_comparison.json` using a default
MPFB standard-rig human. Use both reports as before/after guardrails for
body-proportion changes; do not alter Vern's generator by eye alone.

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

### MPFB Vern prototype

Before fitting clothes or accessories, confirm MPFB body orientation and male
geometry with the body diagnostic:

```powershell
blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_body_diagnostic.py
```

Outputs:

- Source: `Tools/modelgen/source/vern_mpfb_body_diagnostic.blend`
- Report: `docs/art/model_previews/vern_mpfb_body_diagnostic.json`
- Axis previews: `vern_mpfb_body_axis_plus_y.png`, `_minus_y.png`, `_plus_x.png`,
  `_minus_x.png`

The diagnostic applies MPFB's built-in `caucasian-male-old.target.gz` and
`universal-male-old-averagemuscle-averageweight.target.gz` shape keys and records
`male_assertion.verified=true` when those male targets are active. The report also
records face direction using the eye/head rig landmarks: **the MPFB face points
toward Blender -Y (Z-up)**, which exports to glTF/Godot +Z. The older head-centroid
extrema heuristic was wrong. Front is the `minus_y` view, back is `plus_y`.
This differs from the procedural production Vern's facing convention.

The corrected static clothing pass is generated with
`blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_fitted.py`.
`vern_mpfb_wardrobe.py` extracts continuous garments from the body topology,
retains skin weights, removes covered skin, and fits rigid head accessories.
Both generators explicitly disable MPFB's default mixed macro targets before
applying the selected male targets (loading targets alone is additive).
Review `model_previews/vern_mpfb_fitted_{front,side,back,portrait}.png`.
The report counts actual triangles and records the corrected facing. This is
still a separate static prototype, pending user approval and seated/animation work.
Run the round-trip checks with
`blender --background --python-exit-code 1 --python Tools/modelgen/validate_mpfb_fitted.py`.
These check normalized skin weights, facing, covered-skin removal, bounds, and
rigid head-accessory motion. They do not validate Godot or seated deformation.

The neck/material refinement uses `vern_mpfb_collar.py` to sample a rounded,
dipped-front collar from the neck surface, with inherited body weights.
`vern_mpfb_surfaces.py` generates 512px color, tangent-normal and packed
roughness/metallic maps for sweater knit, collar ribbing, trouser twill and hair.
Source PNGs live in `assets/models3d/characters/vern_mpfb/textures/`; the blend
packs them and the GLB embeds them. Clothing UV area is normalized to metres so
yarn scale remains consistent. Hair uses a smooth temple-color gradient instead
of hard per-face gray patches. Headphone shells retain an untextured material.

Review `_fabric.png` for textile detail and `_export_portrait.png` for the
actual re-imported GLB. The validator checks all four textured materials have
embedded base-color, normal and metallic/roughness images, then renders the
round-trip portrait using the same lighting as the source previews.

The subsequent silhouette pass uses `vern_mpfb_proportions.py` to apply the same
piecewise height mapping to every mesh vertex and edit-bone endpoint. The leg
region above the ankles is shortened by 10%, with modest trunk extension;
head/feet retain their size. This is a rest-space change, not pose scaling or an
unverified MPFB target weight. Before/after joint measurements are recorded in
the report; the validator checks the imported measurements and neutral evaluated
mesh against its bind geometry. Current overall height is approximately 1.707m.

`vern_mpfb_shoes.py` builds 26cm shaped loafers with narrower heels and thin
matching soles. `vern_mpfb_hair.py` adds flattened surface-following swept locks
using the same texture projection as the underlayer. Fabric repeat is 16/m,
with stronger color variation and restrained normal strength. Review `_shoes.png`
and `_swatches.png` (left to right: sweater, rib, trousers), plus the validator's
`_export_front.png` and `_export_portrait.png`. These are local generated outputs
under the gitignored preview directory. Appearance still needs human review;
structural validation is not an art-quality verdict.

The current procedural Vern body can also be compared against a rough MPFB-derived
clothed prototype without replacing the runtime asset:

```powershell
blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_prototype.py
```

Outputs:

- Source: `Tools/modelgen/source/vern_mpfb_prototype.blend`
- Prototype GLB: `assets/models3d/characters/vern_mpfb/vern_mpfb_prototype.glb`
- Report: `docs/art/model_previews/vern_mpfb_prototype.json`
- Previews: `vern_mpfb_prototype_front.png`, `vern_mpfb_prototype_side.png`,
  `vern_mpfb_prototype_portrait.png`

This prototype uses MPFB's body and standard rig as the anatomical base, with
rough Vern clothing/accessory overlays for visual review. It is not runtime-wired.
The first overlay pass was visually rejected because the orientation and primitive
clothing fit were wrong; keep it as a failed reference only. Do not replace
`assets/models3d/characters/vern/vern.glb` until seated pose, skinned/fitted
clothing, contact anchors, and `talk_calm` retargeting are migrated.

## Troubleshooting

- `blender` not found: restart the shell after updating PATH, or call Blender by full path.
- Godot shows the prop but lighting is wrong: check `StationLighting3D.ApplyLayerToTree` and visual layers.
- Prop floats or sinks: use bottom-origin positioning, not placeholder center Y.
- Prop faces away: rotate the scene instance around Y in Godot; do not regenerate solely for yaw.
- Thin pieces flicker or disappear: thicken them in the generator and reduce dense detail.
- File is too large: reduce cylinders' vertex counts, join repeated tiny details, and prefer material surfaces over textures.

See also [3D Migration Plan](../technical/THREED_MIGRATION_PLAN.md) and
[Art Style Guide](ART_STYLE.md).
