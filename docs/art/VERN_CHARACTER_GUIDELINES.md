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
For the character-agnostic process distilled from this pass, see
[CHARACTER_ANIMATION_WORKFLOW.md](CHARACTER_ANIMATION_WORKFLOW.md).

## Identity (brief summary)

Middle-aged late-night radio host; short dark swept/side-parted hair with gray
at the temples, mustache, aviator glasses, vintage over-ear headphones, black
high-neck sweater. Paranormal-themed show at a desk with a microphone. See the
VERN_3D_MODEL_BRIEF for the full appearance contract; reattach the reference
photo in fresh sessions.

## Rest pose (seated at the studio desk)

### Reproducible visual review

Runtime speech selects **talk_calm**; talking_default is a fallback. Review both.
The office chair generator includes arm pads. The no-armrest comment in the old
MPFB seating script is obsolete. Prop-origin/mouth distance is not lip contact:
validate cigarette mouth-end and mug rim markers before accepting either action.

From the project root, with a Godot 4.6 mono console executable:

```powershell
pwsh -NoProfile -File Tools/modelgen/rebuild_vern_actions.ps1 -Godot <exe> -VerifyRepeatability
& <exe> --path . --script res://Tools/modelgen/preview_vern.gd -- --review
python Tools/modelgen/package_vern_review.py <printed-review-directory>
```

Packaging requires Pillow. Review bundles live in versioned directories beneath
`%LOCALAPPDATA%/Temp/opencode/vern_reviews`. Each includes 8 fps frames, sheets,
15 GIFs after packaging, and a manifest of resolved clip names. Labels identify
clip, camera, time, and iteration. Side, three-quarter, and contact cameras are
Vern-relative. The harness uses production assets, but disables the animation
controller for isolated seeking, adds a fill light, and hides foreground lamp,
computer, and microphone geometry **only in the review process**. This is not
yet a test of runtime transitions or occlusion in the broadcast camera.

Always rebuild via the wrapper: it reconstructs source clips before authoring.
The authoring pass samples manually (no wall-clock animation advancement) and
applies finger curl relative to bone rest, rather than cumulatively. Repeatability
checks compare all five output clip hashes. Passing those checks establishes
stable input/output, not acceptable posing. Keep each baseline bundle for comparison.

Pending visual gates: comfortable forearm support on actual arm pads; relaxed
wrist/finger orientation; actual talk_calm gestures and transitions; rim/lip and
cigarette/lip contacts; smooth pickup/release. No current origin-distance report
establishes these gates.

The authoring pass now includes actual `talk_calm`, and solves resting wrists
per frame so torso motion does not drag the hands backward. Rest targets use
the office-chair pad dimensions, with palms pronated and fingers pointing forward.
This places hands near pads; full forearm support is still a visual acceptance gate.
`prop_contact_markers.json` records geometry-derived cigarette filter-end and mug
rim points plus sip rotation. `diagnose_vern_contacts.gd` now exits nonzero if
sampled lip contact error exceeds 1 cm. The head-local lip marker was lowered
4 cm after close render review exposed a nose-height target. Marker alignment
does not establish correct finger grip, collision clearance, or transition quality.

Support/motion checks: `validate_vern_support.gd` transforms the prop rest anchors
into the actual World3D StudioTable frame and checks full footprints and surface
height. Contact diagnostics also reject >1 cm pickup/release discontinuities.
The bake resets bone poses before each sample and never teleports the wrist:
only achievable rotations are saved. Reach uses a torso lean, a raised wrist arc,
a stationary grasp interval, and a set-down/open-fingers interval before withdrawal.
Inactive hands are re-solved at their support while the torso moves. Rest palms
are pronated down. Verify these phases in the versioned side/contact captures;
passing positional checks alone does not validate individual finger contact.

Current grip refinement turns the mug handle toward Vern; its rim marker and
wrist grip must be changed together if that orientation changes. Support bounds
include rest rotation (the handle extends beyond the cup body). Coffee/smoking
reach lean is separately tuned, 0.38/0.45 radians, with minimum-jerk easing and
one continuous lift arc. Preserve the stationary grasp/release intervals when
adjusting timing. The cigarette is reversed in the tray so a diagonal finger-axis
grip can approach from the near side without putting the hand over Vern's eyes.

Measured-hand iteration: `probe_vern_hands.gd` prints wrist-local finger joint
positions for idle and held poses. Use those measurements when changing grips.
Cigarette shaft contact at prop-local (0,0,-0.035) targets the midpoint of the
index/middle second joints; the bases adduct toward one another when gripping.
Mug upper-handle contact (0.058,0.084,0) targets the thumb/index distal-joint
midpoint. The palm faces the cup; the handle turns outward during the sip to keep
the hand away from the cheek. Forearm roll shares pronation with the wrist.
`diagnose_vern_contacts.gd` checks these centers during lip contact with a 15 mm
threshold. This is a bone-center proxy, not a skin-surface penetration test.
Review now includes an opposite-side `grip` camera (20 GIFs total); manifests
declare their views so older three-view bundles remain packageable.

Smoking turn correction: cigarette and ashtray now face a near-side pickup
(yaw -90 degrees), replacing the former reversed 180-degree arrangement. Hand
pre-orients early, holds its orientation through grasp, then rolls sideways only
after clearance; the return turn finishes before lowering into the notch.
The active smoking solve seeds ONE guide pose rather than legacy animated arm
roll and bypasses the automatic forearm roll projection (ambiguous when the palm
axis approaches the forearm axis). Run `validate_vern_smoking_turn.gd` with Godot:
it checks world-wrist stability during contact windows and rejects joint flips
over the full 5.5-second clip sampled at 120 Hz. Latest result: 0.04-degree
contact drift, maximum sampled joint step 2.84 degrees (previously ~35 degrees).

Final smoothing pass: action arm/shoulder channels are sampled at 48 Hz with
exact endpoints. A small active clavicle advance during reach lets the props
sit farther inward without increasing torso lean. Table support validation now
requires 7.5 cm full-footprint clearance; current mug/ashtray clearances are
8.16/8.26 cm. `--review --polish` captures only smoking/coffee at 24 fps, four
views each. GIF packaging distributes 10 ms duration rounding and verifies the
saved total duration, preventing a silent playback speedup. Use these higher-rate
bundles to judge smoothing rather than the default 8 fps broad review.

The primary pose is seated at the radio desk. Verify before animating:

- Pelvis rests on the chair; thighs rest on the cushion; knees bend naturally.
- Feet planted naturally; spine relaxed; shoulders relaxed.
- Elbows have natural bends.
- Head is naturally positioned relative to the microphone.
- Hands can rest on the chair armrests or desk surface with natural asymmetry.

## Runtime orientation and body-side contract

Use these definitions for every Vern MPFB animation, contact, diagnostic, and
preview. Do not infer side from the camera image.

- Vern-local `+Y` is up.
- Vern-local `-Z` is front: toward the tray table, microphone, and broadcast camera.
- Vern-local `+X` is Vern's right side; Vern-local `-X` is Vern's left side.
- `*.L` bones are Vern's left body parts; `*.R` bones are Vern's right body parts.
- The current runtime chain is `VernStation` yaw-180 plus `Vern.tscn/Model`
  yaw-180. Those rotations cancel, so the evaluated MPFB skeleton faces the
  in-world table/camera while preserving the Vern-local axes above.
- Preview and validation tools must instantiate the same runtime chain or label
  their output as raw-GLB space. Raw GLB screenshots are not sufficient evidence
  for in-game left/right or front/back behavior.

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
