# Vern Tell - Seated 3D Character Production Brief

## Goal and approved direction

Prepare Vern for the studio and its live broadcast camera. The user supplied an
Art Bell photograph as the appearance reference and approved a slightly cartoony
treatment consistent with KBTV's existing 3D props. First delivery is Vern sitting
still in a separate chair; later work can add breathing, talking and gestures.
The first model is implemented. Production notes below supersede the original
provisional dimensions/placement later in this brief.

## Persistent character guidelines

Before any change to Vern's mesh, proportions, rig, or animation, read and apply:

1. [CHARACTER_GUIDELINES.md](CHARACTER_GUIDELINES.md) - character-agnostic
   principles for any 3D character work.
2. [VERN_CHARACTER_GUIDELINES.md](VERN_CHARACTER_GUIDELINES.md) - Vern-specific
   IK anchors, animation vocabulary, and validation sequence.

Where these guidelines conflict with older statements in this brief (for
example "simple hands are sufficient" or rigid-finger limitations), the
guidelines supersede them. Do not expand the animation library until Vern's
validation sequence looks convincing.

### Current production asset

- `assets/models3d/characters/vern/vern.glb`: 53-bone rig with articulated
  fingers, 23,864 triangles, 11 materials, neutral bind pose and generated clips
  `seated_rest`, `idle_breathing`, `talking_default`, `smoking`, and
  `drink_coffee`.
- `assets/models3d/characters/vern/animations/talk_calm.tres`: production
  on-air speaking clip retargeted from the CC0 `Sitting_Talking` reference and
  injected by `VernCharacter3D` at runtime.
- `Tools/modelgen/source/vern.blend`: editable source. Rebuild using
  `Tools/modelgen/vern.py`; see [3D asset workflow](3D_ASSET_WORKFLOW.md).
- Seated headband top is 1.545m above the asset floor; neutral headband top 1.88m.
  These are measured output dimensions, including the oversized readable headgear.
- `docs/art/model_previews/vern_proportion_audit.json` is the current Vern
  measurement baseline for MPFB-informed anatomy refinement;
  `mpfb_vern_proportion_comparison.json` compares it to a default MPFB
  standard-rig human.
- `VernStation` sits at studio-local `(-0.85, 0.1, -0.05)`, yaw 180 degrees,
  keeping feet clear of the solid table placeholder. Chair and Vern are siblings.
- Face/chest framing marker is Vern-local `(0, 1.26, 0)`; broadcast camera is
  offset `(-0.48, 0.12, 1.2)` from that marker in world axes, FOV 38.
- [Seated review](model_previews/vern_seated.png),
  [portrait](model_previews/vern_portrait.png),
  [neutral rig](model_previews/vern_bind_pose.png),
  [actual broadcast feed](model_previews/vern_godot_feed.png), and
  [studio placement](model_previews/vern_godot_studio.png).
- Body is prepared for further animation; hands have articulated digits and
  runtime prop-follow tests. Facial expressions/lip sync remain future work.

### MPFB replacement prototype

Use the male MPFB body diagnostic before making another clothed prototype:

- `Tools/modelgen/vern_mpfb_body_diagnostic.py`: creates an MPFB body, applies
  built-in male/old target shape keys, adds the standard rig, and renders four
  axis-labelled views.
- `docs/art/model_previews/vern_mpfb_body_diagnostic.json`: records the applied
  male targets, `male_assertion.verified=true` when active, and the confirmed
  face direction (Blender -Y / glTF +Z; corrected using eye/head landmarks).
- `docs/art/model_previews/vern_mpfb_body_axis_*.png`: front/back/side discovery
  renders. The face appears in the `minus_y` view; the back in `plus_y`.

The fitted revision uses `vern_mpfb_fitted.py` + `vern_mpfb_wardrobe.py` and
`model_previews/vern_mpfb_fitted_{front,side,back,portrait}.png`. Clothing is
surface-derived and continuous; accessories are aligned to the actual face.
The former positive-Y diagnostic conclusion and primitive-clothing fit claims
were rejected by direct render inspection. Runtime migration remains pending.

A separate MPFB-derived prototype also exists for visual comparison before
replacing the production animated Vern:

- `Tools/modelgen/vern_mpfb_prototype.py`: creates an MPFB body with standard rig,
  adds rough Vern clothing/accessory overlays, exports the prototype and renders
  review images.
- `assets/models3d/characters/vern_mpfb/vern_mpfb_prototype.glb`: prototype only,
  not referenced by runtime scenes.
- `docs/art/model_previews/vern_mpfb_prototype.json`: measurements and status.
- `docs/art/model_previews/vern_mpfb_prototype_front.png`, `_side.png`, and
  `_portrait.png`: review images.

