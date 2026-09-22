"""Deterministic painted PBR images; only glTF-supported image/BSDF nodes.

Images are generated in memory and PNG-packed into the blend; GLB embeds them.
No external files, random noise, procedural shader nodes, or bake dependency.
"""
import bpy
import numpy as np


def image(name, values, color=False):
    height, width = values.shape[:2]
    rgba = np.ones((height, width, 4), dtype=np.float32)
    rgba[:, :, :3] = values[:, :, None] if values.ndim == 2 else values
    # Byte-backed buffers take encoded sRGB samples directly. Do not linearize
    # these values: the image color-space tag supplies the shader conversion.
    # Verified in the exported PNG: skin RGB is exactly (181, 141, 120).
    img = bpy.data.images.new(name, width=width, height=height, alpha=False, float_buffer=False)
    img.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    img.pixels.foreach_set(np.clip(rgba, 0, 1).ravel())
    img.update()
    img.file_format = 'PNG'
    img.pack()
    return img


def painted(mat, rgb, roughness, normal=None):
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    shader = nodes.get('Principled BSDF')
    uv = nodes.new('ShaderNodeUVMap')
    uv.uv_map = 'UVMap'
    for label, values, color, socket in [
            ('BaseColor', rgb, True, 'Base Color'),
            ('Roughness', roughness, False, 'Roughness')]:
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = image('Vern ' + mat.name + ' ' + label, values, color)
        tex.interpolation = 'Linear'
        links.new(uv.outputs['UV'], tex.inputs['Vector'])
        links.new(tex.outputs['Color'], shader.inputs[socket])
    if normal is not None:
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = image('Vern ' + mat.name + ' Normal', normal)
        links.new(uv.outputs['UV'], tex.inputs['Vector'])
        tangent = nodes.new('ShaderNodeNormalMap')
        tangent.uv_map = 'UVMap'
        links.new(tex.outputs['Color'], tangent.inputs['Color'])
        links.new(tangent.outputs['Normal'], shader.inputs['Normal'])


def grid(size):
    return np.meshgrid((np.arange(size) + .5) / size,
                       (np.arange(size) + .5) / size)


def rgb(hex_value, shape):
    color = np.array([int(hex_value[i:i + 2], 16) / 255 for i in (0, 2, 4)])
    return np.broadcast_to(color, (*shape, 3)).copy()


def face_maps(mat):
    # Loft U=.25 faces forward; V=(Z-1.165)/.32. Features stay off the seam.
    u, v = grid(512)
    color = rgb('b58d78', u.shape)
    rough = np.full(u.shape, .77)
    def wash(x, z, width, height, tint, amount):
        mask = np.exp(-((u - x) / width)**2 - ((v - z) / height)**2)
        color[:] += mask[:, :, None] * np.array(tint) * amount
        return mask
    for x in (.17, .33):
        wash(x, .43, .045, .085, (.07, -.018, -.024), .55)
        tired = wash(x, .535, .042, .027, (-.06, -.05, -.045), .55)
        rough[:] += tired * .025
    wash(.25, .23, .10, .09, (-.028, -.025, -.018), .65)
    wash(.25, .77, .07, .12, (.028, .023, .018), .7)
    painted(mat, color, rough)


def upgrade(p):
    """Keep the prop palette, adding restrained material-specific surface reads."""
    p['face'] = p['skin'].copy()
    p['face'].name = 'Painted warm face planes'
    face_maps(p['face'])
    u, v = grid(512)
    tau = np.pi * 2
    for key, base, rough, ribbed in [
            ('sweater', '24262c', .91, False),
            ('rib', '2b2d33', .94, True),
            ('pants', '20232a', .88, False)]:
        # Interlocking fine cells, not continuous pinstripes. A 512px image
        # retains the stitch relief until portrait-scale minification.
        count, courses = (48, 40) if ribbed else (128, 64)
        across, along = tau * count * u, tau * courses * v
        weave = np.cos(across) * np.cos(along)
        ribs = np.cos(across)
        color = rgb(base, u.shape)
        color += (weave * .009 + (ribs * .004 if ribbed else 0))[:, :, None]
        # Roughness and shallow crossed normals carry the fine cloth read;
        # the charcoal albedo stays quiet. No procedural shader or bump bake.
        nx = np.sin(across) * (.10 if ribbed else .17 * np.cos(along))
        ny = np.sin(along) * (.045 if ribbed else .12 * np.cos(across))
        nz = np.sqrt(1 - nx * nx - ny * ny)
        normal = np.stack((nx, ny, nz), axis=-1) * .5 + .5
        painted(p[key], color, rough + .045 * weave, normal)
    u, v = grid(256)
    for key, base in [('hair', '262521'), ('gray', '696964')]:
        sweep = tau * (6 * u + .28 * np.sin(np.pi * v))
        band = np.cos(sweep)
        color = rgb(base, u.shape) + (band * .012)[:, :, None]
        painted(p[key], color, .83 + .025 * band)
    # Generic skin uses an intentionally even image so palms/ears/nose do not
    # inherit the face's positional paint or acquire random pores/freckles.
    painted(p['skin'], rgb('b58d78', u.shape), np.full(u.shape, .77))
    return p
