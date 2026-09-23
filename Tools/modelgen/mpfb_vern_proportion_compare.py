"""Create a default MPFB human baseline and compare it to Vern's audit.

Run from the repository root with normal Blender startup (not --factory-startup,
because MPFB needs its registered preferences):
  blender --background --python-exit-code 1 --python Tools/modelgen/mpfb_vern_proportion_compare.py

This does not modify Vern. It creates a temporary MPFB default human, adds MPFB's
standard rig, measures matching landmarks, and writes a comparison report.
"""

from __future__ import annotations

import json
from pathlib import Path

import addon_utils
import bpy

ROOT = Path(__file__).resolve().parents[2]
VERN_AUDIT = ROOT / "docs/art/model_previews/vern_proportion_audit.json"
OUT = ROOT / "docs/art/model_previews/mpfb_vern_proportion_comparison.json"


def _round(value: float) -> float:
    return round(float(value), 5)


def _vec(value) -> list[float]:
    return [_round(value[i]) for i in range(3)]


def _distance(a, b) -> float:
    return _round((a - b).length)


def _bounds(obj) -> dict[str, list[float]]:
    graph = bpy.context.evaluated_depsgraph_get()
    vertices = [obj.matrix_world @ v.co for v in obj.evaluated_get(graph).data.vertices]
    return {
        "min": [_round(min(v[i] for v in vertices)) for i in range(3)],
        "max": [_round(max(v[i] for v in vertices)) for i in range(3)],
    }


def _height(bounds: dict[str, list[float]]) -> float:
    return _round(bounds["max"][2] - bounds["min"][2])


def _bone_head(rig, name: str):
    return rig.data.bones[name].head_local.copy()


def _bone_tail(rig, name: str):
    return rig.data.bones[name].tail_local.copy()


def _side_lengths(rig, side: str) -> dict[str, float]:
    shoulder = _bone_head(rig, f"upperarm01.{side}")
    elbow = _bone_head(rig, f"lowerarm01.{side}")
    wrist = _bone_head(rig, f"wrist.{side}")
    hip = _bone_head(rig, f"upperleg01.{side}")
    knee = _bone_head(rig, f"lowerleg01.{side}")
    ankle = _bone_head(rig, f"foot.{side}")
    return {
        "upper_arm": _distance(shoulder, elbow),
        "forearm": _distance(elbow, wrist),
        "upper_to_forearm_ratio": _round(_distance(shoulder, elbow) / max(_distance(elbow, wrist), 0.0001)),
        "thigh": _distance(hip, knee),
        "shin": _distance(knee, ankle),
        "thigh_to_shin_ratio": _round(_distance(hip, knee) / max(_distance(knee, ankle), 0.0001)),
    }


def _measure_mpfb(human, rig) -> dict:
    bounds = _bounds(human)
    height = _height(bounds)
    shoulder_l = _bone_head(rig, "upperarm01.L")
    shoulder_r = _bone_head(rig, "upperarm01.R")
    hip_l = _bone_head(rig, "upperleg01.L")
    hip_r = _bone_head(rig, "upperleg01.R")
    head_base = _bone_head(rig, "head")
    head_top = _bone_tail(rig, "head")
    shoulder_width = _distance(shoulder_l, shoulder_r)
    hip_width = _distance(hip_l, hip_r)
    head_len = _distance(head_base, head_top)
    props = {
        key: getattr(human, key, None)
        for key in (
            "MPFB_HUM_age",
            "MPFB_HUM_gender",
            "MPFB_HUM_height",
            "MPFB_HUM_weight",
            "MPFB_HUM_muscle",
            "MPFB_HUM_proportions",
            "MPFB_HUM_caucasian",
            "MPFB_HUM_african",
            "MPFB_HUM_asian",
        )
    }
    return {
        "phenotype_values": props,
        "bounds": bounds,
        "height": height,
        "width_x": _round(bounds["max"][0] - bounds["min"][0]),
        "depth_y": _round(bounds["max"][1] - bounds["min"][1]),
        "skeleton": {
            "landmarks": {
                "shoulder_l": _vec(shoulder_l),
                "shoulder_r": _vec(shoulder_r),
                "hip_l": _vec(hip_l),
                "hip_r": _vec(hip_r),
                "head_base": _vec(head_base),
            },
            "shoulder_width_skeleton": shoulder_width,
            "hip_width_skeleton": hip_width,
            "shoulder_to_hip_width_ratio": _round(shoulder_width / max(hip_width, 0.0001)),
            "head_bone_length": head_len,
            "left": _side_lengths(rig, "L"),
            "right": _side_lengths(rig, "R"),
        },
        "ratios": {
            "shoulder_width_to_height": _round(shoulder_width / max(height, 0.0001)),
            "head_bone_to_height": _round(head_len / max(height, 0.0001)),
            "height_in_head_bones": _round(height / max(head_len, 0.0001)),
        },
    }


