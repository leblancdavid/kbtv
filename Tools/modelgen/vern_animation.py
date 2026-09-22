"""Baked seated performances and single-instance Godot-local contact contract."""
import json
import math
from pathlib import Path
import bpy
from mathutils import Matrix, Vector, Quaternion
from vern_hands import pose as pose_hand

FPS = 24
DURATIONS = {'seated_rest': 1., 'idle_breathing': 4., 'talking_default': 8.,
             'smoking': 5.5, 'drink_coffee': 5.5}
CONVERT = Matrix.Rotation(-math.pi/2, 4, 'X')
REST = {'coffee_mug': Vector((.34, .43, .73)),
        'ashtray': Vector((-.36, .4475, .73)),
        'cigarette': Vector((-.36, .405, .755)), 'vern_tray_table': Vector((0, 0, 0))}


def smooth(t):
    return max(0, min(1, t)) ** 2 * (3 - 2 * max(0, min(1, t)))


def transform(position, angle=0):
    return Matrix.Translation(position) @ Matrix.Rotation(angle, 4, 'X')


def encode(matrix, bone_local=False):
    # glTF joint bases keep Blender bone-local axes; only the prop's basis is Y-up.
    m = matrix @ CONVERT.inverted() if bone_local else CONVERT @ matrix @ CONVERT.inverted()
    q = m.to_quaternion()
    return {'position': [round(v, 7) for v in m.translation],
            'rotation_quaternion_xyzw': [round(v, 8) for v in (q.x, q.y, q.z, q.w)]}


def interpolate(keys, t):
    # Cubic Hermite: shared transit tangents, zero velocity at repeated/contact keys.
    def tangent(i):
        if i == 0 or i == len(keys)-1:
            return Vector((0, 0, 0))
        before, here, after = keys[i-1:i+2]
        if (here[1].translation-before[1].translation).length < 1e-6 or (after[1].translation-here[1].translation).length < 1e-6:
            return Vector((0, 0, 0))
        return (after[1].translation-before[1].translation)/(after[0]-before[0])
    for i, ((a, ma), (b, mb)) in enumerate(zip(keys, keys[1:])):
        if t <= b:
            u = max(0, min(1, (t-a)/(b-a)))
            p = ((2*u**3-3*u*u+1)*ma.translation + (u**3-2*u*u+u)*(b-a)*tangent(i)
                 + (-2*u**3+3*u*u)*mb.translation + (u**3-u*u)*(b-a)*tangent(i+1))
            return Matrix.LocRotScale(p, ma.to_quaternion().slerp(mb.to_quaternion(), smooth(u)), Vector((1, 1, 1)))
    return keys[-1][1].copy()


def prop_keys(name):
    rest = transform(REST[name])
    if name == 'coffee_mug':
        rot = Matrix.Rotation(.32, 4, 'X')
        sip = rot.copy()
        sip.translation = Vector((0, .107, 1.246)) - rot.to_3x3() @ Vector((0, -.043, .105))
        raised = transform((.25, .36, 1.07))
        return [(0, rest), (1.1, rest), (1.8, raised), (2.4, sip), (3.0, sip),
                (3.7, raised), (4.6, rest), (5.5, rest)]
    mouth = transform((0, .107, 1.246))
    lowered = transform((-.27, .32, 1.00))
    return [(0, rest), (1.1, rest), (1.8, lowered), (2.3, mouth), (2.9, mouth),
            (3.5, lowered), (3.9, lowered), (4.6, rest), (5.5, rest)]


def solve_arm(rig, base, side, wrist, hand_rotation):
    names = ['upper_arm.'+side, 'forearm.'+side, 'hand.'+side]
    # Follow the evaluated torso instead of pinning the shoulder in world space.
    body = rig.pose.bones['chest'].matrix @ base['chest'].inverted()
    base = {n: body @ m for n, m in base.items()}
    shoulder = base[names[0]].translation
    elbow0, wrist0 = base[names[1]].translation, base[names[2]].translation
    a, b = (elbow0-shoulder).length, (wrist0-elbow0).length
    delta = wrist-shoulder
    d = delta.length
    assert abs(a-b) < d < a+b, ('Unreachable hand', side, d, a+b)
    axis = delta.normalized()
    pole = elbow0-shoulder
    pole = (pole-axis*pole.dot(axis)).normalized()
    along = (a*a-b*b+d*d)/(2*d)
    elbow = shoulder+axis*along+pole*math.sqrt(max(0, a*a-along*along))
    for name, start, end, old_end in [(names[0], shoulder, elbow, elbow0),
                                     (names[1], elbow, wrist, wrist0)]:
        old_start = base[name].translation
        swing = (old_end-old_start).rotation_difference(end-start)
        matrix = (swing @ base[name].to_quaternion()).to_matrix().to_4x4()
        matrix.translation = start
        rig.pose.bones[name].matrix = matrix
        bpy.context.view_layer.update()
    matrix = hand_rotation.to_matrix().to_4x4()
    matrix.translation = wrist
    rig.pose.bones[names[2]].matrix = matrix
    bpy.context.view_layer.update()


