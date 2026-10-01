"""Distinct, meter-scale variants of the six reusable station-clutter props."""
import math
from common import box, cylinder, rod, station_palette


def carton(p, open_top=True):
    box('Carton bottom', (0, 0, 0.015), (0.62, 0.45, 0.03), p['wood'])
    for x in (-0.298, 0.298):
        box('Cardboard wall', (x, 0, 0.195), (0.024, 0.45, 0.36), p['wood'])
    for y in (-0.213, 0.213):
        box('Cardboard wall', (0, y, 0.195), (0.6, 0.024, 0.36), p['wood'])
    if open_top:
        box('Folded back flap', (0, -0.273, 0.37), (0.60, 0.14, 0.02), p['wood'])
    else:
        box('Closed carton lid', (0, 0, 0.39), (0.64, 0.47, 0.04), p['wood'])
    box('Carton label', (0, 0.228, 0.21), (0.19, 0.005, 0.07), p['cream'], 0)


def archive_box_manuals():
    p = station_palette()
    carton(p)
    for i in range(5):
        box('Thick service manual', (-0.18, 0, 0.065 + i * 0.065),
            (0.32, 0.32, 0.057), p['black'] if i % 2 else p['shell'])
        box('Manual page edge', (-0.18, 0.17, 0.065 + i * 0.065),
            (0.28, 0.005, 0.043), p['cream'], 0)
    box('Loose inventory card', (0.15, 0.025, 0.23), (0.14, 0.30, 0.004), p['cream'], 0)


def archive_box_tapes():
    p = station_palette()
    carton(p)
    for i in range(4):
        for j in range(2):
            x, y = -0.19 + i * 0.125, -0.09 + j * 0.17
            box('Tape case', (x, y, 0.21), (0.10, 0.12, 0.30),
                p['black'] if (i + j) % 2 else p['shell'], 0.003)
            box('Tape case label', (x, y + 0.062, 0.27), (0.073, 0.003, 0.09), p['cream'], 0)


def archive_box_sealed():
    p = station_palette()
    carton(p, False)
    box('Packing tape seam', (0, 0, 0.413), (0.075, 0.46, 0.006), p['cream'], 0)
    box('Shipping label', (0.14, 0.01, 0.414), (0.18, 0.16, 0.006), p['cream'], 0)
    for i in range(3):
        box('Handwritten inventory line', (0.14, -0.045 + i * 0.04, 0.418),
            (0.12 - i * 0.02, 0.008, 0.002), p['black'], 0)


def bin_shell(p, height=0.32):
    box('Plastic tote base', (0, 0, 0.015), (0.58, 0.44, 0.03), p['shell'])
    for x in (-0.273, 0.273):
        box('Molded side', (x, 0, height / 2), (0.034, 0.44, height), p['shell'])
    for y in (-0.205, 0.205):
        box('Molded end', (0, y, height / 2), (0.55, 0.03, height), p['shell'])
    for y in (-0.213, 0.213):
        box('Reinforced rim', (0, y, height), (0.6, 0.035, 0.025), p['metal'])
    box('White inventory label', (0, 0.224, 0.20), (0.18, 0.005, 0.06), p['cream'], 0)


def plastic_bin_lidded():
    p = station_palette()
    bin_shell(p)
    box('Snapped-on lid', (0, 0, 0.345), (0.64, 0.48, 0.045), p['black'], 0.009)
    for x in (-0.30, 0.30):
        box('Bright locking clip', (x, 0.05, 0.33), (0.028, 0.10, 0.065), p['metal'])
    box('Tape strip', (0.10, 0.06, 0.37), (0.24, 0.095, 0.003), p['cream'], 0)


def plastic_bin_cables():
    p = station_palette()
    bin_shell(p, 0.31)
    for j in range(3):
        y = -0.09 + j * 0.085
        for i in range(10):
            angle = 2 * math.pi * i / 10
            next_angle = 2 * math.pi * (i + 1) / 10
            a = (-0.115 + 0.09 * math.cos(angle), y, 0.30 + 0.09 * math.sin(angle))
            b = (-0.115 + 0.09 * math.cos(next_angle), y, 0.30 + 0.09 * math.sin(next_angle))
            rod('Coiled microphone cable', a, b, 0.012, p['black'])
    box('Bag of adapters', (0.14, 0, 0.27), (0.17, 0.28, 0.16), p['wood'])


def plastic_bin_cassettes():
    p = station_palette()
    bin_shell(p, 0.27)
    for i in range(8):
        x = -0.21 + i * 0.06
        box('Archival cassette sleeve', (x, 0.015, 0.21), (0.041, 0.26, 0.38),
            p['black'] if i % 3 else p['cream'], 0.002)
        box('Number tab', (x, 0.151, 0.33), (0.036, 0.004, 0.06), p['wood'], 0)


