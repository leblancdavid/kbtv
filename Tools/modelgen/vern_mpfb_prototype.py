"""Generate an MPFB-derived Vern body prototype for visual approval.

Run with normal Blender startup, not --factory-startup, because MPFB needs its
registered preferences:
  blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_prototype.py

This intentionally writes separate prototype files and does not replace the
production animated Vern asset.
"""

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

import addon_utils
import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))

import common  # noqa: E402

SOURCE = ROOT / "Tools/modelgen/source/vern_mpfb_prototype.blend"
GLB = ROOT / "assets/models3d/characters/vern_mpfb/vern_mpfb_prototype.glb"
REVIEW = ROOT / "docs/art/model_previews"
REPORT = REVIEW / "vern_mpfb_prototype.json"


def mat(name: str, color: str, metallic=0.0, roughness=0.75):
    return common.material(name, color, metallic, roughness)


def ellipsoid(name: str, loc, scale, material, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def cylinder(name: str, loc, radius, depth, material, axis="Z", vertices=32):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else (0, math.pi / 2, 0) if axis == "X" else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def rod(name: str, start, end, radius, material, vertices=16):
    delta = Vector(end) - Vector(start)
    obj = cylinder(name, (Vector(start) + Vector(end)) / 2, radius, delta.length, material, vertices=vertices)
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    return obj


def bounds(objects) -> dict:
    graph = bpy.context.evaluated_depsgraph_get()
    vertices = [obj.matrix_world @ v.co for obj in objects if obj.type == "MESH" for v in obj.evaluated_get(graph).data.vertices]
    return {
        "min": [round(min(v[i] for v in vertices), 5) for i in range(3)],
        "max": [round(max(v[i] for v in vertices), 5) for i in range(3)],
    }


def measure_rig(rig) -> dict:
    def h(name):
        return rig.data.bones[name].head_local.copy()

    shoulder_l = h("upperarm01.L")
    shoulder_r = h("upperarm01.R")
    hip_l = h("upperleg01.L")
    hip_r = h("upperleg01.R")
    return {
        "shoulder_width": round((shoulder_l - shoulder_r).length, 5),
        "hip_width": round((hip_l - hip_r).length, 5),
        "shoulder_to_hip_ratio": round((shoulder_l - shoulder_r).length / max((hip_l - hip_r).length, 0.0001), 5),
        "upper_arm_l": round((h("upperarm01.L") - h("lowerarm01.L")).length, 5),
        "forearm_l": round((h("lowerarm01.L") - h("wrist.L")).length, 5),
        "thigh_l": round((h("upperleg01.L") - h("lowerleg01.L")).length, 5),
        "shin_l": round((h("lowerleg01.L") - h("foot.L")).length, 5),
    }


def ensure_mpfb():
    enabled, _loaded = addon_utils.check("bl_ext.blender_org.mpfb")
    if not enabled:
        addon_utils.enable("bl_ext.blender_org.mpfb", default_set=False, persistent=False)


def create_human():
    ensure_mpfb()
    bpy.ops.mpfb.create_human()
    human = bpy.data.objects["Human"]
    human.name = "Vern_MPFB_Body"
    # Store requested phenotype values even when the default body geometry remains
    # the practical MPFB baseline in this headless flow.
    human.MPFB_HUM_gender = 1.0
    human.MPFB_HUM_age = 0.65
    human.MPFB_HUM_height = 0.62
    human.MPFB_HUM_weight = 0.58
    human.MPFB_HUM_muscle = 0.42
    bpy.context.view_layer.objects.active = human
    human.select_set(True)
    bpy.ops.mpfb.refit_human()
    bpy.ops.mpfb.add_standard_rig()
    rigs = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if not rigs:
        raise RuntimeError("MPFB did not create a standard rig")
    rig = rigs[0]
    rig.name = "Vern_MPFB_StandardRig"
    skin = mat("MPFB Vern warm skin", "b58d78", 0, 0.78)
    human.data.materials.clear()
    human.data.materials.append(skin)
    return human, rig


def add_vern_identity(materials):
    sweater = materials["sweater"]
    pants = materials["pants"]
    black = materials["black"]
    hair = materials["hair"]
    gray = materials["gray"]
    metal = materials["metal"]

    # Clothing is visual prototype geometry fitted over the MPFB base. The body
    # mesh remains underneath so hands/head keep accurate anatomy.
    ellipsoid("Black high-neck sweater torso", (0, -0.03, 1.06), (0.245, 0.155, 0.36), sweater)
    ellipsoid("Sweater lower body", (0, -0.015, 0.82), (0.225, 0.145, 0.18), sweater)
    cylinder("Folded turtleneck collar", (0, -0.035, 1.40), 0.075, 0.115, sweater, vertices=32)
    for side, s in (("L", 1), ("R", -1)):
        rod(f"Sweater upper sleeve {side}", (s * 0.20, -0.03, 1.30), (s * 0.39, -0.02, 1.08), 0.055, sweater)
        rod(f"Sweater forearm sleeve {side}", (s * 0.39, -0.02, 1.08), (s * 0.52, -0.02, 0.88), 0.047, sweater)
        rod(f"Dark trouser thigh {side}", (s * 0.105, -0.005, 0.86), (s * 0.115, 0.005, 0.48), 0.085, pants)
        rod(f"Dark trouser shin {side}", (s * 0.115, 0.005, 0.48), (s * 0.105, 0.03, 0.11), 0.065, pants)
        ellipsoid(f"Simple dark shoe {side}", (s * 0.105, 0.10, 0.045), (0.07, 0.15, 0.04), black, 24, 8)

    # Identity/readability details.
    ellipsoid("Swept dark hair mass", (0, -0.055, 1.595), (0.118, 0.098, 0.075), hair, 32, 12)
    ellipsoid("Front swept hair", (0.035, 0.005, 1.62), (0.085, 0.05, 0.035), hair, 24, 8)
    for s in (-1, 1):
        rod("Gray temple streak", (s * 0.095, -0.015, 1.57), (s * 0.10, -0.025, 1.50), 0.008, gray)
        ellipsoid("Headphone cushion", (s * 0.145, -0.035, 1.535), (0.025, 0.045, 0.06), black, 24, 8)
        ellipsoid("Headphone cup", (s * 0.168, -0.035, 1.535), (0.018, 0.04, 0.055), sweater, 24, 8)
        rod("Aviator lens rim top", (s * 0.027, 0.075, 1.535), (s * 0.085, 0.07, 1.535), 0.0035, metal, 8)
        rod("Aviator lens rim bottom", (s * 0.027, 0.075, 1.505), (s * 0.085, 0.07, 1.505), 0.0035, metal, 8)
        rod("Aviator lens rim side", (s * 0.027, 0.075, 1.535), (s * 0.027, 0.075, 1.505), 0.0035, metal, 8)
        rod("Aviator lens outer side", (s * 0.085, 0.07, 1.535), (s * 0.085, 0.07, 1.505), 0.0035, metal, 8)
        rod("Mustache lobe", (s * 0.005, 0.083, 1.475), (s * 0.055, 0.073, 1.468), 0.006, hair, 8)
    rod("Glasses bridge", (-0.023, 0.077, 1.522), (0.023, 0.077, 1.522), 0.0035, metal, 8)
    rod("Headphone headband", (-0.13, -0.045, 1.60), (0.13, -0.045, 1.60), 0.011, black, 16)


def render(name, position, target, scale, resolution=(640, 640)):
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
        common.aim(light, (0, 0, 0.95))


def main():
    common.reset()
    bpy.context.scene.unit_settings.system = "METRIC"
    materials = {
        "sweater": mat("Prototype charcoal sweater", "292c33", 0, 0.9),
        "pants": mat("Prototype dark trousers", "20232a", 0, 0.86),
        "black": mat("Prototype black rubber", "11131a", 0, 0.75),
        "hair": mat("Prototype black hair", "262521", 0, 0.84),
        "gray": mat("Prototype gray temples", "696964", 0, 0.85),
        "metal": mat("Prototype worn glasses metal", "8d8a79", 0.5, 0.45),
    }
    human, rig = create_human()
    add_vern_identity(materials)
    bpy.context.view_layer.update()

    for path in (SOURCE.parent, GLB.parent, REVIEW):
        path.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=str(GLB), export_format="GLB", use_selection=True,
        export_yup=True, export_cameras=False, export_lights=False, export_animations=False)

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    b = bounds(mesh_objects)
    report = {
        "asset": "vern_mpfb_prototype",
        "status": "prototype_not_runtime_wired",
        "blender_version": bpy.app.version_string,
        "source": str(SOURCE.relative_to(ROOT)),
        "glb": str(GLB.relative_to(ROOT)),
        "bounds": b,
        "height": round(b["max"][2] - b["min"][2], 5),
        "meshes": len(mesh_objects),
        "armatures": len([obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]),
        "triangles": sum(len(obj.data.polygons) for obj in mesh_objects),
        "rig_measurements": measure_rig(rig),
        "notes": [
            "Uses MPFB body and standard rig as anatomical base.",
            "Vern clothing/accessories are visual prototype overlays, not final skinned clothing.",
            "Do not replace production vern.glb until seated pose, clothing skinning, contact anchors, and talk_calm retarget are migrated.",
        ],
    }
    REPORT.write_text(json.dumps(report, indent=2) + "\n")

    add_lights()
    render("vern_mpfb_prototype_front", (0, 4, 1.05), (0, -0.02, 0.90), 1.95)
    render("vern_mpfb_prototype_side", (4, 0.03, 1.10), (0, -0.02, 0.90), 1.95)
    render("vern_mpfb_prototype_portrait", (0.95, 3.0, 1.58), (0, 0.0, 1.50), 0.56)
    print("VERN_MPFB_PROTOTYPE " + json.dumps({"glb": str(GLB), "height": report["height"], "triangles": report["triangles"]}))


if __name__ == "__main__":
    main()
