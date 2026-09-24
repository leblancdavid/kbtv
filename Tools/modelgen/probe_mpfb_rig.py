"""Probe: dump MPFB standard rig rest hierarchy from the fitted blend."""
import json
import sys
from pathlib import Path

import bpy

BLEND = Path(r"D:\Dev\Games\kbtv\Tools\modelgen\source\vern_mpfb_fitted.blend")

bpy.ops.wm.open_mainfile(filepath=str(BLEND))

rig = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
print("RIG:", rig.name)
rows = []
for b in rig.data.bones:
    rows.append({
        "name": b.name,
        "parent": b.parent.name if b.parent else None,
        "head": [round(v, 4) for v in b.head_local],
        "tail": [round(v, 4) for v in b.tail_local],
        "len": round((b.tail_local - b.head_local).length, 4),
    })
rows.sort(key=lambda r: (r["parent"] or "", r["name"]))
out = Path(r"C:\Users\lblan\AppData\Local\Temp\opencode\mpfb_rig_hierarchy.json")
out.write_text(json.dumps(rows, indent=1))
print("WROTE", out, "bones:", len(rows))