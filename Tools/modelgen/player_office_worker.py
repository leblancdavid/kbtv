"""Build player with normal Blender startup (MPFB enabled); no motion clips."""
import sys
from pathlib import Path
import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import common
import vern_mpfb_fitted as base
from player_wardrobe import wardrobe
from player_model_export import deliver


def build():
    common.reset()
    bpy.context.scene.unit_settings.system = 'METRIC'
    base.ensure_mpfb()
    bpy.ops.mpfb.create_human()
    body = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
    body.name = 'PlayerBody'
    targets = ['caucasian-male-young', 'universal-male-young-averagemuscle-averageweight']
    for target in targets:
        bpy.ops.mpfb.load_target(directory=str(base.TARGET_DIR),
                                files=[{'name': target+'.target.gz'}], weight=1)
    for key in body.data.shape_keys.key_blocks:
        if key != body.data.shape_keys.reference_key:
            key.value = float(key.name in targets)
    assert set(targets).issubset(body.data.shape_keys.key_blocks.keys())
    bpy.context.view_layer.update()
    bpy.ops.mpfb.add_standard_rig()
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    rig.name = 'PlayerRig'
    base.body, base.rig = body, rig
    base.bake_male_shape()
    base.clean_body()
    body.data.materials.clear()
    body.data.materials.append(common.material('Player skin', 'bd967f', roughness=.78))
    wardrobe(body, rig)
    return rig


if __name__ == '__main__':
    deliver(build())
