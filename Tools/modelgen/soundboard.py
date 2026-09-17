"""Eight-channel analog mixer, tabletop origin, 1.12 x 0.5m.

Movable parts (fader caps, knobs + index bars, column lamps, master knob) are
kept as separate named objects during export so Soundboard3D can drive them as
real GLB nodes at runtime. Knob index bars are parented to their knob so one
rotation moves both. All names in MOVABLE are deterministic <100 chars.
"""
from common import box, cylinder, rod, station_palette, label


def _movable_names():
    names = ['MasterKnob']
    for ch in range(8):
        names.append(f'FaderCap_{ch}')
        names.append(f'Lamp_{ch}')
        for side in (0, 1, 2):
            names.append(f'Knob_{ch}_{side}')
            names.append(f'Index_{ch}_{side}')
    return tuple(names)


MOVABLE = _movable_names()


def build():
    p = station_palette()
    box('Mixer chassis', (0, 0, 0.055), (1.12, 0.5, 0.11), p['shell'], 0.012)
    for x in (-0.537, 0.537):
        box('Walnut cheek', (x, 0, 0.065), (0.045, 0.5, 0.13), p['wood'])
    for ch in range(8):
        x = -0.43 + ch * 0.105
        # Knob stack: three rows spread exactly 0.065 apart, shifted down
        # toward the fader slot (front row y 0.02, nearest the track at 0.14).
        # Index bars parented to their knob so rotation drives both; runtime
        # mapping relies on object names only.
        for side, y in enumerate((-0.11, -0.045, 0.02)):
            knob = cylinder(f'Knob_{ch}_{side}', (x, y, 0.128), 0.018, 0.036, p['black'], vertices=12)
            index = box(f'Index_{ch}_{side}', (x, y + 0.009, 0.147), (0.004, 0.012, 0.003), p['cream'], 0)
            index.parent = knob
            index.matrix_parent_inverse = knob.matrix_world.inverted()
        box('Fader track', (x, 0.14, 0.113), (0.012, 0.13, 0.006), p['black'], 0)
        box(f'FaderCap_{ch}', (x, 0.14, 0.131), (0.052, 0.025, 0.027), p['cream'])
        box(f'Lamp_{ch}', (x, 0.055, 0.114), (0.014, 0.014, 0.008), p['red'] if ch == 7 else p['green'])
    for x in (-0.29, 0.03):
        box('VU meter bezel', (x, -0.185, 0.12), (0.24, 0.088, 0.025), p['black'])
        box('VU meter face', (x, -0.185, 0.135), (0.212, 0.066, 0.008), p['cream'])
        rod('VU needle', (x, -0.16, 0.14), (x - 0.04, -0.2, 0.14), 0.002, p['red'])
    cylinder('MasterKnob', (0.425, -0.15, 0.133), 0.034, 0.046, p['metal'])
    label('Mixer badge', 'KBTV / MIX 08', (0, 0.252, 0.055), 0.032, p['cream'])
