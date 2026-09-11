# 3D Asset Workflow

This workflow covers GPT-assisted Blender generation for simple KBTV 3D props.
Use it for low-risk props during the 3D migration visual pass, not for room
layout, collision design, character animation, or final hero assets.

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
| `audio_cabinet.glb` | 0.700 x 1.000 x 0.658 | 12,876 | 7 | `ControlRoom3D/AudioCabinet` at `(4.1, 0, -2.3)`, Y rotation 180. |
| `microphone_stand.glb` | 0.350 x 1.200 x 0.350 | 4,296 | 5 | `StudioRoom3D/StudioMicStand` at `(0.95, 0.2, 0.65)`, Y rotation 180. |

The first in-game review looked good enough to keep this workflow for additional
simple props. Use the same process for the next one or two assets before scaling
up production.

## Troubleshooting

- `blender` not found: restart the shell after updating PATH, or call Blender by full path.
- Godot shows the prop but lighting is wrong: check `StationLighting3D.ApplyLayerToTree` and visual layers.
- Prop floats or sinks: use bottom-origin positioning, not placeholder center Y.
- Prop faces away: rotate the scene instance around Y in Godot; do not regenerate solely for yaw.
- Thin pieces flicker or disappear: thicken them in the generator and reduce dense detail.
- File is too large: reduce cylinders' vertex counts, join repeated tiny details, and prefer material surfaces over textures.

See also [3D Migration Plan](../technical/THREED_MIGRATION_PLAN.md) and
[Art Style Guide](ART_STYLE.md).
