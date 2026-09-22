# Character Reference Library

Local, offline reference assets for character model and animation work -
humanoid base meshes and a reusable animation library. Use these as
**inspiration and starting points** per
[CHARACTER_GUIDELINES.md](CHARACTER_GUIDELINES.md); see
[VERN_CHARACTER_GUIDELINES.md](VERN_CHARACTER_GUIDELINES.md) for how they apply
to Vern.

> **Gitignored.** `docs/references/` is excluded from version control (large
> bundles, ~101 MB). A fresh clone will not contain these files - re-add them
> locally from their sources before relying on the links below.

---

## 1. Animation Library[Standard] - Quaternius

Rigged characters + motion library for humanoid retargeting. **License:** CC0
1.0 Universal (public domain) by [@Quaternius](https://www.patreon.com/quaternius)
(see `License.txt` in the bundle).

| Engine | File | Size |
|---|---|---|
| Godot | [AnimationLibrary_Godot_Standard.glb](../references/Animation%20Library%5BStandard%5D/Godot/AnimationLibrary_Godot_Standard.glb) | 6.4 MB |
| Unity | [AnimationLibrary_Unity_Standard.fbx](../references/Animation%20Library%5BStandard%5D/Unity/AnimationLibrary_Unity_Standard.fbx) | 19.7 MB |
| Unreal Engine | [AL_Standard.fbx](../references/Animation%20Library%5BStandard%5D/Unreal%20Engine/AL_Standard.fbx) | 23.7 MB |
| Preview | [Preview.png](../references/Animation%20Library%5BStandard%5D/Preview.png) | 2.3 MB |

### Extracted clips (Godot GLB, 46 named clips)

**Vern-relevant (seated radio host):**

- `Sitting_Enter` - walk into / settle onto a seat (base for seated transitions)
- `Sitting_Exit` - leaving the seat
- `Sitting_Idle_Loop` - seated rest pose (matches generic §2 rest pose)
- `Sitting_Talking_Loop` - seated speech motion (retarget base for `talk_*`)
- `Idle_Loop`, `Idle_Talking_Loop` - standing variants
- `A_TPose` - bind/reference pose for retargeting and rig alignment

**Full clip list:** `A_TPose`, `Crouch_Fwd_Loop`, `Crouch_Idle_Loop`,
`Dance_Loop`, `Death01`, `Driving_Loop`, `Fixing_Kneeling`, `Hit_Chest`,
`Hit_Head`, `Idle_Loop`, `Idle_Talking_Loop`, `Idle_Torch_Loop`, `Interact`,
`Jog_Fwd_Loop`, `Jump_Land`, `Jump_Loop`, `Jump_Start`, `PickUp_Table`,
`Pistol_Aim_Down`, `Pistol_Aim_Neutral`, `Pistol_Aim_Up`, `Pistol_Idle_Loop`,
`Pistol_Reload`, `Pistol_Shoot`, `Punch_Cross`, `Punch_Enter`, `Punch_Jab`,
`Push_Loop`, `Roll`, `Roll_RM`, `Sitting_Enter`, `Sitting_Exit`,
`Sitting_Idle_Loop`, `Sitting_Talking_Loop`, `Spell_Simple_Enter`,
`Spell_Simple_Exit`, `Spell_Simple_Idle_Loop`, `Spell_Simple_Shoot`,
`Sprint_Loop`, `Swim_Fwd_Loop`, `Swim_Idle_Loop`, `Sword_Attack`,
`Sword_Attack_RM`, `Sword_Idle`, `Walk_Formal_Loop`, `Walk_Loop`.

### Use for Vern

Retarget `Sitting_Idle_Loop` / `Sitting_Talking_Loop` onto Vern's humanoid rig
instead of inventing seated biomechanics from scratch (generic §5 - prefer real
motion; §9 - Godot humanoid retargeting). Use `Sitting_Talking_Loop` as the
seed for `talk_calm` and `PickUp_Table`/`Interact` for grip/release feel.

---

## 2. Human Base Meshes Bundle v1.4.1

Blender asset library of humanoid base meshes. **License not bundled - verify
before shipping any derived asset.** Contains:

| Asset | Link |
|---|---|
| Blender library | [human_base_meshes_bundle.blend](../references/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend) (47 MB) |
| Catalog definitions | [blender_assets.cats.txt](../references/human-base-meshes-bundle-v1.4.1/blender_assets.cats.txt) |

### Catalog (from `blender_assets.cats.txt` + thumbnails)

| Category | Assets | Thumbnail |
|---|---|---|
| Planar | head, skull | [planar_head.png](../references/human-base-meshes-bundle-v1.4.1/thumbnails/planar_head.png) |
| Primitives | body male/female (realistic + stylized), head (generic + realistic) | [primitive_body_male_stylized.png](../references/human-base-meshes-bundle-v1.4.1/thumbnails/primitive_body_male_stylized.png) |
| Realistic | body male/female, eye, foot, hand, head (anim + sculpt), jaw, skeleton, skull | [realistic_body_male.png](../references/human-base-meshes-bundle-v1.4.1/thumbnails/realistic_body_male.png) |
| Stylized | body male/female, eye, foot, hand, head, jaw | [stylized_body_male.png](../references/human-base-meshes-bundle-v1.4.1/thumbnails/stylized_body_male.png) |

All 24 thumbnails live in
[thumbnails/](../references/human-base-meshes-bundle-v1.4.1/thumbnails/).

### Use for character work

Satisfies generic §1 (do not invent anatomy from scratch) - pull a base mesh
rather than modeling proportions; the stylized strand is the closer match to
KBTV's slightly-cartoony Vern. Import into a Blender asset library via the
`.cats.txt` catalog, then proceed through the [3D_ASSET_WORKFLOW.md](3D_ASSET_WORKFLOW.md) tooling.

---

## Workflow cross-reference

| Character stage | Guidance | Reference asset |
|---|---|---|
| Base mesh / proportions | CHARACTER_GUIDELINES §1 | Human Base Meshes bundle |
| Rest pose | CHARACTER_GUIDELINES §2 | `Sitting_Idle_Loop` |
| IK / prop interaction | CHARACTER_GUIDELINES §3, VERN_CHARACTER_GUIDELINES | - |
| Animation vocab layers | CHARACTER_GUIDELINES §4 | library clips |
| Retarget-mocap seed | CHARACTER_GUIDELINES §5 | `Sitting_Talking_Loop` |
| Godot humanoid system | CHARACTER_GUIDELINES §9 | Godot GLB export