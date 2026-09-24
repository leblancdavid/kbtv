"""Broad flattened swept locks fitted to the scalp, rather than round strand tubes."""
import math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from vern_mesh import mesh


def swept_locks(cap, rig, top, rigid):
    bvh = BVHTree.FromPolygons([v.co.copy() for v in cap.data.vertices],
                              [list(p.vertices) for p in cap.data.polygons])
    center = Vector((0, -.015, top-.10))
    # Locks fan left from a right-side part, following the crown's curved surface.
    for lock in range(22):
        right = lock >= 14
        rear = (lock-14)/7 if right else lock/13
        start = Vector((.030, -.091+.157*rear, top-.015-.035*abs(rear-.5)))
        end = Vector((.075 if right else -.072, -.112+.202*rear,
                      top-.072-.046*rear))
        points, normals = [], []
        for step in range(25):
            t = step/24
            guide = start.lerp(end, t)
            guide.z += .036*math.sin(math.pi*t)
            guide.y -= (.010 if right else .028)*math.sin(math.pi*t)
            direction = (guide-center).normalized()
            hit, normal, _, _ = bvh.ray_cast(center, direction, .4)
            if hit is None:
                hit, normal = guide, direction
            if normal.dot(direction) < 0:
                normal = -normal
            points.append(hit+normal*.001)
            normals.append(normal)
        vertices, faces = [], []
        for step, (point, normal) in enumerate(zip(points, normals)):
            t = step/24
            tangent = (points[min(24,step+1)]-points[max(0,step-1)]).normalized()
            across = tangent.cross(normal).normalized()
            taper = max(.08, math.sin(math.pi*t)**.45)
            for col in range(7):
                q = col/3-1
                width = .009 if lock < 9 else .008
                candidate = point+across*q*width*taper
                ray = (candidate-center).normalized()
                hit, hit_normal, _, _ = bvh.ray_cast(center, ray, .4)
                if hit is not None:
                    if hit_normal.dot(ray) < 0:
                        hit_normal = -hit_normal
                    candidate = hit+hit_normal*(.0002+.0025*(1-q*q)*taper)
                vertices.append(tuple(candidate))
        for row in range(24):
            for col in range(6):
                a = row*7+col
                faces.append((a,a+1,a+8,a+7))
        obj = mesh('Swept hair lock %02d'%lock, vertices, faces,
                   cap.data.materials[0], {})
        from vern_mpfb_surfaces import hair_uv
        hair_uv(obj, top)
        rigid(obj, rig)
