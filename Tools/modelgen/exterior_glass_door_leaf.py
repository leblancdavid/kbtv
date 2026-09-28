"""Strip-mall storefront glass door leaf; front +Y, origin bottom center."""
from common import box, material


def glass_material():
    mat = material('Smoky storefront glass', '6f8a8f', roughness=0.18)
    mat.diffuse_color = (0.16, 0.24, 0.26, 0.38)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Alpha'].default_value = 0.38
    shader.inputs['Roughness'].default_value = 0.18
    mat.blend_method = 'BLEND'
    mat.use_screen_refraction = True
    return mat


def build():
    frame = material('Dark anodized aluminum frame', '2e3335', metallic=0.55, roughness=0.38)
    rubber = material('Black rubber door gasket', '111315', roughness=0.75)
    metal = material('Worn stainless pull bar', '858884', metallic=0.65, roughness=0.34)
    glass = glass_material()

    box('Glass pane', (0, 0, 1.02), (0.76, 0.026, 1.52), glass, 0.002)
    box('Left aluminum stile', (-0.44, 0, 0.95), (0.07, 0.08, 1.9), frame, 0.006)
    box('Right aluminum stile', (0.44, 0, 0.95), (0.07, 0.08, 1.9), frame, 0.006)
    box('Top aluminum rail', (0, 0, 1.86), (0.95, 0.08, 0.08), frame, 0.006)
    box('Bottom aluminum rail', (0, 0, 0.04), (0.95, 0.08, 0.08), frame, 0.006)
    box('Mid push rail', (0, 0.044, 0.92), (0.82, 0.038, 0.07), frame, 0.005)
    box('Center rubber gasket', (0.42, 0.048, 0.95), (0.022, 0.02, 1.72), rubber, 0.002)
    for side_name, y in (('Front', 0.085), ('Back', -0.085)):
        side = 1 if y > 0 else -1
        box(f'{side_name} vertical pull bar', (0.25, y, 1.02), (0.045, 0.045, 0.72), metal, 0.012)
        box(f'{side_name} upper pull mount', (0.25, y - 0.022 * side, 1.34), (0.13, 0.032, 0.05), metal, 0.008)
        box(f'{side_name} lower pull mount', (0.25, y - 0.022 * side, 0.7), (0.13, 0.032, 0.05), metal, 0.008)
