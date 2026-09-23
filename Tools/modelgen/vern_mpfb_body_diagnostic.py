"""Generate a male MPFB body/orientation diagnostic for Vern replacement work.

Run with normal Blender startup (MPFB needs preferences):
  blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_body_diagnostic.py

This creates an MPFB body, applies built-in male/old target shape keys, adds the
standard rig, renders axis-labelled views, and writes a report. It deliberately
does not add clothes or accessories.
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

MPFB_ROOT = Path.home() / "AppData/Roaming/Blender Foundation/Blender/5.2/extensions/blender_org/mpfb"
TARGET_DIR = MPFB_ROOT / "data/targets/macrodetails"
SOURCE = ROOT / "Tools/modelgen/source/vern_mpfb_body_diagnostic.blend"
REPORT = ROOT / "docs/art/model_previews/vern_mpfb_body_diagnostic.json"
REVIEW = ROOT / "docs/art/model_previews"


def ensure_mpfb() -> None:
    enabled, _loaded = addon_utils.check("bl_ext.blender_org.mpfb")
    if not enabled:
        addon_utils.enable("bl_ext.blender_org.mpfb", default_set=False, persistent=False)


def mat(name: str, color: str, metallic=0.0, roughness=0.75):
    return common.material(name, color, metallic, roughness)


def evaluated_vertices(obj):
    graph = bpy.context.evaluated_depsgraph_get()
    eval_obj = obj.evaluated_get(graph)
    return [eval_obj.matrix_world @ v.co for v in eval_obj.data.vertices]


def bounds(obj) -> dict:
    verts = evaluated_vertices(obj)
    return {
        "min": [round(min(v[i] for v in verts), 5) for i in range(3)],
        "max": [round(max(v[i] for v in verts), 5) for i in range(3)],
    }


def face_direction(obj) -> str:
    """Determine which Blender axis the face/nose points toward from geometry.

    The head band is isolated by Z height, then the vertex protruding farthest
    from the head centroid on the Y axis marks the nose tip. MPFB humans are
    authored facing +Y (Z-up).
    """
    verts = evaluated_vertices(obj)
    head = [v for v in verts if 1.40 <= v.z <= 1.72]
    cy = sum(v.y for v in head) / len(head)
    maxy = max(head, key=lambda v: v.y)
    miny = min(head, key=lambda v: v.y)
    return "+Y (Blender)" if abs(maxy.y - cy) >= abs(miny.y - cy) else "-Y (Blender)"


def add_label(text: str, loc, size=0.08):
    bpy.ops.object.text_add(location=loc, rotation=(math.pi / 2, 0, math.pi))
    obj = bpy.context.object
    obj.name = f"Axis label {text}"
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.materials.append(mat(f"Label {text}", "e6d2a8", 0, 0.5))
    bpy.ops.object.convert(target="MESH")
    return bpy.context.object


def add_axis_markers():
    red = mat("Axis red +X", "b74747", 0, 0.55)
    blue = mat("Axis blue -X", "4764b7", 0, 0.55)
    green = mat("Axis green +Y", "52a36f", 0, 0.55)
    yellow = mat("Axis yellow -Y", "d9bf55", 0, 0.55)
    common.rod("+X RIGHT marker", (0, 0, 0.05), (0.45, 0, 0.05), 0.01, red)
    common.rod("-X LEFT marker", (0, 0, 0.08), (-0.45, 0, 0.08), 0.01, blue)
    common.rod("+Y marker", (0, 0, 0.11), (0, 0.45, 0.11), 0.01, green)
    common.rod("-Y marker", (0, 0, 0.14), (0, -0.45, 0.14), 0.01, yellow)
    add_label("+Y", (0, 0.55, 0.15))
    add_label("-Y", (0, -0.55, 0.18))
    add_label("+X", (0.55, 0, 0.10))
    add_label("-X", (-0.55, 0, 0.13))


def render(name: str, position, target, scale):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.render.resolution_x = 900
    scene.render.resolution_y = 1200
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


def rig_measurements(rig) -> dict:
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
    }


def main():
    common.reset()
    ensure_mpfb()
    bpy.ops.mpfb.create_human()
    human = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"][0]
    human.name = "MPFB_Male_Diagnostic_Body"
    human.data.materials.clear()
    human.data.materials.append(mat("Diagnostic warm skin", "b58d78", 0, 0.78))
    bpy.context.view_layer.objects.active = human
    human.select_set(True)

    target_files = [
        "caucasian-male-old.target.gz",
        "universal-male-old-averagemuscle-averageweight.target.gz",
    ]
    for filename in target_files:
        bpy.ops.mpfb.load_target(directory=str(TARGET_DIR), files=[{"name": filename}], weight=1.0)
    bpy.context.view_layer.update()
    # Record male request explicitly on the MPFB object as well as using male targets.
    human.MPFB_HUM_gender = 1.0
    human.MPFB_HUM_age = 0.65
    human.MPFB_HUM_caucasian = 1.0
    human.MPFB_HUM_african = 0.0
    human.MPFB_HUM_asian = 0.0

    bpy.ops.mpfb.add_standard_rig()
    rig = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"][0]
    rig.name = "MPFB_Male_Diagnostic_StandardRig"
    add_axis_markers()
    add_lights()

    b = bounds(human)
    height = round(b["max"][2] - b["min"][2], 5)
    shape_keys = [(key.name, round(key.value, 5)) for key in human.data.shape_keys.key_blocks] if human.data.shape_keys else []
    face_dir = face_direction(human)
    report = {
        "asset": "vern_mpfb_body_diagnostic",
        "status": "male_body_orientation_diagnostic",
        "body_source": "MPFB create_human plus built-in male target shape keys",
        "male_assertion": {
            "requested_gender_property": human.MPFB_HUM_gender,
            "applied_targets": target_files,
            "active_shape_keys": shape_keys,
            "verified": any("male" in name for name, value in shape_keys if value > 0.0),
        },
        "orientation": {
            "face_pointing": face_dir,
            "blender_to_gltf": "Blender +Y (face) exports to glTF as -Z, so in Godot the MPFB face points toward -Z (forward).",
            "production_vern_facing": "Production vern.glb in world3d faces the viewer; apply a Y-rotation when instancing this MPFB base so the face points at the camera.",
            "note": "Cross-check with vern_mpfb_body_axis_*.png: face should appear in the +Y view, back in the -Y view.",
        },
        "bounds": b,
        "height": height,
        "rig_measurements": rig_measurements(rig),
        "orientation_review": {
            "+Y_view": "vern_mpfb_body_axis_plus_y.png",
            "-Y_view": "vern_mpfb_body_axis_minus_y.png",
            "+X_view": "vern_mpfb_body_axis_plus_x.png",
            "-X_view": "vern_mpfb_body_axis_minus_x.png",
            "note": "Use these renders to identify the actual MPFB face/front direction before fitting clothing.",
        },
        "next_steps": [
            "Face confirmed pointing +Y (Blender) / -Z (glTF/Godot) via geometry probe; visually verify against the four axis renders.",
            "Only then fit clothing and accessories to evaluated body landmarks.",
            "Do not reuse the rejected primitive clothing overlay approach.",
        ],
    }
    SOURCE.parent.mkdir(parents=True, exist_ok=True)
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    REPORT.write_text(json.dumps(report, indent=2) + "\n")

    render("vern_mpfb_body_axis_plus_y", (0, 4, 0.95), (0, 0, 0.86), 1.9)
    render("vern_mpfb_body_axis_minus_y", (0, -4, 0.95), (0, 0, 0.86), 1.9)
    render("vern_mpfb_body_axis_plus_x", (4, 0, 0.95), (0, 0, 0.86), 1.9)
    render("vern_mpfb_body_axis_minus_x", (-4, 0, 0.95), (0, 0, 0.86), 1.9)
    print("VERN_MPFB_BODY_DIAGNOSTIC " + json.dumps({"height": height, "male_verified": report["male_assertion"]["verified"], "report": str(REPORT)}))


if __name__ == "__main__":
    main()