The first rough clothed prototype was rejected because the body appeared
backward/sideways and the primitive clothing/accessories did not fit. Do not swap
it into `Vern.tscn`. First use the body diagnostic to confirm orientation, then
finish fitted/skinned clothing, seated pose, hand/mouth contact anchors, and
retarget/rebake `talk_calm` against the MPFB skeleton or a Vern-compatible
exported skeleton. The step-by-step, grounded migration (rig facts, VernRig→MPFB
bone-name table, profile-join rebake path, contact anchors, swap, validation
gates) lives in [VERN_MPFB_MIGRATION.md](VERN_MPFB_MIGRATION.md).

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

## Animation pass 1 — approved plan

### Implemented animation-quality baseline (September 2026)

- Five exported actions, neutral bind and original bone names retained; the current
  rig has 21 bones including existing grip/jaw controls. Chair remains separate.
- `talking_default` is now **8 seconds**: asymmetric left/right explanations,
  two-hand emphasis, wrist turns and torso/head accents. Both wrists travel over
  0.26m from rest; continuous positional tangents carry gestures through transit beats.
- Drink/smoke remain 5.5-second one-shots. Spine reclines about 7.5 degrees;
  shoulders follow the chest, mouth contact follows the head, pelvis/feet stay fixed.
  Contact and table holds retain zero velocity; contact samples regenerate with the rig.
- `VernPerformanceProps` instantiates one mug, cigarette, ashtray and tray table,
  samples the shared animation clock, and emits a head-following exhale at 3.5s.
  Automatic unrelated mouth puffs were replaced; room haze/door leaks remain.
- `VernAnimationController` admits a caller action only when its imported duration
  fits before the two-second pre-speak buffer. Completion drives return to idle;
  unexpected speech changes intent but allows the safe return to finish. Consecutive
  speech preserves phase; stale item completion/interruption events are ignored.
- Verified generated full clips in Blender and Godot; actual studio/feed GIFs and
  sampled review sheets live under `model_previews/vern_godot_*`.
- This baseline uses caller-time gestures. The item-use notification/queue described
  in the original production sequence below remains a separate future integration.

The original production plan below specifies four seated animations for the character.
Use one generic talking performance initially; mood variants come later. The
coffee mug is permanent studio dressing. Include an ashtray and cigarette prop
to support the smoking action. The implementation summary above supersedes
the original starting targets; remaining item-use work is called out explicitly.

### Clip contract

| Action | Playback | Starting duration | Performance |
|---|---|---|---|
| `idle_breathing` | Seamless loop | 3–5 seconds | Subtle chest/shoulder breathing and slight head drift; relaxed late-night host. |
| `talking_default` | Seamless loop | 8 seconds (implemented polish) | Alternating whole-arm gestures, wrist turns, two-hand emphasis and torso/head accents. |
| `smoking` | One-shot | 4–6 seconds | Reach for cigarette, lift to lips, inhale, lower, exhale, return cigarette to ashtray. |
| `drink_coffee` | One-shot | 4–6 seconds | Reach, grasp mug, lift, sip with a small tilt, return upright to its resting position, release. |

Durations are starting targets, adjustable for convincing motion. Preserve
`seated_rest` as a fallback/reference pose and the neutral A-pose bind. Keep root,
pelvis contact and feet stable; chair stays separate and stationary. One-shots
begin/end in the same seated rest configuration. Loop endpoints must match in
pose and motion so breathing/talking do not visibly hitch.

Talking should read in the 320x180 feed. Body/head motion is the baseline;
small mouth/jaw motion is welcome if needed for readability, but phoneme lip
sync and mood-specific performances are later work. Current face geometry and
hands follow head/hand bones rigidly. Inspect these limitations before animating;
add minimal grip controls or shape keys if required rather than letting props
float beside an open palm. Keep existing bone names/hierarchy stable.

### Permanent props and contact

- Create separate `coffee_mug.glb`, `ashtray.glb`, and `cigarette.glb` under
  `assets/models3d/props/`, with reproducible generators and editable source.
- Use existing muted, softly beveled PBR prop style. Suggested mug: worn cream
  or muted gray ceramic, clear handle, dark recessed coffee surface. Ashtray:
  simple dark ceramic or worn metal with a cigarette rest. Avoid tiny decoration.
- Mug and ashtray remain visible in the studio. Cigarette rests at the ashtray
  between smoking actions. Place all within a believable seated reach; measure
  the actual table and arm reach before choosing anchors.
- Use a single visible instance of each prop. At grasp, transfer the mug or
  cigarette from its resting anchor to a hand attachment without a position or
  rotation jump; at release, restore the exact resting transform. Ashtray stays
  stationary. Author/bake hand motion against these same anchors.
