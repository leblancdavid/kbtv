"""Small tileable PBR maps, embedded in the fitted GLB (no Blender-only shaders)."""
import math
from pathlib import Path

import bpy
import numpy as np

import common

TEXTURES = Path(__file__).resolve().parents[2]/'assets/models3d/characters/vern_mpfb/textures'


def image(name, pixels, color=False, alpha=None):
    size = pixels.shape[0]
    img = bpy.data.images.new(name, width=size, height=size, alpha=alpha is not None)
    img.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    rgba = np.ones((size, size, 4), dtype=np.float32)
    rgba[:, :, :3] = pixels
    if alpha is not None:
        rgba[:, :, 3] = alpha
    img.pixels.foreach_set(rgba.ravel())
    TEXTURES.mkdir(parents=True, exist_ok=True)
    img.filepath_raw = str(TEXTURES/f'{name}.png')
    img.file_format = 'PNG'
    img.save()
    img.pack()
    return img


def hair_card_material():
    """Opaque roots break into unequal tapered tips; base-color RGBA glTF MASK.

    A threshold node is recognized by Blender 5's glTF exporter and also makes
    the source render use exactly the same cutoff as the exported material.
    """
    size = 512
    v, u = np.mgrid[:size, :size]/(size-1)
    alpha = np.zeros_like(u)
    rng = np.random.default_rng(419)
    for root in np.linspace(.10, .90, 31):
        tip = rng.uniform(.72, .98)
        bend = rng.uniform(-.045, .045)
        path = root+bend*v*v
        width = rng.uniform(.010, .020)*np.clip((tip-v)/.22, 0, 1)
        stroke = np.clip((width-np.abs(u-path))*size+0.5, 0, 1)
        alpha = np.maximum(alpha, stroke)
    edge = np.clip(np.minimum(u,1-u)*size-3,0,1)
    alpha *= edge*np.clip(v*size-2,0,1)
    # UV-aligned strand variation; no bright outlines on transparent pixels.
    stripe = .9+.1*np.cos(math.tau*(u*31+.15*v))
    base = np.broadcast_to(linear('292922'), (size,size,3))*stripe[:,:,None]
    material = common.material('Vern masked hair tips', 'ffffff', 0, .8)
    material.use_backface_culling = False
    material.surface_render_method = 'DITHERED'
    nodes, links = material.node_tree.nodes, material.node_tree.links
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image('Vern_hair_tips_rgba', base, color=True, alpha=alpha)
    tex.extension = 'EXTEND'
    shader = nodes.get('Principled BSDF')
    links.new(tex.outputs['Color'], shader.inputs['Base Color'])
    cutoff = nodes.new('ShaderNodeMath')
    cutoff.operation = 'GREATER_THAN'
    cutoff.inputs[1].default_value = .45
    links.new(tex.outputs['Alpha'], cutoff.inputs[0])
    links.new(cutoff.outputs[0], shader.inputs['Alpha'])
    return material


