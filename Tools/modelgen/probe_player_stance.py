"""Inspect the editable MPFB player source before changing its stance.

blender --background Tools/modelgen/source/player_office_worker.blend --python Tools/modelgen/probe_player_stance.py
"""
import bpy
from mathutils import Vector

rig = bpy.data.objects['PlayerRig']
print('PLAYER_STANCE_SOURCE')
for name in ('root', 'spine05', 'spine04', 'spine03', 'spine02', 'spine01', 'neck01', 'head'):
    bone = rig.data.bones.get(name)
    if bone is not None:
        print(f'{name:20} head={tuple(round(v, 4) for v in bone.head_local)}')
for side in ('L', 'R'):
    for base in ('pelvis', 'upperleg01', 'upperleg02', 'lowerleg01',
                 'lowerleg02', 'foot', 'toe1-1'):
        bone = rig.data.bones.get(f'{base}.{side}')
        if bone is None:
            continue
        print(f'{bone.name:20} head={tuple(round(v, 4) for v in bone.head_local)} '
              f'tail={tuple(round(v, 4) for v in bone.tail_local)}')
for obj in bpy.data.objects:
    if obj.type == 'MESH' and ('shoe' in obj.name.lower() or 'sole' in obj.name.lower()):
        corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
        print('SHOE', obj.name, 'bounds=',
              tuple(round(min(c[i] for c in corners), 4) for i in range(3)),
              tuple(round(max(c[i] for c in corners), 4) for i in range(3)))
