# Character Model & Animation Guidelines

Persistent, character-agnostic principles for creating or modifying 3D character
mesh, rig, and animation in KBTV. **Read this before any character work.** It
applies to every current and future character (NPCs, guest hosts, extras), not
just Vern.

For a character-specific worked example, see
[VERN_CHARACTER_GUIDELINES.md](VERN_CHARACTER_GUIDELINES.md) and the production
facts in [VERN_3D_MODEL_BRIEF.md](VERN_3D_MODEL_BRIEF.md). Authoring/export tooling
lives in [3D_ASSET_WORKFLOW.md](3D_ASSET_WORKFLOW.md).

When these principles conflict with an older per-character brief, this document
supersedes it. Diagnose the underlying cause instead of compensating elsewhere
(see [12. General AI behavior](#12-general-ai-behavior)).

## 1. Do not invent human anatomy from scratch

Prefer a known-good humanoid base model and skeleton.

Recommended pipeline:

MakeHuman/MPFB → Blender → Rigify/standard humanoid rig → animation/mocap →
Blender cleanup → glTF/GLB → Godot.

Use anatomically reasonable human proportions as the foundation. Before adding
detail, verify:

- Head/body proportion.
- Shoulder width.
- Arm length.
- Elbow position.
- Hand position.
- Torso length.
- Hip position.
- Leg length.
- Knee position.
- Foot size.

Joint centers must align correctly with the mesh.

Do not compensate for a bad mesh by changing animations. Fix the character
proportions and rig first.

## 2. Establish a correct rest pose before creating animations

Identify the character's primary in-game rest pose (seated jobs, standing jobs,
etc.) and make it believable before animating. For a seated character, verify:

- Pelvis actually rests on the chair.
- Thighs rest naturally on the seat.
- Knees bend naturally.
- Feet are positioned naturally.
- Spine has a relaxed curve.
- Shoulders are relaxed.
- Elbows have natural bends.
- Hands/forearms can rest comfortably on the desk/chair.
- Head is naturally positioned relative to the main interaction (microphone, desk).

Do not proceed with complex animation until the static rest pose looks believable.

## 3. Use IK for hands and prop interaction

Do not manually animate shoulder, elbow, wrist, and hand rotations
independently when interacting with objects.

Use IK targets for hands whenever possible.

Create explicit interaction targets per prop and positioning surface, for
example `GrabPoint`, `DrinkPoint`, `GripPoint`, `InteractionTarget`,
`LeftArmRestTarget`, `RightArmRestTarget`, `LeftHandRestTarget`,
`RightHandRestTarget`.

Animations should move IK targets toward these known interaction points instead
of guessing arbitrary world coordinates. Each character's script should name its
concrete targets (see VERN_CHARACTER_GUIDELINES.md for an example).

## 4. Separate animations into reusable layers

Avoid creating giant animations such as "character_talking_and_smoking."

Create a reusable animation vocabulary:

- **Base**: several idles (~`idle_01`, `idle_02`, `idle_03`).
- **Torso**: lean forward/back, shift left/right.
- **Head**: look toward the main targets, listen, small head motion.
- **Speaking**: calm/normal/excited/angry variants.
- **Hands**: gesture small/medium/large.
- **Per-prop actions**: reach, grab/hold/use, replace (coffee, cigarette, tools, etc.).
- **Face**: blink, smile, annoyed, speaking facial movement.

Animations should be capable of blending/layering where practical. Compose
performances from layers (idle base + speech + head motion + blinking + a held
prop idle) rather than baking one monolithic clip.

## 5. Prefer real human motion as an animation reference

Whenever possible, use existing humanoid animation/mocap data as the starting
point rather than generating body motion entirely from scratch.

Retarget existing animations onto the character's humanoid skeleton and modify
them as necessary.

AI-generated animation should preferably MODIFY or COMBINE known-good motion
instead of inventing biomechanics.

## 6. Avoid robotic timing

Human body parts should not begin and stop moving simultaneously.

Use overlapping motion and anticipation. When using a prop, sequence the motion
with small timing offsets: eyes glance toward the target → head turns slightly →
shoulder begins → elbow follows → hand approaches → fingers contact → object
lifts → head adjusts → target reaches mouth → pause → moves away → head begins
returning → object returns → fingers release → arm returns.

Movements should have acceleration/deceleration rather than constant-speed
interpolation.

Introduce small timing offsets between the head, shoulders, elbows, wrists,
fingers, and torso.

## 7. Preserve subtle secondary motion

The character should rarely be perfectly motionless.

Use restrained secondary motion such as:

- Breathing.
- Small posture shifts.
- Head movement.
- Eye movement.
- Blinking.
- Small hand adjustments.
- Wrist/finger movement.
- Slight shoulder movement.

Keep these subtle. Do not exaggerate them simply to make the character appear
animated.

## 8. Treat prop use as stateful interactions

Props should have clear ownership states.

Example sequence:

Prop at rest → hand reaches GrabPoint → fingers close → prop attaches to hand →
hand moves toward use position → use animation → hand returns → prop detaches →
hand returns to resting/gesture state.

Where a sequence repeats optional behaviors (e.g. occasional tapping/ashing
while smoking), compose it from smaller states rather than one endlessly looping
full-body animation.

## 9. Target Godot's humanoid animation system

Prefer a standard humanoid skeleton compatible with Godot's humanoid retargeting
system.

Keep animation assets reusable so future characters can potentially share
animations.

Export through glTF/GLB whenever practical.

Design the animation system to work with Godot AnimationPlayer/AnimationTree
rather than baking every behavior into one giant animation.

## 10. Prioritize animation quality in this order

When evaluating a character, fix problems in this order:

1. Human proportions.
2. Skeleton/joint placement.
3. Skin weighting.
4. Natural rest pose.
5. IK configuration.
6. Basic rest idle.
7. Hand/prop interaction.
8. Body animation.
9. Facial animation.
10. Secondary/procedural motion.

Do not attempt to hide problems at an earlier stage with improvements at a later
stage.

## 11. Initial validation animation

Before creating a large animation library, produce one high-quality test
sequence built around the character's primary interaction:

rest idle → notice the prop → reach → grasp → lift → use (drink/inhale/etc.) →
lower → place back at rest → release → return to rest idle.

This sequence should demonstrate:

- Correct anatomy.
- Correct rigging.
- Natural rest posture.
- IK.
- Correct hand placement.
- Prop attachment/detachment.
- Natural timing.
- Overlapping motion.
- Appropriate interpolation.
- Subtle secondary movement.

Do not expand the animation library until this sequence looks convincing.

## 12. General AI behavior

When asked to create or modify a character, first determine whether an existing
rig, animation, IK target, interaction point, or mocap animation can be reused.

Prefer deterministic technical operations over generating arbitrary geometry or
animation.

When something looks wrong, diagnose whether the problem originates from:

- Mesh proportions.
- Rig placement.
- Skin weights.
- IK configuration.
- Animation.
- Prop positioning.
- Animation blending.

Fix the underlying cause instead of compensating elsewhere.

The goal is not maximum animation complexity. The goal is a believable character
whose movements are subtle, natural, reusable, and appropriate for their setting.