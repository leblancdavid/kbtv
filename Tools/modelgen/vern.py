"""Generate Vern: blender --background --factory-startup --python-exit-code 1
--python Tools/modelgen/vern.py. Optional: -- --preview-only (uses saved GLB).
"""
import math
import sys
from pathlib import Path
import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import common
from vern_mesh import loft, ellipsoid, tube, mesh
from vern_rig import create, bind_and_animate


def palette():
    from vern_materials import upgrade
    return upgrade({key: common.material(name, color, metallic, roughness) for key, name, color, metallic, roughness in [
        ('skin', 'Muted warm skin', 'b58d78', 0, .78),
        ('lip', 'Lips and ear warmth', '8c6054', 0, .8),
        ('sweater', 'Charcoal wool', '292c33', 0, .92),
        ('rib', 'Raised knit cuffs', '32353c', 0, .94),
        ('pants', 'Dark twill trousers', '20232a', 0, .88),
        ('black', 'Headphone rubber and soles', '11131a', 0, .76),
        ('hair', 'Soft black hair', '262521', 0, .86),
        ('gray', 'Salt at temples', '696964', 0, .85),
        ('metal', 'Aged pewter frames', '8d8a79', .65, .38),
        ('eye', 'Warm eye whites', 'c6b8a6', 0, .55),
    ]})


