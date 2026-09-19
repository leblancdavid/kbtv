"""Four-strip analog mixer with a broadcast button grid, tabletop origin, 1.12 x 0.5m.

Channels 1-3 (Vern / Caller / Ads-Music) are live strips; channel 4 is the
stereo master pair (two linked faders). Where channels 5-8 used to sit the
board now carries an info screen above a 2x2 grid of blocky broadcast buttons
(Music, Delay, Ads, Drop). One VU meter is centred above the master strip.

The board instance sits in World3D.tscn with a 180-degree yaw (room convention
shared with the other desk props: the +Y authoring edge must face the player,
so authored +X reads as in-game LEFT). The face is therefore authored mirrored:
strips run right-to-left in Blender (Vern at +0.45 ... master at +0.135) so
they read Vern-first left-to-right in game, the button grid sits on the -X
side, and the flat labels are spun 180 degrees to stay readable.

Movable parts (fader caps, knobs + index bars, column lamps, button caps with
their lamp faces + labels parented so one press moves the whole button, and the
screen face) are kept as separate named objects during export so Soundboard3D
can drive them as real GLB nodes at runtime. All names in MOVABLE are
deterministic <100 chars.
"""
import math

import bpy
from common import box, cylinder, rod, station_palette, label, material

# Strip geometry: channels 1-4 authored right-to-left so they read left-to-right
# in game (Vern, Caller, Ads, Master; the instance's yaw-180 mirrors the face).
STRIP_X0 = 0.45
STRIP_PITCH = -0.105
KNOB_ROWS = (-0.11, -0.045, 0.02)

# Broadcast button grid (2x2, authored -X side = in-game right):
# top row Music/Delay, bottom row Ads/Drop.
BUTTONS = (('Music', -0.09, -0.045), ('Delay', -0.385, -0.045),
           ('Ads', -0.09, 0.125), ('Drop', -0.385, 0.125))


def _movable_names():
    names = ['ScreenFace']
    for ch in range(4):
        for side in (0, 1, 2):
            names.append(f'Knob_{ch}_{side}')
            names.append(f'Index_{ch}_{side}')
    names += ['FaderCap_0', 'FaderCap_1', 'FaderCap_2', 'FaderCap_3L', 'FaderCap_3R']
    names += ['Lamp_0', 'Lamp_1', 'Lamp_2', 'Lamp_3L', 'Lamp_3R']
    for name, _, _ in BUTTONS:
        names += [f'Button_{name}', f'BtnLamp_{name}', f'BtnLabel_{name}']
    return tuple(names)


MOVABLE = _movable_names()


def _flat_label(name, text, location, size, mat, parent):
    """Small mesh lettering lying flat on the board face. Spun 180 degrees so it
    reads correctly from the yaw-180 in-game view (see module docstring)."""
    bpy.ops.object.text_add(location=location, rotation=(0, 0, math.pi))
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = 'CENTER'
    obj.data.align_y = 'CENTER'
    obj.data.size = size
    obj.data.extrude = 0.0004
    obj.data.resolution_u = 2
    obj.data.materials.append(mat)
    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.object
    obj.parent = parent
    obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj


def _knob_stack(ch):
    x = STRIP_X0 + ch * STRIP_PITCH
    for side, y in enumerate(KNOB_ROWS):
        knob = cylinder(f'Knob_{ch}_{side}', (x, y, 0.128), 0.018, 0.036, p['black'], vertices=12)
        index = box(f'Index_{ch}_{side}', (x, y + 0.009, 0.147), (0.004, 0.012, 0.003), p['cream'], 0)
        index.parent = knob
        index.matrix_parent_inverse = knob.matrix_world.inverted()


def _fader(x, cap_name, width=0.052):
    box('Fader track', (x, 0.14, 0.113), (0.012, 0.13, 0.006), p['black'], 0)
    box(cap_name, (x, 0.14, 0.131), (width, 0.025, 0.027), p['cream'])


def _lamp(x, name):
    box(name, (x, 0.055, 0.114), (0.014, 0.014, 0.008), p['green'])


def build():
    global p
    p = station_palette()
    box('Mixer chassis', (0, 0, 0.055), (1.12, 0.5, 0.11), p['shell'], 0.012)
    for x in (-0.537, 0.537):
        box('Walnut cheek', (x, 0, 0.065), (0.045, 0.5, 0.13), p['wood'])

    for ch in range(4):
        x = STRIP_X0 + ch * STRIP_PITCH
        _knob_stack(ch)
        if ch == 3:
            # Stereo master pair: two narrow faders + one status lamp each.
            _fader(x - 0.027, 'FaderCap_3L', width=0.046)
            _fader(x + 0.027, 'FaderCap_3R', width=0.046)
            _lamp(x - 0.027, 'Lamp_3L')
            _lamp(x + 0.027, 'Lamp_3R')
        else:
            _fader(x, f'FaderCap_{ch}')
            _lamp(x, f'Lamp_{ch}')

    # VU meter centred above the master strip.
    vx = STRIP_X0 + 3 * STRIP_PITCH
    box('VU meter bezel', (vx, -0.185, 0.12), (0.24, 0.088, 0.025), p['black'])
    box('VU meter face', (vx, -0.185, 0.135), (0.212, 0.066, 0.008), p['cream'])
    rod('VU needle', (vx, -0.16, 0.14), (vx - 0.04, -0.2, 0.14), 0.002, p['red'])

    # Info screen above the button grid (half-height panel). The face stays a
    # separate movable node so the runtime can swap in a viewport texture later;
    # the bezel is chassis.
    box('Screen bezel', (-0.245, -0.195, 0.12), (0.42, 0.060, 0.025), p['black'])
    box('ScreenFace', (-0.245, -0.195, 0.135), (0.38, 0.046, 0.006), p['black'], 0)

    # 2x2 broadcast button grid: big blocky caps, a lamp face and a flat label
    # parented to each cap so a press moves the whole button.
    button_red = material('Broadcast button red', 'b03038', emission=0.12)
    for name, bx, by in BUTTONS:
        cap = box(f'Button_{name}', (bx, by, 0.126), (0.16, 0.13, 0.024), button_red, 0.006)
        lamp = box(f'BtnLamp_{name}', (bx, by, 0.14), (0.145, 0.115, 0.004), p['cream'], 0)
        lamp.parent = cap
        lamp.matrix_parent_inverse = cap.matrix_world.inverted()
        _flat_label(f'BtnLabel_{name}', name, (bx, by, 0.143), 0.02, p['black'], cap)

    label('Mixer badge', 'KBTV / MIX 08', (0, 0.252, 0.055), 0.032, p['cream'])
