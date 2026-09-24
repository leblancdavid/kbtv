"""Low-profile loafers with shaped heel, vamp, toe and matching sole outline."""
import math
from vern_mesh import mesh, tube
import common


def build_shoes(rig, rigid):
    leather = common.material('Worn charcoal loafer leather', '292825', 0, .66)
    rubber = common.material('Thin loafer soles', '151514', 0, .88)
    seam = common.material('Loafer welt', '35312c', 0, .8)
    for side in ('L', 'R'):
        x = rig.data.bones['foot.'+side].head_local.x
        # 26cm adult footwear; asymmetric outline instead of a 30cm egg.
        count = 40
        outline = []
        for i in range(count):
            a = math.tau*i/count
            y = -.078+.130*math.sin(a)
            width = .034+.014*max(0, min(1, (.045-y)/.15))
            outline.append((x+width*math.cos(a), y))
        def surface(name, rows, material):
            verts = []
            for shrink, z, crown in rows:
                for px, py in outline:
                    # Higher heel/instep, low gently squared toe box.
                    instep = math.exp(-((py+.015)/.067)**2)
                    verts.append((x+(px-x)*shrink,
                                  -.078+(py+.078)*shrink, z+crown*instep))
            faces = [tuple(reversed(range(count)))]
            for row in range(len(rows)-1):
                for i in range(count):
                    a, b = row*count+i, row*count+(i+1)%count
                    faces.append((a, b, b+count, a+count))
            faces.append(tuple(range((len(rows)-1)*count, len(verts))))
            return rigid(mesh(name, verts, faces, material, {}), rig, 'foot.'+side)
        surface('Loafer sole '+side, [(1.02,.006,0), (1.03,.013,0),
                                     (1,.018,0)], rubber)
        surface('Leather loafer '+side, [(1,.017,0), (.99,.032,.007),
                (.87,.045,.027), (.60,.049,.035), (.14,.051,.039)], leather)
        points = [(px, py, .019) for px, py in outline]+[(outline[0][0],outline[0][1],.019)]
        rigid(tube('Loafer welt '+side, points, .0012, seam, {}, sides=6), rig, 'foot.'+side)
