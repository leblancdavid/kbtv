"""Independent Godot GLTFDocument import/contact validation; no game code loaded.

python Tools/modelgen/vern_godot_validate.py --godot <Godot 4.6 console exe>
"""
import argparse
import json
import subprocess
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = r'''
extends SceneTree
var failed := false
func check(ok: bool, message: String):
    if not ok:
        failed = true
        push_error(message)
func decode(value: Dictionary) -> Transform3D:
    var p = value.position
    var q = value.rotation_quaternion_xyzw
    return Transform3D(Basis(Quaternion(q[0], q[1], q[2], q[3])), Vector3(p[0], p[1], p[2]))
func pose(skeleton: Skeleton3D) -> Array:
    var values = []
    for i in skeleton.get_bone_count():
        values.append(skeleton.get_bone_global_pose(i))
    return values
func error(a: Transform3D, b: Transform3D) -> float:
    return max(a.origin.distance_to(b.origin), max((a.basis.x-b.basis.x).length(),
        max((a.basis.y-b.basis.y).length(), (a.basis.z-b.basis.z).length())))
func _initialize():
    call_deferred("run")
func run():
    var config = JSON.parse_string(FileAccess.get_file_as_string("res://config.json"))
    var contract = JSON.parse_string(FileAccess.get_file_as_string(config.contacts))
    var document = GLTFDocument.new()
    var state = GLTFState.new()
    check(document.append_from_file(config.glb, state) == OK, "GLB import failed")
    var scene = document.generate_scene(state, 24)
    root.add_child(scene)
    var player: AnimationPlayer = scene.find_children("*", "AnimationPlayer", true, false)[0]
    var skeleton: Skeleton3D = scene.find_children("*", "Skeleton3D", true, false)[0]
    player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
    var report = {"engine": Engine.get_version_info().string, "bones": skeleton.get_bone_count(),
        "animations": {}, "validation": "passed", "scope": "isolated GLTFDocument, no game runtime"}
    var reference = []
    for clip in contract.actions:
        var entry = contract.actions[clip]
        check(player.has_animation(clip), "Missing " + clip)
        if not player.has_animation(clip):
            continue
        var animation = player.get_animation(clip)
        check(abs(animation.length-entry.duration_seconds) < .001, "Duration " + clip)
        # glTF has no loop flag; apply the authoritative companion contract.
        animation.loop_mode = Animation.LOOP_NONE
        player.play(clip)
        var first = []
        var last = []
        var fixed_error = 0.0
        var grip_error = 0.0
        var movement = 0.0
        var mouth_error = 0.0
        var hand_travel = {"hand.L": 0.0, "hand.R": 0.0}
        for frame in range(roundi(animation.length*24)+1):
            var t = frame/24.0
            player.seek(t, true)
            skeleton.force_update_all_bone_transforms()
            var current = pose(skeleton)
            if frame == 0:
                first = current
                if reference.is_empty():
                    reference = current
            for i in current.size():
                movement = max(movement, error(current[i], first[i]))
            for hand in hand_travel:
                var hand_index = skeleton.find_bone(hand)
                hand_travel[hand] = max(hand_travel[hand], current[hand_index].origin.distance_to(first[hand_index].origin))
            for bone in ["root", "pelvis", "foot.L", "foot.R"]:
                var i = skeleton.find_bone(bone)
                check(i >= 0, "Missing bone " + bone)
                fixed_error = max(fixed_error, error(current[i], reference[i]))
            if entry.has("samples") and t >= entry.pickup_seconds and t <= entry.release_seconds:
                var i = skeleton.find_bone(entry.hand_bone)
                var attached = skeleton.transform * current[i] * decode(entry.hand_local_grip)
                grip_error = max(grip_error, error(attached, decode(entry.samples[frame])))
                if t >= entry.contact_seconds[0] and t <= entry.contact_seconds[1]:
                    var mouth = skeleton.transform * current[skeleton.find_bone("head")] * decode(contract.mouth_marker.head_local)
                    var prop = decode(entry.samples[frame])
                    var contact = Vector3(0, .105, .043) if clip == "drink_coffee" else Vector3.ZERO
                    mouth_error = max(mouth_error, mouth.origin.distance_to(prop * contact))
            last = current
        var seam = 0.0
        for i in first.size():
            seam = max(seam, error(first[i], last[i]))
        check(seam < .0001 and fixed_error < .0001, "Unstable pose " + clip)
        check(grip_error < .001, "Grip mismatch " + clip + ": " + str(grip_error))
        check(mouth_error < .001, "Mouth mismatch " + clip + ": " + str(mouth_error))
        if clip == "talking_default":
            check(hand_travel["hand.L"] > .2 and hand_travel["hand.R"] > .2, "Talking hands stuck")
        check(clip == "seated_rest" or movement > .002, "Static clip " + clip)
        report.animations[clip] = {"duration_seconds": animation.length, "tracks": animation.get_track_count(),
            "loop_from_contract": entry.loop, "seam_error": seam, "fixed_error": fixed_error,
            "grip_transform_error": grip_error, "mouth_contact_error": mouth_error,
            "hand_travel_meters": hand_travel, "motion": movement}
    if failed:
        report.validation = "failed"
    FileAccess.open(config.report, FileAccess.WRITE).store_string(JSON.stringify(report, "  "))
    print(JSON.stringify(report))
    quit(1 if failed else 0)
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--godot', required=True)
    args = parser.parse_args()
    base = Path.home() / 'AppData/Local/Temp/opencode'
    with tempfile.TemporaryDirectory(prefix='vern_validate_', dir=base) as temp:
        directory = Path(temp)
        (directory / 'project.godot').write_text('config_version=5\n[application]\nconfig/name="Vern asset validation"\n')
        (directory / 'validate.gd').write_text(SCRIPT)
        config = dict(glb=str(ROOT / 'assets/models3d/characters/vern/vern.glb'),
            contacts=str(ROOT / 'assets/models3d/characters/vern/animation_contacts.json'),
            report=str(ROOT / 'docs/art/model_previews/vern_godot_animation_validation.json'))
        (directory / 'config.json').write_text(json.dumps(config))
        subprocess.run([args.godot, '--headless', '--path', str(directory),
                        '--script', str(directory / 'validate.gd')], check=True, timeout=120)


if __name__ == '__main__':
    main()
