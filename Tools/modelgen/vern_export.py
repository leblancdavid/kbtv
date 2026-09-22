"""Rig-aware export, clean round-trip checks and review renders for Vern."""
import json
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector
import common
from vern_animation import build_actions, DURATIONS, FPS

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / 'docs/art/model_previews'
GLB = ROOT / 'assets/models3d/characters/vern/vern.glb'
SOURCE = ROOT / 'Tools/modelgen/source/vern.blend'


def bounds(objects):
    graph = bpy.context.evaluated_depsgraph_get()
    vertices = [obj.matrix_world @ v.co for obj in objects
                for v in obj.evaluated_get(graph).data.vertices]
    return {'min': [min(v[i] for v in vertices) for i in range(3)],
            'max': [max(v[i] for v in vertices) for i in range(3)]}


def render(name, position, target, scale, resolution=(640, 640)):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.world.color = (.18, .18, .18)
    scene.view_settings.view_transform = 'AgX'
    bpy.ops.object.camera_add(location=position)
    camera = bpy.context.object
    common.aim(camera, target)
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = scale
    scene.camera = camera
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(REVIEW / (name + '.png'))
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def previews():
    import office_chair
    for position, power, size in [((-2, 3, 4), 350, 3), ((2, 2, 2), 170, 2), ((0, -2, 3), 280, 2)]:
        bpy.ops.object.light_add(type='AREA', location=position)
        light = bpy.context.object
        light.data.energy, light.data.size = power, size
        common.aim(light, (0, 0, .9))
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    render('vern_bind_pose', (1, 4, 1.7), (0, 0, .95), 2.1)
    rig.data.pose_position = 'POSE'
    bpy.context.view_layer.update()
    # Chair is review context only and is added AFTER character export.
    office_chair.build()
    render('vern_seated', (2.4, 3.7, 2.1), (0, .1, .78), 1.9)
    render('vern_front', (0, 4, 1.1), (0, .08, .79), 1.8)
    render('vern_side', (4, .02, 1.25), (0, .18, .78), 1.85)
    render('vern_portrait', (1.05, 3.2, 1.68), (0, -.015, 1.31), .61)
    render('vern_feed_preview', (1.3, 2.7, 1.6), (0, 0, 1.27), 1.12, (320, 180))


def deliver(builder, preview_only=False):
    for directory in (GLB.parent, SOURCE.parent, REVIEW):
        directory.mkdir(parents=True, exist_ok=True)
    if not preview_only:
        rig, objects = builder()
        for obj in objects:
            bm = bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
            bm.to_mesh(obj.data)
            bm.free()
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
        obj = bpy.context.object
        obj.name = 'VernMesh'
        assert all(any(g.weight > 0 for g in v.groups) for v in obj.data.vertices)
        bpy.context.view_layer.update()
        seated = bounds([obj])
        rig.data.pose_position = 'REST'
        bpy.context.view_layer.update()
        neutral = bounds([obj])
        assert neutral['max'][0] - neutral['min'][0] < 1.6, 'Twisted bind pose'
        rig.data.pose_position = 'POSE'
        bpy.context.view_layer.update()
        rig.select_set(True)
        build_actions(rig)
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
        bpy.ops.export_scene.gltf(filepath=str(GLB), export_format='GLB',
            use_selection=True, export_yup=True, export_cameras=False,
            export_lights=False, export_animations=True, export_animation_mode='ACTIONS',
            export_force_sampling=True, export_rest_position_armature=True,
            export_anim_slide_to_zero=True)
    common.reset()
    previous_actions = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=str(GLB))
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    # Blender's glTF importer creates a hidden Icosphere bone-display widget.
    # It is editor-only, not a mesh from the GLB.
    widgets = {b.custom_shape for r in rigs for b in r.pose.bones if b.custom_shape}
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in widgets]
    assert len(rigs) == 1 and meshes, 'Missing imported skin/rig'
    rig = rigs[0]
    imported_actions = [a for a in bpy.data.actions if a not in previous_actions]
    actions = {name: next(a for a in imported_actions if a.name.split('.')[0] == name)
               for name in DURATIONS}
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    rig.animation_data.action = actions['seated_rest']
    bpy.context.scene.frame_set(int(actions['seated_rest'].frame_range[0]))
    bpy.context.view_layer.update()
    assert all(o.type in ('MESH', 'ARMATURE', 'EMPTY') for o in bpy.context.scene.objects)
    for obj in meshes:
        assert all(len(v.groups) > 0 for v in obj.data.vertices), 'Unweighted vertex'
        assert any(m.type == 'ARMATURE' and m.object == rig for m in obj.modifiers)
        obj.data.calc_loop_triangles()
    imported = bounds(meshes)
    animation_report = validate_actions(rig, meshes, actions)
    if not preview_only:
        assert all(abs(a-b) < .003 for key in ('min', 'max')
                   for a, b in zip(seated[key], imported[key])), (seated, imported)
        report = dict(asset='vern', validation='passed', blender_version=bpy.app.version_string,
            file_bytes=GLB.stat().st_size, bones=len(rig.data.bones),
            triangles=sum(len(o.data.loop_triangles) for o in meshes),
            materials=len({m.name for o in meshes for m in o.data.materials}),
            authoring_axes='Z up, +Y front; GLB Y up, -Z front',
            neutral_bounds=neutral, seated_bounds=imported,
            animations=animation_report, chair_exported=False)
        (REVIEW / 'vern.json').write_text(json.dumps(report, indent=2) + '\n')
        print('VERN_VALIDATED ' + json.dumps(report))
    if '--skip-previews' not in __import__('sys').argv:
        previews()
        if '--static-only' not in __import__('sys').argv:
            from vern_review import moving_previews
            moving_previews(rig, actions)


def validate_actions(rig, meshes, actions):
    report = {}
    reference = None
    scene = bpy.context.scene
    scene.render.fps = FPS
    for name, action in actions.items():
        rig.animation_data.action = action
        start, end = action.frame_range
        duration = (end-start)/FPS
        assert abs(duration-DURATIONS[name]) < .001, (name, duration)
        poses, sampled_bounds = [], []
        for frame in range(round(start), round(end)+1):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            poses.append({b.name: b.matrix.copy() for b in rig.pose.bones})
            sampled_bounds.append(bounds(meshes))
        if reference is None:
            reference = poses[0]
        error = lambda a, b: max(abs(a[i][j]-b[i][j]) for i in range(4) for j in range(4))
        seam = max(error(poses[0][n], poses[-1][n]) for n in poses[0])
        fixed = max(error(p[n], reference[n]) for p in poses
                    for n in ('root', 'pelvis', 'foot.L', 'foot.R'))
        motion = max(error(p[n], poses[0][n]) for p in poses for n in p)
        assert seam < .0001 and fixed < .0001, (name, seam, fixed)
        if name != 'seated_rest':
            assert motion > .002, ('Static action', name)
        if name in ('smoking', 'drink_coffee'):
            assert max(error(poses[0][n], reference[n]) for n in reference) < .0001
        report[name] = dict(duration_seconds=duration, frames_sampled=len(poses),
            loop=name in ('idle_breathing', 'talking_default'),
            endpoint_matrix_max_error=seam, fixed_root_pelvis_feet_max_error=fixed,
            motion_matrix_max_delta=motion,
            bounds_blender={k: [fn(b[k][i] for b in sampled_bounds) for i in range(3)]
                            for k, fn in [('min', min), ('max', max)]})
    rig.animation_data.action = actions['seated_rest']
    scene.frame_set(int(actions['seated_rest'].frame_range[0]))
    return report
