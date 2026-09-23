"""Generate a male MPFB Vern with fitted, skinned clothing.

Run with normal Blender startup (MPFB needs registered preferences), not
--factory-startup:
  blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_fitted.py

Phase 2 of the MPFB Vern migration. Unlike the rejected prototype, garments are
sized from the evaluated male body's cross-sections and bone landmarks, then
skinned by copying each garment vertex's weights from its nearest body vertex, so
clothing deforms with the MPFB standard rig. Writes prototype files only; does
not touch the production vern.glb.
"""

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

import addon_utils
import bpy
import mathutils
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))

import common  # noqa: E402
from vern_mesh import loft, tube, ellipsoid  # noqa: E402

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


def body_world():
    """World-space coords (male shape baked) for body-surface verts only."""
    group = body.vertex_groups["body"]
    idx = [i for i in range(len(body.data.vertices)) if group_weight(body, group, i) > 0.0]
    return [body.matrix_world @ body.data.vertices[i].co for i in idx], idx


def ring(z, ease, cap, band=0.035):
    """Ellipse fitting a horizontal torso slice at height z.

    Returns a loft ring (x, y, z, rx, ry): y is shifted so the ellipse covers
    the real front/back build; rx/ry are half-widths plus garment ease.
    cap limits |x| to keep arm vertices out of the measurement.
    """
    verts, _ = body_world()
    band_verts = [v for v in verts if abs(v[2] - z) <= band and abs(v[0]) < cap]
    if not band_verts:
        return (0.0, 0.0, z, 0.08, 0.08)
    cx = sum(v[0] for v in band_verts) / len(band_verts)
    cy = sum(v[1] for v in band_verts) / len(band_verts)
    front = max(v[1] for v in band_verts) - cy
    back = cy - min(v[1] for v in band_verts)
    right = max(v[0] for v in band_verts) - cx
    left = cx - min(v[0] for v in band_verts)
    return (cx, cy + (front - back) / 2, z, max(right, left) + ease, (front + back) / 2 + ease)


def limb_radius(points, t, ease, window, cap):
    """Largest radius around the limb axis at parametric t, plus ease."""
    idx_a = min(int(t * (len(points) - 1)), len(points) - 2)
    a, b = Vector(points[idx_a]), Vector(points[idx_a + 1])
    seg = b - a
    d = seg.normalized()
    frac = t * (len(points) - 1) - idx_a
    sample = a + d * frac * seg.length
    verts, _ = body_world()
    best = 0.0
    for v in verts:
        to_v = v - sample
        along = to_v.dot(d)
        if abs(along) > window / 2 or to_v.length > cap:
            continue
        radial = to_v.length_squared - along * along
        if radial > best * best:
            best = math.sqrt(radial)
    return best + ease


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
    bm.free()
    for g in list(body.vertex_groups):
        if not any(group_weight(body, g, i) > 0.0 for i in range(len(body.data.vertices))):
            body.vertex_groups.remove(g)


def skin_garment(garment):
    """Parent to the rig, add an Armature modifier, copy weights from the
    nearest body vertex so the garment deforms exactly like the skin."""
    garment.parent = rig
    mod = garment.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    for v in garment.data.vertices:
        co = garment.matrix_world @ v.co
        _found, idx, _dist = body_kd.find(co)
        for g in body.vertex_groups:
            weight = group_weight(body, g, idx)
            if weight <= 0.0:
                continue
            group = garment.vertex_groups.get(g.name) or garment.vertex_groups.new(name=g.name)
            group.add([v.index], weight, "REPLACE")


