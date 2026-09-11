"""4.8 x 0.6m desk, 0.8m high; open center knee space."""
from common import box, station_palette


def build():
    p = station_palette()
    box('Walnut worktop', (0, 0, 0.76), (4.8, 0.6, 0.08), p['wood'], 0.012)
    box('Front bumper', (0, 0.294, 0.746), (4.76, 0.012, 0.04), p['black'])
    box('Rear cable tray', (0, -0.23, 0.61), (4.45, 0.1, 0.12), p['shell'])
    for x in (-1.96, 1.96):
        box('Recessed plinth', (x, 0, 0.04), (0.62, 0.46, 0.08), p['black'])
        box('Drawer pedestal', (x, -0.02, 0.4), (0.72, 0.5, 0.64), p['shell'])
        for z in (0.21, 0.43, 0.63):
            box('Drawer front', (x, 0.238, z), (0.66, 0.025, 0.16), p['wood'])
            box('Recessed pull', (x, 0.255, z + 0.04), (0.22, 0.015, 0.025), p['black'])
            box('Metal pull lip', (x, 0.267, z + 0.046), (0.2, 0.018, 0.012), p['metal'])
