using System;
using Godot;

namespace KBTV.World3D;

public partial class VernCharacter3D : Node3D
{
	public override void _Ready()
	{
		// Apply before the first rendered frame and before World3D's layer traversal.
		Visible = false;
		if (!ApplySeatedPose(this))
		{
			GD.PushError("Vern: imported model has no AnimationPlayer with seated_rest.");
			return;
		}
		StationLighting3D.ApplyLayerToTree(this, StationLighting3D.StudioLayer);
		Visible = true;
	}

	private static bool ApplySeatedPose(Node node)
	{
		if (node is AnimationPlayer player)
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
		}
		foreach (var child in node.GetChildren())
		{
			if (ApplySeatedPose(child))
			{
				return true;
			}
		}
		return false;
	}
}
