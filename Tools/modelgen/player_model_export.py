"""Keep A-pose source; export a skinned relaxed standing rest with no clips."""
import json
from pathlib import Path
import bpy
from mathutils import Vector, Quaternion
from mathutils.bvhtree import BVHTree
import common
from vern_mpfb_wardrobe import apply
from vern_mpfb_fitted import add_lights, render

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Tools/modelgen/source/player_office_worker.blend'
GLB = ROOT / 'assets/models3d/characters/player/player_office_worker.glb'
REVIEW = ROOT / 'docs/art/model_previews'


def validate_collar(rig, meshes):
    """Measure exported rest-space vertices AND face centers against the neck."""
    body = next(o for o in meshes if o.name == 'PlayerBody')
    surface = BVHTree.FromPolygons([body.matrix_world @ v.co for v in body.data.vertices],
                                  [list(p.vertices) for p in body.data.polygons])
    center = rig.matrix_world @ rig.data.bones['neck01'].head_local
    gaps = []
    for obj in meshes:
        if obj.name not in ('Collar stand', 'Folded shirt collar'):
            continue
        points = [obj.matrix_world @ v.co for v in obj.data.vertices]
        points += [obj.matrix_world @ f.center for f in obj.data.polygons]
        for point in points:
            origin = Vector((center.x, center.y, point.z))
            delta = point-origin
            hit, _, _, _ = surface.ray_cast(origin, delta.normalized(), .4)
            if hit is not None:
                gaps.append(delta.length-(hit-origin).length)
    assert gaps and min(gaps) > .002, ('Exported collar touches neck', min(gaps, default=0))
    return dict(samples=len(gaps), minimum_radial_clearance_m=min(gaps))


def standing(rig):
    # MPFB's neutral A-pose splays the ankles to x=+/-0.196m even though the
    # hip joints are only +/-0.110m apart. Bring them under the hips *before*
    # baking the displayed pose into the exported mesh and skeleton rest.
    # Rotate about each thigh root, then counter-rotate the foot so the shoe
    # and toes remain level instead of rolling onto their edges.
    for side, sign in [('L', 1), ('R', -1)]:
        thigh = rig.pose.bones['upperleg01.'+side]
        swing = Quaternion((0, 1, 0), sign*.071)
        matrix = (swing @ thigh.matrix.to_quaternion()).to_matrix().to_4x4()
        matrix.translation = thigh.matrix.translation
        thigh.matrix = matrix
        bpy.context.view_layer.update()
        foot = rig.pose.bones['foot.'+side]
        counter = Quaternion((0, 1, 0), -sign*.071)
        matrix = (counter @ foot.matrix.to_quaternion()).to_matrix().to_4x4()
        matrix.translation = foot.matrix.translation
        foot.matrix = matrix
        bpy.context.view_layer.update()
    for side, sign in [('L', 1), ('R', -1)]:
        for name, direction in [('upperarm01', (sign*.16, -.025, -1)),
                                ('lowerarm01', (sign*.06, -.12, -1))]:
            pb = rig.pose.bones[name+'.'+side]
            old = pb.matrix.to_3x3() @ Vector((0, 1, 0))
            swing = old.rotation_difference(Vector(direction).normalized())
            matrix = (swing @ pb.matrix.to_quaternion()).to_matrix().to_4x4()
            matrix.translation = pb.matrix.translation
            pb.matrix = matrix
            bpy.context.view_layer.update()


def deliver(rig):
    GLB.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    # Editable neutral source remains intact for future wardrobe and animation work.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    standing(rig)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    # Freeze this display pose into rest coordinates while preserving skin weights.
    for obj in meshes:
        for mod in list(obj.modifiers):
            apply(obj, mod)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in meshes:
        obj.modifiers.new('Armature', 'ARMATURE').object = rig
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=str(GLB), export_format='GLB', use_selection=True,
        export_yup=True, export_cameras=False, export_lights=False,
        export_animations=False, export_skins=True)
    common.reset()
    bpy.ops.import_scene.gltf(filepath=str(GLB))
    rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    assert len(rigs) == 1
    rig = rigs[0]
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and
              any(m.type == 'ARMATURE' for m in o.modifiers)]
    assert meshes and all(all(any(g.weight > 0 for g in v.groups) for v in o.data.vertices) for o in meshes)
    vertices = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    low = [min(v[i] for v in vertices) for i in range(3)]
    high = [max(v[i] for v in vertices) for i in range(3)]
    assert 1.5 < high[2]-low[2] < 2.1 and abs(low[2]) < .04, (low, high)
    ankle_width = abs(rig.data.bones['foot.L'].head_local.x - rig.data.bones['foot.R'].head_local.x)
    assert .25 < ankle_width < .30, ('Exported player stance too wide or too narrow', ankle_width)
    report = dict(asset='player_office_worker', validation='passed', bones=len(rig.data.bones),
                  meshes=len(meshes), triangles=sum(len(f.vertices)-2 for o in meshes for f in o.data.polygons),
                  bounds_min=low, bounds_max=high, front='Godot +Z', animations=[],
                   source_pose='MPFB neutral A-pose', exported_pose='relaxed standing rest',
                   ankle_separation_m=ankle_width)
    report['collar_clearance'] = validate_collar(rig, meshes)
    (REVIEW/'player_office_worker.json').write_text(json.dumps(report, indent=2)+'\n')
    add_lights()
    for name, position in [('front', (0, -4, 1)), ('side', (4, 0, 1)),
                           ('back', (0, 4, 1)), ('three_quarter', (2.6, -4, 2))]:
        render('player_office_worker_'+name, position, (0, 0, .9), 2.05)
    collar_height = rig.data.bones['neck01'].head_local.z
    for name, position in [('front', (0, -3, collar_height+.12)),
                           ('back', (0, 3, collar_height+.15)),
                           ('back_three_quarter', (2, 3, collar_height+.25)),
                           ('side', (3, -.5, collar_height+.15)),
                           ('three_quarter', (2, -3, collar_height+.35))]:
        render('player_collar_'+name, position, (0, 0, collar_height-.02), .48, (640, 640))
    print('PLAYER_VALIDATED '+json.dumps(report))
