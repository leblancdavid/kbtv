"""Compact two-way monitor on a weighted floor stand; 0.9m tall."""
from common import box, cylinder, station_palette, label


def build():
    p = station_palette()
    box('Weighted foot', (0, 0, 0.025), (0.5, 0.48, 0.05), p['black'], 0.015)
    box('Stand column', (0, 0, 0.24), (0.09, 0.09, 0.43), p['metal'])
    box('Stand plate', (0, 0, 0.45), (0.36, 0.3, 0.025), p['black'])
    box('Monitor enclosure', (0, -0.015, 0.68), (0.46, 0.35, 0.44), p['wood'], 0.016)
    box('Front baffle', (0, 0.166, 0.68), (0.425, 0.022, 0.405), p['shell'])
    for z, radius in ((0.61, 0.115), (0.8, 0.049)):
        cylinder('Driver surround', (0, 0.183, z), radius, 0.02, p['black'], 'Y', 24)
        cylinder('Driver cone', (0, 0.196, z), radius * 0.76, 0.012, p['metal'], 'Y', 24)
        cylinder('Dust cap', (0, 0.205, z), radius * 0.38, 0.018, p['black'], 'Y', 16)
    for x in (-0.16, 0.16):
        cylinder('Bass port', (x, 0.183, 0.515), 0.024, 0.012, p['black'], 'Y')
    label('Monitor badge', 'KB', (0.15, 0.18, 0.82), 0.025, p['cream'])