def build_actions(rig):
    scene = bpy.context.scene
    scene.render.fps = FPS
    rest_action = rig.animation_data.action
    rest_action.use_fake_user = True
    scene.frame_set(1)
    base = {b.name: b.matrix.copy() for b in rig.pose.bones}
    basis = {b.name: b.matrix_basis.copy() for b in rig.pose.bones}
    contract = {'schema_version': 1, 'units': 'meters', 'space': 'Vern-local',
        'axes': {'up': '+Y', 'front': '-Z', 'right': '+X'},
        'rotation': 'quaternion_xyzw', 'fps': FPS,
        'bone_local_space': 'Native imported Godot Skeleton3D joint basis, not Vern-local axes. '
                            'Multiply skeleton transform * bone global pose * local grip/marker.',
        'playback': 'Use clip seconds, lerp position and shortest-path slerp rotation between samples. '
                     'During pickup/release interval use evaluated hand pose * hand_local_grip. '
                     'Samples are the authoring reference; outside the held interval use rest. '
                    'Keep one instance per prop. Reset to rest on cancellation.',
        'mouth_marker': {'bone': 'head', 'seated_vern_local': [0, 1.246, -.107]},
        'props': {}, 'actions': {}}
    for name, position in REST.items():
        contract['props'][name] = {'asset': 'res://assets/models3d/props/'+name+'.glb',
                                  'rest': encode(transform(position))}
    contract['props']['coffee_mug']['support'] = {'prop': 'vern_tray_table', 'surface_y': .73}
    contract['props']['ashtray']['support'] = {'prop': 'vern_tray_table', 'surface_y': .73}
    contract['props']['cigarette']['support'] = {'prop': 'ashtray', 'surface_y': .751}
    contract['mouth_marker']['head_local'] = encode(base['head'].inverted() @
                                                   transform((0, .107, 1.246)), bone_local=True)
    for clip, duration in DURATIONS.items():
        entry = {'duration_seconds': duration, 'loop': clip in ('idle_breathing', 'talking_default'),
                 'pickup_seconds': None, 'release_seconds': None, 'exhale_seconds': None}
        contract['actions'][clip] = entry
        if clip == 'seated_rest':
            continue
        action = bpy.data.actions.new(clip)
        action.use_fake_user = True
        rig.animation_data.action = action
        prop = 'coffee_mug' if clip == 'drink_coffee' else 'cigarette' if clip == 'smoking' else None
        if prop:
            side = 'L' if prop == 'coffee_mug' else 'R'
            hand = 'hand.'+side
            offset = Vector((.110, -.085, .055) if side == 'L' else (-.022, -.070, .028))
            pickup_hand = base[hand].copy()
            if side == 'L':
                pickup_hand = Matrix.Rotation(math.pi/2, 4, 'Y') @ pickup_hand
            pickup_hand.translation = REST[prop]+offset
            grip = pickup_hand.inverted() @ transform(REST[prop])
            entry.update(prop=prop, hand_bone=hand, hand_local_grip=encode(grip, bone_local=True),
                pickup_seconds=1.1, release_seconds=4.6,
                exhale_seconds=3.5 if clip == 'smoking' else None,
                contact_seconds=[2.4, 3.0] if side == 'L' else [2.3, 2.9], samples=[])
            keys = prop_keys(prop)
        for frame in range(1, round(duration*FPS)+2):
            t = (frame-1)/FPS
            for bone in rig.pose.bones:
                bone.matrix_basis = basis[bone.name]
            bpy.context.view_layer.update()
            if prop:
                # Recline around the fixed seated spine pivot; pelvis/legs never move.
                lean = .065*smooth((t-1.15)/1.15)*(1-smooth((t-3.1)/1.4))
                pivot = base['spine'].translation
                body = Matrix.Translation(pivot) @ Matrix.Rotation(lean, 4, 'X') @ Matrix.Translation(-pivot)
                rig.pose.bones['spine'].matrix = body @ base['spine']
                bpy.context.view_layer.update()
                matrix = interpolate(keys, t)
                # Blend the trajectory into the moving head frame at the contact hold.
                follow = smooth((t-1.1)/1.2)*(1-smooth((t-3.0)/1.6))
                head_delta = rig.pose.bones['head'].matrix @ base['head'].inverted()
                followed = head_delta @ matrix
                matrix = Matrix.LocRotScale(matrix.translation.lerp(followed.translation, follow),
                    matrix.to_quaternion().slerp(followed.to_quaternion(), follow), Vector((1, 1, 1)))
                target = matrix @ grip.inverted()
                elevated = pickup_hand.copy()
                elevated.translation.z += .09
                initial_up = base[hand].copy()
                initial_up.translation.z += .055
                if t < 1.1:
                    target = interpolate([(0, base[hand]), (.4, initial_up), (.8, elevated), (1.1, pickup_hand)], t)
                elif t > 4.6:
                    target = interpolate([(4.6, pickup_hand), (4.9, elevated), (5.2, initial_up), (5.5, base[hand])], t)
                solve_arm(rig, base, side, target.translation, target.to_quaternion())
                other = 'R' if side == 'L' else 'L'
                solve_arm(rig, base, other, base['hand.'+other].translation,
                          base['hand.'+other].to_quaternion())
                amount = smooth((t-.85)/.25) * (1-smooth((t-4.6)/.25))
                pose_hand(rig, base, side, amount, smoking=clip == 'smoking')
                entry['samples'].append({'time_seconds': round(t, 7), **encode(matrix)})
            else:
                phase = math.tau*t/duration
                breath = (1-math.cos(phase))/2
                rig.pose.bones['chest'].scale = (1+.003*breath, 1+.002*breath, 1+.004*breath)
                rig.pose.bones['chest'].rotation_quaternion @= Quaternion((1, 0, 0), .009*breath)
                rig.pose.bones['head'].rotation_quaternion @= Quaternion((1, 0, 0), .012*math.sin(phase))
                bpy.context.view_layer.update()
                if clip == 'idle_breathing':
                    for side in ('L', 'R'):
                        hand = base['hand.'+side]
                        solve_arm(rig, base, side, hand.translation, hand.to_quaternion())
                if clip == 'talking_default':
                    # Explain with left palm, answer with right, then an open two-hand beat.
                    rig.pose.bones['spine'].rotation_quaternion @= Quaternion((1, 0, 0), .035*math.sin(phase))
                    rig.pose.bones['chest'].rotation_quaternion @= Quaternion((0, 1, 0), .045*math.sin(phase))
                    rig.pose.bones['head'].rotation_quaternion @= Quaternion((0, 0, 1), .055*math.sin(phase*2))
                    bpy.context.view_layer.update()
                    for side, sign in [('L', 1), ('R', -1)]:
                        hand = base['hand.'+side]
                        beats = ([(0, (0, 0, 0), 0), (.35, (0, 0, 0), 0),
                                  (.95, (.02, .025, .12), -.22), (1.5, (.06, .05, .23), -.5),
                                  (1.85, (.06, .05, .23), -.5), (2.5, (.025, .05, .16), -.32),
                                  (3.35, (0, 0, 0), 0), (4.3, (0, 0, 0), 0),
                                  (5.3, (.05, .04, .15), -.4), (6.05, (.07, .055, .12), -.3),
                                  (7.35, (0, 0, 0), 0), (8, (0, 0, 0), 0)] if side == 'L' else
                                 [(0, (0, 0, 0), 0), (2.25, (0, 0, 0), 0),
                                  (2.8, (.01, .025, .09), .15), (3.45, (.055, .055, .22), .45),
                                  (3.85, (.055, .055, .22), .45), (4.7, (.015, .04, .09), .12),
                                  (5.65, (.055, .05, .17), .4), (6.35, (.045, .06, .13), .3),
                                  (7.5, (0, 0, 0), 0), (8, (0, 0, 0), 0)])
                        gesture = []
                        for time, offset, roll in beats:
                            position = hand.translation+Vector((sign*offset[0], offset[1], offset[2]))
                            rotation = Quaternion((0, 1, 0), roll) @ hand.to_quaternion()
                            gesture.append((time, Matrix.LocRotScale(position, rotation, Vector((1, 1, 1)))))
                        target = interpolate(gesture, t)
                        solve_arm(rig, base, side, target.translation, target.to_quaternion())
                        pose_hand(rig, base, side, .18*math.sin(phase/2)**2 * (1+.3*math.sin(phase*3)))
                    # Phrased mouth activity includes closures, instead of a metronomic gape.
                    speech = math.sin(phase/2)**2 * max(0, math.sin(phase*11)+.35*math.sin(phase*17))
                    rig.pose.bones['jaw'].location.y -= .003*speech
                    rig.pose.bones['jaw'].scale.y = 1+1.2*speech
            # Short asymmetric blink, closed briefly then a slower reopening.
            centers = (1.65, 6.7) if clip == 'talking_default' else (2.05,)
            blink = max(smooth((t-c+.09)/.09)*(1-smooth((t-c-.025)/.16)) for c in centers)
            for blink_side in ('L', 'R'):
                rig.pose.bones['eyelid.'+blink_side].scale.y = 1-.96*blink
            for bone in rig.pose.bones:
                for channel in ('location', 'rotation_quaternion', 'scale'):
                    bone.keyframe_insert(channel, frame=frame, group=bone.name)
        # Dense sampling is intentional: no Bezier overshoot at contacts.
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        for key in curve.keyframe_points:
                            key.interpolation = 'LINEAR'
    rig.animation_data.action = rest_action
    scene.frame_set(1)
    path = Path(__file__).resolve().parents[2] / 'assets/models3d/characters/vern/animation_contacts.json'
    path.write_text(json.dumps(contract, indent=2)+'\n')
    return contract