def textured(name, base, height, roughness, repeat=1, normal_strength=.025):
    material = common.material(name, 'ffffff', 0, .9)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes.get('Principled BSDF')
    uv = nodes.new('ShaderNodeTexCoord')
    mapping = nodes.new('ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = (repeat, repeat, 1)
    links.new(uv.outputs['UV'], mapping.inputs['Vector'])
    dx = (np.roll(height, -1, 1)-np.roll(height, 1, 1))*height.shape[0]/2
    dy = (np.roll(height, -1, 0)-np.roll(height, 1, 0))*height.shape[0]/2
    normal = np.stack((-dx*normal_strength, -dy*normal_strength, np.ones_like(height)), axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    orm = np.stack((np.ones_like(height), roughness, np.zeros_like(height)), axis=-1)
    textures = []
    for suffix, pixels, color in [('color', base, True), ('normal', normal*.5+.5, False), ('orm', orm, False)]:
        node = nodes.new('ShaderNodeTexImage')
        node.image = image(name.replace(' ', '_')+'_'+suffix, pixels, color)
        links.new(mapping.outputs['Vector'], node.inputs['Vector'])
        textures.append(node)
    links.new(textures[0].outputs['Color'], shader.inputs['Base Color'])
    normal_node = nodes.new('ShaderNodeNormalMap')
    links.new(textures[1].outputs['Color'], normal_node.inputs['Color'])
    links.new(normal_node.outputs['Normal'], shader.inputs['Normal'])
    channels = nodes.new('ShaderNodeSeparateColor')
    links.new(textures[2].outputs['Color'], channels.inputs['Color'])
    links.new(channels.outputs['Green'], shader.inputs['Roughness'])
    links.new(channels.outputs['Blue'], shader.inputs['Metallic'])
    return material


def linear(hex_color):
    srgb = np.array([int(hex_color[i:i+2], 16)/255 for i in (0, 2, 4)])
    return np.where(srgb <= .04045, srgb/12.92, ((srgb+.055)/1.055)**2.4)


def build_materials(p):
    size = 512
    y, x = np.mgrid[:size, :size]/size
    # Interlocking V rows: a quiet knit, with the ribbing and trouser weave distinct.
    chevron = np.abs(((x*8) % 1)-.5)*2
    knit = .5+.35*np.cos(math.tau*(y*12+chevron*.55))
    knit *= .8+.2*np.cos(math.tau*x*8)
    rib = .5+.45*np.cos(math.tau*x*12)
    twill = .5+.25*np.cos(math.tau*(x*24-y*24))+.1*np.cos(math.tau*(x*24+y*24))
    fleck = np.random.default_rng(71).uniform(-.025, .025, (size, size))
    p['headphone'] = p['sweater'].copy()
    p['headphone'].name = 'Matte headphone shell'
    for key, color, pattern, rough, strength in [
        ('sweater', '363940', knit, .91, .008),
        ('rib', '2d3036', rib, .93, .008),
        ('pants', '24272d', twill, .86, .010)]:
        base = linear(color)*(1+.65*(pattern[:, :, None]-.5)+fleck[:, :, None])
        p[key] = textured('Vern '+key+' fabric', base, pattern,
                          np.clip(rough+.08*(pattern-.5), 0, 1), repeat=16,
                          normal_strength=strength)
    return p


def cloth_uv(obj):
    """Unwrap and normalize UV area to square metres for consistent yarn scale."""
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.015)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.data.calc_loop_triangles()
    uv = obj.data.uv_layers.active.data
    area = 0
    for tri in obj.data.loop_triangles:
        a, b, c = [uv[i].uv for i in tri.loops]
        area += abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5
    scale = math.sqrt(sum(f.area for f in obj.data.polygons)/max(area, 1e-8))
    for loop in uv:
        loop.uv *= scale


def render_swatches(p, render):
    """Review-only fabric tiles at the same physical UV scale as the garments."""
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for obj in meshes:
        obj.hide_render = True
    tiles = []
    for key, x in [('sweater', -.32), ('rib', 0), ('pants', .32)]:
        bpy.ops.mesh.primitive_plane_add(size=.28, location=(x, 0, 1.2),
                                        rotation=(math.pi/2, 0, 0))
        obj = bpy.context.object
        obj.data.materials.append(p[key])
        for uv in obj.data.uv_layers.active.data:
            uv.uv *= .28
        tiles.append(obj)
    render('vern_mpfb_fitted_swatches', (0,-2,1.2), (0,0,1.2), 1.02, (960,400))
    for obj in tiles:
        bpy.data.objects.remove(obj, do_unlink=True)
    for obj in meshes:
        obj.hide_render = False


def hair_material(obj, top):
    """Soft salt-at-temples gradient replaces per-face gray triangles."""
    size = 512
    v, u = np.mgrid[:size, :size]/size
    wave = .5+.5*np.cos(math.tau*(u*64-v*10+1.1*np.sin(math.pi*v)))
    temples = np.exp(-((u-.32)/.075)**2)+np.exp(-((u-.68)/.075)**2)
    temples *= np.clip((.62-v)/.30, 0, 1)
    gray = temples[:, :, None]*.72
    base = linear('24241f')*(1-gray)+linear('696960')*gray
    base *= .90+.20*wave[:, :, None]
    material = textured('Vern swept hair', base, wave,
                        np.full_like(wave, .84), normal_strength=.0003)
    obj.data.materials.clear()
    obj.data.materials.append(material)
    hair_uv(obj, top)


def hair_uv(obj, top):
    """Shared projection keeps lock and underlayer colors continuous."""
    uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name='HairUV')
    for face in obj.data.polygons:
        face.material_index = 0
        coords = []
        for index in face.vertices:
            co = obj.data.vertices[index].co
            coords.append([.5+math.atan2(co.x, -(co.y+.035))/math.tau,
                           (co.z-(top-.14))/.18])
        if max(c[0] for c in coords)-min(c[0] for c in coords) > .5:
            for c in coords:
                if c[0] < .5:
                    c[0] += 1
        for loop, co in zip(face.loop_indices, coords):
            uv.data[loop].uv = co