def paperwork_folders():
    p = station_palette()
    for i in range(5):
        x = -0.035 + (i % 3) * 0.017
        box('Full manila folder', (x, 0.005, 0.006 + i * 0.016),
            (0.47, 0.33, 0.012), p['wood'] if i % 2 else p['cream'], 0.002)
        box('Stamped folder tab', (x - 0.16 + (i % 3) * 0.16, 0.17,
             0.007 + i * 0.016), (0.11, 0.04, 0.012), p['cream'], 0)
    box('Index card', (0.018, 0, 0.076), (0.26, 0.17, 0.006), p['shell'], 0)


def paperwork_scattered():
    p = station_palette()
    for i, (x, y, angle) in enumerate(((-0.09, -0.04, -0.17),
                                       (0.08, -0.015, 0.13), (0.025, 0.065, -0.06))):
        sheet = box('Overlapping loose sheet', (x, y, 0.003 + i * 0.006),
                    (0.37, 0.27, 0.006), p['cream'], 0.001)
        sheet.rotation_euler.z = angle
        for line in range(3):
            box('Typed notes', (x - 0.025, y + 0.075 - line * 0.035, 0.007 + i * 0.006),
                (0.19 - line * 0.03, 0.006, 0.002), p['shell'], 0)
    box('Flagged note', (-0.09, 0.08, 0.025), (0.10, 0.075, 0.006), p['red'], 0)


def paperwork_clipboard():
    p = station_palette()
    box('Worn clipboard', (0, 0, 0.008), (0.34, 0.48, 0.016), p['wood'], 0.006)
    box('Call rundown form', (0, -0.015, 0.02), (0.30, 0.41, 0.006), p['cream'], 0)
    box('Spring clip', (0, 0.213, 0.033), (0.13, 0.042, 0.024), p['metal'])
    for i in range(5):
        y = 0.145 - i * 0.07
        box('Schedule line', (0.035, y, 0.024), (0.19, 0.006, 0.002), p['shell'], 0)
        box('Checklist square', (-0.105, y, 0.024), (0.016, 0.016, 0.002), p['black'], 0)


def deck_chassis(p, width=0.62, height=0.18, depth=0.32):
    box('Equipment housing', (0, 0, height / 2), (width, depth, height), p['shell'], 0.01)
    box('Recessed fascia', (0, depth / 2 + 0.008, height / 2),
        (width - 0.045, 0.016, height - 0.04), p['black'])


def cassette_deck_dual():
    p = station_palette()
    deck_chassis(p, 0.70, 0.18, 0.32)
    for x in (-0.165, 0.165):
        box('Twin tape window', (x, 0.177, 0.115), (0.265, 0.008, 0.085), p['metal'], 0.002)
        for dx in (-0.06, 0.06):
            cylinder('Tape reel hub', (x + dx, 0.186, 0.115), 0.021,
                     0.009, p['black'], 'Y', 12)
        box('Transport keys', (x, 0.187, 0.046), (0.21, 0.018, 0.023), p['cream'])


def cassette_deck_field_recorder():
    p = station_palette()
    deck_chassis(p, 0.42, 0.20, 0.26)
    box('Leather carry grip', (0, -0.075, 0.235), (0.23, 0.065, 0.045), p['black'])
    box('Portable tape window', (-0.045, 0.146, 0.12), (0.21, 0.008, 0.075), p['metal'], 0)
    for x in (-0.09, 0.005):
        cylinder('Reel hub', (x, 0.152, 0.12), 0.022, 0.009, p['black'], 'Y', 12)
    for x in (0.11, 0.16):
        cylinder('Level dial', (x, 0.155, 0.13), 0.027, 0.02, p['metal'], 'Y', 12)
    box('REC key', (0.127, 0.153, 0.06), (0.10, 0.02, 0.029), p['red'])


def cassette_deck_rack_meter():
    p = station_palette()
    deck_chassis(p, 0.73, 0.13, 0.28)
    box('Horizontal tape door', (-0.15, 0.154, 0.071), (0.30, 0.009, 0.068), p['metal'])
    for x in (0.095, 0.215):
        box('Cream VU meter', (x, 0.157, 0.07), (0.09, 0.007, 0.065), p['cream'], 0)
        box('Needle', (x + 0.013, 0.162, 0.071), (0.004, 0.002, 0.042), p['red'], 0)
    for x in (-0.29, 0.29):
        cylinder('Rack screw', (x, 0.158, 0.065), 0.009, 0.011, p['metal'], 'Y', 8)


def crate_shell(p):
    box('Record crate base', (0, 0, 0.017), (0.54, 0.43, 0.034), p['wood'])
    for x in (-0.265, 0.265):
        for z in (0.105, 0.23, 0.35):
            box('Slatted side', (x, 0, z), (0.025, 0.43, 0.065), p['wood'])
    for y in (-0.203, 0.203):
        for z in (0.105, 0.23, 0.35):
            box('Slatted end', (0, y, z), (0.54, 0.024, 0.065), p['wood'])