def body(p):
    from vern_detail import subdivide_rings, sweater_folds
    def torso_weights(i):
        z = torso_rings[i // 32][2]
        if z < .77:
            t = max(0, min(1, (z - .68) / .09))
            return {'pelvis': 1 - t, 'spine': t}
        t = max(0, min(1, (z - .84) / .18))
        return {'spine': 1 - t, 'chest': t}
    torso_rings = [(0, .005, .635, .155, .115), (0, .01, .665, .18, .13),
        (0, .012, .71, .185, .14), (0, .005, .78, .182, .142),
        (0, -.013, .88, .185, .136), (0, -.028, .99, .218, .128),
        (0, -.038, 1.065, .228, .112), (0, -.038, 1.095, .202, .10),
        (0, -.03, 1.12, .13, .083), (0, -.025, 1.145, .067, .069)]
    torso_rings = subdivide_rings(torso_rings, 3)
    torso = loft('Sweater tailored torso', torso_rings, p['sweater'], torso_weights, 32)
    sweater_folds(torso)
    loft('Folded turtleneck', [(0, -.025, z, r, r * .92) for z, r in
        [(1.11, .072), (1.125, .077), (1.16, .074), (1.176, .066)]], p['rib'], {'neck': 1}, 20)
    loft('Sweater lower ribbing', [(0, .01, z, .181, .134) for z in (.656, .673, .693)],
         p['rib'], {'pelvis': 1}, 20)
    for side, s in [('L', 1), ('R', -1)]:
        arm, fore, hand = 'upper_arm.' + side, 'forearm.' + side, 'hand.' + side
        points = [(s*.16, -.035, 1.015), (s*.207, -.033, 1.009), (s*.233, -.040, .996),
                  (s*.258, -.047, .978), (s*.266, -.050, .956), (s*.27, -.053, .935),
                  (s*.276, -.057, .89), (s*.285, -.07, .81), (s*.284, -.02, .776),
                  (s*.28, .075, .756), (s*.28, .185, .742)]
        def sleeve_weights(i):
            ring = i // 16
            t = [0, 0, 0, 0, 0, 0, 0, .45, .9, 1, 1][ring]
            # Deltoid blend: the buried root rings share chest weight so a
            # raised arm compresses the armpit instead of stretching a shelf.
            chest = (0, .55, .3, .12)[ring] if ring < 4 else 0
            weights = {arm: (1 - t) * (1 - chest)}
            if chest:
                weights['chest'] = chest
            if t:
                weights[fore] = t
            return weights
        # Root cap stays buried in the torso; sewn gussets stretched a visible
        # shelf across the armpit whenever a hand rose to the mouth.
        tube('Continuous sweater sleeve ' + side, points,
             [.062, .065, .069, .073, .074, .075, .074, .071, .064, .055, .047],
             p['sweater'], sleeve_weights, 16)
        tube('Knitted wrist cuff ' + side, [(s*.28, .15, .75), (s*.28, .19, .741)],
             .048, p['rib'], {fore: 1}, 16)
        from vern_hands import build as build_hand
        build_hand(side, p)
        thigh, shin, foot = 'thigh.' + side, 'shin.' + side, 'foot.' + side
        points = [(s*.103, -.03, .629), (s*.12, .10, .619), (s*.131, .25, .585),
                  (s*.135, .39, .553), (s*.135, .434, .505), (s*.135, .455, .39),
                  (s*.135, .47, .23), (s*.135, .475, .11)]
        def leg_weights(i):
            ring = i // 16
            if ring == 0:
                return {'pelvis': .55, thigh: .45}
            t = [0, 0, 0, .3, .8, 1, 1, 1][ring]
            return {thigh: 1 - t, shin: t}
        tube('Trousers ' + side, points, [.094, .101, .092, .083, .081, .073, .059, .057],
             p['pants'], leg_weights, 16)
        ellipsoid('Leather shoe ' + side, (s*.135, .552, .07), (.074, .15, .067),
                  p['black'], {foot: 1})
        loft('Shoe sole ' + side, [(s*.135, .558, z, .074, .151) for z in (.009, .024, .034)],
              p['pants'], {foot: 1}, 20)


def face(p):
    from vern_detail import subdivide_rings, face_planes, facial_details
    h = {'head': 1}
    head = loft('Sculpted jaw cheeks and forehead', subdivide_rings([
        (0, .009, 1.165, .052, .057), (0, .018, 1.185, .075, .071),
        (0, .012, 1.215, .092, .082), (0, -.005, 1.25, .108, .098),
        (0, -.012, 1.29, .12, .11), (0, -.015, 1.335, .122, .114),
        (0, -.018, 1.38, .119, .111), (0, -.024, 1.425, .115, .103),
        (0, -.026, 1.455, .098, .086), (0, -.026, 1.478, .066, .061),
        (0, -.026, 1.485, .012, .012)]), p['face'], h, 48)
    face_planes(head)
    # Nose is a shaped closed volume with a bridge, bulb and two nostril wings.
    loft('Nose bridge and tip', [(0, .115, 1.282, .014, .012),
        (0, .14, 1.292, .027, .027), (0, .156, 1.307, .027, .027),
        (0, .139, 1.336, .018, .025), (0, .112, 1.366, .014, .014)], p['skin'], h, 12)
    for s in (-1, 1):
        ellipsoid('Nostril wing', (s*.023, .124, 1.296), (.014, .019, .012), p['skin'], h)
        ellipsoid('Nostril shadow', (s*.022, .137, 1.288), (.007, .006, .003), p['lip'], h)
        ellipsoid('Ear', (s*.12, -.013, 1.306), (.025, .025, .048), p['skin'], h)
        ellipsoid('Inner ear', (s*.14, .001, 1.308), (.006, .015, .03), p['lip'], h)
        lid = {'eyelid.'+('L' if s == 1 else 'R'): 1}
        ellipsoid('Eye', (s*.053, .092, 1.348), (.028, .010, .012), p['eye'], lid)
        ellipsoid('Iris', (s*.053, .102, 1.348), (.008, .004, .009), p['hair'], lid)
        tube('Heavy upper eyelid', [(s*.028, .102, 1.353), (s*.051, .105, 1.36),
             (s*.08, .091, 1.351)], .006, p['skin'], lid)
        tube('Dark eyebrow', [(s*.027, .105, 1.38), (s*.052, .108, 1.386),
             (s*.086, .086, 1.375)], [.007, .009, .006], p['hair'], h)
    tube('Quiet mouth', [(-.038, .09, 1.246), (0, .103, 1.247), (.038, .09, 1.246)],
          .004, p['lip'], {'jaw': 1})
    ellipsoid('Mouth opening', (0, .107, 1.245), (.026, .0025, .002), p['black'], {'jaw': 1})
    # Two tapered mustache lobes, kept off the upper lip.
    for s in (-1, 1):
        tube('Signature mustache', [(s*.004, .111, 1.274), (s*.018, .114, 1.269),
             (s*.034, .106, 1.265), (s*.047, .092, 1.257)],
              [.007, .0095, .008, .0035], p['hair'], h, 10)
    facial_details(p)


def hair_and_accessories(p):
    from vern_detail import swept_cap
    from vern_mesh import ring_uvs
    h = {'head': 1}
    vertices, sides, rows = [], 64, 26
    for row in range(rows):
        for i in range(sides):
            a = i * math.tau / sides
            front = max(0, math.sin(a))
            end = 1.92 - .73 * front + .27 * max(0, -math.sin(a))
            theta = .02 + (end - .02) * row / (rows - 1)
            relief = swept_cap(a, theta)
            vertices.append(((.13 + relief) * math.sin(theta) * math.cos(a),
                -.026 + (.122 + relief) * math.sin(theta) * math.sin(a),
                1.407 + .107 * math.cos(theta) + .006 * math.cos(a)
                + relief))
    faces = [tuple(reversed(range(sides)))]
    for row in range(rows - 1):
        for i in range(sides):
            a, b = row * sides + i, row * sides + (i + 1) % sides
            faces.append((a, a + sides, b + sides, b))
    faces.append(tuple(range((rows - 1) * sides, rows * sides)))
    mesh('Swept hair cap', vertices, faces, p['hair'], h,
         ring_uvs(faces, sides, rows))
    for s in (-1, 1):
        tube('Gray temple', [(s*.12, .018, 1.38), (s*.121, .022, 1.356),
             (s*.12, .023, 1.323)], [.01, .009, .006], p['gray'], h)
        # Slightly drooped rectangular aviators, with open lenses for clear eyes.
        cx = s*.061
        outline = [(-.046, .028), (.038, .028), (.049, .017), (.047, -.025),
                   (.031, -.035), (-.031, -.035), (-.047, -.025), (-.049, .014), (-.046, .028)]
        points = [(cx + x, .132 - .20*abs(cx+x), 1.346 + z) for x, z in outline]
        tube('Aviator frame', points, .0038, p['metal'], h, 8)
        tube('Spectacle temple arm', [(s*.109, .108, 1.364), (s*.13, .039, 1.357),
             (s*.13, -.04, 1.34)], .0038, p['metal'], h)
        ellipsoid('Headphone ear cushion', (s*.146, -.024, 1.343), (.023, .047, .066), p['black'], h)
        ellipsoid('Headphone outer cup', (s*.166, -.027, 1.343), (.018, .044, .062), p['rib'], h)
        ellipsoid('Headphone cup inset', (s*.18, -.027, 1.343), (.004, .031, .045), p['black'], h)
        tube('Headphone slider', [(s*.175, -.028, 1.37), (s*.169, -.028, 1.422)],
             .009, p['metal'], h)
    tube('Glasses bridge', [(-.017, .13, 1.36), (0, .14, 1.365), (.017, .13, 1.36)],
         .0035, p['metal'], h)
    points = [(.172 * math.cos(t * math.pi / 20), -.034,
               1.35 + .181 * math.sin(t * math.pi / 20)) for t in range(21)]
    tube('Padded headphone headband', points, .014, p['black'], h, 10)


def build():
    common.reset()
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    p = palette()
    body(p)
    face(p)
    hair_and_accessories(p)
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    rig = create()
    bind_and_animate(rig, objects)
    return rig, objects


if __name__ == '__main__':
    from vern_export import deliver
    if '--preview-only' not in sys.argv:
        from vern_props import generate
        generate()
    deliver(build, '--preview-only' in sys.argv)
