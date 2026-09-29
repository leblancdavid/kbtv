"""Table-mounted articulated broadcast boom microphone."""
import bpy
from common import box, cylinder, rod, station_palette, material


def build():
    p = station_palette()
    dark = material('Boom arm black enamel', '181b22', 0.25, 0.58)
    chrome = material('Boom pivot worn metal', '83898e', 0.65, 0.42)
    grille = material('Microphone dark grille', '20242c', 0.15, 0.75)

    # Table clamp and vertical pivot. Origin sits on tabletop for easy placement.
    box('Table edge clamp top jaw', (0, 0.0, 0.04), (0.16, 0.18, 0.055), dark, 0.006)
    box('Table edge clamp lower jaw', (0, 0.0, -0.075), (0.15, 0.15, 0.05), dark, 0.006)
    box('Clamp screw block', (0, 0.08, -0.015), (0.06, 0.05, 0.11), chrome, 0.004)
    cylinder('Clamp screw knob', (0, 0.125, -0.015), 0.035, 0.025, dark, 'Y', vertices=18)
    cylinder('Desk boom vertical post', (0, 0, 0.23), 0.025, 0.38, chrome, vertices=18)
    cylinder('Base swivel collar', (0, 0, 0.42), 0.045, 0.055, dark, vertices=18)

    # Dual-arm boom: two parallel rods per segment with chunky readable hinges.
    shoulder = (0.0, 0.0, 0.46)
    elbow = (-0.34, -0.05, 0.77)
    wrist = (-0.74, -0.01, 0.96)
    for offset in (-0.026, 0.026):
        rod('Lower parallel boom arm', (shoulder[0] + offset, shoulder[1], shoulder[2]),
            (elbow[0] + offset, elbow[1], elbow[2]), 0.011, dark)
        rod('Upper parallel boom arm', (elbow[0] + offset, elbow[1], elbow[2]),
            (wrist[0] + offset, wrist[1], wrist[2]), 0.011, dark)
    for name, loc in (('Shoulder hinge', shoulder), ('Elbow hinge', elbow), ('Wrist hinge', wrist)):
        cylinder(name, loc, 0.048, 0.09, chrome, 'Y', vertices=18)
        cylinder(name + ' black cap left', (loc[0] - 0.052, loc[1], loc[2]), 0.026, 0.012, dark, 'Y', vertices=16)
        cylinder(name + ' black cap right', (loc[0] + 0.052, loc[1], loc[2]), 0.026, 0.012, dark, 'Y', vertices=16)

    # Small cable runs along the arm, offset so it reads as separate from rods.
    rod('Boom cable lower sag', (0.02, -0.035, 0.44), (-0.34, -0.085, 0.73), 0.005, p['black'])
    rod('Boom cable upper sag', (-0.34, -0.085, 0.73), (-0.74, -0.045, 0.92), 0.005, p['black'])

    # Shock mount and side-address capsule aimed toward the seated host.
    box('Shock mount square frame', (-0.86, 0.005, 0.95), (0.17, 0.045, 0.20), chrome, 0.009)
    box('Shock mount inner gap', (-0.86, 0.029, 0.95), (0.115, 0.012, 0.145), p['black'], 0.004)
    for z in (0.88, 1.02):
        rod('Shock elastic cross cord', (-0.92, 0.04, z), (-0.80, 0.04, 0.95), 0.004, p['black'])
        rod('Shock elastic cross cord', (-0.80, 0.04, z), (-0.92, 0.04, 0.95), 0.004, p['black'])
    box('Broadcast microphone body', (-0.86, 0.065, 0.95), (0.12, 0.16, 0.23), dark, 0.025)
    box('Front grille panel', (-0.86, 0.153, 0.955), (0.092, 0.018, 0.175), grille, 0.014)
    for i in range(7):
        box('Mic grille rib', (-0.86, 0.165, 0.885 + i * 0.023), (0.074, 0.007, 0.004), chrome, 0.001)

    # Keep validation's bottom-origin contract while preserving clamp thickness.
    for obj in bpy.context.scene.objects:
        obj.location.z += 0.1
