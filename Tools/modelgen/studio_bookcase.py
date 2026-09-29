"""Back-wall studio bookcase with books, records, bins, and audio gear."""
from common import box, cylinder, station_palette, label


def build():
    p = station_palette()

    # Warm, heavy home-studio shelving with dark backing for noir contrast.
    box('Bookcase rear panel', (0, -0.33, 0.86), (1.55, 0.05, 1.66), p['wood'], 0.006)
    for x in (-0.78, 0.0, 0.78):
        box('Bookcase vertical divider', (x, 0.0, 0.86), (0.065, 0.68, 1.72), p['wood'], 0.008)
    for z in (0.08, 0.48, 0.88, 1.28, 1.68):
        box('Bookcase shelf slab', (0, 0.0, z), (1.62, 0.70, 0.07), p['wood'], 0.008)
    box('Bookcase top cap', (0, 0, 1.75), (1.72, 0.76, 0.08), p['wood'], 0.012)
    box('Bookcase base plinth', (0, 0.02, 0.045), (1.72, 0.78, 0.09), p['black'], 0.008)

    # Row of mismatched books.
    for i in range(13):
        x = -0.65 + i * 0.105
        h = 0.23 + (i % 4) * 0.035
        mat = p['cream'] if i % 5 == 0 else (p['shell'] if i % 2 else p['black'])
        box('Studio book spine', (x, 0.315, 0.48 + h * 0.5), (0.072, 0.055, h), mat, 0.002)
        box('Book spine stripe', (x, 0.347, 0.51 + h * 0.5), (0.04, 0.006, 0.012), p['red'] if i % 3 == 0 else p['green'], 0)

    # Records in crates and leaning sleeves.
    box('Record storage bin left', (-0.43, 0.12, 0.215), (0.46, 0.42, 0.24), p['shell'], 0.006)
    box('Record storage bin right', (0.42, 0.12, 0.215), (0.50, 0.42, 0.24), p['shell'], 0.006)
    for i in range(8):
        x = -0.60 + i * 0.045
        box('Record sleeve left', (x, 0.345, 0.255), (0.026, 0.036, 0.28), p['cream'] if i % 3 == 0 else p['black'], 0.001)
    for i in range(9):
        x = 0.22 + i * 0.045
        box('Record sleeve right', (x, 0.345, 0.25), (0.026, 0.036, 0.27), p['cream'] if i % 4 == 0 else p['wood'], 0.001)

    # Audio gear and bins on upper shelves.
    box('Shelf tape deck', (-0.45, 0.22, 1.02), (0.50, 0.25, 0.17), p['shell'], 0.008)
    box('Tape deck face', (-0.45, 0.36, 1.025), (0.44, 0.025, 0.13), p['black'], 0.003)
    for x in (-0.58, -0.44):
        cylinder('Tape reel', (x, 0.377, 1.03), 0.042, 0.012, p['metal'], 'Y', vertices=18)
    for i in range(4):
        box('Tape deck VU mark', (-0.31 + i * 0.035, 0.382, 1.06), (0.018, 0.004, 0.012), p['green'], 0)
    box('Cable bin', (0.42, 0.15, 1.025), (0.48, 0.34, 0.22), p['black'], 0.006)
    box('Cable bin label', (0.42, 0.327, 1.045), (0.18, 0.008, 0.06), p['cream'], 0)

    box('Small radio receiver', (0.36, 0.22, 1.405), (0.52, 0.25, 0.16), p['shell'], 0.008)
    box('Radio dial glass', (0.20, 0.36, 1.425), (0.17, 0.018, 0.07), p['green'], 0.004)
    cylinder('Radio tuning knob', (0.57, 0.368, 1.425), 0.035, 0.016, p['metal'], 'Y', vertices=18)
    label('Radio tiny label', 'AM', (0.34, 0.365, 1.35), 0.025, p['cream'])

    for i in range(5):
        x = -0.64 + i * 0.12
        box('Upper archive box', (x, 0.13, 1.43), (0.095, 0.34, 0.25), p['cream'] if i % 2 else p['wood'], 0.004)
        box('Archive box pull', (x, 0.307, 1.43), (0.052, 0.007, 0.025), p['black'], 0)
