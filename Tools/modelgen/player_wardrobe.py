"""Separate fitted shirt, jeans, shoes and hair on Vern's MPFB standard rig."""
import math
import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
import common
from vern_mesh import mesh, ellipsoid, tube
from vern_mpfb_wardrobe import shell, rigid
from vern_mpfb_shoes import build_shoes


def wardrobe(body, rig):
    shirt_mat = common.material('Player oxford blue', '82979e', roughness=.9)
    trim = common.material('Player collar and placket', '95a8ac', roughness=.9)
    denim = common.material('Player indigo denim', '35485e', roughness=.95)
    hair = common.material('Player chestnut brown', '51382a', roughness=.86)
    ivory = common.material('Player buttons and eyes', 'd0c7b4', roughness=.65)
    dark = common.material('Player pupils', '20211f', roughness=.65)
    neck = rig.data.bones['neck01'].head_local.z
    waist = rig.data.bones['upperleg01.L'].head_local.z+.07
    cuts = []
    for side in ('L', 'R'):
        wrist = rig.data.bones['wrist.'+side].head_local
        fore = rig.data.bones['lowerarm02.'+side].head_local
        outward = (wrist-fore).normalized()
        cuts.append((wrist-outward*.018, outward))
    shirt = shell(body, 'Continuous wool pullover', shirt_mat,
                  [((0, 0, waist), (0, 0, -1)), ((0, 0, neck), (0, 0, 1)), *cuts], .023)
    shirt.name = 'Shirt'
    from player_collar import fit_neckline
    fit_neckline(body, shirt, rig, neck)
    jeans = shell(body, 'Continuous tailored trousers', denim,
                  [((0, 0, waist+.035), (0, 0, 1)), ((0, 0, .065), (0, 0, -1)),
                   ((.24, 0, 0), (1, 0, 0)), ((-.24, 0, 0), (-1, 0, 0))], .018)
    jeans.name = 'Jeans'
    build_shoes(rig, rigid)
    # Surface-following shirt details, with weights copied from nearest shirt vertex.
    surface = BVHTree.FromPolygons([v.co for v in shirt.data.vertices],
                                  [list(p.vertices) for p in shirt.data.polygons])
    def front(x, z):
        hit, _, _, _ = surface.ray_cast(Vector((x, -1, z)), Vector((0, 1, 0)), 2)
        assert hit is not None, ('Shirt detail missed', x, z)
        return (x, hit.y-.004, z)
    def weighted(obj):
        obj.vertex_groups.clear()
        groups = {g.index: obj.vertex_groups.new(name=g.name) for g in shirt.vertex_groups}
        for v in obj.data.vertices:
            nearest = min(shirt.data.vertices, key=lambda p: (p.co-v.co).length_squared)
            for g in nearest.groups:
                groups[g.group].add([v.index], g.weight, 'REPLACE')
        obj.parent = rig
        obj.modifiers.new('Armature', 'ARMATURE').object = rig
        return obj
    rows = [waist+.035+(neck-waist-.085)*i/24 for i in range(25)]
    verts = [front(x, z) for z in rows for x in (-.012, .012)]
    weighted(mesh('Shirt placket', verts, [(i, i+1, i+3, i+2) for i in range(0, 48, 2)], trim, {}))
    for i in range(6):
        z = waist+.035+(neck-waist-.11)*i/5
        weighted(ellipsoid('Shirt button', front(0, z), (.004, .003, .004), ivory, {}, 12, 8))
    from player_collar import build_collar, open_neck
    build_collar(body, shirt, rig, neck, front, weighted, trim)
    open_neck(shirt, neck)
    build_head(body, rig, hair, ivory, dark)
    # Outfit-specific visible skin; full MPFB body is regenerated for future outfits.
    bm = bmesh.new()
    bm.from_mesh(body.data)
    hand_x = min(abs(p.x) for p, _ in cuts)-.06
    neck_center = rig.data.bones['neck01'].head_local
    def covered(v):
        hand = abs(v.co.x) > hand_x and any((v.co-p).dot(n) > -.012 for p, n in cuts)
        if hand or v.co.z >= neck+.005:
            return False
        neckline = (v.co.x-neck_center.x)**2+(v.co.y-neck_center.y)**2 < .080**2
        open_front = abs(v.co.x) < .05 and v.co.y < neck_center.y and v.co.z >= neck-.065
        return v.co.z < neck-.115 or not (neckline or open_front)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if covered(v)], context='VERTS')
    bm.to_mesh(body.data)
    bm.free()


def build_head(body, rig, hair, ivory, dark):
    top = max(v.co.z for v in body.data.vertices)
    center = Vector((0, -.025, top-.10))
    scalp = BVHTree.FromPolygons([v.co for v in body.data.vertices],
                                [list(p.vertices) for p in body.data.polygons])
    vertices, faces = [], []
    sides, rows = 64, 24
    for row in range(rows):
        for col in range(sides):
            angle = math.tau*col/sides
            front = max(0, math.cos(angle))
            boundary = top-.145+.09*front**2+.016*abs(math.sin(angle))
            def point(theta):
                d = Vector((math.sin(theta)*math.sin(angle),
                            -math.sin(theta)*math.cos(angle), math.cos(theta)))
                hit, normal, _, _ = scalp.ray_cast(center, d, .4)
                assert hit is not None
                if normal.dot(d) < 0:
                    normal = -normal
                lift = .006+.012*front*max(0, d.z)
                lift += .0015*math.sin(7*angle+5*theta)*max(0, d.z)
                return hit+normal*lift
            lo, hi = .05, 2.45
            for _ in range(18):
                mid = (lo+hi)/2
                if point(mid).z > boundary:
                    lo = mid
                else:
                    hi = mid
            vertices.append(tuple(point(.012+(lo-.012)*row/(rows-1))))
    faces.append(tuple(reversed(range(sides))))
    for row in range(rows-1):
        for col in range(sides):
            a, b = row*sides+col, row*sides+(col+1)%sides
            faces.append((a, b, b+sides, a+sides))
    rigid(mesh('BrownHair', vertices, faces, hair, {}), rig)
    eye = rig.data.bones['eye.L'].head_local
    for s in (-1, 1):
        rigid(ellipsoid('Eye', (s*eye.x, -.120, eye.z),
                        (.0125, .013, .012), ivory, {}, 24, 16), rig)
        rigid(ellipsoid('Iris', (s*eye.x, -.1327, eye.z),
                        (.0043, .001, .0043), hair, {}, 16, 10), rig)
        rigid(ellipsoid('Pupil', (s*eye.x, -.1335, eye.z),
                        (.002, .0007, .002), dark, {}, 12, 8), rig)
        rigid(tube('Brown eyebrow', [(s*.012, -.141, eye.z+.022),
                   (s*.030, -.140, eye.z+.026), (s*.048, -.128, eye.z+.022)],
                   [.002, .003, .0015], hair, {}, sides=8), rig)
