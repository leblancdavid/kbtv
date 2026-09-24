"""Blender round-trip regression checks for the fitted MPFB GLB.

Run: blender --background --python-exit-code 1 --python Tools/modelgen/validate_mpfb_fitted.py
Structural checks supplement, never replace, the four rendered views.
"""
import json
import math
import sys
import struct
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from vern_mpfb_body_diagnostic import face_direction
report = json.loads((ROOT/'docs/art/model_previews/vern_mpfb_fitted.json').read_text())
assert report['body_verts_before_clothing_occlusion'] == 13380, 'Helper geometry survived cleanup'
assert report['front_axis'] == {'blender': '-Y', 'godot': '+Z'}
assert report['default_macro_targets_disabled']
glb = (ROOT/report['glb']).read_bytes()
chunk_size, chunk_type = struct.unpack_from('<II', glb, 12)
assert chunk_type == 0x4e4f534a
gltf = json.loads(glb[20:20+chunk_size])
textured_names = {'Vern sweater fabric', 'Vern rib fabric', 'Vern pants fabric', 'Vern swept hair'}
textured = [m for m in gltf['materials'] if m['name'] in textured_names]
assert len(textured) == 4
for material in textured:
    for info in [material['pbrMetallicRoughness']['baseColorTexture'],
                 material['pbrMetallicRoughness']['metallicRoughnessTexture'], material['normalTexture']]:
        texture = gltf['textures'][info['index']]
        assert 'bufferView' in gltf['images'][texture['source']], 'Texture is not embedded'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/report['glb']))
rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
assert len(rigs) == 1
rig = rigs[0]
widgets = {b.custom_shape for b in rig.pose.bones if b.custom_shape}
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in widgets]
assert len(meshes) == report['meshes']
assert rig.data.bones['eye.L'].head_local.y < rig.data.bones['head'].head_local.y
assert face_direction(bpy.data.objects['Vern_MPFB_Body']) == '-Y (Blender)'
skin = bpy.data.objects['Vern_MPFB_Body']
neck_z = rig.data.bones['neck01'].head_local.z
hand_min_x = abs(rig.data.bones['wrist.L'].head_local.x)-.10
assert not any(v.co.z < neck_z-.04 and abs(v.co.x) < hand_min_x
               for v in skin.data.vertices), 'Covered torso/leg skin survived occlusion cleanup'
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
assert 1.70 <= height <= 1.72, height
from vern_mpfb_proportions import measurements
legs = measurements(rig)
assert .44 < (legs['thigh']+legs['shin'])/height < .46, legs
for key, value in legs.items():
    assert abs(value-report['proportions']['after'][key]) < .001, key
locks = [o for o in meshes if o.name.startswith('Swept hair lock ')]
assert len(locks) >= 14
for side in ('L', 'R'):
    shoe = bpy.data.objects['Leather loafer '+side]
    points = [shoe.matrix_world @ v.co for v in shoe.data.vertices]
    assert .24 < max(v.y for v in points)-min(v.y for v in points) < .28
    assert max(v.z for v in points)-min(v.z for v in points) < .085
# Neutral evaluated geometry must agree with the exported bind mesh after tailoring.
depsgraph = bpy.context.evaluated_depsgraph_get()
for obj in meshes:
    evaluated = obj.evaluated_get(depsgraph)
    geometry = evaluated.to_mesh()
    assert max((a.co-b.co).length for a, b in zip(obj.data.vertices, geometry.vertices)) < .001, obj.name
    evaluated.to_mesh_clear()
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
head.rotation_euler.z = 0
bpy.context.view_layer.update()
from vern_mpfb_fitted import add_lights, render
add_lights()
render('vern_mpfb_fitted_export_portrait', (.65, -2.6, 1.618), (0, -.035, 1.508), .48)
render('vern_mpfb_fitted_export_front', (0,-3.4,.88), (0,-.02,.88), 2.02)
result = {'status': 'passed', 'meshes': len(meshes), 'bones': len(rig.data.bones),
          'height': round(height, 5), 'normalized_weights': True, 'rigid_head_motion': True,
          'leg_measurements': legs, 'swept_locks': len(locks), 'neutral_bind_match': True,
          'embedded_pbr_materials': len(textured), 'export_portrait_rendered': True,
          'note': 'Blender round-trip; Godot runtime and seated deformation not tested.'}
(ROOT/'docs/art/model_previews/vern_mpfb_fitted_validation.json').write_text(json.dumps(result, indent=2)+'\n')
print('MPFB_FITTED_VALIDATION', json.dumps(result))
