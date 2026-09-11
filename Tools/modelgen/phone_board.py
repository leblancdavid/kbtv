"""Desktop call-screening bank with handset, keypad, and six line keys."""
from common import box, rod, station_palette, label


def build():
    p = station_palette()
    box('Console base', (0, 0, 0.045), (0.78, 0.46, 0.09), p['shell'], 0.012)
    box('Inset panel', (0.09, 0, 0.094), (0.52, 0.4, 0.012), p['black'])
    for x in (-0.29, -0.18):
        box('Handset cradle', (x, -0.01, 0.12), (0.045, 0.31, 0.06), p['metal'])
    box('Handset grip', (-0.235, 0, 0.177), (0.095, 0.26, 0.065), p['black'], 0.018)
    for y in (-0.15, 0.15):
        box('Handset end', (-0.235, y, 0.15), (0.14, 0.1, 0.105), p['black'], 0.016)
    for row in range(4):
        for col in range(3):
            box('Dial key', (-0.02 + col * 0.058, -0.01 + row * 0.044, 0.108),
                (0.043, 0.031, 0.022), p['cream'])
    for row in range(6):
        y = -0.13 + row * 0.054
        box('Line key', (0.26, y, 0.112), (0.09, 0.034, 0.026), p['shell'])
        box('Line status', (0.325, y, 0.106), (0.015, 0.02, 0.014), p['green'] if row < 2 else p['red'])
    box('Caller display', (0.045, -0.145, 0.106), (0.21, 0.06, 0.014), p['green'])
    label('Station badge', 'KBTV / LINES', (0, 0.232, 0.043), 0.032, p['cream'])
    for i in range(8):
        rod('Coiled cord', (-0.35, -0.16 + i * 0.02, 0.07),
            (-0.365, -0.15 + i * 0.02, 0.11), 0.006, p['black'])
