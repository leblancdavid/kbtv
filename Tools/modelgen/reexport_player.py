"""Export the approved editable player source after changing the displayed pose.

blender --background Tools/modelgen/source/player_office_worker.blend --python-exit-code 1 --python Tools/modelgen/reexport_player.py
"""
import bpy
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from player_model_export import SOURCE, deliver

assert Path(bpy.data.filepath).resolve() == SOURCE.resolve(), bpy.data.filepath
deliver(bpy.data.objects['PlayerRig'])
