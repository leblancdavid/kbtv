"""Generate a male MPFB Vern with fitted, skinned clothing.

Run with normal Blender startup (MPFB needs registered preferences), not
--factory-startup:
  blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_fitted.py

Phase 2 of the MPFB Vern migration. Garments are cut from body topology, relaxed
into cloth and retain interpolated MPFB skin weights. Accessories follow the
head. Authoring front is Blender -Y (Godot +Z). Writes prototype files only.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import addon_utils
import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))

import common  # noqa: E402

MPFB_ROOT = Path.home() / "AppData/Roaming/Blender Foundation/Blender/5.2/extensions/blender_org/mpfb"
TARGET_DIR = MPFB_ROOT / "data/targets/macrodetails"
SOURCE = ROOT / "Tools/modelgen/source/vern_mpfb_fitted.blend"
GLB = ROOT / "assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
REVIEW = ROOT / "docs/art/model_previews"
REPORT = REVIEW / "vern_mpfb_fitted.json"

MALE_TARGETS = [
    "caucasian-male-old.target.gz",
    "universal-male-old-averagemuscle-averageweight.target.gz",
]


def mat(name: str, color: str, metallic=0.0, roughness=0.75):
    return common.material(name, color, metallic, roughness)


def ensure_mpfb():
    enabled, _loaded = addon_utils.check("bl_ext.blender_org.mpfb")
    if not enabled:
        addon_utils.enable("bl_ext.blender_org.mpfb", default_set=False, persistent=False)


def bone(name):
    return rig.data.bones[name].head_local.copy()


def group_weight(obj, group, i):
    """Vertex weight, 0.0 for vertices not present in the group at all."""
    try:
        return group.weight(i)
    except RuntimeError:
        return 0.0


def bake_male_shape():
    """Fold the male target shape keys into the basis mesh permanently."""
    keys = body.data.shape_keys
    if not keys or len(keys.key_blocks) < 2:
        return
    basis = keys.reference_key
    deltas = [Vector() for _ in body.data.vertices]
    for block in keys.key_blocks:
        if block is basis or abs(block.value) < 1e-6:
            continue
        for i, v in enumerate(block.data):
            deltas[i] += block.value * (v.co - basis.data[i].co)
    for block in [b for b in keys.key_blocks if b is not basis]:
        body.shape_key_remove(block)
    for i, v in enumerate(body.data.vertices):
        v.co += deltas[i]
    bpy.context.view_layer.update()


def clean_body():
    """Drop MASK modifier and every non-skin vertex so the export carries only
    the naked-body surface. Selection must be made on the edit bmesh, not
    MeshVertex.select, which does not reliably sync in this context."""
    import bmesh
    for mod in [m for m in body.modifiers if m.type == "MASK"]:
        body.modifiers.remove(mod)
    group = body.vertex_groups["body"]
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bm = bmesh.from_edit_mesh(body.data)
    for v in bm.verts:
        v.select = group_weight(body, group, v.index) <= 0.0
    bmesh.update_edit_mesh(body.data)
    bpy.ops.mesh.delete(type="VERT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for g in list(body.vertex_groups):
        if not any(group_weight(body, g, i) > 0.0 for i in range(len(body.data.vertices))):
            body.vertex_groups.remove(g)


def render(name, position, target, scale, resolution=(640, 720)):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Vern review environment')
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get('Background')
    background.inputs['Color'].default_value = (.05, .05, .05, 1)
    background.inputs['Strength'].default_value = 1
    scene.view_settings.view_transform = "AgX"
    bpy.ops.object.camera_add(location=position)
    camera = bpy.context.object
    common.aim(camera, target)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = scale
    scene.camera = camera
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(REVIEW / f"{name}.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def add_lights():
    for position, power, size in [((-2, 3, 4), 350, 3), ((2, 2, 2), 170, 2), ((0, -2, 3), 280, 2)]:
        bpy.ops.object.light_add(type="AREA", location=position)
        light = bpy.context.object
        light.data.energy = power
        light.data.size = size
        common.aim(light, (0, 0, 0.9))


def main():
    global body, rig
    common.reset()
    bpy.context.scene.unit_settings.system = "METRIC"
    p = {
        "skin": mat("Vern warm skin", "b58d78", 0, 0.78),
        "sweater": mat("Charcoal wool sweater", "292c33", 0, 0.92),
        "rib": mat("Raised knit cuffs", "32353c", 0, 0.94),
        "pants": mat("Dark twill trousers", "20232a", 0, 0.88),
        "black": mat("Headphone rubber and soles", "11131a", 0, 0.76),
        "hair": mat("Soft black hair", "262521", 0, 0.86),
        "gray": mat("Salt at temples", "696964", 0, 0.85),
        "metal": mat("Aged pewter frames", "8d8a79", 0.65, 0.38),
    }
    from vern_mpfb_surfaces import build_materials
    build_materials(p)

    ensure_mpfb()
    bpy.ops.mpfb.create_human()
    body = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
    body.name = "Vern_MPFB_Body"
    body.data.materials.clear()
    body.data.materials.append(p["skin"])
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    for filename in MALE_TARGETS:
        bpy.ops.mpfb.load_target(directory=str(TARGET_DIR), files=[{"name": filename}], weight=1.0)
    body.MPFB_HUM_gender = 1.0
    body.MPFB_HUM_age = 0.65
    # create_human starts with mixed default male/female macro targets (including
    # breast targets). Loading two male targets adds to those; it does not replace
    # them. Use only the explicitly selected complete male macro combination.
    target_names = {name.removesuffix('.target.gz') for name in MALE_TARGETS}
    for key in body.data.shape_keys.key_blocks:
        if key != body.data.shape_keys.reference_key:
            key.value = 1.0 if key.name in target_names else 0.0
    assert target_names.issubset(body.data.shape_keys.key_blocks.keys()), 'Missing male macro targets'
    bpy.context.view_layer.update()
    bpy.ops.mpfb.add_standard_rig()
    rig = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
    rig.name = "Vern_MPFB_StandardRig"
    bake_male_shape()

    shoulder_l, shoulder_r = bone("upperarm01.L"), bone("upperarm01.R")
    hip_l, hip_r = bone("upperleg01.L"), bone("upperleg01.R")

    from vern_mpfb_wardrobe import build_wardrobe
    clean_body()
    clean_body_verts = len(body.data.vertices)
    build_wardrobe(body, rig, p)
    from vern_mpfb_proportions import tailor_proportions
    proportions = tailor_proportions(rig)

    garments = [o for o in bpy.context.scene.objects if o.type == "MESH" and o is not body]
    bpy.context.view_layer.update()

    for path in (SOURCE.parent, GLB.parent, REVIEW):
        path.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=str(GLB), export_format="GLB", use_selection=True,
        export_yup=True, export_cameras=False, export_lights=False, export_animations=False,
        export_skins=True)

    mesh_objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    verts = [o.matrix_world @ v.co for o in mesh_objects for v in o.data.vertices]
    minv = [min(v[i] for v in verts) for i in range(3)]
    maxv = [max(v[i] for v in verts) for i in range(3)]
    report = {
        "asset": "vern_mpfb_fitted",
        "status": "fitted_skinned_prototype",
        "blender_version": bpy.app.version_string,
        "source": str(SOURCE.relative_to(ROOT)),
        "glb": str(GLB.relative_to(ROOT)),
        "male_targets": MALE_TARGETS,
        "default_macro_targets_disabled": True,
        "proportions": proportions,
        "surface_maps": {"materials": ["sweater", "rib", "pants", "hair"],
                         "maps": ["base_color", "normal", "roughness_metallic"],
                          "embedded_in_glb": True, "tile_size": 512},
        "hair": {
            "clumps": sum(o.name.startswith('Swept hair clump ') for o in mesh_objects),
            "cards": sum(o.name.startswith('Hair edge card ') for o in mesh_objects),
            "card_material": "Vern masked hair tips", "alpha_mode": "MASK",
            "triangles": sum(len(f.vertices)-2 for o in mesh_objects
                             if o.name.startswith(('Swept hair clump ', 'Hair edge card ', 'Fitted swept scalp'))
                             for f in o.data.polygons),
        },
        "bounds": {"min": [round(x, 5) for x in minv], "max": [round(x, 5) for x in maxv]},
        "height": round(maxv[2] - minv[2], 5),
        "meshes": len(mesh_objects),
        "armatures": len([o for o in bpy.context.scene.objects if o.type == "ARMATURE"]),
        "triangles": sum(sum(len(f.vertices) - 2 for f in o.data.polygons) for o in mesh_objects),
        "body_verts": len(body.data.vertices),
        "body_verts_before_clothing_occlusion": clean_body_verts,
        "front_axis": {"blender": "-Y", "godot": "+Z"},
        "skinned_garments": len(garments),
        "rig_measurements": {
            "shoulder_width": round((shoulder_l - shoulder_r).length, 5),
            "hip_width": round((hip_l - hip_r).length, 5),
        },
        "notes": [
            "Continuous garments extracted from body topology; original skin weights retained.",
            "MPFB face is Blender -Y / glTF +Z; rigid accessories follow head.",
            "Body export baked male shape keys and dropped MASK/helper geometry.",
            "Covered skin removed after garment construction; full base reproducible from MPFB.",
            "Fitted dipped collar; textured knit/twill and softly blended temples with embedded PBR maps.",
            "Still prototype: not runtime-wired; seated pose, contact anchors and talk_calm retarget are later phases.",
        ],
    }
    REPORT.write_text(json.dumps(report, indent=2) + "\n")

    add_lights()
    render("vern_mpfb_fitted_front", (0, -3.4, 0.88), (0, -0.02, 0.88), 2.02)
    render("vern_mpfb_fitted_side", (3.2, 0.0, 0.88), (0, -0.02, 0.88), 2.02)
    render("vern_mpfb_fitted_back", (0, 3.4, 0.88), (0, -0.02, 0.88), 2.02)
    render("vern_mpfb_fitted_portrait", (0.65, -2.6, 1.618), (0, -0.035, 1.508), 0.48)
    render("vern_mpfb_fitted_fabric", (.45, -2.6, 1.318), (0, -.04, 1.198), .62)
    render("vern_mpfb_fitted_shoes", (.7, -2, .32), (0, -.06, .10), .55)
    from vern_mpfb_surfaces import render_swatches
    render_swatches(p, render)
    print("VERN_MPFB_FITTED " + json.dumps({"glb": str(GLB), "height": report["height"],
        "meshes": report["meshes"], "triangles": report["triangles"],
        "skinned_garments": report["skinned_garments"]}))


if __name__ == "__main__":
    main()
