"""Blender round-trip regression checks for the fitted MPFB GLB.

Run: blender --background --python-exit-code 1 --python Tools/modelgen/validate_mpfb_fitted.py
Structural checks supplement, never replace, the four rendered views.
"""
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
report = json.loads((ROOT/'docs/art/model_previews/vern_mpfb_fitted.json').read_text())
assert report['body_verts_before_clothing_occlusion'] == 13380, 'Helper geometry survived cleanup'
assert report['front_axis'] == {'blender': '-Y', 'godot': '+Z'}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/report['glb']))
rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
assert len(rigs) == 1
rig = rigs[0]
widgets = {b.custom_shape for b in rig.pose.bones if b.custom_shape}
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in widgets]
assert len(meshes) == report['meshes']
assert rig.data.bones['eye.L'].head_local.y < rig.data.bones['head'].head_local.y
for obj in meshes:
    assert any(m.type == 'ARMATURE' and m.object == rig for m in obj.modifiers), obj.name
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), obj.name
    assert all(abs(sum(g.weight for g in v.groups)-1) < .002 for v in obj.data.vertices), obj.name
for name in ['Continuous wool pullover', 'Continuous tailored trousers', 'Folded high collar']:
    obj = bpy.data.objects[name]
    assert len(obj.data.polygons) > 20, name
for name in ['Aviator frame 1', 'Aviator frame -1', 'Arched headphone headband']:
    obj = bpy.data.objects[name]
    assert all(obj.vertex_groups[g.group].name == 'head' for v in obj.data.vertices for g in v.groups)
    if 'frame' in name:
        assert max((obj.matrix_world @ v.co).y for v in obj.data.vertices) < -.13
verts = [obj.matrix_world @ v.co for obj in meshes for v in obj.data.vertices]
height = max(v.z for v in verts)-min(v.z for v in verts)
assert abs(height-report['height']) < .001, (height, report['height'])
# Exercise a head rotation: rigid accessories must move without changing size.
frame = bpy.data.objects['Aviator frame 1']
def evaluated_points(obj):
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    result = [obj.matrix_world @ v.co for v in mesh.vertices]
    evaluated.to_mesh_clear()
    return result
before = evaluated_points(frame)
head = rig.pose.bones['head']
head.rotation_mode = 'XYZ'
head.rotation_euler.z = .25
bpy.context.view_layer.update()
after = evaluated_points(frame)
assert max((a-b).length for a, b in zip(before, after)) > .005
assert abs((before[0]-before[-1]).length-(after[0]-after[-1]).length) < .00001
result = {'status': 'passed', 'meshes': len(meshes), 'bones': len(rig.data.bones),
          'height': round(height, 5), 'normalized_weights': True, 'rigid_head_motion': True,
          'note': 'Blender round-trip; Godot runtime and seated deformation not tested.'}
(ROOT/'docs/art/model_previews/vern_mpfb_fitted_validation.json').write_text(json.dumps(result, indent=2)+'\n')
print('MPFB_FITTED_VALIDATION', json.dumps(result))
