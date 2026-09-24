"""Seat the fitted MPFB Vern into the production studio VernStation chair.

Phase 1 of the MPFB-into-production migration. Loads the fitted source blend
(vern_mpfb_fitted.py output), drives the standard rig into a seated resting
pose, bakes a 1-second `seated_rest` action (frames 1 and 25, 24 fps), re-exports
the GLB with animations enabled, renders seated review previews, and appends
`seated` metrics to the report without touching standing-validator fields.

Grounding locked before writing this module:
  - MPFB face is Blender -Y (report front_axis), so knees go toward -Y.
  - VernStation/SeatAnchor local (0, 0.53, -0.005) anchors hip z 0.53.
  - The studio chair has no armrest; Vern hands rest on the runtime tray table
    (surface_y 0.73; production seated wrist z 0.742) -> wrists target z 0.73.
  - FK technique mirrors production (vern_rig.py bind_and_animate /
    vern_animation.py solve_arm): assign pose_bone.matrix + view_layer.update(),
    then keyframe the basis channels at frames 1 and 25 @ 24 fps.

Run with normal Blender startup (not --factory-startup):
  blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_seat.py
"""

from __future__ import annotations

import json
import math
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

from vern_mpfb_fitted import GLB, REPORT, REVIEW, SOURCE, add_lights, render  # noqa: E402

HIP_Z = 0.53        # VernStation/SeatAnchor local (0, 0.53, -0.005)
ANKLE_Z = 0.05      # feet near the floor (bind foot z was ~0.066)
WRIST_Z = 0.73      # runtime tray table surface_y
WRIST_FORWARD = 0.31  # in front of the hip plane (front = Blender -Y)
WRIST_X = 0.28      # horizontal offset; character-left is +X when facing -Y
FRONT = -1.0
FPS = 24

REQUIRED = [
    "root", "pelvis.L", "pelvis.R",
    "upperleg01.L", "upperleg02.L", "lowerleg01.L", "lowerleg02.L", "foot.L",
    "upperleg01.R", "upperleg02.R", "lowerleg01.R", "lowerleg02.R", "foot.R",
    "upperarm01.L", "upperarm02.L", "lowerarm01.L", "lowerarm02.L", "wrist.L",
    "upperarm01.R", "upperarm02.R", "lowerarm01.R", "lowerarm02.R", "wrist.R",
]

ANIMATED = ["root"]
for _s in ("L", "R"):
    ANIMATED += [f"upperleg01.{_s}", f"upperleg02.{_s}", f"lowerleg01.{_s}",
                 f"lowerleg02.{_s}", f"foot.{_s}", f"upperarm01.{_s}", f"upperarm02.{_s}",
                 f"lowerarm01.{_s}", f"lowerarm02.{_s}", f"wrist.{_s}"]


def seg_len(name):
    b = rig.data.bones[name]
    return (b.tail_local - b.head_local).length


def aim(name, target):
    """Rotation-difference swing setting pose directly (production solve_arm idiom)."""
    pb = rig.pose.bones[name]
    head = pb.matrix.translation.copy()
    delta = Vector(target) - head
    if delta.length < 1e-6:
        return head
    old = pb.matrix.to_3x3() @ Vector((0, 1, 0))
    swing = old.rotation_difference(delta.normalized())
    matrix = (swing @ pb.matrix.to_quaternion()).to_matrix().to_4x4()
    matrix.translation = head
    pb.matrix = matrix
    bpy.context.view_layer.update()
    return head


def glb_animations():
    with open(GLB, "rb") as f:
        magic = f.read(4)
        assert magic == b"glTF", "Not a GLB"
        _version, _length = struct.unpack("<II", f.read(8))
        chunk_len, chunk_type = struct.unpack("<II", f.read(8))
        assert chunk_type == 0x4E4F534A, "First chunk is not JSON"
        root = json.loads(f.read(chunk_len).decode("utf-8"))
    return [a.get("name") for a in root.get("animations", [])]


def toe_tip(side):
    """World tail of the deepest toe-named descendant of the foot, or the foot tail."""
    bones = rig.data.bones
    name = f"foot.{side}"
    while True:
        kids = [b.name for b in bones
                if b.parent and b.parent.name == name and b.name.startswith("toe")
                and (b.name.endswith(side) or "." not in b.name)]
        if not kids:
            break
        name = kids[0]
    pb = rig.pose.bones[name]
    b = bones[name]
    return pb.matrix.translation + pb.matrix.to_3x3() @ (b.tail_local - b.head_local)


