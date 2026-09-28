"""Plain office interior door leaf; front +Y, origin bottom center."""
from common import box, material


def build():
    door = material('Muted painted office door', '74736c', roughness=0.78)
    edge = material('Darker painted bevels', '555550', roughness=0.82)
    metal = material('Dark brushed handle metal', '3d4042', metallic=0.45, roughness=0.42)
    kick = material('Dull lower kick plate', '6f756f', metallic=0.35, roughness=0.55)

    box('Door slab', (0, 0, 0.95), (0.95, 0.07, 1.9), door, 0.012)
    for side_name, y, handle_x in (('Front', 0.041, -0.33), ('Back', -0.041, 0.33)):
        side = 1 if y > 0 else -1
        box(f'{side_name} left stile', (-0.43, y, 0.95), (0.035, 0.018, 1.68), edge, 0.003)
        box(f'{side_name} right stile', (0.43, y, 0.95), (0.035, 0.018, 1.68), edge, 0.003)
        box(f'{side_name} top rail', (0, y, 1.72), (0.78, 0.018, 0.035), edge, 0.003)
        box(f'{side_name} mid rail', (0, y, 1.02), (0.78, 0.016, 0.03), edge, 0.002)
        box(f'{side_name} lower rail', (0, y, 0.32), (0.78, 0.018, 0.035), edge, 0.003)
        box(f'{side_name} upper inset', (0, y + 0.002 * side, 1.37), (0.66, 0.01, 0.48), door, 0.004)
        box(f'{side_name} lower inset', (0, y + 0.002 * side, 0.67), (0.66, 0.01, 0.45), door, 0.004)
        box(f'{side_name} scuffed kick plate', (0, y + 0.008 * side, 0.16), (0.66, 0.012, 0.18), kick, 0.004)
        box(f'{side_name} lever rose', (handle_x, y + 0.015 * side, 0.98), (0.09, 0.014, 0.09), metal, 0.01)
        box(f'{side_name} lever handle', (handle_x - 0.08 * side, y + 0.037 * side, 0.98), (0.2, 0.035, 0.035), metal, 0.01)
