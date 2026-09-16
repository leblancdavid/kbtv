using System;
using Godot;

namespace KBTV.World3D;

public partial class VernCharacter3D : Node3D
{
	public AnimationPlayer? AnimPlayer { get; private set; }

	public override void _Ready()
	{
		// Apply before the first rendered frame and before World3D's layer traversal.
		Visible = false;
		AnimPlayer = FindAnimPlayer(this);
		if (AnimPlayer == null || !ApplySeatedPose(AnimPlayer))
		{
			GD.PushError("Vern: imported model has no AnimationPlayer with seated_rest.");
			return;
		}
		StationLighting3D.ApplyLayerToTree(this, StationLighting3D.StudioLayer);
		var props = new VernPerformanceProps { Name = "PerformanceProps" };
		AddChild(props);
		props.Initialize(AnimPlayer);
		Visible = true;
	}

	private static AnimationPlayer? FindAnimPlayer(Node node)
	{
		if (node is AnimationPlayer player)
			return player;
		foreach (var child in node.GetChildren())
		{
			var found = FindAnimPlayer(child);
			if (found != null) return found;
		}
		return null;
	}

	private static bool ApplySeatedPose(AnimationPlayer player)
	{
		foreach (var animation in player.GetAnimationList())
		{
			// Imported libraries may qualify the clip as "library/seated_rest".
			if (animation != "seated_rest"
				&& !animation.EndsWith("/seated_rest", StringComparison.Ordinal))
			{
				continue;
			}
			player.Play(animation, customBlend: 0);
			player.Advance(0);
			player.Pause(); // Stop would reset playback; Pause holds the evaluated pose.
			return true;
		}
		return false;
	}
}
