"""Two-way monitor on a tall weighted stand; 1.8m tall floor-standing."""
from common import box, cylinder, station_palette, label


def build():
    p = station_palette()
    box('Weighted foot', (0, 0, 0.025), (0.55, 0.42, 0.05), p['black'], 0.015)
    box('Stand column', (0, 0, 0.575), (0.09, 0.09, 1.05), p['metal'])
    box('Stand plate', (0, 0, 1.12), (0.36, 0.3, 0.025), p['black'])
    box('Monitor enclosure', (0, -0.015, 1.46), (0.52, 0.38, 0.66), p['wood'], 0.016)
    box('Front baffle', (0, 0.186, 1.46), (0.48, 0.022, 0.61), p['shell'])
    for z, radius in ((1.3, 0.145), (1.56, 0.065)):
        cylinder('Driver surround', (0, 0.194, z), radius, 0.02, p['black'], 'Y', 24)
        cylinder('Driver cone', (0, 0.207, z), radius * 0.76, 0.012, p['metal'], 'Y', 24)
        cylinder('Dust cap', (0, 0.216, z), radius * 0.38, 0.018, p['black'], 'Y', 16)
    for x in (-0.185, 0.185):
        cylinder('Bass port', (x, 0.186, 1.18), 0.028, 0.012, p['black'], 'Y')
    label('Monitor badge', 'KB', (0.15, 0.196, 1.62), 0.028, p['cream'])
