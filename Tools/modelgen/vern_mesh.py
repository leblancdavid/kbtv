"""Small mesh vocabulary for Vern. Geometry is authored in the seated pose.

The exporter inverse-skins each vertex into the neutral bind pose, retaining
the weights and live armature. All coordinates are Blender Z-up/front +Y.
"""
import math
import bpy
from mathutils import Vector


def mesh(name, vertices, faces, mat, weights, uv_faces=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    uv = data.uv_layers.new(name='UVMap')
    if uv_faces is None:
        # Deterministic front projection for small custom details.
        xs, zs = [v[0] for v in vertices], [v[2] for v in vertices]
        dx, dz = max(max(xs) - min(xs), 1e-6), max(max(zs) - min(zs), 1e-6)
        uv_faces = [[((vertices[i][0] - min(xs)) / dx,
                      (vertices[i][2] - min(zs)) / dz) for i in f] for f in faces]
    for poly, coords in zip(data.polygons, uv_faces):
        for loop, coord in zip(poly.loop_indices, coords):
            uv.data[loop].uv = coord
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


def ring_uvs(faces, sides, rows, row_v=None):
    """Per-loop cylindrical UVs: wrap faces use U=1, not a cross-image seam."""
    row_v = row_v if row_v is not None else [j / (rows - 1) for j in range(rows)]
    result = []
    for face_index, face in enumerate(faces):
        if face_index in (0, len(faces) - 1):
            result.append([(.5 + .48 * math.cos(i % sides * math.tau / sides),
                            .5 + .48 * math.sin(i % sides * math.tau / sides)) for i in face])
            continue
        seam = {i % sides for i in face} == {0, sides - 1}
        result.append([(1.0 if seam and i % sides == 0 else (i % sides) / sides,
                        row_v[i // sides]) for i in face])
    return result


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
    height = max(rings[-1][2] - rings[0][2], 1e-6)
    row_v = [(r[2] - rings[0][2]) / height for r in rings]
    return mesh(name, vertices, faces, mat, weights, ring_uvs(faces, sides, len(rings), row_v))


def ellipsoid(name, center, radii, mat, weights, segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings,
                                       radius=1, location=center)
    obj = bpy.context.object
    vertices = [tuple(Vector(center) + Vector((v.co.x * radii[0],
                 v.co.y * radii[1], v.co.z * radii[2]))) for v in obj.data.vertices]
    faces = [tuple(p.vertices) for p in obj.data.polygons]
    uv_faces = [[tuple(obj.data.uv_layers.active.data[i].uv) for i in p.loop_indices]
                for p in obj.data.polygons]
    bpy.data.objects.remove(obj, do_unlink=True)
    return mesh(name, vertices, faces, mat, weights, uv_faces)


def tube(name, points, radius, mat, weights, sides=8):
    vertices = []
    previous_u = None
    for j, point in enumerate(points):
        tangent = Vector(points[min(j + 1, len(points) - 1)]) - Vector(points[max(0, j - 1)])
        tangent.normalize()
        ref = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        # Parallel-transport the ring frame: arbitrary per-ring axes twist tubes.
        u = (previous_u - tangent * previous_u.dot(tangent)).normalized() if previous_u is not None else tangent.cross(ref).normalized()
        previous_u = u
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
    lengths = [0.0]
    for a, b in zip(points, points[1:]):
        lengths.append(lengths[-1] + (Vector(b) - Vector(a)).length)
    row_v = [v / max(lengths[-1], 1e-6) for v in lengths]
    return mesh(name, vertices, faces, mat, weights, ring_uvs(faces, sides, len(points), row_v))
