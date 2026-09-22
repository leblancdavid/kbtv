"""Broad sculptural detail in seated authoring space; no rig/contact changes."""
import math
from vern_mesh import tube


def subdivide_rings(rings, steps=2):
    """Linear ring insertion leaves silhouette landmarks and weighting readable."""
    result = []
    for a, b in zip(rings, rings[1:]):
        for step in range(steps):
            t = step / steps
            result.append(tuple(x + (y - x) * t for x, y in zip(a, b)))
    return result + [rings[-1]]


def sweater_folds(obj):
    for vertex in obj.data.vertices:
        x, y, z = vertex.co
        front = max(0, y + .02) / .17
        # Three broad compression folds over the seated waist. Fade at sides;
        # geometry catches real lighting rather than painted directional shade.
        belly = math.exp(-((z - .755) / .09)**2) * math.exp(-(x / .16)**4)
        fold = math.sin((z - .68) * 90 + x * 9) * .0045 * belly
        shoulder = math.exp(-((z - (1.048 - abs(x) * .19)) / .025)**2)
        vertex.co.y += front * (fold - .003 * shoulder)
    obj.data.update()


def sew_sleeves(torso, sleeves):
    """Replace two 4x4 torso patches with sleeve roots: one watertight sweater.

    Keep per-loop UV seams and existing skin weights. No remesh/weight transfer
    operator, proximity welding, or change to cuffs, hands, rig or contacts.
    """
    import bpy
    from vern_mesh import mesh
    vertices = [tuple(v.co) for v in torso.data.vertices]
    def weights(obj):
        names = {g.index: g.name for g in obj.vertex_groups}
        return [{names[g.group]: g.weight for g in v.groups} for v in obj.data.vertices]
    skin = weights(torso)
    faces, uv_faces = [], []
    patches, borders = [], []
    for center in (0, 16):
        def index(row, col):
            return row * 32 + (center + col) % 32
        patches.append({index(r, c) for r in range(14, 19) for c in range(-2, 3)})
        borders.append([index(14, c) for c in range(-2, 3)]
                       + [index(r, 2) for r in range(15, 19)]
                       + [index(18, c) for c in range(1, -3, -1)]
                       + [index(r, -2) for r in range(17, 14, -1)])
    uv = torso.data.uv_layers.active.data
    for poly in torso.data.polygons:
        if any(set(poly.vertices) <= patch for patch in patches):
            continue
        faces.append(tuple(poly.vertices))
        uv_faces.append([tuple(uv[i].uv) for i in poly.loop_indices])
    for sleeve, border in zip(sleeves, borders):
        # Find the least-twisted cyclic correspondence to the tube's ring frame.
        candidates = []
        for order in (border, list(reversed(border))):
            candidates.extend(order[k:] + order[:k] for k in range(16))
        def distance(order):
            return sum(sum((sleeve.data.vertices[i].co[k] - vertices[j][k])**2
                           for k in range(3)) for i, j in enumerate(order))
        root = min(candidates, key=distance)
        offset = len(vertices) - 16
        vertices.extend(tuple(v.co) for v in list(sleeve.data.vertices)[16:])
        skin.extend(weights(sleeve)[16:])
        uv = sleeve.data.uv_layers.active.data
        for poly in list(sleeve.data.polygons)[1:]:  # Discard the buried root cap.
            faces.append(tuple(root[i] if i < 16 else offset + i for i in poly.vertices))
            uv_faces.append([tuple(uv[i].uv) for i in poly.loop_indices])
    used = sorted({i for face in faces for i in face})
    remap = {old: new for new, old in enumerate(used)}
    result = mesh('Continuous tailored sweater', [vertices[i] for i in used],
                  [tuple(remap[i] for i in face) for face in faces],
                  torso.data.materials[0], lambda i: skin[used[i]], uv_faces)
    for obj in [torso, *sleeves]:
        bpy.data.objects.remove(obj, do_unlink=True)
    return result


def face_planes(obj):
    for vertex in obj.data.vertices:
        x, y, z = vertex.co
        if abs(x) < .055 and 1.23 < z < 1.285:
            continue
        front = max(0, y) / .1
        # Cheekbones and under-cheek transition, all below 4mm relief.
        # The mouth region is excluded to retain exact prop contact geometry.
        cheek = math.exp(-((abs(x) - .068) / .03)**2 - ((z - 1.309) / .025)**2)
        hollow = math.exp(-((abs(x) - .082) / .028)**2 - ((z - 1.275) / .022)**2)
        vertex.co.y += front * (.0035 * cheek - .002 * hollow)
    obj.data.update()


def facial_details(p):
    h = {'head': 1}
    for s in (-1, 1):
        tube('Soft lower eyelid', [(s*.028, .100, 1.344),
             (s*.053, .103, 1.338), (s*.079, .092, 1.345)],
             [.002, .0032, .0015], p['skin'], h, 8)
        tube('Ear antihelix', [(s*.140, .009, 1.289),
             (s*.143, .014, 1.309), (s*.138, .010, 1.331)],
             [.0025, .0035, .002], p['skin'], h, 8)


def swept_cap(a, theta):
    """Broad swept locks with a quiet hairline and a visible off-center part."""
    front = max(0, math.sin(a))
    end = 1.92 - .73 * front + .27 * max(0, -math.sin(a))
    flow = a + 1.10 * theta - .18 * math.sin(2 * a)
    ridge = (.5 + .5 * math.cos(6 * flow)) ** 2
    fade = min(1, max(0, (end - theta) / .22))
    fade = fade * fade * (3 - 2 * fade)
    envelope = math.sin(theta) * fade
    relief = .010 * ridge * envelope * (.5 + .5 * front)
    # Wider part can survive the feed downsample; no individual hair strands.
    part = math.exp(-((flow - 1.45) / .12)**2) * envelope
    return relief - .0035 * part
