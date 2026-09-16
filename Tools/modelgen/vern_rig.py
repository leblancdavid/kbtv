"""Neutral A-pose skeleton, authored seated transforms and inverse skinning."""
import bpy
from mathutils import Matrix, Vector


def create():
    # name: (parent, neutral head/tail, seated head/tail)
    bones = {
        'root': (None, (0, 0, 0), (0, 0, .2), (0, 0, 0), (0, 0, .2)),
        'pelvis': ('root', (0, 0, .97), (0, 0, 1.1), (0, 0, .635), (0, -.015, .765)),
        'spine': ('pelvis', (0, 0, 1.1), (0, 0, 1.30), (0, -.015, .765), (0, -.035, .965)),
        'chest': ('spine', (0, 0, 1.30), (0, 0, 1.48), (0, -.035, .965), (0, -.035, 1.145)),
        'neck': ('chest', (0, 0, 1.48), (0, 0, 1.55), (0, -.035, 1.145), (0, -.02, 1.215)),
        'head': ('neck', (0, 0, 1.55), (0, 0, 1.82), (0, -.02, 1.215), (0, -.02, 1.485)),
    }
    for side, s in [('L', 1), ('R', -1)]:
        def p(x, y, z):
            return (s * x, y, z)
        bones.update({
            'upper_arm.' + side: ('chest', p(.19, 0, 1.43), p(.38, 0, 1.20), p(.195, -.04, 1.095), p(.285, -.07, .80)),
            'forearm.' + side: ('upper_arm.' + side, p(.38, 0, 1.20), p(.53, 0, .98), p(.285, -.07, .80), p(.28, .185, .742)),
            'hand.' + side: ('forearm.' + side, p(.53, 0, .98), p(.60, 0, .87), p(.28, .185, .742), p(.27, .30, .72)),
            'thigh.' + side: ('pelvis', p(.105, 0, .97), p(.105, 0, .535), p(.105, 0, .635), p(.135, .415, .54)),
            'shin.' + side: ('thigh.' + side, p(.105, 0, .535), p(.105, 0, .105), p(.135, .415, .54), p(.135, .475, .105)),
            'foot.' + side: ('shin.' + side, p(.105, 0, .105), p(.105, .19, .06), p(.135, .475, .105), p(.135, .665, .06)),
        })
    bpy.ops.object.armature_add()
    rig = bpy.context.object
    rig.name = 'VernRig'
    bpy.ops.object.mode_set(mode='EDIT')
    rig.data.edit_bones.remove(rig.data.edit_bones[0])
    for name, (parent, head, tail, _, _) in bones.items():
        bone = rig.data.edit_bones.new(name)
        bone.head, bone.tail = head, tail
        if parent:
            bone.parent = rig.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for name, (_, _, _, head, tail) in bones.items():
        direction = Vector(tail) - Vector(head)
        rest = rig.data.bones[name]
        swing = (rest.tail_local - rest.head_local).rotation_difference(direction)
        rotation = (swing @ rest.matrix_local.to_quaternion()).to_matrix().to_4x4()
        rig.pose.bones[name].matrix = Matrix.Translation(Vector(head)) @ rotation
        bpy.context.view_layer.update()
    # Auxiliary controls share their parent's frame; original bone hierarchy stays intact.
    seated = {b.name: b.matrix.copy() for b in rig.pose.bones}
    pivots = {'grip.L': Vector((.278, .24, .732)),
              'grip.R': Vector((-.278, .24, .732)), 'jaw': Vector((0, .103, 1.246))}
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for name, parent in [('grip.L', 'hand.L'), ('grip.R', 'hand.R'), ('jaw', 'head')]:
        bone = rig.data.edit_bones.new(name)
        source = rig.data.edit_bones[parent]
        pivot = source.matrix @ (seated[parent].inverted() @ pivots[name])
        bone.head = pivot
        bone.tail = pivot + (source.tail-source.head).normalized() * .06
        bone.roll = source.roll
        bone.parent = source
    bpy.ops.object.mode_set(mode='OBJECT')
    for name, parent in [('grip.L', 'hand.L'), ('grip.R', 'hand.R'), ('jaw', 'head')]:
        matrix = seated[parent].copy()
        matrix.translation = pivots[name]
        rig.pose.bones[name].matrix = matrix
        bpy.context.view_layer.update()
    return rig


def bind_and_animate(rig, objects):
    transforms = {b.name: rig.pose.bones[b.name].matrix @ b.matrix_local.inverted()
                  for b in rig.data.bones}
    for obj in objects:
        names = {g.index: g.name for g in obj.vertex_groups}
        for vertex in obj.data.vertices:
            blended = Matrix(((0.,) * 4,) * 4)
            for group in vertex.groups:
                blended += transforms[names[group.group]] * group.weight
            vertex.co = blended.inverted() @ vertex.co
        obj.parent = rig
        modifier = obj.modifiers.new('Vern deformation', 'ARMATURE')
        modifier.object = rig
    for frame in (1, 25):
        for bone in rig.pose.bones:
            bone.rotation_mode = 'QUATERNION'
            for prop in ('location', 'rotation_quaternion', 'scale'):
                bone.keyframe_insert(prop, frame=frame, group=bone.name)
    rig.animation_data.action.name = 'seated_rest'
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 25
    bpy.context.scene.render.fps = 24
    bpy.context.scene.frame_set(1)
    rig.show_in_front = True
