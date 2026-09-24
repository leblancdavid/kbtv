# Vern MPFB → Production Migration Plan

Phase-goal: promote the approved fitted prototype
(`assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb`, MPFB standard rig)
into the runtime `Vern.tscn` slot without breaking the character's animation,
contact, or placement contract.

**Gate (from VERN_3D_MODEL_BRIEF.md):** do not swap `Vern.tscn` until seated
pose, skinned/fitted clothing, hand/mouth contact anchors, and `talk_calm`
retarget/rebake are migrated.

## Rig facts (grounded by direct GLB probes, 2026-09-24)

| Asset | Armature root | Joints | Facing (Godot) | Notes |
|---|---|---|---|---|
| `vern_mpfb_fitted.glb` | `Vern_MPFB_StandardRig` | 137 | **+Z** (Blender -Y) | Standing rest, 1.707m |
| `vern.glb` | `VernRig` | 53 | **-Z** | Seated, holds `seated_rest` |

**MPFB standard rig names (relevant subset):** `root`; split pelvis
`pelvis.L`/`pelvis.R` (no single pelvis); `upperleg01/02.L(R)`,
`lowerleg01/02.L(R)`, `foot.L(R)`, `toe1-1.L(R)`; spine chain
`spine05→spine04→spine03→spine02→spine01` (with `breast.L(R)`); `clavicle.L(R)`,
`shoulder01.L(R)`, `upperarm01/02.L(R)`, `lowerarm01/02.L(R)`, `wrist.L(R)`;
fingers `finger1-1…finger5-3` + `metacarpal1-5` per side; `neck01/02/03`,
`head`, `jaw`; facial set (`oris*`, `levator*`, `orbicularis*`, `oculi*`,
`temporalis*`, `risorius*`, `tongue*` groups). **No `grip`, no `eyelid`,
no single `hand` bone.**

**Production VernRig hierarchy:** `root>pelvis>spine>chest>neck>head`
(children `eyelid.L`, `eyelid.R`, `jaw`); arms
`upper_arm.L(R)>forearm>hand` (children `grip`, `index/little/middle/ring/thumb
_1…_3`); legs `thigh>shin>foot`.

**Consequence:** the MPFB export uses a different, larger skeleton than Vern's
runtime. Clip migration cannot be a plain file copy; it requires a bone-name
transfer, and the runtime skeleton path must follow the new armature.

## Migration steps

### 1. Seated pose (authored)

The fitted GLB exports standing. Author a seated rest pose on the MPFB standard
rig matching the current VernStation chair fit (SeatAnchor `(0, 0.53, -0.005)`,
yaw 180 for the +Z-facing model). Export a `seated_rest` action with identical
keys over a short duration (existing `vern_rig.py`/`vern.py` pattern), and
preserve the female-target-free male baseline. `VernCharacter3D` must apply the
pose before first visibility (same mechanism as today).

### 2. Clip migration via profile join (reuse `bake_talk_calm.gd` machinery)

The production bake tools prove the approach: two Godot `BoneMap` resources are
joined through the shared SkeletonProfile (profile→source names from
`bone_map_ref.tres`, profile→target names from `bone_map_vern.tres`), and the
raw reference GLB is read directly to map DEF-* bones onto the target names.

For MPFB this becomes a **VernRig→MPFB** transfer:

1. Create `bone_map_mpfb.tres` (profile → MPFB standard rig names).
2. Extend the profile-join to: VernRig clip tracks → MPFB names, using the
   table below where names diverge.
3. Re-bake `talk_calm.tres` (and any clips that reference Vern-native bones)
   with the MPFB skeleton path, e.g. `Vern_MPFB_StandardRig/Skeleton3D:<bone>`.

Current MPFB bake outputs `talk_calm`, `idle_breathing`, `talking_default`,
`smoking`, and `drink_coffee` clips. The authored source clips and MPFB
`seated_rest` clip do not carry a root ROT track, so the MPFB re-bake
intentionally emits **no root ROT track**. Root keeps the exact skeleton rest
basis and only receives the seated root POS track; this avoids quaternion
reconstruction drift from the fitted root rest basis' tiny non-orthonormal
component.

**VernRig → MPFB name table (divergences):**

