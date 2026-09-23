# Vern Character Guidelines

Vern-specific application of [CHARACTER_GUIDELINES.md](CHARACTER_GUIDELINES.md).
**Read the generic document first**, then read this document. The generic
document supersedes any earlier per-character statements on mesh, rig, or
animation (including "simple hands are sufficient" and any rigid-hand/finger
limits in older briefs).

Production facts (tests, studio placement, chair, materials, exports) are
defined in [VERN_3D_MODEL_BRIEF.md](VERN_3D_MODEL_BRIEF.md). Workflow and tooling
live in [3D_ASSET_WORKFLOW.md](3D_ASSET_WORKFLOW.md). For reusable base meshes and
seated/talking animation seeds, see
[CHARACTER_REFERENCE_LIBRARY.md](CHARACTER_REFERENCE_LIBRARY.md) (e.g.
`Sitting_Idle`, `Sitting_Talking`).

## Identity (brief summary)

Middle-aged late-night radio host; short dark swept/side-parted hair with gray
at the temples, mustache, aviator glasses, vintage over-ear headphones, black
high-neck sweater. Paranormal-themed show at a desk with a microphone. See the
VERN_3D_MODEL_BRIEF for the full appearance contract; reattach the reference
photo in fresh sessions.

## Rest pose (seated at the studio desk)

The primary pose is seated at the radio desk. Verify before animating:

- Pelvis rests on the chair; thighs rest on the cushion; knees bend naturally.
- Feet planted naturally; spine relaxed; shoulders relaxed.
- Elbows have natural bends.
- Head is naturally positioned relative to the microphone.
- Hands can rest on the chair armrests or desk surface with natural asymmetry.

## Interaction anchors

Author IK targets toward these known points instead of guessing coordinates:

| Prop/Surface | Target | Purpose |
|---|---|---|
| CoffeeCup | `GrabPoint` | Hand attaches to grasp the cup |
| CoffeeCup | `DrinkPoint` | Cup position at the mouth |
| Cigarette | `GripPoint` | Hand grips the cigarette |
| Ashtray | `CigaretteTarget` | Cigarette position while resting/ashing |
| Microphone | `InteractionTarget` | Head proximity while speaking |
| Chair | `LeftArmRestTarget` | Left hand/arm rest |
| Chair | `RightArmRestTarget` | Right hand/arm rest |
| Desk | `LeftHandRestTarget` | Left hand rest |
| Desk | `RightHandRestTarget` | Right hand rest |

Keep anchor names stable so later animations and future characters can reuse them.

## Animation vocabulary

Reusable layers (blend/compose rather than one giant clip):

- **Base**: `seated_idle_01`, `seated_idle_02`, `seated_idle_03`.
- **Torso**: `lean_forward`, `lean_back`, `shift_left`, `shift_right`.
- **Head**: `look_microphone`, `look_console`, `look_coffee`, `listen`,
  `small_head_motion`.
- **Talking**: `talk_calm`, `talk_normal`, `talk_excited`, `talk_angry`.
- **Hands**: `gesture_small`, `gesture_medium`, `gesture_large`.
- **Coffee**: `reach_coffee`, `grab_coffee`, `drink_coffee`, `replace_coffee`.
- **Smoking**: `cigarette_idle`, `raise_cigarette`, `inhale`, `lower_cigarette`,
  `ash_cigarette`.
- **Face**: `blink`, `smile`, `annoyed`, speaking facial movement.

A composed performance layers over a base idle, e.g. `seated_idle` +
`talk_calm` + `small_head_motion` + `blinking`.

## Stateful prop interactions

**Coffee:** cup on desk → right hand reaches `GrabPoint` → fingers close → cup
attaches to hand → hand moves toward mouth → `drink_coffee` → hand returns to
desk → cup detaches → hand returns to resting/gesture state.

**Smoking:** compose from smaller states, not an endless full-body loop:
`cigarette_idle` → `raise_cigarette` → `inhale` → pause → `lower_cigarette` →
`cigarette_idle`. Occasionally: `cigarette_idle` → move toward ashtray →
`ash_cigarette`/tap → return → `cigarette_idle`.

## Validation sequence (do this before expanding the library)

Produce one high-quality test sequence first:

seated idle → notice coffee → reach for coffee → grasp cup → lift cup → drink →
lower cup → place cup on desk → release cup → return to seated idle.

It must demonstrate correct anatomy/rigging, natural seated posture, IK, correct
hand placement, prop attachment/detachment, natural timing, overlapping motion,
appropriate interpolation, and subtle secondary movement. Do not expand the
animation library until this sequence looks convincing.

## Current asset state and remaining gaps

The current production asset is no longer the early rigid-hand prototype. Before
retouching it, confirm the latest generated reports in `docs/art/model_previews/`:

- `vern.json` reports the generated GLB contract: 53 bones, articulated digits,
  textured materials, and the shipped clip set.
- `vern_godot_animation_validation.json` validates fixed pelvis/feet, loop seams,
  prop grip, mouth contact, and hand travel in an isolated Godot import.
- `vern_proportion_audit.json` is the current generated-Vern proportion audit;
  `mpfb_vern_proportion_comparison.json` compares it to a default MPFB
  standard-rig human.
- `talk_calm` is the production on-air speaking clip. It is injected at runtime
  from `assets/models3d/characters/vern/animations/talk_calm.tres` and falls
  back to `talking_default` if loading fails.

Remaining gaps, in priority order:

- Compare Vern's current proportions against the MPFB baseline, then adjust only
  the generator values needed to improve adult anatomy.
- Evaluate the MPFB body diagnostic before making more procedural body tweaks.
  Male body targets are confirmed active and the face points toward Blender -Y
  (glTF/Godot +Z), verified against eye/head landmarks and rendered views.
  The previous centroid-based positive-Y conclusion was incorrect. Use that facing when migrating toward the MPFB body/rig
  instead of continuing to tune the tube-based procedural body.
- Keep bone names, clip names, `animation_contacts.json`, and prop rest/grip
  contracts stable unless a downstream runtime/test update is made in the same
  change.
- Move further from monolithic authored loops toward layered speech/head/hand
  vocabulary after the seated rest pose and coffee validation sequence still read
  well with the refined proportions.
- Facial animation/lip sync remains future work; keep current jaw/eyelid tracks
  working while proportion changes are evaluated.

## Apply order for Vern work

1. Run `Tools/modelgen/vern_proportion_audit.py` and save the current baseline.
2. Run `Tools/modelgen/mpfb_vern_proportion_compare.py`, then decide which
   proportions actually need changing.
3. Edit the procedural generator with stable bone names and contact contracts.
4. Rebuild Vern, rerun Godot import/contact validation, and review moving clips.
5. Validate the coffee sequence (see above) before expanding more clips.
6. Only then expand the talking/smoking vocabulary and layer blends.
