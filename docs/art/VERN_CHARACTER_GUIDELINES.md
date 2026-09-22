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
`Sitting_Idle_Loop`, `Sitting_Talking_Loop`).

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

## Known limitations to fix (current asset)

When retouching the existing Vern asset, address these in priority order
(generic doc §10); these are documented as the primary remaining gaps:

- Hands: four fingers share one grip bone; thumb is rigid. Replace with
  articulated fingers/thumb for believable prop contact.
- `talking_default` is a fixed looping clip; sampling has produced stiff,
  "sampled prop trajectories." Rework toward layered/responsive performance.
- Prop pickup/release uses separate sampled trajectories; move to IK against the
  anchors above with seamless hand/rest-anchor transfers.
- Flat-color materials are acceptable to keep; articulation and contact are the
  current focus over material richness.

## Apply order for Vern work

1. Verify/refresh proportions, joint alignment, and seams with the rest pose.
2. Fix hands and grip bones; keep the existing skeleton/bone names stable.
3. Configure IK targets from the anchor table above.
4. Validate the coffee sequence (see above) before building more clips.
5. Only then expand the talking/smoking vocabulary and layer blends.