def render(name, position, target, scale, resolution=(640, 720)):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.world.color = (0.18, 0.18, 0.18)
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
    global body, rig, body_kd
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
    bpy.ops.mpfb.add_standard_rig()
    rig = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
    rig.name = "Vern_MPFB_StandardRig"
    bake_male_shape()

    verts, idx = body_world()
    body_kd = mathutils.kdtree.KDTree(len(verts))
    for i, v in enumerate(verts):
        body_kd.insert(v, idx[i])
    body_kd.balance()

    shoulder_l, shoulder_r = bone("upperarm01.L"), bone("upperarm01.R")
    hip_l, hip_r = bone("upperleg01.L"), bone("upperleg01.R")

    rings = []
    caps = {1.095: 0.13, 1.03: 0.19, 0.975: 0.13, 0.915: 0.14, 0.855: 0.15, 0.795: 0.15, 0.735: 0.15, 0.675: 0.15}
    for z in (1.095, 1.03, 0.975, 0.915, 0.855, 0.795, 0.735, 0.675):
        rings.append(ring(z, 0.015, caps[z]))
    loft("Sweater fitted torso", rings, p["sweater"], lambda i: {}, sides=20)

    turtleneck = [ring(z, ease, 0.085, band=0.02) for z, ease in ((1.115, 0.006), (1.135, 0.008), (1.155, 0.006))]
    loft("Folded turtleneck collar", turtleneck, p["rib"], lambda i: {}, sides=20)

    hem = ring(0.675, 0.010, 0.15)
    loft("Sweater lower ribbing", [
        (hem[0], hem[1], 0.665, hem[3], hem[4]),
        (hem[0], hem[1], 0.682, hem[3], hem[4]),
    ], p["rib"], lambda i: {}, sides=20)

    for side, s in [("L", 1), ("R", -1)]:
        arm_pts = [bone(f"upperarm0{i}.{side}") for i in (1, 2)]
        arm_pts += [bone(f"lowerarm0{i}.{side}") for i in (1, 2)]
        arm_pts.append(bone(f"wrist.{side}"))
        radii = [limb_radius(arm_pts, k / (len(arm_pts) - 1), 0.010, 0.05, 0.12)
                 for k in range(len(arm_pts))]
        tube(f"Sweater fitted sleeve {side}", arm_pts, radii, p["sweater"], lambda i: {}, sides=12)
        cuff_mid = (Vector(arm_pts[-2]) + Vector(arm_pts[-1])) / 2
        cuff_r = limb_radius(arm_pts, 1.0, 0.010, 0.04, 0.12)
        loft(f"Knitted wrist cuff {side}", [
            (cuff_mid[0], cuff_mid[1], cuff_mid[2] - 0.012, cuff_r, cuff_r * 0.95),
            (cuff_mid[0], cuff_mid[1], cuff_mid[2], cuff_r, cuff_r * 0.95),
            (cuff_mid[0], cuff_mid[1], cuff_mid[2] + 0.012, cuff_r, cuff_r * 0.95),
        ], p["rib"], lambda i: {}, sides=12)

    for side, s in [("L", 1), ("R", -1)]:
        leg_pts = [bone(f"upperleg0{i}.{side}") for i in (1, 2)]
        leg_pts += [bone(f"lowerleg0{i}.{side}") for i in (1, 2)]
        leg_pts.append(bone(f"foot.{side}"))
        leg_radii = [limb_radius(leg_pts, k / (len(leg_pts) - 1), 0.012, 0.06, 0.14)
                     for k in range(len(leg_pts))]
        tube(f"Fitted trousers {side}", leg_pts, leg_radii, p["pants"], lambda i: {}, sides=12)
        foot = bone(f"foot.{side}")
        ellipsoid(f"Leather shoe {side}", foot + Vector((s * 0.02, 0.10, -0.01)),
                  (0.065, 0.135, 0.05), p["black"], lambda i: {}, segments=16, rings=6)

    hair_bone = bone("head")
    ellipsoid("Swept dark hair mass", hair_bone + Vector((0, -0.04, 0.075)),
              (0.115, 0.095, 0.065), p["hair"], lambda i: {}, segments=16, rings=8)
    ellipsoid("Front swept hair", hair_bone + Vector((0.03, 0.012, 0.10)),
              (0.085, 0.045, 0.03), p["hair"], lambda i: {}, segments=16, rings=6)
    for s in (-1, 1):
        temple = hair_bone + Vector((s * 0.10, -0.02, 0.045))
        common.rod(f"Gray temple streak {s}", temple + Vector((0, 0, 0.03)),
                   temple - Vector((0, 0, 0.03)), 0.008, p["gray"])
        cup = hair_bone + Vector((s * 0.145, -0.03, 0.01))
        ellipsoid(f"Headphone cushion {s}", cup, (0.022, 0.04, 0.055), p["black"],
                  lambda i: {}, segments=12, rings=6)
        ellipsoid(f"Headphone cup {s}", cup + Vector((s * 0.022, 0.0, 0.0)),
                  (0.015, 0.036, 0.05), p["sweater"], lambda i: {}, segments=12, rings=6)
        for dz in (0.012, -0.012):
            common.rod(f"Aviator rim {s} {dz}",
                       hair_bone + Vector((s * 0.03, 0.085, 0.012 + dz)),
                       hair_bone + Vector((s * 0.09, 0.082, 0.012 + dz)), 0.0035, p["metal"])
        common.rod(f"Aviator rim side {s}",
                   hair_bone + Vector((s * 0.03, 0.085, 0.024)),
                   hair_bone + Vector((s * 0.03, 0.085, 0.0)), 0.0035, p["metal"])
        common.rod(f"Mustache lobe {s}",
                   hair_bone + Vector((s * 0.005, 0.088, -0.048)),
                   hair_bone + Vector((s * 0.05, 0.078, -0.054)), 0.006, p["hair"])
    common.rod("Glasses bridge", hair_bone + Vector((-0.025, 0.088, 0.0)),
               hair_bone + Vector((0.025, 0.088, 0.0)), 0.0035, p["metal"])
    common.rod("Headphone band", hair_bone + Vector((-0.115, -0.035, 0.075)),
               hair_bone + Vector((0.115, -0.035, 0.075)), 0.011, p["black"])

    garments = [o for o in bpy.context.scene.objects if o.type == "MESH" and o is not body]
    for garment in garments:
        skin_garment(garment)

    clean_body()
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
        "bounds": {"min": [round(x, 5) for x in minv], "max": [round(x, 5) for x in maxv]},
        "height": round(maxv[2] - minv[2], 5),
        "meshes": len(mesh_objects),
        "armatures": len([o for o in bpy.context.scene.objects if o.type == "ARMATURE"]),
        "triangles": sum(len(o.data.polygons) for o in mesh_objects),
        "body_verts": len(body.data.vertices),
        "skinned_garments": len(garments),
        "rig_measurements": {
            "shoulder_width": round((shoulder_l - shoulder_r).length, 5),
            "hip_width": round((hip_l - hip_r).length, 5),
        },
        "notes": [
            "Garments measured from evaluated male body cross-sections and bone landmarks.",
            "Every garment vertex inherits weights from its nearest body vertex (deforms with skin).",
            "Body export baked male shape keys and dropped MASK/helper geometry.",
            "Still prototype: not runtime-wired; seated pose, contact anchors and talk_calm retarget are later phases.",
        ],
    }
    REPORT.write_text(json.dumps(report, indent=2) + "\n")

    add_lights()
    render("vern_mpfb_fitted_front", (0, 3.4, 1.0), (0, -0.02, 0.85), 1.45)
    render("vern_mpfb_fitted_side", (3.2, 0.0, 1.0), (0, -0.02, 0.85), 1.45)
    render("vern_mpfb_fitted_back", (0, -3.4, 1.0), (0, -0.02, 0.85), 1.45)
    render("vern_mpfb_fitted_portrait", (0.8, 2.6, 1.62), (0, 0.0, 1.50), 0.5)
    print("VERN_MPFB_FITTED " + json.dumps({"glb": str(GLB), "height": report["height"],
        "meshes": report["meshes"], "triangles": report["triangles"],
        "skinned_garments": report["skinned_garments"]}))


if __name__ == "__main__":
    main()