"""Surface-fitted open shirt neck and a turned collar with a separate stand."""
import math
import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from vern_mesh import mesh
from vern_mpfb_wardrobe import rigid


def fit_neckline(body, shirt, rig, neck):
    """Relaxed garment extraction widens the neck hole; fit it back to the neck."""
    surface = BVHTree.FromPolygons([v.co for v in body.data.vertices],
                                  [list(p.vertices) for p in body.data.polygons])
    center = rig.data.bones['neck01'].head_local.copy()
    center.z = neck
    for vertex in shirt.data.vertices:
        blend = max(0, min(1, (vertex.co.z-(neck-.055))/.055))**.5
        if blend == 0:
            continue
        radial = vertex.co-center
        radial.z = 0
        if radial.length < .001:
            continue
        direction = radial.normalized()
        hit, _, _, _ = surface.ray_cast(center, direction, .3)
        assert hit is not None
        target = hit+direction*.004
        vertex.co.x += (target.x-vertex.co.x)*blend
        vertex.co.y += (target.y-vertex.co.y)*blend
        # Preserve shoulder clearance while drawing the neckline inward.
        origin = Vector((center.x, center.y, vertex.co.z))
        skin, _, _, _ = surface.ray_cast(origin, direction, .5)
        if skin is not None:
            radius = (vertex.co-origin).length
            required = (skin-origin).length+.004
            if radius < required:
                vertex.co = origin+direction*required
    shirt.data.update()


def open_neck(shirt, neck):
    bm = bmesh.new()
    bm.from_mesh(shirt.data)
    # Split both sides of the V before removing its interior, retaining weights.
    for normal in [(-1.4, 0, 1), (1.4, 0, 1), (0, 1, 0)]:
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                              plane_co=(0, 0, neck-.05), plane_no=normal,
                              dist=.000001)
    faces = [f for f in bm.faces if f.calc_center_median().y < 0 and
             f.calc_center_median().z > neck-.05+1.4*abs(f.calc_center_median().x)+.00001]
    bmesh.ops.delete(bm, geom=faces, context='FACES')
    bm.to_mesh(shirt.data)
    bm.free()
    shirt.data.update()


def build_collar(body, shirt, rig, neck, front, weighted, material):
    surface = BVHTree.FromPolygons([v.co for v in shirt.data.vertices],
                                  [list(p.vertices) for p in shirt.data.polygons])
    neck_surface = BVHTree.FromPolygons([v.co for v in body.data.vertices],
                                       [list(p.vertices) for p in body.data.polygons])
    center = rig.data.bones['neck01'].head_local.copy()
    center.z = neck-.003
    segments, rows = 96, 9
    band, fold = [], []
    def clear_neck(point, clearance):
        origin = Vector((center.x, center.y, point.z))
        radial = point-origin
        radial.z = 0
        direction = radial.normalized()
        hit, _, _, _ = neck_surface.ray_cast(origin, direction, .4)
        assert hit is not None, 'Collar clearance ray missed neck'
        if radial.length < (hit-origin).length+clearance:
            return hit+direction*clearance
        return point
    for i in range(segments+1):
        angle = .35+(math.tau-.70)*i/segments
        direction = Vector((math.sin(angle), -math.cos(angle), 0))
        hit, _, _, _ = neck_surface.ray_cast(center, direction, .3)
        assert hit is not None, 'Collar stand missed neck surface'
        front_amount = max(0, math.cos(angle))**3
        # Linear rise from the approved front into a level rear band: a
        # smoothstep gives the side a bowed, crooked top edge in profile.
        rear = max(0, min(1, (min(angle, math.tau-angle)-.45)/1.15))
        ridge = hit+direction*.006
        ridge.z = neck+.011-.004*front_amount
        lower = ridge-direction*.002
        lower.z = ridge.z-.014
        # Rear stand rises against the actual neck, rather than lying on shoulders.
        ridge.z += .040*rear
        elevated_center = Vector((center.x, center.y, ridge.z))
        elevated_hit, _, _, _ = neck_surface.ray_cast(elevated_center, direction, .3)
        assert elevated_hit is not None, 'Raised collar missed neck'
        ridge.x += (elevated_hit.x+direction.x*.006-ridge.x)*rear
        ridge.y += (elevated_hit.y+direction.y*.006-ridge.y)*rear
        for row in range(rows):
            band.append(tuple(clear_neck(lower.lerp(ridge, row/(rows-1)), .006)))
        edge = ridge+direction*(.021-.009*rear)
        edge.z = neck-.021+.019*rear
        # Turn the front ends down into points; sides/back remain a folded band.
        tip_blend = max(0, 1-min(angle-.35, math.tau-.35-angle)/.95)
        sign = 1 if angle < math.pi else -1
        tip = Vector(front(sign*.055, neck-.045))
        tip.y -= .002
        edge = edge.lerp(tip, tip_blend)
        for row in range(rows):
            t = row/(rows-1)
            point = ridge.lerp(edge, t)
            # Straight generators across the fold give pressed fabric, not a tube.
            # On the back the fold is a stiff band: projecting each vertex to
            # a different shirt triangle creates the polygonal side bulge.
            # Only the falling front tips need garment clearance.
            if rear < .05:
                origin = Vector((center.x, center.y, point.z))
                radial = point-origin
                radial.z = 0
                if radial.length > .001:
                    normal = radial.normalized()
                    cloth, _, _, _ = surface.ray_cast(origin, normal, .4)
                    if cloth is not None and (cloth-origin).length+.002 > radial.length:
                        point = cloth+normal*.002
            fold.append(tuple(clear_neck(point, .010)))
    band_faces = [(i*rows+j, (i+1)*rows+j, (i+1)*rows+j+1, i*rows+j+1)
                  for i in range(segments) for j in range(rows-1)]
    fold_faces = [(i*rows+j, (i+1)*rows+j, (i+1)*rows+j+1, i*rows+j+1)
                  for i in range(segments) for j in range(rows-1)]
    for name, vertices, faces in [('Collar stand', band, band_faces),
                                   ('Folded shirt collar', fold, fold_faces)]:
        # Nearest-shirt weights include shoulders: lowering the arms crumples the
        # collar. A stiff collar belongs to the upper torso, not the sleeves.
        obj = rigid(mesh(name, vertices, faces, material, {}), rig,
                    rig.data.bones['neck01'].parent.name)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(obj.data)
        bm.free()
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
        solid = obj.modifiers.new('Turned fabric thickness', 'SOLIDIFY')
        solid.thickness = .0015
        solid.offset = 0
