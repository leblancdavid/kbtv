# Vern Tell - Seated 3D Character Production Brief

## Goal and approved direction

Prepare Vern for the studio and its live broadcast camera. The user supplied an
Art Bell photograph as the appearance reference and approved a slightly cartoony
treatment consistent with KBTV's existing 3D props. First delivery is Vern sitting
still in a separate chair; later work can add breathing, talking and gestures.
This document is the production handoff for GPT-6 Astra; model creation is next.

## Visual reference hierarchy

1. **Style:** existing `assets/models3d/props/` models and their previews:
   [office chair](model_previews/office_chair.png),
   [audio cabinet](model_previews/audio_cabinet.png), and
   [microphone](model_previews/microphone_stand.png).
2. **Appearance:** the user-supplied Art Bell photo in this conversation. Attach
   it again when starting a fresh Astra session; it has not been saved to the repo.
3. **Atmosphere:** the muted late-night noir direction in [ART_STYLE.md](ART_STYLE.md).
   Its legacy pixel-grid, oblique sprite and baked directional shading rules do
   not define this true-3D character's geometry or lighting.

Use broad simple forms, softly beveled/faceted edges, restrained surface detail,
and rough PBR materials. Slightly enlarge the head, hands, glasses and headphones
for readability, while retaining believable adult proportions. Aim for a cohesive
stylized person beside the existing furniture. Avoid a sphere-and-cylinder dummy,
chibi proportions, dense sculpted wrinkles, individual hair strands or glossy skin.

### Appearance and pose

- Middle-aged man; short dark swept/side-parted hair, subtle gray at temples.
- Dark mustache, broad rectangular aviator-style glasses, prominent nose and brows.
- Large vintage over-ear headphones; simple headband and padded cups.
- Charcoal/near-black high-neck sweater, dark trousers and simple dark shoes.
- Muted warm skin; focused, slightly tired expression and relaxed shoulders.
- Seated comfortably, back supported, thighs resting on cushion, knees bent,
  feet planted. Hands initially rest on armrests or lap, with natural asymmetry.
- Face generally toward the broadcast area, visible in a front three-quarter shot.
  The reference photo guides character identity more strongly than its exact pose.

Use `Tools/modelgen/common.py` for material creation and palette conventions:
charcoal `#3a3d48`, near-black `#11131a`, metal `#697078`, wood `#5a5340`.
Reuse colors but give cloth/hair/skin nonmetallic materials (the shared shell is
metallic powdercoat). Provisional skin base `#b58d78`, roughness around 0.7.
Make lenses absent or very subtle initially so reflections do not obscure eyes.

## Character/chair and animation contract

**Two independent assets, assembled under one Godot placement root.** Reuse
`office_chair.glb` for the first fit pass. If proportions require a dedicated host
chair, create `studio_host_chair.glb` using the same style, leaving the shared chair
intact. Chair geometry is never joined to Vern or skinned to his skeleton.

- Model Vern in a neutral A-pose, with clean joint topology and a basic armature.
- Minimum bones: root, pelvis, spine/chest, neck/head, paired upper/lower arms,
  hands, thighs, shins and feet. Keep names/hierarchy stable for later animation.
- Skin clothing/body with sensible weights; glasses, hair and headphones follow
  the head rigidly. Simple hands are sufficient; finger and facial rigs can follow.
- Export a `seated_rest` animation with identical keys over a short duration.
  Godot must explicitly apply that pose on initialization, including before a
  show begins. Keeping a pose in Blender alone does not ensure a seated GLB import.
- Preserve neutral bind pose and editable weights. Do not apply the armature
  modifier or permanently bake bent limbs into the only source mesh.
- This minimal rig is preparation, not a guarantee of future deformation quality:
  inspect shoulder, elbow, hip and knee bends before approving the source.

Suggested scene assembly (new wrapper, names proposed):

```text
VernStation (Node3D; floor-level placement and shared yaw)
├── Chair (office_chair.glb)
├── SeatAnchor (Marker3D; cushion contact reference)
└── Vern (character scene; rig, meshes, held seated_rest pose)
    └── LookTarget (Marker3D; face/chest framing reference)
```

SeatAnchor describes cushion contact, not pelvis bone center. Fit pelvis and
thigh volume above it. Vern is a sibling of the chair, so later stand-up or chair
replacement does not require changing the character's skeleton.

## Scale, axes and current integration facts

Use 1 unit = 1 meter, Blender Z-up/front +Y, exported Godot Y-up/front -Z.
Character root and chair root use a common floor-level origin under seat center,
with unit instance scale. Mesh bounds need not be centered around the root.
Provisional adult standing height: 1.7-1.8m; seated floor-to-headphones roughly
1.3-1.45m. Fit to the actual chair rather than forcing those estimates.

