"""Standalone tabletop studio audio interface, selectable in Godot."""
from common import box, cylinder, station_palette


ORIGIN = (0.72, 0.07, 0.755)


def local(point):
    return tuple(point[i] - ORIGIN[i] for i in range(3))


def build():
    p = station_palette()

    box('Desktop audio deck chassis', local((0.72, 0.07, 0.80)), (0.82, 0.44, 0.09), p['shell'], 0.012)
    box('Audio deck recessed face', local((0.72, 0.304, 0.807)), (0.74, 0.026, 0.062), p['black'], 0.004)
    box('Audio deck top plate', local((0.72, 0.07, 0.866)), (0.76, 0.36, 0.022), p['metal'], 0.006)

    for i in range(5):
        x = 0.39 + i * 0.09
        box('Audio slider track', local((x, 0.02, 0.881)), (0.050, 0.12, 0.006), p['black'], 0)
        box('Audio slider cap', local((x + (-0.012 + i * 0.006), 0.02, 0.894)), (0.025, 0.030, 0.016), p['cream'], 0.002)

    for i in range(4):
        cylinder('Audio row knob', local((0.78 + i * 0.075, 0.155, 0.899)), 0.022, 0.018, p['metal'], vertices=18)

    for i in range(4):
        box('Aligned deck indicator', local((0.97 + i * 0.038, -0.07, 0.889)), (0.020, 0.018, 0.010), p['green'] if i < 3 else p['red'], 0.001)

    box('Deck label strip', local((0.88, -0.07, 0.888)), (0.14, 0.018, 0.010), p['cream'], 0)
