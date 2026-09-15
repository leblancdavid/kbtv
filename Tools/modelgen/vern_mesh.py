"""Small mesh vocabulary for Vern. Geometry is authored in the seated pose.

The exporter inverse-skins each vertex into the neutral bind pose, retaining
the weights and live armature. All coordinates are Blender Z-up/front +Y.
"""
import math
import bpy
from mathutils import Vector


def mesh(name, vertices, faces, mat, weights):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    for poly in data.polygons:
        poly.use_smooth = True
    for i in range(len(vertices)):
        for bone, weight in (weights(i) if callable(weights) else weights).items():
            group = obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
            group.add([i], weight, 'REPLACE')
    return obj


def loft(name, rings, mat, weights, sides=16):
    """Horizontal ellipse rings: (x,y,z,width-radius,depth-radius)."""
    vertices = [(x + rx * math.cos(i * math.tau / sides),
                 y + ry * math.sin(i * math.tau / sides), z)
                for x, y, z, rx, ry in rings for i in range(sides)]
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(rings) - 1):
        for i in range(sides):
            a, b = j * sides + i, j * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.append(tuple(range((len(rings) - 1) * sides, len(vertices))))
    return mesh(name, vertices, faces, mat, weights)


def ellipsoid(name, center, radii, mat, weights, segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings,
                                       radius=1, location=center)
    obj = bpy.context.object
    vertices = [tuple(Vector(center) + Vector((v.co.x * radii[0],
                 v.co.y * radii[1], v.co.z * radii[2]))) for v in obj.data.vertices]
    faces = [tuple(p.vertices) for p in obj.data.polygons]
    bpy.data.objects.remove(obj, do_unlink=True)
    return mesh(name, vertices, faces, mat, weights)


def tube(name, points, radius, mat, weights, sides=8):
    vertices = []
    for j, point in enumerate(points):
        tangent = Vector(points[min(j + 1, len(points) - 1)]) - Vector(points[max(0, j - 1)])
        tangent.normalize()
        ref = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        u = tangent.cross(ref).normalized()
        v = tangent.cross(u).normalized()
        r = radius[j] if isinstance(radius, list) else radius
        vertices.extend(tuple(Vector(point) + r * (u * math.cos(i * math.tau / sides)
                        + v * math.sin(i * math.tau / sides))) for i in range(sides))
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(points) - 1):
        for i in range(sides):
            a, b = j * sides + i, j * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.append(tuple(range((len(points) - 1) * sides, len(vertices))))
    return mesh(name, vertices, faces, mat, weights)
