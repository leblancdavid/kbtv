"""Five-spoke rolling chair; front +Y, bottom-center pivot for runtime push."""
import math
from common import box, cylinder, rod, station_palette


def build():
    p = station_palette()
    for i in range(5):
        angle = i * math.tau / 5
        x, y = math.cos(angle) * 0.29, math.sin(angle) * 0.29
        rod('Base spoke', (0, 0, 0.13), (x, y, 0.09), 0.023, p['metal'])
        cylinder('Caster', (x, y, 0.04), 0.04, 0.055, p['black'], 'Y', 12)
    cylinder('Gas lift', (0, 0, 0.285), 0.036, 0.31, p['metal'])
    box('Seat pan', (0, 0, 0.435), (0.48, 0.46, 0.06), p['black'], 0.025)
    box('Worn upholstered seat', (0, 0.005, 0.49), (0.5, 0.48, 0.08), p['shell'], 0.03)
    rod('Back support', (0, -0.19, 0.43), (0, -0.235, 0.88), 0.025, p['metal'])
    box('Back shell', (0, -0.24, 0.81), (0.46, 0.065, 0.42), p['black'], 0.03)
    box('Back cushion', (0, -0.195, 0.82), (0.42, 0.065, 0.36), p['shell'], 0.025)
    for x in (-0.28, 0.28):
        rod('Arm support', (x * 0.8, 0, 0.44), (x, 0, 0.665), 0.018, p['metal'])
        box('Arm pad', (x, 0.015, 0.675), (0.065, 0.3, 0.045), p['black'], 0.018)
