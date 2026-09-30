# Character Animation Workflow

Reusable workflow for generating and polishing KBTV character animations. This
process came from the Vern seated coffee/smoking pass, but it should apply to
future seated hosts, callers, and prop interactions.

## Core Rule

Do not judge animation from numeric contact reports alone. Use deterministic
rebuilds and validators to catch regressions, then make final decisions from
labeled moving review captures.

## Proven Sequence

1. Establish the runtime truth first.
2. Rebuild from clean source clips before every authoring pass.
3. Capture a labeled visual baseline before changing poses.
4. Fix one visible failure at a time.
5. Validate geometry, continuity, support, and build/tests after each meaningful
   pass.
6. Keep every review bundle so changes can be compared instead of guessed from
   memory.

## Runtime Truth Checklist

Before editing an animation, confirm these facts from the actual runtime scene:

- Which clip the controller really plays. Vern speech uses `talk_calm`, not
  `talking_default`, except as fallback.
- The scene transform chain and local axes. For Vern, `+Y` is up, `-Z` is front,
  and `+X` is his right side.
- The real prop/table/chair geometry, including generated asset dimensions.
- Whether review-only flags are active, such as a held pose that disables the
  runtime animation controller.
- Which objects occlude the production camera versus the isolated review camera.

Old comments are not authoritative. We lost time because an obsolete note said
the chair had no armrests, while the generator did create arm pads.

## Contact Anchors

Use geometry-derived markers rather than prop origins.

- Mouth contact should target the cigarette filter end or mug rim, not the prop
  origin.
- Finger contact should target measured wrist-local finger joint centers before
  fine skin-surface tuning.
- If a prop orientation changes, update the rim/filter/grip markers in the same
  change.
- Validate pickup and release continuity at the rest anchor, not only at the use
  pose.

For Vern, this data lives in `animation_contacts.json` and
`prop_contact_markers.json`.

## Authoring Lessons

Author believable stateful actions instead of forcing a wrist directly between
endpoints.

- Rest pose first, then reach, stationary grasp, lift/use, return, stationary
  set-down, finger release, and withdrawal.
- Reset skeleton/bone state before each sampled frame. Avoid cumulative pose
  drift.
- Bake achievable rotations only. Do not teleport wrist origins to fake contact.
- Keep inactive hands solved to their support target while torso motion runs.
- Use minimum-jerk easing and separate timing offsets for torso, shoulder,
  clavicle, wrist, and fingers.
- Add small clavicle/shoulder advance for reach before adding more torso lean.
- Share forearm roll across lower-arm and wrist bones where possible; wrist-only
  roll tends to look stiff or twisty.
- Preserve stationary grasp/release intervals. They make prop ownership readable.

## Props On Surfaces

Surface support must use the actual scene transform and full prop footprint.

- Check table-space bounds, not just local coordinates.
- Include rotated handles, tray edges, cigarette length, and any offset markers.
- Leave visible clearance from table edges; Vern's latest pass uses a 7.5 cm
  minimum and currently clears by about 8 cm.
- Moving props deeper often requires shoulder/clavicle reach adjustments, not
  only prop-anchor edits.

## Twist And Flip Debugging

Twist problems are often invisible in endpoint checks.

- Sample clips densely, around 120 Hz, and report maximum per-joint angular step.
- Check wrist orientation drift during contact windows.
- If an automatic forearm-roll projection becomes ambiguous, seed one stable
  guide pose and bypass the projection for that action.
- Rotate a prop only after it has visually cleared its rest surface if the pickup
  would otherwise look like a twisting grab.

The smoking fix used a near-side pickup, then rolled the cigarette sideways only
after lift clearance, and reversed that roll before set-down.

## Review Capture

Each review bundle should include:

- Versioned output directory under `%LOCALAPPDATA%/Temp/opencode`.
- Manifest with clip names, times, and camera/view labels.
- Runtime-scene instancing, not raw GLB screenshots.
- Side view, three-quarter view, contact view, and an opposite-side grip view for
  hand work.
- GIFs or sheets with labels containing clip, camera, time, and iteration.

Use low frame-rate broad reviews for coverage, then higher frame-rate polish
reviews for smoothness. Vern's broad review is 8 fps; action-only polish review
is 24 fps.

## Validation Gates

Run validators to catch regressions, but still require visual acceptance.

- Deterministic rebuild/repeatability hash check.
- Contact diagnostics for mouth markers, pickup/release errors, and finger-center
  proxies.
- Surface support validator for full prop footprints.
- Twist validator for wrist drift and angular discontinuities.
- `dotnet build KBTV.csproj`.
- Focused runtime tests for the animation controller and character integration.

Passing these gates means the clip is stable and mechanically plausible. It does
not prove natural finger contact, skin-surface penetration, or runtime blend
quality.

## Minimum Tooling For A New Character

Before generating a large animation set, create these for the character:

- A clean rebuild script that restores source clips and reapplies authoring.
- A preview script that captures labeled versioned review bundles from the
  runtime scene.
- Contact diagnostics for the character's main mouth/hand/prop markers.
- Surface support diagnostics for table, chair, floor, or carried-object rests.
- A continuity/twist diagnostic for any action with wrist rotation or prop roll.

Do not expand the library until one complete interaction sequence survives both
visual review and these validators.
