"""Standalone tabletop desk lamp, selectable in Godot."""
import bpy
from mathutils import Vector
from common import cylinder, finish, material, rod, station_palette


ORIGIN = (-1.24, -0.19, 0.742)


def local(point):
    return tuple(point[i] - ORIGIN[i] for i in range(3))


def cone_between(name, start, end, radius_start, radius_end, mat, vertices=32):
    delta = Vector(end) - Vector(start)
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius_start,
                                   radius2=radius_end, depth=delta.length,
                                   location=(Vector(start) + Vector(end)) / 2)
    obj = bpy.context.object
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, mat, 0.001)


def build():
    p = station_palette()
    shade = material('Soft ivory desk lamp shade', 'c9c1ae', 0.0, 0.72, 0.08)
    brass = material('Aged brass lamp neck', '9a7c45', 0.65, 0.45)
    glow = material('Warm lamp diffuser', 'd9bd78', 0.0, 0.42, 0.55)

    cylinder('Desk lamp round base', local((-1.24, -0.19, 0.762)), 0.13, 0.038, p['cream'], vertices=32)
    cylinder('Lamp base dark foot', local((-1.24, -0.19, 0.748)), 0.135, 0.012, p['black'], vertices=32)
    cylinder('Lamp base switch', local((-1.17, -0.105, 0.79)), 0.018, 0.014, p['metal'], 'Y', vertices=16)
    rod('Lamp straight stem', local((-1.24, -0.19, 0.78)), local((-1.24, -0.19, 1.18)), 0.012, brass)
    rod('Lamp sloped neck', local((-1.24, -0.19, 1.18)), local((-1.08, -0.06, 1.28)), 0.011, brass)
    cone_between('Outer hollow cone lamp shade', local((-1.08, -0.06, 1.28)), local((-0.95, 0.12, 1.19)), 0.072, 0.178, shade)
    cone_between('Inner warm shade cavity', local((-1.07, -0.045, 1.272)), local((-0.956, 0.112, 1.193)), 0.052, 0.143, glow)
    cylinder('Visible warm bulb', local((-1.005, 0.055, 1.223)), 0.042, 0.062, glow, vertices=24)
    cone_between('Lamp brass cap', local((-1.115, -0.095, 1.305)), local((-1.075, -0.055, 1.282)), 0.060, 0.050, brass, vertices=24)
