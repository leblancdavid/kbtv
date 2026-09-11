"""Compact late-night broadcast rack, 0.70 W x 1.00 H x 0.65 D meters."""
from common import box, cylinder, material, rod


def build():
    shell = material('Charcoal powdercoat', '3a3d48', 0.25)
    dark = material('Rack face charcoal', '252831', 0.15)
    trim = material('Worn slate metal', '697078', 0.55, 0.5)
    black = material('Recesses and rubber', '11131a')
    cream = material('Warm meter face', 'b4a98b')
    green = material('Muted phosphor indicator', '3a8a78', emission=1.5)
    red = material('Oxblood indicator', 'a23a3a', emission=0.6)
    for x in (-0.27, 0.27):
        for y in (-0.24, 0.24):
            cylinder('Rubber foot', (x, y, 0.025), 0.038, 0.05, black)
    box('Cabinet enclosure', (0, -0.025, 0.525), (0.7, 0.6, 0.95), shell, 0.012)
    box('Dark front recess', (0, 0.279, 0.53), (0.63, 0.014, 0.86), black)
    for x in (-0.315, 0.315):
        box('Rack mounting rail', (x, 0.29, 0.53), (0.024, 0.024, 0.86), trim)
    for row, z in enumerate((0.23, 0.43, 0.63, 0.83)):
        box('Rack unit %d' % row, (0, 0.294, z), (0.585, 0.025, 0.178), dark)
        for x in (-0.276, 0.276):
            for dz in (-0.063, 0.063):
                cylinder('Mounting screw', (x, 0.31, z + dz), 0.005, 0.005, trim, 'Y', 8)
        if row == 3:
            for x in (-0.14, 0.055):
                box('VU bezel', (x, 0.311, z + 0.012), (0.15, 0.014, 0.087), black)
                box('VU face', (x, 0.32, z + 0.012), (0.128, 0.006, 0.064), cream, 0.001)
                rod('VU needle', (x, 0.324, z - 0.012),
                    (x - 0.025, 0.324, z + 0.03), 0.0018, red)
            cylinder('Meter gain', (0.218, 0.312, z), 0.021, 0.024, trim, 'Y')
        else:
            for x in (-0.2, -0.115, -0.03, 0.055):
                cylinder('Control knob', (x, 0.309, z + 0.022), 0.021, 0.026, black, 'Y')
                box('Knob index', (x, 0.323, z + 0.034), (0.004, 0.002, 0.01), cream, 0)
            for i in range(3):
                box('Status light', (0.16 + i * 0.028, 0.31, z + 0.024),
                    (0.012, 0.008, 0.015), green if i < 2 else red, 0.001)
            for i in range(8):
                box('Vent slot', (-0.21 + i * 0.061, 0.308, z - 0.046),
                    (0.038, 0.004, 0.011), black, 0.001)
    box('Rear service panel', (0, -0.326, 0.48), (0.52, 0.008, 0.64), dark)
    for i in range(8):
        box('Rear ventilation', (0, -0.331, 0.3 + i * 0.04), (0.4, 0.003, 0.013), black)