- Keep prop meshes separate from Vern's skinned body. Define hand grip offsets,
  resting anchors and pickup/release/exhale timing in the wrapper/controller or
  companion metadata; glTF does not carry Godot method-call tracks.
- Verify wrist/handle alignment, mouth contact and clearance throughout the
  motion. Do not fake a sip by leaving the mug on the table or showing a duplicate.
- Keep smoke in Godot. Coordinate a puff at the exhale beat with a head-following
  mouth marker; inspect `StudioSmoke3D`'s existing timed puffs to avoid duplicate
  emissions. Preserve room haze and door-leak behavior.

### Production and runtime sequence

1. Block out clips and prop contacts in Blender; review silhouette, chair fit,
   grasp/sip/inhale poses before polishing. Keep generator changes reproducible.
2. Extend `vern_export.py` to export and enumerate every action, explicitly
   select each imported action for validation, and report names/durations/bounds
   in `vern.json`. Its current checks assume a single active `seated_rest` action.
3. Extend `VernCharacter3D` with named playback and one-shot completion handling.
   Apply a seated pose before first visibility, then loop idle; handle qualified
   imported animation names and use short blends (initial target 0.15–0.25s).
4. Provide a review harness to play all four clips repeatedly with props in the
   studio and capture both world view and actual broadcast feed.
5. Wire generic talking to Vern dialogue start/end/interruption using existing
   broadcast events and matching line IDs. Caller speech/music must not trigger
   it; stale completion events must not stop a newer Vern line.
6. Add a lightweight successful-use notification at `ItemManager` for coffee
   and cigarettes. Existing `ItemRow` applies effects after its 30-second timer;
   request the visual action then, without changing stat/replenishment timing.
7. Serialize one-shots. Queue consumable gestures while Vern is speaking; if
   speech starts mid-gesture, finish the prop-safe return before showing talking.
   Broadcast audio must never wait for animation. On teardown/cancellation,
   restore prop ownership/transforms and remove event subscriptions.

Keep playback on the Godot main thread. Use the project's dependency/event
patterns for integration; place prop-specific offsets and behavior with the
prop/controller rather than adding them to room-level setup.

### Animation acceptance checks

- Re-import into clean Blender and Godot: all five clips (including
  `seated_rest`) exist, skinning survives, and loops/one-shots have correct modes.
- Sample each clip through its full duration; no bind-pose flashes, root drift,
  chair/table penetration, foot sliding or detached accessories.
- Review moving footage, not only stills: two full cycles for each loop and
  pickup/contact/release for each one-shot, in both studio view and 320x180 feed.
- Mug/cigarette have no visible attachment jumps, duplication or float; repeated
  uses leave props in exactly the same resting transforms. Inspect grip from
  front and side as well as the broadcast angle.
- Exercise idle → talking → idle, repeated one-shots, queued requests, speech
  beginning mid-gesture, interrupted dialogue and scene exit/re-entry.
- Update `VernCharacterIntegrationTests`: its existing paused/unchanging-pose
  assertion must become a seated-startup and moving-idle assertion. Add meaningful
  coverage for clip completion, prop restoration and dialogue state handling.
- Establish a fresh test baseline, then `dotnet build` and run relevant tests
  through `pwsh -NoProfile -File run-tests.ps1`; compare any full-suite failures
  with that baseline. Record validation and any remaining visual limitations.

### GPT-6 Astra animation handoff prompt

```text
Implement Animation pass 1 in docs/art/VERN_3D_MODEL_BRIEF.md.
First read and apply the persistent character guidelines:
docs/art/CHARACTER_GUIDELINES.md (generic principles) and
docs/art/VERN_CHARACTER_GUIDELINES.md (Vern-specific anchors/vocabulary).
Use the existing Vern model and rig, preserving his appearance, neutral bind
pose, seated_rest action, current chair fit and separate chair asset.

Add idle_breathing and talking_default loops plus smoking and drink_coffee
one-shots. Start with a generic restrained talking performance; mood variants
come later. Create matching separate coffee mug, ashtray and cigarette props.
Mug and ashtray are permanent studio dressing; the cigarette rests at the
ashtray between uses. Animate actual pickup, mouth contact and return using
one visible prop instance with seamless hand/rest-anchor transfers.

Work through Blender blocking/contact review, reproducible generation/export,
Godot playback review, then runtime dialogue/item-use integration. Follow the
clip, contact, scheduling and validation requirements in the brief. Keep smoke
Godot-side and coordinate its exhale beat. Preserve existing gameplay timing.

Deliver updated generators, editable Blender sources, GLBs, named-action
validation report, moving previews and real Godot studio/broadcast-feed review.
Update tests for moving idle, one-shot completion and prop restoration. Build
and test using repository instructions and report remaining visual limitations.
```

## Original model-production handoff (completed)

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