| VernRig | MPFB |
|---|---|
| `pelvis` | `pelvis.L` + `pelvis.R` (split; copy identical transforms to both, or map legs from each side) |
| `spine`/`chest` | `spine01…spine05` chain (spread) |
| `upper_arm.L` | `upperarm01.L` |
| `forearm.L` | `lowerarm01.L` |
| `hand.L` | `wrist.L` (MPFB has no hand bone) |
| `thumb _1…_3` | `finger1-1…finger1-3` per side |
| `index _1…_3` | `finger2-1…finger2-3` per side |
| `middle _1…_3` | `finger3-1…finger3-3` per side |
| `ring _1…_3` | `finger4-1…finger4-3` per side |
| `little _1…_3` | `finger5-1…finger5-3` per side |
| `grip` | dropped (MPFB uses four/five independent finger chains) |
| `thigh.L` | `upperleg01.L` |
| `shin.L` | `lowerleg01.L` |
| `foot.L` | `foot.L` |
| `eyelid.L/R` | no direct equivalent (nearest `oculi*`; likely drop + keep jaw) |
| `jaw` | `jaw` |
| `head`/`neck` | `head`/`neck01…03` |

Notes:
- The CC0 reference (`AnimationLibrary_Godot_Standard.glb`) is the shared
  motion source for `talk_calm`, so the existing DEF-* mapping plus the
  profile join can drive the MPFB export directly, skipping the VernRig
  intermediate where clips originate from the reference.
- Author/produced Vern clips (arms/fingers from `talking_default`, jaw/eyelid)
  should be transferred with the same table and re-verified with the
  `reimpl_validate.gd`/`val_fidelity` fidelity checks.

### 3. Contact anchors

`animation_contacts.json` currently references `hand.L`, `hand.R`, `head`.
For MPFB the anchors must target the new bone names:
`wrist.L`/`wrist.R` (or authored empties parented to the wrists), `head`.
Re-verify coffee lift, cigarette reach, and mouth contact against the seated
pose; the prop trajectories in `VernPerformanceProps` sample these transforms.

### 4. Runtime wiring

- `Vern.tscn`: swap the mesh/skeleton reference to
  `vern_mpfb_fitted.glb`; update the skeleton path constant used by the
  animation libraries (`VernRig/Skeleton3D` → MPFB armature path).
- If the MPFB full facial rig offsets performance, export a Vern-compatible
  subset skeleton instead and retitle this step; the bone table above still
  applies.
- Apply the +Z facing / yaw-180 placement at the `VernStation` placement root
  (currently authoring convention is Godot -Z).
- `VernCharacter3D` injects `talk_calm.tres` at runtime; keep
  `HasAnimation` guard + `talking_default` fallback working.

### 5. Validation gates

- `validate_mpfb_fitted.py` green on the exported asset (normalized weights,
  facing, covered-skin removal, bounds, rigid head motion, embedded RGBA mask
  for hair).
- Re-run `reimpl_validate.gd`/`val_fidelity` after each clip rebake; confirm
  planted feet/seam/motion/head/jaw/hand metrics in the same ranges as the
  production V5 bake.
- Runtime seat-fix gate: `VernCharacterIntegrationTests` must confirm root,
  pelvis.L/R, and foot.L/R remain fixed when `idle_breathing` blends in from
  `seated_rest`.
- Godot runtime: user visual review of seated Vern in the studio chair
  (silhouette/chair fit, coffee/smoking contacts, headband clearance).
- `VernAnimationControllerTests` 9/9 and `VernCharacterIntegrationTests`
  (incl. `talk_calm` injection assert) remain green.
- `docs/art/model_previews/vern_mpfb_fitted_export_{front,portrait}.png` and a
  new seated render for the review.

## Status

- [x] Hair rebuild exported + validated (embedded RGBA MASK, rigid motion).
- [x] 1. Seated pose authored and fitted to chair.
- [x] 2. `bone_map_mpfb.tres` + MPFB clip rebakes (`talk_calm`, `idle_breathing`, `talking_default`, `smoking`, `drink_coffee`).
- [x] 3. Contact anchors remapped and re-verified.
- [x] 4. `Vern.tscn` swapped + facing fixed.
- [ ] 5. Final preview/user visual review green.
