"""Large studio side table without separately placeable tabletop gear."""
from common import box, rod, station_palette


def build():
    p = station_palette()
    # Desk: broad enough for separate lamp, CRT, audio deck, mic, and hand props.
    box('Studio table top', (0, 0, 0.73), (3.81, 1.44, 0.08), p['wood'], 0.018)
    box('Table front apron', (0, 0.74, 0.635), (3.64, 0.06, 0.15), p['shell'], 0.008)
    box('Cable tray under table', (0.25, -0.67, 0.61), (2.58, 0.07, 0.14), p['black'], 0.004)
    for x in (-1.73, 1.73):
        for y in (-0.53, 0.53):
            box('Stubby table leg', (x, y, 0.35), (0.09, 0.09, 0.68), p['shell'], 0.01)
            box('Rubber foot', (x, y, 0.02), (0.17, 0.15, 0.04), p['black'], 0.004)

    # Cable greebles under the rear edge.
    for i, x in enumerate((-0.35, -0.18, 0.05, 0.22)):
        rod('Hanging table cable', (x, -0.43, 0.70), (x + 0.05, -0.47, 0.48 - i * 0.03), 0.006, p['black'])
