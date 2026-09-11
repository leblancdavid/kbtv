"""Run: blender --background --python Tools/modelgen/generate.py"""
import json
import argparse
import importlib
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import common

CONTROL_PROPS = ('control_desk', 'phone_board', 'soundboard', 'crt_computer',
                 'office_chair', 'monitor_speaker', 'storage_shelf', 'on_air_sign')
ASSETS = ('audio_cabinet', 'microphone_stand') + CONTROL_PROPS


def stats():
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    corners = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    low = [min(v[i] for v in corners) for i in range(3)]
    high = [max(v[i] for v in corners) for i in range(3)]
    triangles = 0
    for obj in meshes:
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
        assert all(p.area > 1e-12 for p in obj.data.polygons), obj.name
    return dict(meshes=len(meshes), triangles=triangles,
                materials=len({m.name for o in meshes for m in o.data.materials}),
                bounds_min=low, bounds_max=high,
                dimensions=[high[i] - low[i] for i in range(3)])


def preview(path):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 32
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.world.color = (0.22, 0.22, 0.22)
    bounds = stats()
    center = Vector([(a + b) / 2 for a, b in zip(bounds['bounds_min'], bounds['bounds_max'])])
    span = max(bounds['dimensions'])
    bpy.ops.object.camera_add(location=center + Vector((1.65, 2.5, 1.65)) * span)
    camera = bpy.context.object
    common.aim(camera, center)
    camera.data.type = 'ORTHO'
    corners = [o.matrix_world @ Vector(v) for o in scene.objects if o.type == 'MESH' for v in o.bound_box]
    view_rotation = camera.rotation_euler.to_matrix().transposed()
    projected = [view_rotation @ (v - center) for v in corners]
    camera.data.ortho_scale = max(max(v[i] for v in projected) - min(v[i] for v in projected)
                                 for i in (0, 1)) * 1.2
    scene.camera = camera
    for position, energy, size in [((1, 2, 3), 350, 2), ((-2, 1, 1.5), 200, 2), ((0, -2, 2), 400, 1.5)]:
        bpy.ops.object.light_add(type='AREA', location=center + Vector(position) * span)
        light = bpy.context.object
        light.data.energy = energy * span * span
        light.data.shape = 'DISK'
        light.data.size = size * span
        common.aim(light, center)
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def generate(name, builder):
    common.reset()
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    builder.build()
    # Collapse static components into one object; materials remain separate surfaces.
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(mesh, dist=0.000001, edges=list(mesh.edges))
    bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.update()
    bpy.context.view_layer.update()
    source_stats = stats()
    model_dir = ROOT / 'assets/models3d/props'
    source_dir = HERE / 'source'
    review_dir = ROOT / 'docs/art/model_previews'
    for directory in (model_dir, source_dir, review_dir):
        directory.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(source_dir / (name + '.blend')))
    glb_path = model_dir / (name + '.glb')
    bpy.ops.export_scene.gltf(filepath=str(glb_path), export_format='GLB',
                             use_selection=True, export_yup=True,
                             export_cameras=False, export_lights=False)
    # Check actual exported data by loading it into a clean scene.
    common.reset()
    bpy.ops.import_scene.gltf(filepath=str(glb_path))
    bpy.context.view_layer.update()
    imported = stats()
    assert imported['triangles'] == source_stats['triangles']
    assert all(abs(a - b) < 0.0001 for a, b in zip(source_stats['dimensions'], imported['dimensions']))
    assert abs(imported['bounds_min'][2]) < 0.0001, 'Origin must sit at base'
    assert all(o.type in ('MESH', 'EMPTY') for o in bpy.context.scene.objects)
    report = dict(asset=name, validation='passed', blender_version=bpy.app.version_string,
                  authoring_axes='Z up, +Y front; GLB Y up, -Z front',
                  file_bytes=glb_path.stat().st_size, reimported_blender_stats=imported)
    (review_dir / (name + '.json')).write_text(json.dumps(report, indent=2) + '\n')
    preview(review_dir / (name + '.png'))
    print('VALIDATED ' + json.dumps(report))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Generate selected reproducible station props.')
    parser.add_argument('--assets', nargs='+', choices=ASSETS)
    parser.add_argument('--control-room', action='store_true', help='Only new control-room props; reuse accepted assets.')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    for asset_name in args.assets or (CONTROL_PROPS if args.control_room else ASSETS):
        generate(asset_name, importlib.import_module(asset_name))
