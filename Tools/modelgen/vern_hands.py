"""Articulated digits; seated coordinates shared by skin and rig authoring."""
import bpy
from mathutils import Matrix, Vector, Quaternion
from vern_mesh import tube, ellipsoid


def digits(side):
    s = 1 if side == 'L' else -1
    result = {}
    for i, name in enumerate(('index', 'middle', 'ring', 'little')):
        x = s * (.249 + i * .018)
        length = (.069, .079, .073, .056)[i]
        result[name] = [Vector((x, .244 + length*t, .732 - .018*t*t))
                        for t in (0, .46, .77, 1)]
    result['thumb'] = [Vector((s*x, y, z)) for x, y, z in
        ((.247, .211, .738), (.228, .229, .731), (.223, .248, .723), (.231, .265, .718))]
    return result


def add_controls(rig):
    seated = {b.name: b.matrix.copy() for b in rig.pose.bones}
    poses = {}
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for side in ('L', 'R'):
        hand = rig.data.edit_bones['hand.'+side]
        inverse = hand.matrix @ seated[hand.name].inverted()
        for digit, points in digits(side).items():
            parent = hand
            for j in range(3):
                name = f'{digit}_{j+1}.{side}'
                bone = rig.data.edit_bones.new(name)
                bone.head, bone.tail = inverse @ points[j], inverse @ points[j+1]
                bone.parent = parent
                # Recover exactly this frame when moving back to the seated pose.
                poses[name] = seated[hand.name] @ hand.matrix.inverted() @ bone.matrix
                parent = bone
    bpy.ops.object.mode_set(mode='OBJECT')
    for name, matrix in poses.items():
        rig.pose.bones[name].matrix = matrix
        bpy.context.view_layer.update()


def build(side, palette):
    s = 1 if side == 'L' else -1
    hand = 'hand.'+side
    ellipsoid('Shaped palm '+side, (s*.278, .223, .739), (.041, .050, .022), palette['skin'], {hand: 1})
    for digit, joints in digits(side).items():
        points, radii, weights = [], [], []
        r = .0115 if digit == 'thumb' else (.008 if digit == 'little' else .0095)
        for j in range(3):
            for t in (0, .25, .75):
                points.append(joints[j].lerp(joints[j+1], t))
                radii.append(r*(1-.24*(j+t)/3))
                name = f'{digit}_{j+1}.{side}'
                weights.append({name: 1} if j == 0 or t >= .25 else
                               {f'{digit}_{j}.{side}': .5, name: .5})
        points.append(joints[-1]); radii.append(r*.48)
        weights.append({f'{digit}_3.{side}': 1})
        tube(f'Articulated {digit}.{side}', points, radii, palette['skin'], lambda i: weights[i//10], 10)
        ellipsoid(f'{digit} nail.{side}', joints[-2].lerp(joints[-1], .65)+Vector((0, 0, r*.67)),
                  (r*.62, .008, .0017), palette['skin'], {f'{digit}_3.{side}': 1}, 12, 6)


def pose(rig, base, side, amount, smoking=False):
    """Curl around each digit's anatomical transverse axis, not a shared palm pivot."""
    for i, digit in enumerate(('index', 'middle', 'ring', 'little', 'thumb')):
        angles = ((.10, .14, .10) if i < 2 else (.5, .65, .35)) if smoking else (.48, .85, .52)
        if digit == 'thumb':
            angles = (.12, .18, .16) if smoking else (.25, .42, .25)
        for j, angle in enumerate(angles):
            name = f'{digit}_{j+1}.{side}'
            axis = base[name].to_quaternion().inverted() @ Vector((-1, 0, 0))
            rig.pose.bones[name].rotation_quaternion @= Quaternion(axis, angle*amount)
