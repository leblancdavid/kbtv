"""Matched rest-space tailoring of mesh and skeleton; never pose-bone scaling."""
import bpy


def measurements(rig):
    b = rig.data.bones
    hip, knee, ankle = [b[n+'.L'].head_local for n in
                        ('upperleg01', 'lowerleg01', 'foot')]
    return dict(thigh=(hip-knee).length, shin=(knee-ankle).length,
                hip_height=hip.z, knee_height=knee.z, ankle_height=ankle.z)


def tailor_proportions(rig):
    before = measurements(rig)
    ankle, hip = before['ankle_height'], before['hip_height']
    neck = rig.data.bones['neck01'].head_local.z
    # Shorter legs, slightly longer trunk, unchanged head and foot dimensions.
    leg_scale = .90
    drop = (hip-ankle)*(1-leg_scale)
    trunk_gain = drop-.042
    def height(z):
        if z <= ankle:
            return z
        if z <= hip:
            return ankle+(z-ankle)*leg_scale
        if z <= neck:
            return z-drop+trunk_gain*(z-hip)/(neck-hip)
        return z-.042
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        inverse = obj.matrix_world.inverted()
        for vertex in obj.data.vertices:
            world = obj.matrix_world @ vertex.co
            world.z = height(world.z)
            vertex.co = inverse @ world
        obj.data.update()
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:
        bone.head.z = height(bone.head.z)
        bone.tail.z = height(bone.tail.z)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.update()
    return dict(before=before, after=measurements(rig), leg_scale=leg_scale,
                method='matched mesh and edit-bone rest-space remap')