def main():
    global rig
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.render.fps = FPS
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 25
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()

    armatures = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    assert len(armatures) == 1, f"Expected one armature, found {len(armatures)}"
    rig = armatures[0]
    rig.data.pose_position = "POSE"
    for n in REQUIRED:
        assert n in rig.data.bones, f"Missing bone {n}"
    tops = [b.name for b in rig.data.bones if not b.parent]
    assert tops == ["root"], f"Unexpected top-level bones: {tops}"

    rest = {b.name: b.matrix.copy() for b in rig.pose.bones}
    drop = HIP_Z - rest["upperleg01.L"].translation.z
    root_m = rig.pose.bones["root"].matrix.copy()
    root_m.translation.z += drop
    rig.pose.bones["root"].matrix = root_m
    bpy.context.view_layer.update()
    base = {b.name: b.matrix.copy() for b in rig.pose.bones}

    lthigh_L = seg_len("upperleg01.L") + seg_len("upperleg02.L")
    lthigh_R = seg_len("upperleg01.R") + seg_len("upperleg02.R")
    lshin_L = seg_len("lowerleg01.L") + seg_len("lowerleg02.L")
    lshin_R = seg_len("lowerleg01.R") + seg_len("lowerleg02.R")
    lfoot_L, lfoot_R = seg_len("foot.L"), seg_len("foot.R")
    assert abs(lthigh_L - lthigh_R) < 1e-4 and abs(lshin_L - lshin_R) < 1e-4
    L_THIGH, L_SHIN, L_FOOT = lthigh_L, lshin_L, lfoot_L

    knees = {}
    ankles = {}
    for s in ("L", "R"):
        hip = base[f"upperleg01.{s}"].translation
        h_drop = HIP_Z - (ANKLE_Z + L_SHIN)
        assert h_drop < L_THIGH, "Thigh cannot reach the shin target"
        dy = math.sqrt(max(0.0, L_THIGH * L_THIGH - h_drop * h_drop))
        knee = hip + Vector((0.0, FRONT * dy, -h_drop))
        aim(f"upperleg01.{s}", knee)
        aim(f"upperleg02.{s}", knee)
        ankles[s] = knee + Vector((0.0, 0.0, -L_SHIN))
        aim(f"lowerleg01.{s}", ankles[s])
        aim(f"lowerleg02.{s}", ankles[s])
        foot_dir = Vector((0.0, FRONT, -0.06)).normalized()
        ball = ankles[s] + foot_dir * L_FOOT
        aim(f"foot.{s}", ball)

    elbows = {}
    wrists = {}
    for s in ("L", "R"):
        shoulder = base[f"upperarm01.{s}"].translation
        elbow0 = base[f"lowerarm01.{s}"].translation
        wrist0 = base[f"wrist.{s}"].translation
        a = (elbow0 - shoulder).length
        b = (wrist0 - elbow0).length
        target = Vector((WRIST_X if s == "L" else -WRIST_X, FRONT * WRIST_FORWARD, WRIST_Z))
        delta = target - shoulder
        d = delta.length
        assert abs(a - b) < d < a + b, (f"Unreachable {s} wrist", round(d, 4), round(a + b, 4))
        axis = delta.normalized()
        pole = elbow0 - shoulder
        pole = (pole - axis * pole.dot(axis)).normalized()
        along = (a * a - b * b + d * d) / (2 * d)
        elbow = shoulder + axis * along + pole * math.sqrt(max(0.0, a * a - along * along))
        aim(f"upperarm01.{s}", elbow)
        aim(f"upperarm02.{s}", elbow)
        aim(f"lowerarm01.{s}", target)
        aim(f"lowerarm02.{s}", target)
        fore = rig.pose.bones[f"lowerarm02.{s}"].matrix.to_quaternion().normalized()
        m = fore.to_matrix().to_4x4()
        m.translation = target
        rig.pose.bones[f"wrist.{s}"].matrix = m
        bpy.context.view_layer.update()
        elbows[s] = elbow
        wrists[s] = rig.pose.bones[f"wrist.{s}"].matrix.translation.copy()

    for frame in (1, 25):
        bpy.context.scene.frame_set(frame)
        for name in ANIMATED:
            pose = rig.pose.bones[name]
            pose.rotation_mode = "QUATERNION"
            pose.keyframe_insert("location", frame=frame, group=name)
            pose.keyframe_insert("rotation_quaternion", frame=frame, group=name)
    action = rig.animation_data.action
    action.name = "seated_rest"
    action.use_fake_user = True
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()

    hip_z = rig.pose.bones["upperleg01.L"].matrix.translation.z
    knee_z = rig.pose.bones["lowerleg01.L"].matrix.translation.z
    ankle_z = rig.pose.bones["foot.L"].matrix.translation.z
    wrist_L = rig.pose.bones["wrist.L"].matrix.translation.copy()
    wrist_R = rig.pose.bones["wrist.R"].matrix.translation.copy()
    elbow_L = rig.pose.bones["lowerarm01.L"].matrix.translation.copy()
    elbow_R = rig.pose.bones["lowerarm01.R"].matrix.translation.copy()
    tip_z = toe_tip("L").z
    assert abs(hip_z - HIP_Z) < 0.01, hip_z
    assert abs(knee_z - (ANKLE_Z + L_SHIN)) < 0.02, knee_z
    assert abs(ankle_z - ANKLE_Z) < 0.02, ankle_z
    assert -0.02 <= tip_z <= 0.12, tip_z
    shoulder_z = base["upperarm01.L"].translation.z
    for w, e in ((wrist_L, elbow_L), (wrist_R, elbow_R)):
        assert abs(w.z - WRIST_Z) < 0.02, w
        assert -0.42 < w.y < -0.20, w
        assert shoulder_z - 0.22 < e.z < shoulder_z + 0.02, e

    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=str(GLB), export_format="GLB",
        use_selection=True, export_yup=True, export_cameras=False,
        export_lights=False, export_animations=True, export_animation_mode="ACTIONS",
        export_force_sampling=True, export_rest_position_armature=True,
        export_anim_slide_to_zero=True)
    anims = glb_animations()
    assert "seated_rest" in anims, f"Animation missing from GLB: {anims}"

    report = json.loads(REPORT.read_text(encoding="utf-8"))
    report["seated"] = {
        "action": "seated_rest", "fps": FPS, "frames": [1, 25],
        "root_drop": round(drop, 5),
        "hip_z": round(hip_z, 5), "knee_z": round(knee_z, 5),
        "ankle_z": round(ankle_z, 5), "toe_tip_z": round(tip_z, 5),
        "wrist_L": [round(wrist_L.x, 5), round(wrist_L.y, 5), round(wrist_L.z, 5)],
        "wrist_R": [round(wrist_R.x, 5), round(wrist_R.y, 5), round(wrist_R.z, 5)],
        "elbow_L": [round(elbow_L.x, 5), round(elbow_L.y, 5), round(elbow_L.z, 5)],
        "elbow_R": [round(elbow_R.x, 5), round(elbow_R.y, 5), round(elbow_R.z, 5)],
        "hip_anchor": "VernStation/SeatAnchor local (0,0.53,-0.005)",
        "hand_support": "runtime vern_tray_table surface_y 0.73",
        "facing": "MPFB front Blender -Y (knees toward -Y); Phase 4 yaw swap handles scene orientation",
    }
    REPORT.write_text(json.dumps(report, indent=2) + "\n")

    add_lights()
    render("vern_mpfb_seated_front", (0, -2.9, 0.78), (0, -0.05, 0.78), 1.62)
    render("vern_mpfb_seated_side", (3.1, 0.0, 0.78), (0, -0.05, 0.78), 1.62)
    render("vern_mpfb_seated_threequarter", (1.6, -2.4, 0.82), (0, -0.05, 0.78), 1.62)
    print("VERN_MPFB_SEAT " + json.dumps({
        "glb": str(GLB), "action": "seated_rest", "hip_z": round(hip_z, 5),
        "knee_z": round(knee_z, 5), "ankle_z": round(ankle_z, 5),
        "toe_tip_z": round(tip_z, 5), "wrist_L": [round(v, 5) for v in wrist_L],
        "wrist_R": [round(v, 5) for v in wrist_R],
        "animations_in_glb": anims}))


if __name__ == "__main__":
    main()