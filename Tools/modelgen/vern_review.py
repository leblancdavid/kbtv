"""Moving round-trip Blender review, front/side contacts and 320x180 feed proxy."""
import json
import subprocess
import tempfile
from pathlib import Path
import bpy
from mathutils import Matrix, Vector, Quaternion
import common
from vern_animation import CONVERT, DURATIONS

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / 'docs/art/model_previews'


def decode(value):
    x, y, z, w = value['rotation_quaternion_xyzw']
    matrix = Quaternion((w, x, y, z)).to_matrix().to_4x4()
    matrix.translation = Vector(value['position'])
    return CONVERT.inverted() @ matrix @ CONVERT


def moving_previews(rig, actions):
    data = json.loads((ROOT / 'assets/models3d/characters/vern/animation_contacts.json').read_text())
    props = {}
    for name, info in data['props'].items():
        before = set(bpy.context.scene.objects)
        bpy.ops.import_scene.gltf(filepath=str(ROOT / info['asset'].removeprefix('res://')))
        props[name] = list(set(bpy.context.scene.objects)-before)
        for obj in props[name]:
            obj.matrix_world = decode(info['rest'])
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.image_settings.file_format = 'PNG'
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    scene.camera = camera
    camera.data.type = 'ORTHO'
    with tempfile.TemporaryDirectory(prefix='vern_review_') as temp:
        directory = Path(temp)
        for clip, action in actions.items():
            if clip == 'seated_rest':
                continue
            rig.animation_data.action = action
            entry = data['actions'][clip]
            for index in range(round(DURATIONS[clip]*12)+1):
                scene.frame_set(int(action.frame_range[0])+index*2)
                for name, info in data['props'].items():
                    value = entry['samples'][index*2] if entry.get('prop') == name else info['rest']
                    for obj in props[name]:
                        obj.matrix_world = decode(value)
                views = [('wide', (2.4, 3.7, 2.1), (0, .18, .79), 2.25, (480, 360)),
                         ('feed', (1.3, 2.7, 1.6), (0, 0, 1.27), 1.12, (320, 180))]
                if index in (0, 13, 29, 35, 42, 55, 66):
                    views += [('front', (0, 4, 1.25), (0, .22, 1), 1.45, (480, 360)),
                              ('side', (4, .02, 1.25), (0, .22, 1), 1.45, (480, 360))]
                for label, position, target, scale, resolution in views:
                    camera.location = position
                    common.aim(camera, target)
                    camera.data.ortho_scale = scale
                    scene.render.resolution_x, scene.render.resolution_y = resolution
                    scene.render.filepath = str(directory / f'{clip}_{label}_{index:03}.png')
                    bpy.ops.render.render(write_still=True)
        subprocess.run(['python', str(Path(__file__).with_name('vern_pack_review.py')),
                        str(directory), str(REVIEW)], check=True)
    bpy.data.objects.remove(camera, do_unlink=True)