Existing `office_chair.py` dimensions (relative to its floor origin):

| Measurement | Value |
|---|---|
| Cushion top / seat contact | 0.53m |
| Seat width x depth | 0.50 x 0.48m |
| Armrest top | 0.6975m |
| Armrest center X | +/-0.28m |
| Seat center in Godot X/Z | (0, -0.005) |

Current scene facts, all positions **studio-local** unless stated otherwise:

- `scenes/world3d/World3D.tscn`: studio root is world `(0, 0, -4)`.
- `ChairLeft`: center-origin placeholder at `(-0.85, 0.2, 0.55)`.
  Floor mesh top is Y=0.1; initially place a bottom-origin chair at
  `(-0.85, 0.1, 0.55)`, not at the placeholder's center Y=0.2.
- `VernStandIn`: small sphere at `(-0.85, 0.75, 0.45)`, not an anatomical target.
- Shared yaw of about 180 degrees is a starting point to face +Z toward the
  table/camera; verify actual hand, knee and table clearance in the scene.
- `RoundTablePlaceholder` is a solid scaled box with top Y=0.6. It may obstruct
  a real seated body's knees. Inspect fit before changing placement/geometry;
  do not distort Vern's anatomy to hide a placeholder-table intersection.
- `StudioRoom3D.cs`: Vern chair collider center `(-0.85, 0.35, 0.55)`,
  size `(0.9, 0.7, 0.7)`; smoke origin `(-0.85, 1.25, 0.35)`.
- `World3D.cs`: feed is 320x180, FOV 38, camera `(-2.15, 1.55, 2.3)`,
  looking at `(-0.85, 0.85, 0.45)`. Retarget to the actual face/chest after fitting;
  current target is based on the sphere and will likely be low for the new model.
- Apply `StationLighting3D.StudioLayer` to all character/chair visual descendants,
  including any instances created after the initial lighting traversal.

## Production workflow and deliverables

Extend [3D_ASSET_WORKFLOW.md](3D_ASSET_WORKFLOW.md) with a **character-specific**
generator/export path. The current `generate.py` joins selected objects as static
geometry and validates only MESH/EMPTY objects; it cannot be used unchanged for
Vern's armature and animation. Reuse material helpers and static chair tooling.

Proposed output paths:
- `Tools/modelgen/vern.py`: reproducible character generator/export entry point.
- `Tools/modelgen/source/vern.blend`: editable neutral rig and seated action.
- `assets/models3d/characters/vern/vern.glb`: character, skin and seated action.
- `scenes/world3d/Vern.tscn`: wrapper that initializes the held seated pose.
- `docs/art/model_previews/vern_*.png` and `vern.json`: review views and measurements.

Build and review in stages: silhouette/head and chair fit; likeness/materials;
rig/held pose export; Godot placement and camera review. Spend detail on the face,
glasses, mustache and headphones before clothing folds. Provisional budget:
roughly 8k-20k character triangles and 6-10 materials; prioritize the actual look.

Acceptance checks:
- Re-import GLB in clean Blender and Godot; verify armature, skin and seated clip.
- No exported preview cameras, lights, floors or chair in the character asset.
- Report rest/posed bounds separately, triangle/material/bone counts and clip names.
- Head accessories follow a test head rotation; limbs bend without obvious gaps.
- No floating hips/feet, cushion penetration, disconnected hands or table clipping.
- Preview front, side, three-quarter and seated-in-chair views alongside existing props.
- Review both world camera and actual 320x180 feed: face reads, dark clothes retain
  volume, glasses remain visible, and smoke/microphone do not obscure the head.
- Follow repository build/test instructions for implementation changes; compare
  failures against a fresh baseline rather than relying on older session counts.

## GPT-6 Astra handoff prompt

> Implement `docs/art/VERN_3D_MODEL_BRIEF.md` in this repository. Use the attached
> Art Bell photo for Vern's likeness and the existing office chair, audio cabinet
> and microphone GLBs/previews for style. Slightly cartoony is welcome: simple
> softened forms, muted PBR materials and readable glasses/headphones/mustache.
> Build a separate rigged Vern with a held `seated_rest` pose, initially fitting
> the existing office chair. Preserve a neutral bind pose for future animation.
> Use a character-specific Blender export path and keep source reproducible.
> Integrate him in the studio, replacing the Vern/chair placeholders, correcting
> floor-origin placement and reviewing body/table fit. Frame his actual face in
> the live camera. Deliver editable source, GLB, preview views and validation
> report. Review silhouette and chair fit before investing in facial detail.
