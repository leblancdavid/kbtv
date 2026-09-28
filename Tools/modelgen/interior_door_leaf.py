"""Plain office interior door leaf; front +Y, origin bottom center."""
from common import box, material


def build():
    door = material('Muted painted office door', '74736c', roughness=0.78)
    edge = material('Darker painted bevels', '555550', roughness=0.82)
    metal = material('Dark brushed handle metal', '3d4042', metallic=0.45, roughness=0.42)
    kick = material('Dull lower kick plate', '6f756f', metallic=0.35, roughness=0.55)

    box('Door slab', (0, 0, 0.95), (0.95, 0.07, 1.9), door, 0.012)
    box('Left stile', (-0.43, 0.039, 0.95), (0.035, 0.018, 1.68), edge, 0.003)
    box('Right stile', (0.43, 0.039, 0.95), (0.035, 0.018, 1.68), edge, 0.003)
    box('Top rail', (0, 0.04, 1.72), (0.78, 0.018, 0.035), edge, 0.003)
    box('Mid rail', (0, 0.041, 1.02), (0.78, 0.016, 0.03), edge, 0.002)
    box('Lower rail', (0, 0.04, 0.32), (0.78, 0.018, 0.035), edge, 0.003)
    box('Subtle upper inset', (0, 0.043, 1.37), (0.66, 0.01, 0.48), door, 0.004)
    box('Subtle lower inset', (0, 0.043, 0.67), (0.66, 0.01, 0.45), door, 0.004)
    box('Scuffed kick plate', (0, 0.047, 0.16), (0.66, 0.012, 0.18), kick, 0.004)
    box('Lever rose', (0.33, 0.054, 0.98), (0.09, 0.014, 0.09), metal, 0.01)
    box('Lever handle', (0.25, 0.076, 0.98), (0.2, 0.035, 0.035), metal, 0.01)
