"""Surface-derived wardrobe for the MPFB base (Z-up, face towards -Y)."""
import math

import bmesh
import bpy
from mathutils import Vector

import common
from vern_mesh import ellipsoid, tube


def apply(obj, modifier):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def shell(body, name, material, planes, ease, predicate=None):
    """Copy topology/weights, cut actual boundary loops, then tailor the shell."""
    obj = body.copy()
    obj.data = body.data.copy()
    obj.name = name
    bpy.context.collection.objects.link(obj)
    obj.modifiers.clear()
    obj.data.materials.clear()
    obj.data.materials.append(material)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for point, normal in planes:
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                              dist=0.00001, plane_co=point, plane_no=normal,
                              clear_outer=True, clear_inner=False)
    if predicate:
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not predicate(v.co)], context='VERTS')
    # Plane cuts can retain disconnected thumb tips or internal mouth islands.
    components, unseen = [], set(bm.verts)
    while unseen:
        todo, component = [unseen.pop()], set()
        while todo:
            v = todo.pop()
            component.add(v)
            for edge in v.link_edges:
                other = edge.other_vert(v)
                if other in unseen:
                    unseen.remove(other)
                    todo.append(other)
        components.append(component)
    keep = max(components, key=len)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v not in keep], context='VERTS')
    bm.to_mesh(obj.data)
    bm.free()
    smooth = obj.modifiers.new('Relax anatomical detail into fabric', 'SMOOTH')
    smooth.factor = 0.8
    smooth.iterations = 2 if predicate or name == 'Folded high collar' else 14
    apply(obj, smooth)
    obj.data.update()
    offsets = [v.normal.copy() * ease for v in obj.data.vertices]
    for v, offset in zip(obj.data.vertices, offsets):
        v.co += offset
        if name == 'Continuous wool pullover' and abs(v.co.x) < .18 and 1.05 < v.co.z < 1.37 and v.co.y < -.02:
            # A loose wool front bridges the chest instead of tracing anatomy.
            envelope = -.153 * math.sqrt(max(.05, 1-(v.co.x/.215)**2))
            blend = min(1, (v.co.z-1.05)/.08, (1.37-v.co.z)/.055)
            v.co.y += blend*(envelope-v.co.y)
    # Restore planar cut boundaries after relaxation, so hems meet cleanly.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for v in bm.verts:
        if any(e.is_boundary for e in v.link_edges):
            if predicate:
                low, high = 1.5, 1.72
                for _ in range(20):
                    mid = (low+high)/2
                    if predicate(Vector((v.co.x, v.co.y, mid))):
                        high = mid
                    else:
                        low = mid
                v.co.z = low-.003
            if planes:
                point, normal = min(planes, key=lambda plane:
                    abs((v.co-Vector(plane[0])).dot(Vector(plane[1]))))
                n = Vector(normal)
                distance = (v.co-Vector(point)).dot(n)
                if abs(distance) < .045:
                    v.co -= n*distance
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    if planes:
        for v in obj.data.vertices:
            for point, normal in planes:
                n = Vector(normal)
                distance = (v.co-Vector(point)).dot(n)
                if distance > 0:
                    v.co -= n*distance
    if name == 'Continuous wool pullover':
        finish = obj.modifiers.new('Soften tailored front', 'SMOOTH')
        finish.factor = .65
        finish.iterations = 5
        apply(obj, finish)
    solid = obj.modifiers.new('Turned fabric edge', 'SOLIDIFY')
    solid.thickness = 0.003
    solid.offset = -1
    apply(obj, solid)
    for f in obj.data.polygons:
        f.use_smooth = True
    mod = obj.modifiers.new('Armature', 'ARMATURE')
    mod.object = body.parent
    return obj


def rigid(obj, rig, bone='head'):
    obj.vertex_groups.clear()
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    obj.parent = rig
    mod = obj.modifiers.new('Armature', 'ARMATURE')
    mod.object = rig
    return obj


def build_wardrobe(body, rig, p):
    neck = rig.data.bones['neck01'].head_local.z
    waist = rig.data.bones['upperleg01.L'].head_local.z + 0.07
    wrist_planes = []
    for side in ['L', 'R']:
        wrist = rig.data.bones[f'wrist.{side}'].head_local
        forearm = rig.data.bones[f'lowerarm02.{side}'].head_local
        outward = (wrist-forearm).normalized()
        wrist_planes.append((wrist-outward*.018, outward))
    shell(body, 'Continuous wool pullover', p['sweater'], [
        ((0, 0, waist), (0, 0, -1)), ((0, 0, neck), (0, 0, 1)),
        *wrist_planes], 0.019)
    shell(body, 'Continuous tailored trousers', p['pants'], [
        ((0, 0, waist + .045), (0, 0, 1)), ((0, 0, .065), (0, 0, -1)),
        ((.24, 0, 0), (1, 0, 0)), ((-.24, 0, 0), (-1, 0, 0))], .016)
    from vern_mesh import loft
    collar = loft('Folded high collar', [(0, -.039, neck-.016, .093, .086),
                  (0, -.039, neck-.005, .090, .084),
                  (0, -.039, neck+.006, .088, .081),
                  (0, -.039, neck+.010, .085, .078)], p['rib'], {}, sides=48)
    bm = bmesh.new()
    bm.from_mesh(collar.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if len(f.verts)>4], context='FACES_ONLY')
    bm.to_mesh(collar.data)
    bm.free()
    rigid(collar, rig, 'neck01')
    for side, s in [('L', 1), ('R', -1)]:
        foot = rig.data.bones[f'foot.{side}'].head_local
        shoe = ellipsoid(f'Leather loafer {side}', (foot.x, -.09, .053),
                         (.061, .150, .046), p['black'], {}, segments=24, rings=12)
        for v in shoe.data.vertices:
            v.co.z = max(.019, v.co.z)
        rigid(shoe, rig, f'foot.{side}')
        rigid(common.box(f'Loafer sole {side}', (foot.x, -.09, .015),
                         (.116, .276, .025), p['black'], bevel=.012), rig, f'foot.{side}')
    build_head(body, rig, p)
    # The clothed body remains in the diagnostic source; hidden skin is removed
    # from this clothed asset to prevent cloth/skin intersections in animation.
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < neck-.035
                    and not any((v.co-point).dot(normal) > -.012
                                for point, normal in wrist_planes)], context='VERTS')
    bm.to_mesh(body.data)
    bm.free()


