using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace KBTV.World3D;

/// <summary>Single visible props driven by the same baked contact clock as the hands.</summary>
public partial class VernPerformanceProps : Node3D
{
    private readonly Dictionary<string, Node3D> _props = new();
    private JsonDocument _contract = null!;
    private AnimationPlayer _player = null!;
    private Skeleton3D _skeleton = null!;
    private string _previousClip = "";
    private double _previousTime;

    public void Initialize(AnimationPlayer player)
    {
        _player = player;
        _skeleton = (Skeleton3D)GetParent().FindChildren("*", "Skeleton3D", true, false)[0];
        _contract = JsonDocument.Parse(FileAccess.GetFileAsString(
            "res://assets/models3d/characters/vern/animation_contacts.json"));
        foreach (var prop in _contract.RootElement.GetProperty("props").EnumerateObject())
        {
            var node = GD.Load<PackedScene>(prop.Value.GetProperty("asset").GetString()!).Instantiate<Node3D>();
            node.Name = prop.Name;
            AddChild(node);
            node.Transform = Decode(prop.Value.GetProperty("rest"));
            _props.Add(prop.Name, node);
        }
        StationLighting3D.ApplyLayerToTree(this, StationLighting3D.StudioLayer);
        // After AnimationPlayer evaluation, including when the controller is not processing.
        ProcessPriority = 100;
    }

    public override void _Process(double delta)
    {
        if (_contract == null) return;
        var clip = _player.AssignedAnimation.ToString().Split('/')[^1];
        var actions = _contract.RootElement.GetProperty("actions");
        actions.TryGetProperty(clip, out var action);
        var time = _player.CurrentAnimationPosition;
        if (action.ValueKind == JsonValueKind.Object && action.TryGetProperty("exhale_seconds", out var exhale)
            && exhale.ValueKind == JsonValueKind.Number && time >= exhale.GetDouble()
            && (_previousClip != clip || _previousTime < exhale.GetDouble()))
        {
            var mouth = _contract.RootElement.GetProperty("mouth_marker");
            var world = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_skeleton.FindBone("head"))
                * Decode(mouth.GetProperty("head_local"));
            foreach (var node in GetTree().GetNodesInGroup("vern_smoke"))
                if (node is StudioSmoke3D smoke) smoke.EmitExhale(world.Origin);
        }
        _previousClip = clip;
        _previousTime = time;
        foreach (var prop in _contract.RootElement.GetProperty("props").EnumerateObject())
        {
            var pose = Decode(prop.Value.GetProperty("rest"));
            if (action.ValueKind == JsonValueKind.Object && action.TryGetProperty("prop", out var name)
                && name.GetString() == prop.Name)
            {
                var samples = action.GetProperty("samples");
                var frame = Mathf.Clamp(_player.CurrentAnimationPosition * 24, 0, samples.GetArrayLength()-1);
                var index = (int)frame;
                pose = Decode(samples[index]).InterpolateWith(
                    Decode(samples[System.Math.Min(index+1, samples.GetArrayLength()-1)]), (float)(frame-index));
            }
            _props[prop.Name].Transform = pose;
        }
    }

    public override void _ExitTree() => _contract?.Dispose();

    private static Transform3D Decode(JsonElement value)
    {
        var p = value.GetProperty("position");
        var q = value.GetProperty("rotation_quaternion_xyzw");
        return new Transform3D(new Basis(new Quaternion(q[0].GetSingle(), q[1].GetSingle(),
            q[2].GetSingle(), q[3].GetSingle())), new Vector3(p[0].GetSingle(), p[1].GetSingle(), p[2].GetSingle()));
    }
}