def record_crate_full():
    p = station_palette()
    crate_shell(p)
    for i in range(14):
        x = -0.225 + i * 0.0345
        height = 0.41 + (i % 5) * 0.018
        sleeve = box('Packed LP jacket', (x, 0, 0.035 + height / 2),
                     (0.025, 0.37, height), p['black'] if i % 3 else p['cream'], 0.001)
        sleeve.rotation_euler.y = (i % 4 - 1.5) * 0.025
        box('Album spine marking', (x, 0.189, 0.28), (0.02, 0.003, 0.025),
            p['red'] if i % 4 == 0 else p['metal'], 0)


def record_crate_singles():
    p = station_palette()
    crate_shell(p)
    for i in range(8):
        x = -0.21 + i * 0.059
        box('7-inch single sleeve', (x, 0.032, 0.228), (0.035, 0.29, 0.30),
            p['cream'] if i % 2 else p['shell'], 0.002)
        box('Spine thumb label', (x, 0.182, 0.31), (0.027, 0.004, 0.055), p['wood'], 0)
    for x in (-0.24, 0, 0.24):
        box('Index divider', (x, -0.02, 0.24), (0.015, 0.36, 0.38), p['black'])


def record_crate_leaning():
    p = station_palette()
    crate_shell(p)
    for i in range(6):
        x = -0.13 + i * 0.06
        sleeve = box('Leaning LP jacket', (x, 0.01, 0.263),
                     (0.027, 0.36, 0.45), p['shell'] if i % 2 else p['cream'], 0.001)
        sleeve.rotation_euler.y = -0.10 - i * 0.022
        box('Jacket stripe', (x, 0.193, 0.34), (0.025, 0.003, 0.025), p['red'], 0)
    box('Out-of-place record sleeve', (-0.19, -0.04, 0.38),
        (0.035, 0.36, 0.42), p['black'])


def headphone_band(p, radius=0.18, sweep=math.pi, width=0.019):
    for i in range(12):
        angle = sweep * i / 12
        next_angle = sweep * (i + 1) / 12
        a = (-radius * math.cos(angle), -0.11 * math.sin(angle), 0.055)
        b = (-radius * math.cos(next_angle), -0.11 * math.sin(next_angle), 0.055)
        rod('Headband', a, b, width, p['black'])


def cup(p, x, z=0.04, narrow=False):
    size = (0.07, 0.11, 0.07) if narrow else (0.09, 0.13, 0.08)
    box('Padded earcup shell', (x, 0.04, z), size, p['shell'], 0.009)
    box('Ear pad', (x, 0.045, z - (0.025 if narrow else 0.03)),
        (size[0] - 0.014, size[1] - 0.02, 0.02), p['black'])


def spare_headphones_headset():
    p = station_palette()
    headphone_band(p)
    for x in (-0.19, 0.19):
        cup(p, x)
    rod('Talkback microphone boom', (0.19, 0.07, 0.06), (0.095, 0.19, 0.055), 0.009, p['metal'])
    box('Mic capsule', (0.09, 0.19, 0.055), (0.035, 0.025, 0.025), p['black'])


def spare_headphones_single_ear():
    p = station_palette()
    headphone_band(p, radius=0.145, width=0.015)
    cup(p, -0.155)
    box('Open-side band tip', (0.15, 0.045, 0.045), (0.04, 0.045, 0.045), p['metal'])
    for i in range(5):
        a = (-0.17 - 0.03 * i, 0.05 + 0.015 * i, 0.03)
        b = (-0.20 - 0.03 * i, 0.065 + 0.015 * i, 0.03)
        rod('Coiled headphone lead', a, b, 0.006, p['black'])


def spare_headphones_folded():
    p = station_palette()
    # The cups fold in over the short, compact headband.
    headphone_band(p, radius=0.105, sweep=math.pi, width=0.022)
    for x in (-0.08, 0.08):
        cup(p, x, z=0.035, narrow=True)
        box('Swivel hinge', (x, -0.045, 0.055), (0.033, 0.04, 0.035), p['metal'])


BUILDERS = {name: globals()[name] for name in (
    'archive_box_manuals', 'archive_box_tapes', 'archive_box_sealed',
    'plastic_bin_lidded', 'plastic_bin_cables', 'plastic_bin_cassettes',
    'paperwork_folders', 'paperwork_scattered', 'paperwork_clipboard',
    'cassette_deck_dual', 'cassette_deck_field_recorder', 'cassette_deck_rack_meter',
    'record_crate_full', 'record_crate_singles', 'record_crate_leaning',
    'spare_headphones_headset', 'spare_headphones_single_ear', 'spare_headphones_folded',
)}
