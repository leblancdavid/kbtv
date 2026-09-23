"""Measure Vern's current generated proportions before MPFB-informed edits.

Run from the repository root:
  blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern_proportion_audit.py

The report is intentionally read-only: it builds the current procedural Vern in
memory, measures mesh bounds and skeletal landmarks, and writes a JSON baseline.
Use it before changing the generator, then rerun after edits to compare drift.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / "docs/art/model_previews/vern_proportion_audit.json"
sys.path.insert(0, str(HERE))

import common  # noqa: E402
import vern  # noqa: E402


def _round(value: float) -> float:
    return round(float(value), 5)


def _vec(value) -> list[float]:
    return [_round(value[i]) for i in range(3)]


def _bounds(objects) -> dict[str, list[float]]:
    graph = bpy.context.evaluated_depsgraph_get()
    vertices = [obj.matrix_world @ v.co for obj in objects for v in obj.evaluated_get(graph).data.vertices]
    return {
        "min": [_round(min(v[i] for v in vertices)) for i in range(3)],
        "max": [_round(max(v[i] for v in vertices)) for i in range(3)],
    }


def _vertices(objects):
    graph = bpy.context.evaluated_depsgraph_get()
    return [obj.matrix_world @ v.co for obj in objects for v in obj.evaluated_get(graph).data.vertices]


def _slice_widths(objects, bounds: dict[str, list[float]]) -> dict[str, dict[str, float]]:
    vertices = _vertices(objects)
    z_min = bounds["min"][2]
    height = _height(bounds)
    result = {}
    for label, fraction in {
        "knee_height": 0.30,
        "hip_height": 0.50,
        "waist_height": 0.60,
        "chest_height": 0.74,
        "shoulder_height": 0.80,
    }.items():
        z = z_min + height * fraction
        band = [v for v in vertices if abs(v.z - z) <= 0.025]
        if not band:
            result[label] = {"z": _round(z), "width_x": 0.0, "depth_y": 0.0, "samples": 0}
            continue
        result[label] = {
            "z": _round(z),
            "width_x": _round(max(v.x for v in band) - min(v.x for v in band)),
            "depth_y": _round(max(v.y for v in band) - min(v.y for v in band)),
            "samples": len(band),
        }
    return result


def _height(bounds: dict[str, list[float]]) -> float:
    return _round(bounds["max"][2] - bounds["min"][2])


def _width_x(bounds: dict[str, list[float]]) -> float:
    return _round(bounds["max"][0] - bounds["min"][0])


def _depth_y(bounds: dict[str, list[float]]) -> float:
    return _round(bounds["max"][1] - bounds["min"][1])


def _rest_head(rig, bone_name: str):
    return rig.data.bones[bone_name].head_local.copy()


def _rest_tail(rig, bone_name: str):
    return rig.data.bones[bone_name].tail_local.copy()


def _pose_head(rig, bone_name: str):
    return rig.pose.bones[bone_name].matrix.translation.copy()


def _distance(a, b) -> float:
    return _round((a - b).length)


def _side_lengths(rig, side: str, source) -> dict[str, float]:
    shoulder = source(rig, f"upper_arm.{side}")
    elbow = source(rig, f"forearm.{side}")
    wrist = source(rig, f"hand.{side}")
    hip = source(rig, f"thigh.{side}")
    knee = source(rig, f"shin.{side}")
    ankle = source(rig, f"foot.{side}")
    return {
        "upper_arm": _distance(shoulder, elbow),
        "forearm": _distance(elbow, wrist),
        "upper_to_forearm_ratio": _round(_distance(shoulder, elbow) / max(_distance(elbow, wrist), 0.0001)),
        "thigh": _distance(hip, knee),
        "shin": _distance(knee, ankle),
        "thigh_to_shin_ratio": _round(_distance(hip, knee) / max(_distance(knee, ankle), 0.0001)),
    }


def _skeleton_measurements(rig, source) -> dict:
    shoulder_l = source(rig, "upper_arm.L")
    shoulder_r = source(rig, "upper_arm.R")
    hip_l = source(rig, "thigh.L")
    hip_r = source(rig, "thigh.R")
    head_base = source(rig, "head")
    head_top = _rest_tail(rig, "head") if source == _rest_head else _pose_head(rig, "head") + (_rest_tail(rig, "head") - _rest_head(rig, "head"))
    return {
        "landmarks": {
            "shoulder_l": _vec(shoulder_l),
            "shoulder_r": _vec(shoulder_r),
            "hip_l": _vec(hip_l),
            "hip_r": _vec(hip_r),
            "head_base": _vec(head_base),
        },
        "shoulder_width_skeleton": _distance(shoulder_l, shoulder_r),
        "hip_width_skeleton": _distance(hip_l, hip_r),
        "shoulder_to_hip_width_ratio": _round(_distance(shoulder_l, shoulder_r) / max(_distance(hip_l, hip_r), 0.0001)),
        "head_bone_length": _distance(head_base, head_top),
        "left": _side_lengths(rig, "L", source),
        "right": _side_lengths(rig, "R", source),
    }


def _ratio_notes(height: float, skeleton: dict) -> dict[str, float]:
    shoulder = skeleton["shoulder_width_skeleton"]
    head = skeleton["head_bone_length"]
    return {
        "shoulder_width_to_height": _round(shoulder / max(height, 0.0001)),
        "head_bone_to_height": _round(head / max(height, 0.0001)),
        "height_in_head_bones": _round(height / max(head, 0.0001)),
    }


def main() -> None:
    common.reset()
    rig, objects = vern.build()
    bpy.context.view_layer.update()

    seated_bounds = _bounds(objects)
    seated_skeleton = _skeleton_measurements(rig, _pose_head)

    rig.data.pose_position = "REST"
    bpy.context.view_layer.update()
    neutral_bounds = _bounds(objects)
    neutral_skeleton = _skeleton_measurements(rig, _rest_head)

    report = {
        "schema_version": 1,
        "purpose": "Current Vern proportion baseline before MPFB-informed mesh edits.",
        "generated_by": "Tools/modelgen/vern_proportion_audit.py",
        "blender_version": bpy.app.version_string,
        "mpfb_reference": {
            "status": "captured_in_separate_report",
            "report": "docs/art/model_previews/mpfb_vern_proportion_comparison.json",
            "note": "This file is Vern's current-model audit. MPFB comparison is generated separately with mpfb_vern_proportion_compare.py, using normal Blender startup so the MPFB add-on can load preferences.",
        },
        "current_vern": {
            "neutral": {
                "bounds": neutral_bounds,
                "height": _height(neutral_bounds),
                "width_x": _width_x(neutral_bounds),
                "depth_y": _depth_y(neutral_bounds),
                "mesh_slice_widths": _slice_widths(objects, neutral_bounds),
                "skeleton": neutral_skeleton,
                "ratios": _ratio_notes(_height(neutral_bounds), neutral_skeleton),
            },
            "seated": {
                "bounds": seated_bounds,
                "height": _height(seated_bounds),
                "width_x": _width_x(seated_bounds),
                "depth_y": _depth_y(seated_bounds),
                "mesh_slice_widths": _slice_widths(objects, seated_bounds),
                "skeleton": seated_skeleton,
                "ratios": _ratio_notes(_height(seated_bounds), seated_skeleton),
            },
        },
        "initial_findings": [
            "Use this as the committed pre-edit baseline; do not judge MPFB fit until a real MPFB baseline is captured.",
            "Current runtime contract depends on stable bone names, seated_rest, idle_breathing, smoking, drink_coffee, and injected talk_calm.",
            "Any mesh/proportion edit must rerun vern.py, vern_godot_validate.py, VernAnimationControllerTests, and VernCharacterIntegrationTests.",
        ],
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(report, indent=2) + "\n")
    print("VERN_PROPORTION_AUDIT " + json.dumps({"path": str(OUT), "neutral_height": report["current_vern"]["neutral"]["height"], "seated_height": report["current_vern"]["seated"]["height"]}))


if __name__ == "__main__":
    main()
