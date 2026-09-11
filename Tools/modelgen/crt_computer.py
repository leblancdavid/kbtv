"""Compact CRT terminal and keyboard fitting the 0.6m-deep desk."""
from common import box, cylinder, station_palette, label


def build():
    p = station_palette()
    box('Terminal foot', (0, -0.065, 0.025), (0.38, 0.3, 0.05), p['black'])
    box('Swivel riser', (0, -0.065, 0.07), (0.19, 0.18, 0.06), p['shell'])
    box('Deep CRT housing', (0, -0.07, 0.295), (0.6, 0.35, 0.39), p['shell'], 0.035)
    box('Front bezel', (0, 0.105, 0.3), (0.61, 0.045, 0.4), p['metal'], 0.015)
    box('Screen surround', (0, 0.132, 0.327), (0.51, 0.02, 0.29), p['black'], 0.018)
    box('Phosphor glass', (0, 0.145, 0.327), (0.458, 0.016, 0.244), p['green'], 0.022)
    # Sparse terminal lines, no textures or excessive emission.
    for i, width in enumerate((0.23, 0.32, 0.19, 0.28)):
        box('Terminal line', (0.19 - width / 2, 0.155, 0.405 - i * 0.045),
            (width, 0.003, 0.009), p['cream'], 0)
    cylinder('Power switch', (-0.24, 0.135, 0.148), 0.014, 0.014, p['black'], 'Y')
    label('Terminal badge', 'KBTV', (0.15, 0.131, 0.148), 0.026, p['cream'])
    for i in range(6):
        box('Rear vent', (0, -0.247, 0.23 + i * 0.032), (0.37, 0.005, 0.012), p['black'])
    box('Keyboard base', (0, 0.225, 0.018), (0.57, 0.1, 0.036), p['shell'], 0.008)
    for row in range(3):
        for col in range(12):
            box('Key', (-0.247 + col * 0.045, 0.192 + row * 0.025, 0.042),
                (0.035, 0.018, 0.012), p['cream'], 0.001)
