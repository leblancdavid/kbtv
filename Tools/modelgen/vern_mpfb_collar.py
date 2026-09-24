"""Rounded, dipped-front collar sampled against the actual MPFB neck."""
import math

from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

from vern_mesh import mesh


def build_collar(body, rig, material):
    coords = [v.co.copy() for v in body.data.vertices]
    bvh = BVHTree.FromPolygons(coords, [list(f.vertices) for f in body.data.polygons])
    kd = KDTree(len(coords))
    for i, co in enumerate(coords):
        kd.insert(co, i)
    kd.balance()
    neck = rig.data.bones['neck01'].head_local
    # Outer base -> rolled lip -> inner return: closed cloth, no flat plate caps.
    profile = [(-.014, .020), (-.004, .015), (.008, .010), (.015, .009),
               (.019, .007), (.018, .004), (.012, .003), (-.014, .015)]
    sides = 64
    vertices = []
    for height, ease in profile:
        radii, origins, directions = [], [], []
        for i in range(sides):
            angle = math.tau*i/sides
            direction = Vector((math.cos(angle), math.sin(angle), 0))
            # Front edge stays below the chin instead of intersecting it.
            dip = .020*max(0, -direction.y)**2
            origin = Vector((neck.x, neck.y, neck.z+height-dip))
            hit, _, _, _ = bvh.ray_cast(origin, direction, .3)
            assert hit is not None, f'Neck surface missing at collar sample {i}'
            radii.append((hit-origin).length+ease)
            origins.append(origin)
            directions.append(direction)
        for _ in range(3):
            radii = [(radii[(i-1)%sides]+2*radii[i]+radii[(i+1)%sides])/4
                     for i in range(sides)]
        vertices.extend(tuple(o+d*r) for o, d, r in zip(origins, directions, radii))
    faces, uvs = [], []
    for row in range(len(profile)):
        for i in range(sides):
            next_row, next_i = (row+1)%len(profile), (i+1)%sides
            faces.append((row*sides+i, row*sides+next_i,
                          next_row*sides+next_i, next_row*sides+i))
            uvs.append([(i/sides, row/len(profile)), ((i+1)/sides, row/len(profile)),
                        ((i+1)/sides, (row+1)/len(profile)), (i/sides, (row+1)/len(profile))])
    def weights(i):
        _, index, _ = kd.find(Vector(vertices[i]))
        groups = {body.vertex_groups[g.group].name: g.weight
                  for g in body.data.vertices[index].groups
                  if body.vertex_groups[g.group].name in rig.data.bones}
        total = sum(groups.values())
        assert total > 0
        return {name: value/total for name, value in groups.items()}
    obj = mesh('Folded high collar', vertices, faces, material, weights, uvs)
    circumference = sum((Vector(vertices[2*sides+i])-Vector(vertices[2*sides+(i+1)%sides])).length
                        for i in range(sides))
    section = sum((Vector(vertices[row*sides])-Vector(vertices[((row+1)%len(profile))*sides])).length
                  for row in range(len(profile)))
    for loop in obj.data.uv_layers.active.data:
        loop.uv.x *= circumference
        loop.uv.y *= section
    obj.parent = rig
    modifier = obj.modifiers.new('Armature', 'ARMATURE')
    modifier.object = rig
    return obj
