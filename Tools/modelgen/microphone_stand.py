"""Vintage capsule microphone on a weighted desktop/floor stand, 1.2m tall."""
from common import box, cylinder, material, rod


def build():
    black = material('Stand charcoal enamel', '252831', 0.35, 0.5)
    rubber = material('Base rubber', '11131a')
    chrome = material('Muted brushed chrome', '899096', 0.75, 0.38)
    grille = material('Dark grille interior', '1f222a', 0.15)
    red = material('Vintage oxblood badge', 'a23a3a')
    cylinder('Rubber underside', (0, 0, 0.012), 0.175, 0.024, rubber, vertices=32)
    cylinder('Weighted circular base', (0, 0, 0.039), 0.17, 0.04, black, vertices=32)
    cylinder('Base socket', (0, 0, 0.085), 0.041, 0.07, black)
    cylinder('Lower stand tube', (0, 0, 0.37), 0.019, 0.52, black)
    cylinder('Height locking collar', (0, 0, 0.625), 0.03, 0.055, black)
    cylinder('Upper telescoping tube', (0, 0, 0.78), 0.013, 0.3, chrome)
    cylinder('Stand lock knob', (0, 0.034, 0.625), 0.016, 0.034, black, 'Y')
    box('Yoke bottom', (0, 0, 0.941), (0.196, 0.036, 0.022), chrome)
    for x in (-0.087, 0.087):
        box('Yoke arm', (x, 0, 1.013), (0.022, 0.036, 0.15), chrome, 0.009)
        obj = cylinder('Capsule pivot', (x, 0, 1.07), 0.023, 0.035, black)
        obj.rotation_euler[1] = 1.57079632679
    box('Capsule shell', (0, 0, 1.091), (0.137, 0.093, 0.218), chrome, 0.032)
    box('Front grille inset', (0, 0.046, 1.10), (0.11, 0.012, 0.164), grille, 0.02)
    for i in range(9):
        z = 1.036 + i * 0.015
        width = 0.085 if i in (0, 8) else 0.104
        box('Horizontal grille rib', (0, 0.054, z), (width, 0.007, 0.005), chrome, 0.002)
    box('Grille center spine', (0, 0.057, 1.098), (0.009, 0.006, 0.149), chrome)
    box('Maker badge', (0, 0.048, 1.003), (0.032, 0.007, 0.017), red, 0.002)
    # Broad rear grooves read at game distance without a dense wire mesh.
    for i in range(5):
        box('Rear grille groove', (0, -0.047, 1.065 + i * 0.018),
            (0.082, 0.004, 0.006), grille, 0.001)