def _ratio(current: float, reference: float) -> float:
    return _round(current / max(reference, 0.0001))


def _compare(vern: dict, mpfb: dict) -> dict:
    vs = vern["current_vern"]["neutral"]
    vsk = vs["skeleton"]
    msk = mpfb["skeleton"]
    return {
        "height_ratio_vern_to_mpfb": _ratio(vs["height"], mpfb["height"]),
        "shoulder_width_ratio_vern_to_mpfb": _ratio(vsk["shoulder_width_skeleton"], msk["shoulder_width_skeleton"]),
        "hip_width_ratio_vern_to_mpfb": _ratio(vsk["hip_width_skeleton"], msk["hip_width_skeleton"]),
        "head_length_ratio_vern_to_mpfb": _ratio(vsk["head_bone_length"], msk["head_bone_length"]),
        "upper_arm_ratio_vern_to_mpfb": _ratio(vsk["left"]["upper_arm"], msk["left"]["upper_arm"]),
        "forearm_ratio_vern_to_mpfb": _ratio(vsk["left"]["forearm"], msk["left"]["forearm"]),
        "thigh_ratio_vern_to_mpfb": _ratio(vsk["left"]["thigh"], msk["left"]["thigh"]),
        "shin_ratio_vern_to_mpfb": _ratio(vsk["left"]["shin"], msk["left"]["shin"]),
        "interpretation": [
            "Ratios compare Vern's current neutral skeleton to MPFB's default rigged human.",
            "Use ratios as directional guidance only: Vern is stylized and has oversized readable headgear/accessories.",
            "Prefer modest generator edits that preserve Vern's bone names, chair fit, and contact contracts.",
        ],
    }


def main() -> None:
    if not VERN_AUDIT.exists():
        raise FileNotFoundError(f"Run vern_proportion_audit.py first: {VERN_AUDIT}")

    enabled, _loaded = addon_utils.check("bl_ext.blender_org.mpfb")
    if not enabled:
        addon_utils.enable("bl_ext.blender_org.mpfb", default_set=False, persistent=False)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.ops.mpfb.create_human()
    human = bpy.data.objects["Human"]
    bpy.context.view_layer.objects.active = human
    human.select_set(True)
    bpy.ops.mpfb.add_standard_rig()
    rig = bpy.data.objects["Human.rig"]
    bpy.context.view_layer.update()

    vern = json.loads(VERN_AUDIT.read_text())
    mpfb = _measure_mpfb(human, rig)
    report = {
        "schema_version": 1,
        "purpose": "Compare current generated Vern proportions against a default MPFB standard-rig human.",
        "generated_by": "Tools/modelgen/mpfb_vern_proportion_compare.py",
        "blender_version": bpy.app.version_string,
        "mpfb_addon": "bl_ext.blender_org.mpfb",
        "mpfb_baseline": mpfb,
        "vern_source": str(VERN_AUDIT.relative_to(ROOT)),
        "comparison": _compare(vern, mpfb),
    }
    OUT.write_text(json.dumps(report, indent=2) + "\n")
    print("MPFB_VERN_PROPORTION_COMPARE " + json.dumps({"path": str(OUT), **report["comparison"]}))


if __name__ == "__main__":
    main()
