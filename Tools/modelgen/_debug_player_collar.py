"""Temporary render diagnostic for the current exported collar."""
import sys
from pathlib import Path
import bpy
sys.path.insert(0, str(Path(__file__).resolve().parent))
from vern_mpfb_fitted import add_lights, render
from player_model_export import GLB
bpy.ops.import_scene.gltf(filepath=str(GLB))
add_lights()
rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
h = rig.data.bones['neck01'].head_local.z
stand = bpy.data.objects['Collar stand']
fold = bpy.data.objects['Folded shirt collar']
for name, hide_stand, hide_fold in [('full', False, False), ('stand', False, True), ('fold', True, False), ('bare', True, True)]:
    stand.hide_render = hide_stand
    fold.hide_render = hide_fold
    render('player_collar_diagnostic_'+name, (3, -.35, h+.17), (0, 0, h-.02), .27, (840, 840))
