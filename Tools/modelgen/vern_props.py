"""Separate contact props and reachable tray; callable standalone with Blender."""
import json
import math
import sys
from pathlib import Path
import bpy
from mathutils import Matrix

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import common

ROOT = HERE.parents[1]


def lathe(name, profile, mat, notch=False):
    n = 48
    verts = []
    for r, z in profile:
        for i in range(n):
            a = i * math.tau / n
            dz = -.006 if notch and z > .018 and abs(math.cos(a)) < .14 else 0
            verts.append((r * math.cos(a), r * math.sin(a), z + dz))
    faces = [(j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i)
             for j in range(len(profile)-1) for i in range(n)]
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def build(name):
    p = common.station_palette()
    ceramic = common.material('Worn gray cream ceramic', 'aaa28f', 0, .42)
    if name == 'coffee_mug':
        lathe('Hollow ceramic cup', [(0, 0), (.032, 0), (.039, .009),
              (.045, .098), (.044, .105), (.038, .105), (.034, .018), (0, .018)], ceramic)
        common.cylinder('Recessed coffee', (0, 0, .082), .037, .003,
                        common.material('Dark coffee', '241810', 0, .3), vertices=48)
        bpy.ops.mesh.primitive_torus_add(major_radius=.032, minor_radius=.009,
            major_segments=32, minor_segments=10, location=(.058, 0, .057),
            rotation=(math.pi/2, 0, 0))
        common.finish(bpy.context.object, 'Ceramic handle', ceramic)
    elif name == 'ashtray':
        lathe('Notched ashtray', [(r*.72, z) for r, z in [(0, 0), (.064, 0), (.068, .008), (.067, .027),
              (.052, .027), (.047, .009), (0, .009)]], p['metal'], True)
    elif name == 'cigarette':
        paper = common.material('Off white paper', 'c9c1ae', 0, .9)
        filter_mat = common.material('Cork filter', '9d7951', 0, .9)
        common.cylinder('Filter mouth end', (0, .011, 0), .0045, .022, filter_mat, 'Y')
        common.cylinder('Paper shaft', (0, .052, 0), .004, .06, paper, 'Y')
        common.cylinder('Ash tip', (0, .083, 0), .0042, .004, p['black'], 'Y')
        common.cylinder('Ember', (0, .081, 0), .0043, .002, p['red'], 'Y')
    elif name == 'vern_tray_table':
        common.box('Tray top', (0, .47, .715), (1.12, .28, .03), p['wood'], .012)
        for x in (-.52, .52):
            common.box('Outboard leg', (x, .40, .35), (.035, .045, .70), p['shell'])
            common.box('Floor foot', (x, .40, .016), (.09, .40, .032), p['black'])
    else:
        raise ValueError(name)


def generate():
    reports = {}
    for name in ('coffee_mug', 'ashtray', 'cigarette', 'vern_tray_table'):
        common.reset()
        build(name)
        bpy.ops.object.select_all(action='SELECT')
        bpy.context.view_layer.objects.active = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
        bpy.ops.object.join()
        obj = bpy.context.object
        bpy.context.scene.cursor.location = (0, 0, 0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.name = name
        path = ROOT / 'assets/models3d/props' / (name + '.glb')
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(HERE / 'source' / (name + '.blend')))
        bpy.ops.export_scene.gltf(filepath=str(path), export_format='GLB', use_selection=True)
        common.reset()
        bpy.ops.import_scene.gltf(filepath=str(path))
        objects = list(bpy.context.scene.objects)
        assert objects and all(o.type == 'MESH' for o in objects)
        vertices = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
        for o in objects:
            o.data.calc_loop_triangles()
        reports[name] = dict(bounds_blender={k: [fn(v[i] for v in vertices) for i in range(3)]
            for k, fn in [('min', min), ('max', max)]},
            triangles=sum(len(o.data.loop_triangles) for o in objects), round_trip='passed')
    (ROOT / 'docs/art/model_previews/vern_props.json').write_text(json.dumps(reports, indent=2))


if __name__ == '__main__':
    generate()
