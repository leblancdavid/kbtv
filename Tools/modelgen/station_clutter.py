"""Small modular station clutter. Blender Z up, front +Y; Godot front -Z."""
import math
from common import box, cylinder, rod, station_palette


def archive_box():
    p = station_palette()
    box('Carton floor', (0, 0, 0.015), (0.66, 0.47, 0.03), p['wood'])
    for x in (-0.317, 0.317):
        box('Worn cardboard side', (x, 0, 0.205), (0.026, 0.47, 0.38), p['wood'])
    for y in (-0.222, 0.222):
        box('Worn cardboard end', (0, y, 0.205), (0.63, 0.026, 0.38), p['wood'])
    box('Folded rear flap', (0, -0.286, 0.39), (0.63, 0.14, 0.025), p['wood'])
    box('Handwritten box label', (0, 0.238, 0.22), (0.28, 0.006, 0.095), p['cream'], 0)
    for i in range(9):
        x = -0.25 + i * 0.061
        height = 0.32 + (i % 3) * 0.025
        box('Manila file jacket', (x, -0.01, 0.045 + height / 2),
            (0.025, 0.34, height), p['cream'] if i % 3 == 0 else p['wood'], 0.001)
        box('File tab', (x, -0.11 if i % 2 else 0.10, 0.055 + height),
            (0.023, 0.075, 0.035), p['cream'], 0)


def plastic_bin():
    p = station_palette()
    box('Bin base', (0, 0, 0.018), (0.58, 0.43, 0.036), p['shell'])
    for x in (-0.273, 0.273):
        box('Molded side', (x, 0, 0.18), (0.034, 0.43, 0.34), p['shell'])
    for y in (-0.199, 0.199):
        box('Molded end', (0, y, 0.18), (0.55, 0.032, 0.34), p['shell'])
    for x in (-0.28, 0.28):
        box('Rolled bin rim', (x, 0, 0.351), (0.04, 0.46, 0.026), p['metal'])
    for y in (-0.21, 0.21):
        box('Rolled bin rim', (0, y, 0.351), (0.60, 0.04, 0.026), p['metal'])
    box('Inventory label', (0, 0.217, 0.23), (0.24, 0.006, 0.07), p['cream'], 0)
    for x in (-0.17, 0.0, 0.17):
        box('Spare equipment pouch', (x, -0.02, 0.29), (0.13, 0.23, 0.12), p['black'])
        box('Pouch tag', (x, 0.098, 0.305), (0.064, 0.004, 0.034), p['cream'], 0)


def paperwork_stack():
    p = station_palette()
    for i in range(8):
        box('Loose call sheet', ((i % 3 - 1) * 0.011, (i % 4 - 1.5) * 0.007, 0.003 + i * 0.007),
            (0.43, 0.31, 0.006), p['cream'] if i % 3 else p['wood'], 0.001)
    box('Oxblood file cover', (0.024, -0.011, 0.065), (0.455, 0.326, 0.012), p['red'])
    box('Folded rundown', (0.012, 0.012, 0.075), (0.39, 0.27, 0.006), p['cream'], 0)
    for i, width in enumerate((0.27, 0.19, 0.24)):
        box('Printed line', (-0.025, 0.115 - i * 0.05, 0.079),
            (width, 0.008, 0.002), p['shell'], 0)
    box('Binder clip', (0, -0.143, 0.083), (0.09, 0.032, 0.015), p['black'])


def cassette_deck():
    p = station_palette()
    box('Retired tape deck housing', (0, 0, 0.092), (0.61, 0.34, 0.184), p['shell'], 0.009)
    box('Recessed fascia', (0, 0.177, 0.097), (0.57, 0.016, 0.145), p['black'])
    box('Tape window', (-0.083, 0.188, 0.115), (0.27, 0.008, 0.085), p['metal'], 0.002)
    for x in (-0.15, -0.015):
        cylinder('Tape spindle', (x, 0.197, 0.115), 0.025, 0.009, p['black'], 'Y', 12)
    for i in range(4):
        box('Transport key', (0.115 + i * 0.039, 0.195, 0.066),
            (0.03, 0.015, 0.023), p['metal'], 0.002)
    box('Faded meter window', (0.17, 0.19, 0.133), (0.14, 0.008, 0.043), p['cream'], 0)
    box('Meter needle', (0.14, 0.196, 0.132), (0.008, 0.003, 0.025), p['red'], 0)


def record_crate():
    p = station_palette()
    box('Crate bottom', (0, 0, 0.018), (0.55, 0.43, 0.036), p['wood'])
    for x in (-0.27, 0.27):
        for z in (0.105, 0.22, 0.335):
            box('Crate side slat', (x, 0, z), (0.024, 0.43, 0.07), p['wood'])
    for y in (-0.205, 0.205):
        for z in (0.105, 0.22, 0.335):
            box('Crate end slat', (0, y, z), (0.55, 0.022, 0.07), p['wood'])
    for i in range(10):
        x = -0.215 + 0.048 * i
        height = 0.41 + (i % 4) * 0.022
        mat = p['cream'] if i % 4 == 0 else (p['shell'] if i % 3 else p['black'])
        box('Worn vinyl sleeve', (x, 0, 0.04 + height / 2),
            (0.025, 0.35, height), mat, 0.001)
        box('Sleeve stripe', (x, 0.178, 0.29), (0.024, 0.003, 0.023),
            p['red'] if i % 3 == 0 else p['wood'], 0)


def spare_headphones():
    p = station_palette()
    # Flat on a table: a stout curved headband and two visibly separate earcups.
    for i in range(13):
        angle = math.pi * i / 12
        a = (-0.18 * math.cos(angle), -0.11 * math.sin(angle), 0.055)
        next_angle = math.pi * (i + 1) / 12
        b = (-0.18 * math.cos(next_angle), -0.11 * math.sin(next_angle), 0.055)
        if i < 12:
            rod('Padded headband segment', a, b, 0.019, p['black'])
    for x in (-0.19, 0.19):
        box('Ear cup shell', (x, 0.04, 0.038), (0.087, 0.125, 0.075), p['shell'], 0.012)
        box('Soft ear cushion', (x, 0.045, 0.008), (0.07, 0.105, 0.016), p['black'], 0.006)
        box('Cup badge', (x, 0.04, 0.079), (0.038, 0.048, 0.004), p['metal'], 0)


BUILDERS = {
    'archive_box': archive_box,
    'plastic_bin': plastic_bin,
    'paperwork_stack': paperwork_stack,
    'cassette_deck': cassette_deck,
    'record_crate': record_crate,
    'spare_headphones': spare_headphones,
}
