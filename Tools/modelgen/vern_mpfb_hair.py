"""Reference-directed side part: smooth foundation, swept masses, masked tips.

Blender -Y is front. Guides are scalp-relative; only roots/edges conform,
leaving the center of each lock free to carry volume and directional flow.
"""
import math
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from vern_mesh import mesh
from vern_mpfb_surfaces import hair_material, hair_uv, hair_card_material


def build_hair(body, rig, top, rigid):
    center = Vector((0, -.025, top-.10))
    scalp = BVHTree.FromPolygons([v.co.copy() for v in body.data.vertices],
                                [list(p.vertices) for p in body.data.polygons])

    def surface(direction, extra=0):
        direction = direction.normalized()
        hit, normal, _, _ = scalp.ray_cast(center, direction, .4)
        if hit is None:
            raise ValueError('Hair guide missed the MPFB scalp')
        if normal.dot(direction) < 0:
            normal = -normal
        front = max(0, -direction.y)
        crown = max(0, direction.z)
        # Fuller forelock, short sides; leave room under the headphone band.
        lift = .003 + .014*front*crown + .004*crown
        part = math.exp(-((hit.x-.028)/.007)**2)*front*crown
        return hit+normal*(lift-.003*part+extra), normal

    vertices, faces, boundaries = [], [], []
    sides, rows = 128, 48
    for row in range(rows):
        for col in range(sides):
            angle = math.tau*col/sides
            front = max(0, math.cos(angle))
            # Continuous hairline, slight recession at both temples, short nape.
            boundary = top-.145+.088*front**2+.016*abs(math.sin(angle))
            lo, hi = .05, 2.45
            for _ in range(20):
                theta = (lo+hi)/2
                d = Vector((math.sin(theta)*math.sin(angle),
                            -math.sin(theta)*math.cos(angle), math.cos(theta)))
                point, _ = surface(d)
                if point.z > boundary:
                    lo = theta
                else:
                    hi = theta
            theta = .012+(lo-.012)*row/(rows-1)
            if row == rows-1:
                boundaries.append(lo)
            d = Vector((math.sin(theta)*math.sin(angle),
                        -math.sin(theta)*math.cos(angle), math.cos(theta)))
            point, _ = surface(d)
            vertices.append(tuple(point))
    faces.append(tuple(range(sides)))
    for row in range(rows-1):
        for col in range(sides):
            a, b = row*sides+col, row*sides+(col+1)%sides
            faces.append((a, a+sides, b+sides, b))
    cap = mesh('Fitted swept scalp', vertices, faces, body.data.materials[0], {})
    sculpt_sweeps(cap, surface, center, top)
    hair_material(cap, top)
    rigid(cap, rig)
    build_edge_cards(surface, boundaries, rig, rigid)


def build_locks(surface, center, cap, rig, top, rigid):
    card_material = hair_card_material()
    for index in range(10):
        short = index >= 6
        depth = (index-6)/3 if short else index/5
        # A family of sweeps, rather than radial spokes or independent tubes.
        start = Vector((.030, -.145+.22*depth, top-.042+.040*math.sin(math.pi*depth)))
        end = Vector((.076 if short else -.074, -.128+.20*depth,
                      top-.081-.032*depth))
        control = start.lerp(end, .55)
        control.y -= .010 if not short else .005
        control.z += .018 if not short else .008
        points, normals = [], []
        for row in range(25):
            t = row/24
            guide = (1-t)**2*start+2*t*(1-t)*control+t*t*end
            point, normal = surface(guide-center)
            points.append(point)
            normals.append(normal)
        verts, faces = [], []
        for row, (point, normal) in enumerate(zip(points, normals)):
            t = row/24
            tangent = (points[min(24,row+1)]-points[max(0,row-1)]).normalized()
            across = tangent.cross(normal).normalized()
            taper = .05+.95*math.sin(math.pi*t)**.65
            width = (.017 if short else .022)*taper
            for col in range(9):
                q = col/4-1
                edge, n = surface(point+across*q*width-center)
                # Broad lenticular cross-section, with buried edges and rounded tip.
                height = (.003 if short else .005)*max(0,1-q*q)**1.5*math.sin(math.pi*t)**.8
                verts.append(tuple(edge+n*(-.0003+height)))
        for row in range(24):
            for col in range(8):
                a = row*9+col
                faces.append((a,a+1,a+10,a+9))
        obj = mesh('Swept hair clump %02d'%index, verts, faces, cap.data.materials[0], {})
        hair_uv(obj, top)
        rigid(obj, rig)
        # Shallow curved strips on the final third of each lock, extending its tip.
        card_verts, card_faces, uvs = [], [], []
        for row in range(9):
            t = row/8
            sample = 16+min(row,8)
            point, normal = points[sample], normals[sample]
            tangent = (points[min(24,sample+1)]-points[sample-1]).normalized()
            across = tangent.cross(normal).normalized()
            point += tangent*(.004*t*t)+normal*(.002+.003*(1-t))
            for col in range(3):
                card_verts.append(tuple(point+across*(col-1)*.010))
                uvs.append((col/2,t))
        for row in range(8):
            for col in range(2):
                a = row*3+col
                card_faces.append((a,a+1,a+4,a+3))
        obj = mesh('Hair edge card %02d'%index, card_verts, card_faces, card_material, {},
                   [[uvs[i] for i in face] for face in card_faces])
        rigid(obj, rig)