def build_head(body, rig, p):
    # Measure against the real eyes and scalp, not the head bone's pivot alone.
    eye = rig.data.bones['eye.L'].head_local
    top = max(v.co.z for v in body.data.vertices)
    def hairline(co):
        front = max(0, min(1, (-co.y-.025)/.10))
        return co.z > top-.137 + .076*front + .012*abs(co.x)/.075
    hair = shell(body, 'Fitted swept scalp', p['hair'], [], .009, hairline)
    hair.modifiers.clear()
    for v in hair.data.vertices:
        lift = max(0, min(1, (v.co.z-(top-.060))/.060))
        v.co.z += (.010+.008*max(0, 1-v.co.x/.07))*lift
        v.co.x += .010*lift
    hair.data.materials.append(p['gray'])
    for f in hair.data.polygons:
        co = sum((hair.data.vertices[i].co for i in f.vertices), Vector())/len(f.vertices)
        if abs(co.x) > .056 and co.z < eye.z+.032 and co.y < -.035:
            f.material_index = 1
    rigid(hair, rig)
    sclera = common.material('Warm ivory eyes', 'b5aaa0', 0, .6)
    iris = common.material('Hazel iris', '554b37', 0, .65)
    for s in (-1, 1):
        rigid(ellipsoid(f'Eye {s}', (s*eye.x, -.120, eye.z),
                        (.0125, .013, .012), sclera, {}, segments=24, rings=16), rig)
        rigid(ellipsoid(f'Iris {s}', (s*eye.x, -.1327, eye.z),
                        (.0043, .001, .0043), iris, {}, segments=20, rings=12), rig)
        rigid(ellipsoid(f'Pupil {s}', (s*eye.x, -.1335, eye.z),
                        (.002, .0007, .002), p['black'], {}, segments=16, rings=10), rig)
        cup = (s*.09, -.007, eye.z-.012)
        rigid(ellipsoid(f'Headphone earpad {s}', cup, (.016, .040, .051),
                        p['black'], {}, segments=24, rings=16), rig)
        rigid(ellipsoid(f'Headphone shell {s}', (s*.108, cup[1], cup[2]),
                        (.013, .034, .044), p['sweater'], {}, segments=24, rings=16), rig)
        # Closed, gently squared aviator rims. Thin metal leaves the eyes readable.
        points = []
        for i in range(49):
            a = math.tau*i/48
            x = s*eye.x + .024*math.copysign(abs(math.cos(a))**.72, math.cos(a))
            z = eye.z + .017*math.copysign(abs(math.sin(a))**.85, math.sin(a))
            points.append((x, -.157 + .11*abs(x), z))
        rigid(tube(f'Aviator frame {s}', points, .0017, p['metal'], {}, sides=8), rig)
        rigid(tube(f'Glasses temple arm {s}', [(s*.052, -.151, eye.z+.005),
                   (s*.071, -.102, eye.z+.007), (s*.074, -.022, eye.z)],
                   .0018, p['metal'], {}, sides=8), rig)
        rigid(tube(f'Tapered mustache {s}', [(s*.002, -.145, eye.z-.062),
                   (s*.012, -.144, eye.z-.061), (s*.022, -.140, eye.z-.064),
                   (s*.027, -.136, eye.z-.067)], [.003, .0045, .003, .0008],
                   p['hair'], {}, sides=10), rig)
        rigid(tube(f'Eyebrow {s}', [(s*.011, -.143, eye.z+.023),
                   (s*.026, -.146, eye.z+.026), (s*.046, -.135, eye.z+.023)],
                   [.002, .003, .001], p['hair'], {}, sides=8), rig)
    rigid(tube('Glasses bridge', [(-.005, -.160, eye.z+.006),
               (0, -.166, eye.z+.009), (.005, -.160, eye.z+.006)],
               .0015, p['metal'], {}, sides=8), rig)
    band = [(.104*math.cos(a), -.008, eye.z-.005+(top+.030-eye.z+.005)*math.sin(a))
            for a in [math.pi*i/32 for i in range(33)]]
    rigid(tube('Arched headphone headband', band, .009, p['black'], {}, sides=10), rig)
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z > top-.025], context='VERTS')
    bm.to_mesh(body.data)
    bm.free()
