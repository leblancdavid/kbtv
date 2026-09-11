"""Eight-channel analog mixer, tabletop origin, 1.12 x 0.5m."""
from common import box, cylinder, rod, station_palette, label


def build():
    p = station_palette()
    box('Mixer chassis', (0, 0, 0.055), (1.12, 0.5, 0.11), p['shell'], 0.012)
    for x in (-0.537, 0.537):
        box('Walnut cheek', (x, 0, 0.065), (0.045, 0.5, 0.13), p['wood'])
    for ch in range(8):
        x = -0.43 + ch * 0.105
        for y in (-0.095, -0.025):
            cylinder('Channel knob', (x, y, 0.128), 0.018, 0.036, p['black'], vertices=12)
            box('Knob index', (x, y + 0.009, 0.147), (0.004, 0.012, 0.003), p['cream'], 0)
        box('Fader track', (x, 0.12, 0.113), (0.012, 0.15, 0.006), p['black'], 0)
        box('Fader cap', (x, 0.08 + (ch % 3) * 0.025, 0.131), (0.052, 0.025, 0.027), p['cream'])
        box('Channel lamp', (x, 0.011, 0.114), (0.014, 0.014, 0.008), p['red'] if ch == 7 else p['green'])
    for x in (-0.29, 0.03):
        box('VU meter bezel', (x, -0.185, 0.12), (0.24, 0.088, 0.025), p['black'])
        box('VU meter face', (x, -0.185, 0.135), (0.212, 0.066, 0.008), p['cream'])
        rod('VU needle', (x, -0.16, 0.14), (x - 0.04, -0.2, 0.14), 0.002, p['red'])
    cylinder('Master gain', (0.425, -0.15, 0.133), 0.034, 0.046, p['metal'])
    label('Mixer badge', 'KBTV / MIX 08', (0, 0.252, 0.055), 0.032, p['cream'])
