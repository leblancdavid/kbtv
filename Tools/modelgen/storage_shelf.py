"""Low archive shelf: 1.2 W x 1.1 H x 0.8 D, boxes and tape binders."""
from common import box, station_palette


def build():
    p = station_palette()
    for x in (-0.575, 0.575):
        for y in (-0.375, 0.375):
            box('Steel upright', (x, y, 0.55), (0.05, 0.05, 1.1), p['shell'])
    box('Rear panel', (0, -0.386, 0.57), (1.1, 0.028, 1.04), p['wood'])
    for z in (0.09, 0.55, 1.07):
        box('Shelf tray', (0, 0, z), (1.15, 0.76, 0.06), p['metal'])
    for x in (-0.32, 0.27):
        box('Archive carton', (x, 0.045, 0.305), (0.48, 0.56, 0.37), p['wood'], 0.006)
        box('Carton lid', (x, 0.045, 0.5), (0.5, 0.58, 0.03), p['cream'])
        box('Archive label', (x, 0.328, 0.34), (0.16, 0.006, 0.075), p['cream'], 0)
        box('Handle slot', (x, 0.333, 0.41), (0.11, 0.005, 0.025), p['black'])
    for i in range(7):
        x = -0.46 + i * 0.092
        box('Tape binder', (x, 0.02, 0.77), (0.075, 0.44, 0.38), p['shell'] if i % 2 else p['black'])
        box('Binder label', (x, 0.243, 0.81), (0.051, 0.006, 0.11), p['cream'], 0)
    box('Spare equipment case', (0.39, 0.02, 0.7), (0.28, 0.47, 0.24), p['wood'])
